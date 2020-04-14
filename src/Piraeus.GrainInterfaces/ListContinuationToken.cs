using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Piraeus.GrainInterfaces
{
    [Serializable]
    [JsonObject]
    public class ListContinuationToken
    {
        public ListContinuationToken()
        {
        }

        [JsonProperty("remaining")]
        public int Remaining { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("items")]
        public List<string> Items { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}