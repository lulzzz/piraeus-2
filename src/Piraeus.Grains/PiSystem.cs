using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using Piraeus.GrainInterfaces;

namespace Piraeus.Grains
{
    [Reentrant]
    [StorageProvider(ProviderName = "store")]
    [Serializable]
    public class PiSystem : Grain<PiSystemState>, IPiSystem
    {
        [NonSerialized] private readonly ILog logger;

        [NonSerialized] private IDisposable leaseTimer;

        public PiSystem(ILog logger = null)
        {
            this.logger = logger;
        }

        #region Clear

        public async Task ClearAsync()
        {
            try
            {
                foreach (ISubscription item in State.Subscriptions.Values)
                {
                    string id = await item.GetIdAsync();
                    if (id != null)
                    {
                        await UnsubscribeAsync(id);
                    }
                }

                if (State.Metadata != null)
                {
                    ISigmaAlgebra resourceList = GrainFactory.GetGrain<ISigmaAlgebra>("resourcelist");
                    await resourceList.RemoveAsync(State.Metadata.ResourceUriString);
                }

                await ClearStateAsync();
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Pi-system clear.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        #endregion Clear

        #region Activation/Deactivation

        public override Task OnActivateAsync()
        {
            State.Subscriptions ??= new Dictionary<string, ISubscription>();
            State.LeaseExpiry ??= new Dictionary<string, Tuple<DateTime, string>>();
            State.MetricLeases ??= new Dictionary<string, IMetricObserver>();
            State.ErrorLeases ??= new Dictionary<string, IErrorObserver>();

            return Task.CompletedTask;
        }

        public override async Task OnDeactivateAsync()
        {
            await WriteStateAsync();
        }

        #endregion Activation/Deactivation

        #region Resource Metadata

        public async Task<EventMetadata> GetMetadataAsync()
        {
            return await Task.FromResult(State.Metadata);
        }

        public async Task<CommunicationMetrics> GetMetricsAsync()
        {
            CommunicationMetrics metrics = new CommunicationMetrics(State.Metadata.ResourceUriString,
                State.MessageCount, State.ByteCount, State.ErrorCount, State.LastMessageTimestamp,
                State.LastErrorTimestamp);
            return await Task.FromResult(metrics);
        }

        public async Task UpsertMetadataAsync(EventMetadata metadata)
        {
            _ = metadata ?? throw new ArgumentNullException(nameof(metadata));

            try
            {

                if (State.Metadata != null && metadata.ResourceUriString != this.GetPrimaryKeyString())
                {
                    throw new ResourceIdentityMismatchException("Metadata resource mismatch.");
                }

                State.Metadata = metadata;

                ISigmaAlgebra resourceList = GrainFactory.GetGrain<ISigmaAlgebra>("resourcelist");
                await resourceList.AddAsync(metadata.ResourceUriString);

                await WriteStateAsync();
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, $"Pi-system UpdateMetadataAsync '{metadata.ResourceUriString}'");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        #endregion Resource Metadata

        #region Subscribe/Unsubscribe

        public async Task<IEnumerable<string>> GetSubscriptionListAsync()
        {
            try
            {
                if (State.Subscriptions == null || State.Subscriptions.Count == 0)
                {
                    return null;
                }

                string[] result = State.Subscriptions.Keys.ToArray();
                return await Task.FromResult<IEnumerable<string>>(result);
            }
            catch (Exception ex)
            {
                await logger.LogErrorAsync(ex, "Pi-system GetSubscriptionListAsync");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        public async Task SubscribeAsync(ISubscription subscription)
        {
            try
            {
                _ = subscription ?? throw new ArgumentNullException(nameof(subscription));

                string id = await subscription.GetIdAsync();
                Uri uri = new Uri(id);

                if (State.Subscriptions.ContainsKey(id))
                {
                    State.Subscriptions[id] = subscription;
                }
                else
                {
                    State.Subscriptions.Add(id, subscription);
                }

                SubscriptionMetadata metadata = await subscription.GetMetadataAsync();

                if (!metadata.IsEphemeral && !string.IsNullOrEmpty(metadata.Identity) &&
                    metadata.NotifyAddress == null)
                {
                    ISubscriber subscriber = GrainFactory.GetGrain<ISubscriber>(metadata.Identity.ToLowerInvariant());
                    await subscriber.AddSubscriptionAsync(metadata.SubscriptionUriString);
                }

                await WriteStateAsync();
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Pi-system Subscribe");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        public async Task UnsubscribeAsync(string subscriptionUriString, string identity)
        {
            try
            {
                _ = subscriptionUriString ?? throw new ArgumentNullException(nameof(subscriptionUriString));
                _ = identity ?? throw new ArgumentNullException(nameof(identity));

                await UnsubscribeAsync(subscriptionUriString);

                ISubscriber subscriber = GrainFactory.GetGrain<ISubscriber>(identity.ToLowerInvariant());
                await subscriber.RemoveSubscriptionAsync(subscriptionUriString);
                await WriteStateAsync();
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Pi-system Unsubscribe.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        public async Task UnsubscribeAsync(string subscriptionUriString)
        {
            try
            {
                _ = subscriptionUriString ?? throw new ArgumentNullException(nameof(subscriptionUriString));

                if (State.Subscriptions.ContainsKey(subscriptionUriString))
                {
                    State.Subscriptions.Remove(subscriptionUriString);
                }

                await WriteStateAsync();
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Pi-system Unsubscribe.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        #endregion Subscribe/Unsubscribe

        #region Publish

        public async Task PublishAsync(EventMessage message)
        {
            try
            {
                _ = message ?? throw new ArgumentNullException(nameof(message));

                State.MessageCount++;
                State.ByteCount += message.Message.LongLength;
                State.LastMessageTimestamp = DateTime.UtcNow;

                List<Task> taskList = new List<Task>();

                if (State.Subscriptions.Count > 0)
                {
                    ISubscription[] subscriptions = State.Subscriptions.Values.ToArray();
                    foreach (var item in subscriptions)
                        taskList.Add(item.NotifyAsync(message));

                    await Task.WhenAll(taskList);
                }

                await NotifyMetricsAsync();
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Pi-system Publish.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        public async Task PublishAsync(EventMessage message, List<KeyValuePair<string, string>> indexes)
        {
            try
            {
                _ = message ?? throw new ArgumentNullException(nameof(message));

                State.MessageCount++;
                State.ByteCount += message.Message.LongLength;
                State.LastMessageTimestamp = DateTime.UtcNow;
                if (indexes == null)
                {
                    await PublishAsync(message);
                }
                else
                {
                    if (State.Subscriptions.Count > 0)
                    {
                        List<Task> taskList = new List<Task>();

                        ISubscription[] subscriptions = State.Subscriptions.Values.ToArray();
                        foreach (var item in subscriptions)
                            taskList.Add(item.NotifyAsync(message, indexes));

                        await Task.WhenAll(taskList);
                    }
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Pi-system Publish.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        #endregion Publish

        #region Observers

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

                return await Task.FromResult(leaseKey);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Pi-system add metric observer.");
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
                return await Task.FromResult(leaseKey);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Pi-system add error observer.");
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
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Pi-system remove observer.");
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
                    return await Task.FromResult(true);
                }

                return await Task.FromResult(false);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, "Pi-system remove observer.");
                await NotifyErrorAsync(ex);
                throw;
            }
        }

        #endregion Observers

        #region private methods

        private async Task CheckLeaseExpiryAsync(object args)
        {
            var metricQuery =
                State.LeaseExpiry.Where(c => c.Value.Item1 < DateTime.UtcNow && c.Value.Item2 == "Metric");
            var errorQuery = State.LeaseExpiry.Where(c => c.Value.Item1 < DateTime.UtcNow && c.Value.Item2 == "Error");

            List<string> metricLeaseKeyList = new List<string>(metricQuery.Select(c => c.Key));
            List<string> errorLeaseKeyList = new List<string>(errorQuery.Select(c => c.Key));

            foreach (string item in metricLeaseKeyList)
            {
                State.MetricLeases.Remove(item);
                State.LeaseExpiry.Remove(item);
            }

            foreach (string item in errorLeaseKeyList)
            {
                State.ErrorLeases.Remove(item);
                State.LeaseExpiry.Remove(item);
            }

            if (State.LeaseExpiry.Count == 0)
            {
                leaseTimer.Dispose();
                leaseTimer = null;
            }

            await Task.CompletedTask;
        }

        private async Task NotifyErrorAsync(Exception ex)
        {
            if (State.ErrorLeases.Count > 0)
            {
                foreach (var item in State.ErrorLeases.Values)
                    item.NotifyError(State.Metadata.ResourceUriString, ex);
            }

            await Task.CompletedTask;
        }

        private async Task NotifyMetricsAsync()
        {
            if (State.MetricLeases.Count > 0)
            {
                foreach (var item in State.MetricLeases.Values)
                {
                    item.NotifyMetrics(new CommunicationMetrics(State.Metadata.ResourceUriString, State.MessageCount,
                        State.ByteCount, State.ErrorCount, State.LastMessageTimestamp.Value, State.LastErrorTimestamp));
                }
            }

            await Task.CompletedTask;
        }

        #endregion private methods
    }
}