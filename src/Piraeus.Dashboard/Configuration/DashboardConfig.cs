using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Piraeus.Dashboard.Configuration
{
    [Serializable]
    [JsonObject]
    public class DashboardConfig
    {
        public DashboardConfig()
        {
        }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }
    }
}
