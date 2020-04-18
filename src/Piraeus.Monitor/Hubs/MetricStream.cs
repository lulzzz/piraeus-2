using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Piraeus.Monitor.Hubs
{
    public class MetricStream : IMetricStream
    {
        private readonly HubAdapter adapter;

        private readonly IHubContext<PiSystemHub> context;

        public MetricStream(IHubContext<PiSystemHub> context, HubAdapter adapter)
        {
            this.context = context;
            this.adapter = adapter;
            this.adapter.OnNotify += Adapter_OnNotify;
        }

        public async Task SubscribeAsync(string resourceUriString)
        {
            await adapter.AddMetricObserverAsync(resourceUriString);
        }

        public async Task UnsubscribeAsync(string resourceUriString)
        {
            await adapter.RemoveMetricObserverAsync(resourceUriString);
        }

        private async void Adapter_OnNotify(object sender, NotificationEventArgs e)
        {
            await context.Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(e.Metrics));
        }
    }
}