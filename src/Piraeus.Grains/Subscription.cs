using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using Piraeus.GrainInterfaces;
using Piraeus.Grains.Notifications;

namespace Piraeus.Grains
{
    [Reentrant]
    [StorageProvider(ProviderName = "store")]
    [Serializable]
    public class Subscription : Grain<SubscriptionState>, ISubscription
    {
        [NonSerialized] private readonly ILog logger;

        [NonSerialized] private IDisposable leaseTimer;

        [NonSerialized] private Queue<EventMessage> memoryMessageQueue;

        [NonSerialized] private IDisposable messageQueueTimer;

        [NonSerialized] private EventSink sink;

        public Subscription(ILog logger = null)
        {
            this.logger = logger;
        }

        #region Metrics

        public async Task<CommunicationMetrics> GetMetricsAsync()
        {
            CommunicationMetrics metrics = new CommunicationMetrics(State.Metadata.SubscriptionUriString,
                State.MessageCount, State.ByteCount, State.ErrorCount, State.LastMessageTimestamp,
                State.LastErrorTimestamp);
            return await Task.FromResult(metrics);
        }

        #endregion Metrics

        #region ID

        public async Task<string> GetIdAsync()
        {
            if (State.Metadata == null)
            {
                return null;
            }

            return await Task.FromResult(State.Metadata.SubscriptionUriString);
        }

        #endregion ID

        #region Clear

        public async Task ClearAsync()
        {
            await ClearStateAsync();
        }

        #endregion Clear

        #region Activatio/Deactivation

        public override Task OnActivateAsync()
        {
            State.ErrorLeases ??= new Dictionary<string, IErrorObserver>();
            State.LeaseExpiry ??= new Dictionary<string, Tuple<DateTime, string>>();
            State.MessageLeases ??= new Dictionary<string, IMessageObserver>();
            State.MessageQueue ??= new Queue<EventMessage>();
            State.MetricLeases ??= new Dictionary<string, IMetricObserver>();
            memoryMessageQueue = new Queue<EventMessage>();

            DequeueAsync(State.MessageQueue).Ignore();

            return Task.CompletedTask;
        }

        public override async Task OnDeactivateAsync()
        {
            await WriteStateAsync();
        }

        #endregion Activatio/Deactivation

        #region Metadata

        public async Task<SubscriptionMetadata> GetMetadataAsync()
        {
            return await Task.FromResult(State.Metadata);
        }

        public async Task UpsertMetadataAsync(SubscriptionMetadata metadata)
        {
            try
            {
                _ = metadata ?? throw new ArgumentNullException(nameof(metadata));

                State.Metadata = metadata;
                await WriteStateAsync();

                if (sink != null)
                {
                    sink = EventSinkFactory.Create(State.Metadata);
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription get metadata.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        #endregion Metadata

        #region Notification

        public async Task NotifyAsync(EventMessage message)
        {
            try
            {
                _ = message ?? throw new ArgumentNullException(nameof(message));

                State.MessageCount++;
                State.ByteCount += message.Message.LongLength;
                State.LastMessageTimestamp = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(State.Metadata.NotifyAddress))
                {
                    if (sink == null && !EventSinkFactory.IsInitialized)
                    {
                        IServiceIdentity identity = GrainFactory.GetGrain<IServiceIdentity>("PiraeusIdentity");
                        byte[] certBytes = await identity.GetCertificateAsync();
                        List<KeyValuePair<string, string>> kvps = await identity.GetClaimsAsync();
                        X509Certificate2 certificate = certBytes == null ? null : new X509Certificate2(certBytes);
                        List<Claim> claims = GetClaims(kvps);

                        sink = EventSinkFactory.Create(State.Metadata, claims, certificate);
                        sink.OnResponse += Sink_OnResponse;
                    }

                    await sink.SendAsync(message);
                }
                else if (State.MessageLeases.Count > 0)
                {
                    foreach (var observer in State.MessageLeases.Values)
                        observer.Notify(message);
                }
                else
                {
                    if (State.Metadata.DurableMessaging && State.Metadata.TTL.HasValue)
                    {
                        await QueueDurableMessageAsync(message);
                    }
                    else
                    {
                        await QueueInMemoryMessageAsync(message);
                    }
                }

                await NotifyMetricsAsync();
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription notify.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        public async Task NotifyAsync(EventMessage message, List<KeyValuePair<string, string>> indexes)
        {
            try
            {
                _ = message ?? throw new ArgumentNullException(nameof(message));

                if (indexes == null)
                {
                    await NotifyAsync(message);
                    return;
                }

                var query = indexes.Where(c =>
                    c.Value.First() != '~' &&
                    State.Metadata.Indexes.Contains(new KeyValuePair<string, string>(c.Key, c.Value)) ||
                    c.Value.First() == '~' &&
                    !State.Metadata.Indexes.Contains(new KeyValuePair<string, string>(c.Key, c.Value.TrimStart('~'))));

                if (indexes.Count == query.Count())
                {
                    await NotifyAsync(message);
                }
                else
                {
                    State.MessageCount++;
                    State.ByteCount += message.Message.LongLength;
                    State.LastMessageTimestamp = DateTime.UtcNow;
                    await NotifyMetricsAsync();
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription notify with indexes.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        private List<Claim> GetClaims(List<KeyValuePair<string, string>> kvps)
        {
            List<Claim> list = null;
            if (kvps != null)
            {
                list = new List<Claim>();
                foreach (var kvp in kvps)
                    list.Add(new Claim(kvp.Key, kvp.Value));
            }

            return list;
        }

        private async void Sink_OnResponse(object sender, EventSinkResponseArgs e)
        {
            IPiSystem pisystem = GrainFactory.GetGrain<IPiSystem>(e.Message.ResourceUri);
            await pisystem.PublishAsync(e.Message);
        }

        #endregion Notification

        #region Observers

        public async Task<string> AddObserverAsync(TimeSpan lifetime, IMessageObserver observer)
        {
            try
            {
                _ = observer ?? throw new ArgumentNullException(nameof(observer));

                string leaseKey = Guid.NewGuid().ToString();
                State.MessageLeases.Add(leaseKey, observer);
                State.LeaseExpiry.Add(leaseKey, new Tuple<DateTime, string>(DateTime.UtcNow.Add(lifetime), "Message"));

                leaseTimer ??= RegisterTimer(CheckLeaseExpiryAsync, null, TimeSpan.FromSeconds(10.0),
                    TimeSpan.FromSeconds(60.0));

                await DequeueAsync(State.MessageQueue);
                await WriteStateAsync();

                return await Task.FromResult(leaseKey);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription add message observer.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        public async Task<string> AddObserverAsync(TimeSpan lifetime, IMetricObserver observer)
        {
            try
            {
                _ = observer ?? throw new ArgumentNullException(nameof(observer));

                string leaseKey = Guid.NewGuid().ToString();
                State.MetricLeases.Add(leaseKey, observer);
                State.LeaseExpiry.Add(leaseKey, new Tuple<DateTime, string>(DateTime.UtcNow.Add(lifetime), "Metric"));

                leaseTimer ??= RegisterTimer(CheckLeaseExpiryAsync, null, TimeSpan.FromSeconds(10.0),
                    TimeSpan.FromSeconds(60.0));

                await WriteStateAsync();
                return await Task.FromResult(leaseKey);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription add metric observer.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        public async Task<string> AddObserverAsync(TimeSpan lifetime, IErrorObserver observer)
        {
            try
            {
                _ = observer ?? throw new ArgumentNullException(nameof(observer));

                string leaseKey = Guid.NewGuid().ToString();
                State.ErrorLeases.Add(leaseKey, observer);
                State.LeaseExpiry.Add(leaseKey, new Tuple<DateTime, string>(DateTime.UtcNow.Add(lifetime), "Error"));

                leaseTimer ??= RegisterTimer(CheckLeaseExpiryAsync, null, TimeSpan.FromSeconds(10.0),
                    TimeSpan.FromSeconds(60.0));

                await WriteStateAsync();
                return await Task.FromResult(leaseKey);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription add error observer.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        public async Task RemoveObserverAsync(string leaseKey)
        {
            try
            {
                _ = leaseKey ?? throw new ArgumentNullException(nameof(leaseKey));

                State.LeaseExpiry.Remove(leaseKey);
                State.MetricLeases.Remove(leaseKey);
                State.ErrorLeases.Remove(leaseKey);

                await WriteStateAsync();
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription remove observer.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        public async Task<bool> RenewObserverLeaseAsync(string leaseKey, TimeSpan lifetime)
        {
            try
            {
                _ = leaseKey ?? throw new ArgumentNullException(nameof(leaseKey));

                if (State.LeaseExpiry.ContainsKey(leaseKey))
                {
                    Tuple<DateTime, string> value = State.LeaseExpiry[leaseKey];
                    Tuple<DateTime, string> newValue =
                        new Tuple<DateTime, string>(DateTime.UtcNow.Add(lifetime), value.Item2);
                    State.LeaseExpiry[leaseKey] = newValue;
                    await WriteStateAsync();
                    return await Task.FromResult(true);
                }

                return await Task.FromResult(false);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription renew observer lease.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        #endregion Observers

        #region private methods

        private async Task CheckLeaseExpiryAsync(object args)
        {
            try
            {
                var messageQuery =
                    State.LeaseExpiry.Where(c => c.Value.Item1 < DateTime.UtcNow && c.Value.Item2 == "Message");
                var metricQuery =
                    State.LeaseExpiry.Where(c => c.Value.Item1 < DateTime.UtcNow && c.Value.Item2 == "Metric");
                var errorQuery =
                    State.LeaseExpiry.Where(c => c.Value.Item1 < DateTime.UtcNow && c.Value.Item2 == "Error");

                List<string> messageLeaseKeyList = new List<string>(messageQuery.Select(c => c.Key));
                List<string> metricLeaseKeyList = new List<string>(metricQuery.Select(c => c.Key));
                List<string> errorLeaseKeyList = new List<string>(errorQuery.Select(c => c.Key));

                foreach (var item in messageLeaseKeyList)
                {
                    State.MessageLeases.Remove(item);
                    State.LeaseExpiry.Remove(item);

                    if (State.Metadata.IsEphemeral)
                    {
                        await UnsubscribeFromResourceAsync();
                    }
                }

                foreach (var item in metricLeaseKeyList)
                {
                    State.MetricLeases.Remove(item);
                    State.LeaseExpiry.Remove(item);
                }

                foreach (var item in errorLeaseKeyList)
                {
                    State.ErrorLeases.Remove(item);
                    State.LeaseExpiry.Remove(item);
                }

                if (State.LeaseExpiry.Count == 0 &&
                    State.MessageLeases.Count == 0 &&
                    State.ErrorLeases.Count == 0)
                {
                    leaseTimer.Dispose();
                    leaseTimer = null;
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription check lease expiry.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        private async Task CheckQueueAsync(object args)
        {
            try
            {
                if (State.MessageLeases.Count > 0)
                {
                    if (memoryMessageQueue != null)
                    {
                        await DequeueAsync(memoryMessageQueue);

                        if (memoryMessageQueue.Count > 0)
                        {
                            DelayDeactivation(State.Metadata.TTL.Value);
                        }
                    }

                    if (State.MessageQueue != null)
                    {
                        await DequeueAsync(State.MessageQueue);
                    }
                }
                else
                {
                    messageQueueTimer.Dispose();
                    messageQueueTimer = null;
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription check queue.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        private async Task DequeueAsync(Queue<EventMessage> queue)
        {
            try
            {
                EventMessage[] msgs = queue != null && queue.Count > 0 ? queue.ToArray() : null;
                queue.Clear();

                if (msgs != null)
                {
                    foreach (EventMessage msg in msgs)
                    {
                        if (msg.Timestamp.Add(State.Metadata.TTL.Value) > DateTime.UtcNow)
                        {
                            await NotifyAsync(msg);

                            if (State.Metadata.SpoolRate.HasValue)
                            {
                                await Task.Delay(State.Metadata.SpoolRate.Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription dequeue.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        private async Task NotifyErrorAsync(Exception ex)
        {
            try
            {
                if (State.ErrorLeases.Count == 0)
                {
                    return;
                }

                foreach (var item in State.ErrorLeases.Values)
                    item.NotifyError(State.Metadata.SubscriptionUriString, ex);
            }
            catch (Exception ex1)
            {
                await logger?.LogErrorAsync(ex1, "Subscription notify errors.");
            }
        }

        private async Task NotifyMetricsAsync()
        {
            try
            {
                if (State.MetricLeases.Count == 0)
                {
                    return;
                }

                foreach (var item in State.MetricLeases.Values)
                {
                    item.NotifyMetrics(new CommunicationMetrics(State.Metadata.SubscriptionUriString,
                        State.MessageCount, State.ByteCount, State.ErrorCount, State.LastMessageTimestamp.Value,
                        State.LastErrorTimestamp));
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription notify metrics.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        private async Task QueueDurableMessageAsync(EventMessage message)
        {
            try
            {
                if (State.MessageQueue.Count > 0)
                {
                    while (State.MessageQueue.Peek().Timestamp.Add(State.Metadata.TTL.Value) < DateTime.UtcNow)
                        State.MessageQueue.Dequeue();
                }

                State.MessageQueue.Enqueue(message);

                messageQueueTimer ??= RegisterTimer(CheckQueueAsync, null, TimeSpan.FromSeconds(1.0),
                    TimeSpan.FromSeconds(5.0));

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription queue durable message.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        private async Task QueueInMemoryMessageAsync(EventMessage message)
        {
            try
            {
                memoryMessageQueue.Enqueue(message);
                messageQueueTimer ??= RegisterTimer(CheckQueueAsync, null, TimeSpan.FromSeconds(1.0),
                    TimeSpan.FromSeconds(5.0));

                DelayDeactivation(State.Metadata.TTL.Value);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription queue in-memory message.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        private async Task UnsubscribeFromResourceAsync()
        {
            try
            {
                string uriString = State.Metadata.SubscriptionUriString;
                Uri uri = new Uri(uriString);

                string resourceUriString = uriString.Replace("/" + uri.Segments[^1], "");
                IPiSystem resource = GrainFactory.GetGrain<IPiSystem>(resourceUriString);

                if (State.Metadata != null && !string.IsNullOrEmpty(State.Metadata.SubscriptionUriString))
                {
                    await resource.UnsubscribeAsync(State.Metadata.SubscriptionUriString);
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Subscription unsubscribe from resource.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        #endregion private methods
    }
}