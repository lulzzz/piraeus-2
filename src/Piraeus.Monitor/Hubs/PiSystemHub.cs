using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Piraeus.Monitor.Hubs
{
    public class PiSystemHub : Hub
    {
        private readonly HashSet<string> container;

        private readonly IMetricStream metricStream;

        public PiSystemHub(IMetricStream metricStream)
        {
            this.metricStream = metricStream;
            container = new HashSet<string>();
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string[] items = container.ToArray();

            foreach (var item in items)
                await metricStream.UnsubscribeAsync(item);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SubscribeAsync(string resourceUriString)
        {
            if (!container.Contains(resourceUriString))
            {
                container.Add(resourceUriString);
                await metricStream.SubscribeAsync(resourceUriString);
            }
        }

        public async Task UnsubscribeAsync(string resourceUriString)
        {
            if (container.Contains(resourceUriString))
            {
                container.Remove(resourceUriString);
                await metricStream.UnsubscribeAsync(resourceUriString);
            }
        }
    }
}