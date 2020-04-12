using Newtonsoft.Json;
using Piraeus.Grains;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Piraeus.Core;
using Orleans;
using Piraeus.Core.Metadata;

namespace Piraeus.Dashboard.Hubs
{
    public class HubAdapter
    {
        public HubAdapter(IClusterClient clusterClient)
        {
            if(!GraphManager.IsInitialized)
            {
                manager = GraphManager.Create(clusterClient);
            }
            else
            {
                manager = GraphManager.Instance;
            }

            leaseTime = TimeSpan.FromSeconds(10.0);
            container = new Dictionary<string, string>();
            timer = new System.Timers.Timer(leaseTime.TotalMilliseconds / 2);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        public event EventHandler<NotificationEventArgs> OnNotify;

        private GraphManager manager;
        private TimeSpan leaseTime;
        private Dictionary<string, string> container;
        private System.Timers.Timer timer;

        public async Task<List<string>> GetPiSystems()
        {
            return await manager.GetSigmaAlgebraAsync();
        }

        public async Task<EventMetadata> GetPiSystemMetadata(string resourceUriString)
        {
            return await manager.GetPiSystemMetadataAsync(resourceUriString);
        }

        public async Task AddMetricObserverAsync(string resourceUriString)
        {
            if(!container.ContainsKey(resourceUriString))
            {
                MetricObserver observer = new MetricObserver();
                observer.OnNotify += Observer_OnNotify;
                string leaseKey = await manager.AddResourceObserverAsync(resourceUriString, leaseTime, observer);
                container.Add(resourceUriString, leaseKey);

                if (!timer.Enabled)
                    timer.Enabled = true;
            }            
        }

        public async Task RemoveMetricObserverAsync(string resourceUriString)
        {
            if(container.ContainsKey(resourceUriString))
            {
                string leaseKey = container[resourceUriString];
                await manager.RemoveResourceObserverAsync(resourceUriString, leaseKey);
            }
        }

        private void Observer_OnNotify(object sender, MetricNotificationEventArgs e)
        {
            OnNotify?.Invoke(this, new NotificationEventArgs(e.Metrics));
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (container.Count == 0)
                timer.Enabled = false;

            KeyValuePair<string, string>[] items = container.ToArray();

            List<Task> taskList = new List<Task>();

            foreach (var item in items)
            {                
                taskList.Add(manager.RenewResourceObserverLeaseAsync(item.Key, item.Value, leaseTime));
            }

            if(taskList.Count > 0)
            {
                Task.WhenAll(taskList).GetAwaiter();
            }
        }
    }
}
