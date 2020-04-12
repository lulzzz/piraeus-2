using Newtonsoft.Json;
using System;

namespace Piraeus.Monitor
{
    [Serializable]
    [JsonObject]
    public class MonitorConfig
    {
        public MonitorConfig()
        {
        }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }
    }
}