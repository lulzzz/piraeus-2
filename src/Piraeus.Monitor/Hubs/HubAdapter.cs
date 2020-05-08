using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Orleans;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using Piraeus.Grains;

namespace Piraeus.Monitor.Hubs
{
    public class HubAdapter
    {
        private readonly Dictionary<string, string> container;

        private readonly TimeSpan leaseTime;

        private readonly GraphManager manager;

        private readonly Timer timer;

        public HubAdapter(IClusterClient clusterClient)
        {
            if (!GraphManager.IsInitialized)
            {
                manager = GraphManager.Create(clusterClient);
            }
            else
            {
                manager = GraphManager.Instance;
            }

            leaseTime = TimeSpan.FromSeconds(30.0);
            container = new Dictionary<string, string>();
            timer = new Timer(leaseTime.TotalMilliseconds / 2);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        public event EventHandler<NotificationEventArgs> OnNotify;

        public async Task AddMetricObserverAsync(string resourceUriString)
        {
            if (!container.ContainsKey(resourceUriString))
            {
                KeyValuePair<string, string>[] kvps = container.ToArray();
                foreach (var item in kvps)
                    await RemoveMetricObserverAsync(item.Key);

                MetricObserver observer = new MetricObserver();
                observer.OnNotify += Observer_OnNotify;
                string leaseKey = await manager.AddResourceObserverAsync(resourceUriString, leaseTime, observer);
                container.Add(resourceUriString, leaseKey);

                if (!timer.Enabled)
                {
                    timer.Enabled = true;
                }
            }
        }

        public async Task<EventMetadata> GetMetadataAsync(string resourceUriString)
        {
            return await manager.GetPiSystemMetadataAsync(resourceUriString);
        }

        public async Task<EventMetadata> GetPiSystemMetadata(string resourceUriString)
        {
            return await manager.GetPiSystemMetadataAsync(resourceUriString);
        }

        public async Task<List<string>> GetPiSystemsAsync()
        {
            return await manager.GetSigmaAlgebraAsync();
        }

        public async Task<List<string>> GetPiSystemsAsync(int index, int quantity)
        {
            return await manager.GetSigmaAlgebraAsync(index, quantity);
        }

        public async Task<ListContinuationToken> GetPiSystemsAsync(ListContinuationToken token)
        {
            return await manager.GetSigmaAlgebraAsync(token);
        }

        public async Task RemoveMetricObserverAsync(string resourceUriString)
        {
            if (container.ContainsKey(resourceUriString))
            {
                string leaseKey = container[resourceUriString];
                await manager.RemoveResourceObserverAsync(resourceUriString, leaseKey);
            }
        }

        private void Observer_OnNotify(object sender, MetricNotificationEventArgs e)
        {
            OnNotify?.Invoke(this, new NotificationEventArgs(e.Metrics));
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (container.Count == 0)
            {
                timer.Enabled = false;
            }

            KeyValuePair<string, string>[] items = container.ToArray();

            List<Task> taskList = new List<Task>();

            foreach (var item in items)
                taskList.Add(manager.RenewResourceObserverLeaseAsync(item.Key, item.Value, leaseTime));

            if (taskList.Count > 0)
            {
                Task.WhenAll(taskList).GetAwaiter();
            }
        }
    }
}