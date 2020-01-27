﻿using Newtonsoft.Json;
using System;

namespace Samples.Http.Client
{
    [Serializable]
    [JsonObject]
    public class SampleConfig
    {
        public SampleConfig()
        {
        }

        [JsonProperty("dns")]
        public string DnsName { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("audience")]
        public string Audience { get; set; }

        [JsonProperty("identityClaimType")]
        public string IdentityNameClaimType { get; set; }

        [JsonProperty("symmetricKey")]
        public string SymmetricKey { get; set; }
    }
}
