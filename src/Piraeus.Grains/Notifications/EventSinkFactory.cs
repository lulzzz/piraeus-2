using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Piraeus.Core.Metadata;

namespace Piraeus.Grains.Notifications
{
    public abstract class EventSinkFactory
    {
        private static X509Certificate2 cert;

        private static List<Claim> claims;

        public static bool IsInitialized
        {
            get; private set;
        }

        public static EventSink Create(SubscriptionMetadata metadata, List<Claim> claimset = null,
            X509Certificate2 certificate = null)
        {
            _ = metadata ?? throw new ArgumentNullException(nameof(metadata));

            if (string.IsNullOrEmpty(metadata.NotifyAddress))
            {
                throw new NullReferenceException("Subscription metadata has no NotifyAddress for passive event sink.");
            }

            if (!IsInitialized)
            {
                cert ??= certificate;
                claims ??= claimset;
            }

            Uri uri = new Uri(metadata.NotifyAddress);
            IsInitialized = true;

            if (uri.Scheme == "http" || uri.Scheme == "https")
            {
                if (uri.Authority.Contains("blob.core.windows.net"))
                {
                    return new AzureBlobStorageSink(metadata);
                }

                if (uri.Authority.Contains("queue.core.windows.net"))
                {
                    return new AzureQueueStorageSink(metadata);
                }

                if (uri.Authority.Contains("documents.azure.com"))
                {
                    return new CosmosDBSink(metadata);
                }

                return new RestWebServiceSink(metadata, claims, cert);
            }

            if (uri.Scheme == "iothub")
            {
                return new IoTHubSink(metadata);
            }

            if (uri.Scheme == "eh")
            {
                return new EventHubSink(metadata);
            }

            if (uri.Scheme == "sb")
            {
                return new ServiceBusTopicSink(metadata);
            }

            if (uri.Scheme == "eventgrid")
            {
                return new EventGridSink(metadata);
            }

            if (uri.Scheme == "redis")
            {
                return new RedisSink(metadata);
            }

            throw new InvalidOperationException(string.Format("EventSinkFactory cannot find concrete type for {0}",
                metadata.NotifyAddress));

            throw new Exception("ouch!");
        }
    }
}