using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Piraeus.Dashboard.Hubs;

namespace Piraeus.Dashboard.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public List<string> PiSystems;
        private HubAdapter adapter;

        public IndexModel(HubAdapter adapter)
        {
            this.adapter = adapter;
        }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            this.PiSystems = adapter.GetPiSystems().GetAwaiter().GetResult();
        }
    }
}
