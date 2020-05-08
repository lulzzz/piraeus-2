using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Capl.Authorization;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Clustering.Redis;
using Orleans.Hosting;
using Piraeus.Configuration;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using Piraeus.Core.Utilities;
using Piraeus.GrainInterfaces;

namespace Piraeus.Grains
{
    public class GraphManager
    {
        private readonly IClusterClient client;

        public static GraphManager Instance
        {
            get; private set;
        }

        public static bool IsInitialized => Instance != null && Instance.client != null;

        public static GraphManager Create(IClusterClient client)
        {
            if (Instance == null)
            {
                Instance = new GraphManager(client);
            }

            return Instance;
        }

        public static GraphManager Create(OrleansConfig config)
        {
            if (Instance == null)
            {
                Instance = new GraphManager(config);
            }

            return Instance;
        }

        #region Static Resource Operations

        #region ctor

        public GraphManager(IClusterClient client)
        {
            this.client = client;
        }

        public GraphManager(OrleansConfig config)
            : this(config.DataConnectionString, config.GetLoggerTypes(), Enum.Parse<LogLevel>(config.LogLevel, true),
                config.InstrumentationKey)
        {
        }

        public GraphManager(string connectionString, LoggerType loggers, LogLevel logLevel,
            string instrumentationKey = null)
        {
            ClientBuilder builder = new ClientBuilder();
            builder.ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IPiSystem).Assembly));

            AddStorageProvider(builder, connectionString);
            AddAppInsighlts(builder, loggers, instrumentationKey);
            AddLoggers(builder, loggers, logLevel);
            client = builder.Build();
            client.Connect(CreateRetryFilter()).GetAwaiter().GetResult();
        }

        private IClientBuilder AddAppInsighlts(IClientBuilder builder, LoggerType loggers,
            string instrumentationKey = null)
        {
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                return builder;
            }

            if (loggers.HasFlag(LoggerType.AppInsights))
            {
                builder.AddApplicationInsightsTelemetryConsumer(instrumentationKey);
            }

            return builder;
        }

        private IClientBuilder AddLoggers(IClientBuilder builder, LoggerType loggers, LogLevel logLevel)
        {
            builder.ConfigureLogging(op =>
            {
                if (loggers.HasFlag(LoggerType.Console))
                {
                    op.AddConsole();
                    op.SetMinimumLevel(logLevel);
                }

                if (loggers.HasFlag(LoggerType.Debug))
                {
                    op.AddDebug();
                    op.SetMinimumLevel(logLevel);
                }
            });

            return builder;
        }

        private IClientBuilder AddStorageProvider(IClientBuilder builder, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                builder.UseLocalhostClustering();
            }
            else
            {
                if (connectionString.Contains("6379") || connectionString.Contains("6380"))
                {
                    builder.UseRedisGatewayListProvider(options => options.ConnectionString = connectionString);
                }
                else
                {
                    builder.UseAzureStorageClustering(options => options.ConnectionString = connectionString);
                }
            }

            return builder;
        }

        private Func<Exception, Task<bool>> CreateRetryFilter(int maxAttempts = 5)
        {
            int attempt = 0;
            return RetryFilter;

            async Task<bool> RetryFilter(Exception exception)
            {
                attempt++;
                Console.WriteLine(
                    $"Cluster client attempt {attempt} of {maxAttempts} failed to connect to cluster.  Exception: {exception}");
                if (attempt > maxAttempts)
                {
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(4));
                return true;
            }
        }

        #endregion ctor

        public async Task<string> AddResourceObserverAsync(string resourceUriString, TimeSpan lifetime,
            MetricObserver observer)
        {
            IMetricObserver objRef = await client.CreateObjectReference<IMetricObserver>(observer);
            IPiSystem resource = GetPiSystem(resourceUriString);
            return await resource.AddObserverAsync(lifetime, objRef);
        }

        public async Task<string> AddResourceObserverAsync(string resourceUriString, TimeSpan lifetime,
            ErrorObserver observer)
        {
            IErrorObserver objRef = await client.CreateObjectReference<IErrorObserver>(observer);
            IPiSystem resource = GetPiSystem(resourceUriString);
            return await resource.AddObserverAsync(lifetime, objRef);
        }

        public async Task ClearPiSystemAsync(string resourceUriString)
        {
            IPiSystem resource = GetPiSystem(resourceUriString);
            await resource.ClearAsync();
        }

        public IPiSystem GetPiSystem(string resourceUriString)
        {
            Uri uri = new Uri(resourceUriString);
            string uriString = uri.ToCanonicalString(false);
            return client.GetGrain<IPiSystem>(uriString);
        }

        public async Task<EventMetadata> GetPiSystemMetadataAsync(string resourceUriString)
        {
            IPiSystem resource = GetPiSystem(resourceUriString);
            return await resource.GetMetadataAsync();
        }

        public async Task<CommunicationMetrics> GetPiSystemMetricsAsync(string resourceUriString)
        {
            IPiSystem resource = GetPiSystem(resourceUriString);
            return await resource.GetMetricsAsync();
        }

        public async Task<IEnumerable<string>> GetPiSystemSubscriptionListAsync(string resourceUriString)
        {
            IPiSystem resource = GetPiSystem(resourceUriString);
            return await resource.GetSubscriptionListAsync();
        }

        public async Task PublishAsync(string resourceUriString, EventMessage message)
        {
            IPiSystem resource = GetPiSystem(resourceUriString);
            await resource.PublishAsync(message);
        }

        public async Task PublishAsync(string resourceUriString, EventMessage message,
            List<KeyValuePair<string, string>> indexes)
        {
            IPiSystem resource = GetPiSystem(resourceUriString);
            await resource.PublishAsync(message, indexes);
        }

        public async Task RemoveResourceObserverAsync(string resourceUriString, string leaseKey)
        {
            IPiSystem resource = GetPiSystem(resourceUriString);
            await resource.RemoveObserverAsync(leaseKey);
        }

        public async Task<bool> RenewResourceObserverLeaseAsync(string resourceUriString, string leaseKey,
            TimeSpan lifetime)
        {
            IPiSystem resource = GetPiSystem(resourceUriString);
            return await resource.RenewObserverLeaseAsync(leaseKey, lifetime);
        }

        public async Task<string> SubscribeAsync(string resourceUriString, SubscriptionMetadata metadata)
        {
            Uri uri = new Uri(resourceUriString);
            string subscriptionUriString = uri.ToCanonicalString(true) + Guid.NewGuid();
            metadata.SubscriptionUriString = subscriptionUriString;

            ISubscription subscription = GetSubscription(subscriptionUriString);
            await subscription.UpsertMetadataAsync(metadata);

            IPiSystem resource = GetPiSystem(uri.ToCanonicalString(false));
            await resource.SubscribeAsync(subscription);

            return subscriptionUriString;
        }

        public async Task UnsubscribeAsync(string subscriptionUriString)
        {
            Uri uri = new Uri(subscriptionUriString);
            string resourceUriString = uri.ToCanonicalString(false, true);
            IPiSystem resource = GetPiSystem(resourceUriString);

            await resource.UnsubscribeAsync(subscriptionUriString);
        }

        public async Task UnsubscribeAsync(string subscriptionUriString, string identity)
        {
            Uri uri = new Uri(subscriptionUriString);
            string resourceUriString = uri.ToCanonicalString(false, true);
            IPiSystem resource = GetPiSystem(resourceUriString);

            await resource.UnsubscribeAsync(subscriptionUriString, identity);
        }

        public async Task UpsertPiSystemMetadataAsync(EventMetadata metadata)
        {
            Uri uri = new Uri(metadata.ResourceUriString);
            metadata.ResourceUriString = uri.ToCanonicalString(false);
            IPiSystem resource = GetPiSystem(metadata.ResourceUriString);
            await resource.UpsertMetadataAsync(metadata);
        }

        #endregion Static Resource Operations

        #region Static Subscription Operations

        public async Task<string> AddSubscriptionObserverAsync(string subscriptionUriString, TimeSpan lifetime,
            MessageObserver observer)
        {
            IMessageObserver observerRef = await client.CreateObjectReference<IMessageObserver>(observer);
            ISubscription subscription = GetSubscription(subscriptionUriString);
            return await subscription.AddObserverAsync(lifetime, observerRef);
        }

        public async Task<string> AddSubscriptionObserverAsync(string subscriptionUriString, TimeSpan lifetime,
            MetricObserver observer)
        {
            IMetricObserver observerRef = await client.CreateObjectReference<IMetricObserver>(observer);
            ISubscription subscription = GetSubscription(subscriptionUriString);
            return await subscription.AddObserverAsync(lifetime, observerRef);
        }

        public async Task<string> AddSubscriptionObserverAsync(string subscriptionUriString, TimeSpan lifetime,
            ErrorObserver observer)
        {
            IErrorObserver observerRef = await client.CreateObjectReference<IErrorObserver>(observer);
            ISubscription subscription = GetSubscription(subscriptionUriString);
            return await subscription.AddObserverAsync(lifetime, observerRef);
        }

        public ISubscription GetSubscription(string subscriptionUriString)
        {
            Uri uri = new Uri(subscriptionUriString);
            return client.GetGrain<ISubscription>(uri.ToCanonicalString(false));
        }

        public async Task<SubscriptionMetadata> GetSubscriptionMetadataAsync(string subscriptionUriString)
        {
            Uri uri = new Uri(subscriptionUriString);
            ISubscription subscription = GetSubscription(uri.ToCanonicalString(false));
            return await subscription.GetMetadataAsync();
        }

        public async Task<CommunicationMetrics> GetSubscriptionMetricsAsync(string subscriptionUriString)
        {
            ISubscription subscription = GetSubscription(subscriptionUriString);
            return await subscription.GetMetricsAsync();
        }

        public async Task RemoveSubscriptionObserverAsync(string subscriptionUriString, string leaseKey)
        {
            ISubscription subscription = GetSubscription(subscriptionUriString);
            await subscription.RemoveObserverAsync(leaseKey);
        }

        public async Task<bool> RenewObserverLeaseAsync(string subscriptionUriString, string leaseKey,
            TimeSpan lifetime)
        {
            ISubscription subscription = GetSubscription(subscriptionUriString);
            return await subscription.RenewObserverLeaseAsync(leaseKey, lifetime);
        }

        public async Task SubscriptionClearAsync(string subscriptionUriString)
        {
            ISubscription subscription = GetSubscription(subscriptionUriString);
            await subscription.ClearAsync();
        }

        public async Task UpsertSubscriptionMetadataAsync(SubscriptionMetadata metadata)
        {
            ISubscription subscription = GetSubscription(metadata.SubscriptionUriString);
            await subscription.UpsertMetadataAsync(metadata);
        }

        #endregion Static Subscription Operations

        #region Static Subscriber Operations

        public async Task AddSubscriberSubscriptionAsync(string identity, string subscriptionUriString)
        {
            ISubscriber subscriber = GetSubscriber(identity);

            if (subscriber != null)
            {
                await subscriber.AddSubscriptionAsync(subscriptionUriString);
            }
        }

        public async Task ClearSubscriberSubscriptionsAsync(string identity)
        {
            ISubscriber subscriber = GetSubscriber(identity);

            if (subscriber != null)
            {
                await subscriber.ClearAsync();
            }
        }

        public ISubscriber GetSubscriber(string identity)
        {
            if (string.IsNullOrEmpty(identity))
            {
                return null;
            }

            return client.GetGrain<ISubscriber>(identity.ToLowerInvariant());
        }

        public async Task<IEnumerable<string>> GetSubscriberSubscriptionsListAsync(string identity)
        {
            ISubscriber subscriber = GetSubscriber(identity);

            if (subscriber != null)
            {
                return await subscriber.GetSubscriptionsAsync();
            }

            return null;
        }

        public async Task RemoveSubscriberSubscriptionAsync(string identity, string subscriptionUriString)
        {
            ISubscriber subscriber = GetSubscriber(identity);

            if (subscriber != null)
            {
                await subscriber.RemoveSubscriptionAsync(subscriptionUriString);
            }
        }

        #endregion Static Subscriber Operations

        #region Static ResourceList

        public async Task<List<string>> GetSigmaAlgebraAsync()
        {
            ISigmaAlgebra resourceList = client.GetGrain<ISigmaAlgebra>("resourcelist");
            return await resourceList.GetListAsync();
        }

        public async Task<List<string>> GetSigmaAlgebraAsync(string filter)
        {
            ISigmaAlgebra resourceList = client.GetGrain<ISigmaAlgebra>("resourcelist");
            return await resourceList.GetListAsync(filter);
        }

        public async Task<List<string>> GetSigmaAlgebraAsync(int index, int quantity)
        {
            ISigmaAlgebra resourceList = client.GetGrain<ISigmaAlgebra>("resourcelist");
            return await resourceList.GetListAsync(index, quantity);
        }

        public async Task<ListContinuationToken> GetSigmaAlgebraAsync(ListContinuationToken token)
        {
            ISigmaAlgebra resourceList = client.GetGrain<ISigmaAlgebra>("resourcelist");
            return await resourceList.GetListAsync(token);
        }

        #endregion Static ResourceList

        #region Static Access Control

        public async Task ClearAccessControlPolicyAsync(string policyUriString)
        {
            IAccessControl accessControl = GetAccessControlPolicy(policyUriString);
            await accessControl.ClearAsync();
        }

        public IAccessControl GetAccessControlPolicy(string policyUriString)
        {
            Uri uri = new Uri(policyUriString);
            string uriString = uri.ToCanonicalString(false);
            return client.GetGrain<IAccessControl>(uriString);
        }

        public async Task<AuthorizationPolicy> GetAccessControlPolicyAsync(string policyUriString)
        {
            IAccessControl accessControl = GetAccessControlPolicy(policyUriString);
            return await accessControl.GetPolicyAsync();
        }

        public async Task UpsertAcessControlPolicyAsync(string policyUriString, AuthorizationPolicy policy)
        {
            IAccessControl accessControl = GetAccessControlPolicy(policyUriString);
            await accessControl.UpsertPolicyAsync(policy);
        }

        #endregion Static Access Control

        #region Static Service Identity

        public async Task AddServiceIdentityCertificateAsync(string key, string path, string password)
        {
            IServiceIdentity identity = GetServiceIdentity(key);
            X509Certificate2 cert = new X509Certificate2(path, password);
            if (cert != null)
            {
                byte[] certBytes = cert.Export(X509ContentType.Pfx, password);
                await identity.AddCertificateAsync(certBytes);
            }
        }

        public async Task AddServiceIdentityCertificateAsync(string key, string store, string location,
            string thumbprint, string password)
        {
            IServiceIdentity identity = GetServiceIdentity(key);
            X509Certificate2 cert = GetLocalCertificate(store, location, thumbprint);

            if (cert != null)
            {
                byte[] certBytes = cert.Export(X509ContentType.Pfx, password);
                await identity.AddCertificateAsync(certBytes);
            }
        }

        public async Task AddServiceIdentityClaimsAsync(string key, List<KeyValuePair<string, string>> claims)
        {
            IServiceIdentity identity = GetServiceIdentity(key);
            await identity.AddClaimsAsync(claims);
        }

        public IServiceIdentity GetServiceIdentity(string key)
        {
            return client.GetGrain<IServiceIdentity>(key);
        }

        private static X509Certificate2 GetLocalCertificate(string store, string location, string thumbprint)
        {
            if (string.IsNullOrEmpty(store) || string.IsNullOrEmpty(location) || string.IsNullOrEmpty(thumbprint))
            {
                return null;
            }

            thumbprint = Regex.Replace(thumbprint, @"[^\da-fA-F]", string.Empty).ToUpper();

            StoreName storeName = (StoreName)Enum.Parse(typeof(StoreName), store, true);
            StoreLocation storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), location, true);

            X509Store certStore = new X509Store(storeName, storeLocation);
            certStore.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection certCollection =
                certStore.Certificates.Find(X509FindType.FindByThumbprint,
                    thumbprint.ToUpper(), false);
            X509Certificate2Enumerator enumerator = certCollection.GetEnumerator();
            X509Certificate2 cert = null;
            while (enumerator.MoveNext())
                cert = enumerator.Current;
            return cert;
        }

        #endregion Static Service Identity
    }
}