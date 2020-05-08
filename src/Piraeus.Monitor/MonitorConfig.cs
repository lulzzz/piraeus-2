using System;
using Newtonsoft.Json;

namespace Piraeus.Monitor
{
    [Serializable]
    [JsonObject]
    public class MonitorConfig
    {
        [JsonProperty("clientId")]
        public string ClientId
        {
            get; set;
        }

        [JsonProperty("domain")]
        public string Domain
        {
            get; set;
        }

        [JsonProperty("tenantId")]
        public string TenantId
        {
            get; set;
        }
    }
}