using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Piraeus.Auditing;
using Piraeus.Core;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using Piraeus.GrainInterfaces;
using Piraeus.Grains;

namespace Piraeus.Adapters
{
    public class OrleansAdapter
    {
        private readonly IAuditor auditor;

        private readonly string channelType;

        private readonly Dictionary<string, Tuple<string, string>> container;

        private readonly Dictionary<string, IMessageObserver> durableObservers;

        private readonly Dictionary<string, IMessageObserver> ephemeralObservers;

        private readonly GraphManager graphManager;

        private readonly ILog logger;

        private readonly string protocolType;

        private bool disposedValue;

        private string identity;

        private Timer leaseTimer;

        public OrleansAdapter(string identity, string channelType, string protocolType, GraphManager graphManager,
            ILog logger = null)
        {
            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);
            this.identity = identity;
            this.channelType = channelType;
            this.protocolType = protocolType;
            this.graphManager = graphManager;
            this.logger = logger;
            container = new Dictionary<string, Tuple<string, string>>();
            ephemeralObservers = new Dictionary<string, IMessageObserver>();
            durableObservers = new Dictionary<string, IMessageObserver>();
        }

        public event EventHandler<ObserveMessageEventArgs> OnObserve;

        public string Identity
        {
            set => identity = value;
        }

        public async Task<List<string>> LoadDurableSubscriptionsAsync(string identity)
        {
            _ = identity ?? throw new ArgumentNullException(nameof(identity));

            List<string> list = new List<string>();

            IEnumerable<string> subscriptionUriStrings =
                await graphManager.GetSubscriberSubscriptionsListAsync(identity);

            if (subscriptionUriStrings == null || subscriptionUriStrings.Count() == 0)
            {
                return null;
            }

            foreach (var item in subscriptionUriStrings)
            {
                if (!durableObservers.ContainsKey(item))
                {
                    MessageObserver observer = new MessageObserver();
                    observer.OnNotify += Observer_OnNotify;

                    TimeSpan leaseTime = TimeSpan.FromSeconds(20.0);

                    string leaseKey = await graphManager.AddSubscriptionObserverAsync(item, leaseTime, observer);

                    durableObservers.Add(item, observer);

                    Uri uri = new Uri(item);
                    string resourceUriString = item.Replace(uri.Segments[^1], "");

                    list.Add(resourceUriString);

                    if (!container.ContainsKey(resourceUriString))
                    {
                        container.Add(resourceUriString, new Tuple<string, string>(item, leaseKey));
                    }
                }
            }

            if (subscriptionUriStrings.Count() > 0)
            {
                EnsureLeaseTimer();
            }

            return list.Count == 0 ? null : list;
        }

        public async Task PublishAsync(EventMessage message, List<KeyValuePair<string, string>> indexes = null)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            AuditRecord record = null;
            DateTime receiveTime = DateTime.UtcNow;

            try
            {
                record = new MessageAuditRecord(message.MessageId, identity, channelType,
                    protocolType.ToUpperInvariant(), message.Message.Length, MessageDirectionType.In, true,
                    receiveTime);

                if (indexes == null || indexes.Count == 0)
                {
                    await graphManager.PublishAsync(message.ResourceUri, message);
                    await logger?.LogDebugAsync($"Published to '{message.ResourceUri}' by {identity} without indexes.");
                }
                else
                {
                    await graphManager.PublishAsync(message.ResourceUri, message, indexes);
                    await logger?.LogDebugAsync($"Published to '{message.ResourceUri}' by {identity} with indexes.");
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, $"Error during publish to '{message.ResourceUri}' for {identity}");
                record = new MessageAuditRecord(message.MessageId, identity, channelType,
                    protocolType.ToUpperInvariant(), message.Message.Length, MessageDirectionType.In, false,
                    receiveTime, ex.Message);
            }
            finally
            {
                if (message.Audit)
                {
                    await auditor?.WriteAuditRecordAsync(record);
                }
            }
        }

        public async Task<string> SubscribeAsync(string resourceUriString, SubscriptionMetadata metadata)
        {
            _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));
            _ = metadata ?? throw new ArgumentNullException(nameof(metadata));

            try
            {
                metadata.IsEphemeral = true;
                string subscriptionUriString = await graphManager.SubscribeAsync(resourceUriString, metadata);

                MessageObserver observer = new MessageObserver();
                observer.OnNotify += Observer_OnNotify;

                TimeSpan leaseTime = TimeSpan.FromSeconds(20.0);

                string leaseKey =
                    await graphManager.AddSubscriptionObserverAsync(subscriptionUriString, leaseTime, observer);

                ephemeralObservers.Add(subscriptionUriString, observer);

                if (!container.ContainsKey(resourceUriString))
                {
                    container.Add(resourceUriString, new Tuple<string, string>(subscriptionUriString, leaseKey));
                }

                EnsureLeaseTimer();
                logger?.LogDebugAsync(
                    $"Subscribed to '{resourceUriString}' with '{subscriptionUriString}' for {identity}.");
                return subscriptionUriString;
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, $"Error during subscribe to '{resourceUriString}' for {identity}");
                throw ex;
            }
        }

        public async Task UnsubscribeAsync(string resourceUriString)
        {
            _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

            try
            {
                if (container.ContainsKey(resourceUriString))
                {
                    if (ephemeralObservers.ContainsKey(container[resourceUriString].Item1))
                    {
                        await graphManager.RemoveSubscriptionObserverAsync(container[resourceUriString].Item1,
                            container[resourceUriString].Item2);
                        await graphManager.UnsubscribeAsync(container[resourceUriString].Item1);
                        ephemeralObservers.Remove(container[resourceUriString].Item1);
                    }

                    container.Remove(resourceUriString);
                    await logger?.LogDebugAsync($"Unsubscribed '{resourceUriString}'.");
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, $"Error during unsubscribe to '{resourceUriString}' for {identity}.");
                throw ex;
            }
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (leaseTimer != null)
                    {
                        leaseTimer.Stop();
                        leaseTimer.Dispose();
                    }

                    RemoveDurableObserversAsync().GetAwaiter();
                    RemoveEphemeralObserversAsync().GetAwaiter();
                }

                disposedValue = true;
            }
        }

        #endregion Dispose

        #region private methods

        private void EnsureLeaseTimer()
        {
            if (leaseTimer == null)
            {
                leaseTimer = new Timer(30000);
                leaseTimer.Elapsed += LeaseTimer_Elapsed;
                leaseTimer.Start();
            }
        }

        private void LeaseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            KeyValuePair<string, Tuple<string, string>>[] kvps = container.ToArray();

            if (kvps == null || kvps.Length == 0)
            {
                leaseTimer.Stop();
                return;
            }

            Task leaseTask = Task.Factory.StartNew(async () =>
            {
                if (kvps != null && kvps.Length > 0)
                {
                    foreach (var kvp in kvps)
                    {
                        await graphManager.RenewObserverLeaseAsync(kvp.Value.Item1, kvp.Value.Item2,
                            TimeSpan.FromSeconds(60.0));
                    }
                }
            });

            leaseTask.LogExceptions();
        }

        private void Observer_OnNotify(object sender, MessageNotificationArgs e)
        {
            OnObserve?.Invoke(this, new ObserveMessageEventArgs(e.Message));
        }

        private async Task RemoveDurableObserversAsync()
        {
            List<string> list = new List<string>();

            int cnt = durableObservers.Count;
            if (durableObservers.Count > 0)
            {
                List<Task> taskList = new List<Task>();
                KeyValuePair<string, IMessageObserver>[] kvps = durableObservers.ToArray();
                foreach (var item in kvps)
                {
                    IEnumerable<KeyValuePair<string, Tuple<string, string>>> items =
                        container.Where(c => c.Value.Item1 == item.Key);
                    foreach (var lease in items)
                    {
                        list.Add(lease.Value.Item1);

                        if (durableObservers.ContainsKey(lease.Value.Item1))
                        {
                            Task task = graphManager.RemoveSubscriptionObserverAsync(lease.Value.Item1,
                                lease.Value.Item2);
                            taskList.Add(task);
                        }
                    }
                }

                if (taskList.Count > 0)
                {
                    await Task.WhenAll(taskList);
                }

                durableObservers.Clear();
                RemoveFromContainer(list);
                await logger?.LogInformationAsync(
                    "'{0}' - Durable observers removed by Orleans Adapter for identity '{1}'", cnt, identity);
            }
            else
            {
                await logger?.LogInformationAsync(
                    "No Durable observers found by Orleans Adapter to be removed for identity '{0}'", identity);
            }
        }

        private async Task RemoveEphemeralObserversAsync()
        {
            List<string> list = new List<string>();
            int cnt = ephemeralObservers.Count;

            if (ephemeralObservers.Count > 0)
            {
                KeyValuePair<string, IMessageObserver>[] kvps = ephemeralObservers.ToArray();
                List<Task> unobserveTaskList = new List<Task>();
                foreach (var item in kvps)
                {
                    IEnumerable<KeyValuePair<string, Tuple<string, string>>> items =
                        container.Where(c => c.Value.Item1 == item.Key);

                    foreach (var lease in items)
                    {
                        list.Add(lease.Value.Item1);
                        if (ephemeralObservers.ContainsKey(lease.Value.Item1))
                        {
                            Task unobserveTask =
                                graphManager.RemoveSubscriptionObserverAsync(lease.Value.Item1, lease.Value.Item2);
                            unobserveTaskList.Add(unobserveTask);
                        }
                    }
                }

                if (unobserveTaskList.Count > 0)
                {
                    await Task.WhenAll(unobserveTaskList);
                }

                ephemeralObservers.Clear();
                RemoveFromContainer(list);
                await logger?.LogInformationAsync(
                    "'{0}' - Ephemeral observers removed by Orleans Adapter for identity '{1}'", cnt, identity);
            }
            else
            {
                await logger?.LogInformationAsync(
                    "No Ephemeral observers found by Orleans Adapter to be removed for identity '{0}'", identity);
            }
        }

        private void RemoveFromContainer(string subscriptionUriString)
        {
            List<string> list = new List<string>();
            var query = container.Where(c => c.Value.Item1 == subscriptionUriString);

            foreach (var item in query)
                list.Add(item.Key);

            foreach (string item in list)
                container.Remove(item);
        }

        private void RemoveFromContainer(List<string> subscriptionUriStrings)
        {
            foreach (var item in subscriptionUriStrings)
                RemoveFromContainer(item);
        }

        #endregion private methods
    }
}