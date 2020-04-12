using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Piraeus.Dashboard.Hubs
{
    public class PiSystemHub : Hub
    {
        public PiSystemHub(IMetricStream metricStream)
        {
            this.metricStream = metricStream;
            this.container = new HashSet<string>();
        }

        private IMetricStream metricStream;
        private HashSet<string> container;


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
                await metricStream.SubscribeAsync(resourceUriString);
            }
        }

    }
}
