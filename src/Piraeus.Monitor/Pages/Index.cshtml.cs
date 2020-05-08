using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Orleans;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using Piraeus.Monitor.Hubs;

namespace Piraeus.Monitor.Pages
{
    public class IndexModel : PageModel
    {
        private readonly HubAdapter adapter;

        private readonly ILogger<IndexModel> logger;

        public IndexModel(IClusterClient clusterClient, ILogger<IndexModel> logger)
        {
            this.logger = logger;
            adapter = new HubAdapter(clusterClient);
            Container = new Dictionary<string, EventMetadata>();
        }

        public Dictionary<string, EventMetadata> Container
        {
            get; internal set;
        }

        public int Counter
        {
            get; set;
        }

        public int Index
        {
            get; internal set;
        }

        public List<string> PiSystems
        {
            get; internal set;
        }

        public int Quantity
        {
            get; internal set;
        }

        //public void OnGet()
        //{
        //    //PiSystems = adapter.GetPiSystemsAsync().GetAwaiter().GetResult();
        //    OnGet(0, 10);
        //}

        public void OnGet(int index, int quantity)
        {
            ListContinuationToken ltoken;

            if (index == 0 && quantity == 0)
            {
                ltoken = new ListContinuationToken { Index = 0, Quantity = 10 };
            }
            else
            {
                ltoken = new ListContinuationToken { Index = index, Quantity = quantity };
            }

            ltoken = adapter.GetPiSystemsAsync(ltoken).GetAwaiter().GetResult();

            if (ltoken == null)
            {
                return;
            }

            foreach (string item in ltoken.Items)
            {
                EventMetadata metadata = adapter.GetMetadataAsync(item).GetAwaiter().GetResult();
                Container.Add(item, metadata);
            }

            Index = ltoken.Index;
            Quantity = ltoken.Quantity;
        }
    }
}