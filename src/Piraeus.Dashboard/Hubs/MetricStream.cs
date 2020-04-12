using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Piraeus.Dashboard.Hubs
{
    public class MetricStream : IMetricStream
    {
        public MetricStream(IHubContext<PiSystemHub> context, HubAdapter adapter)
        {
            this.context = context;
            this.adapter = adapter;
            this.adapter.OnNotify += Adapter_OnNotify;
        }

        private async void Adapter_OnNotify(object sender, NotificationEventArgs e)
        {
            await context.Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(e.Metrics));
        }

        private IHubContext<PiSystemHub> context;
        private HubAdapter adapter;

        public async Task SubscribeAsync(string resourceUriString)
        {
            await adapter.AddMetricObserverAsync(resourceUriString);
        }

        public async Task UnsubscribeAsync(string resourceUriString)
        {
            await adapter.AddMetricObserverAsync(resourceUriString);
        }
    }
}
