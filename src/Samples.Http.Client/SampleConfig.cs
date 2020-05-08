using System;
using Newtonsoft.Json;

namespace Samples.Http.Client
{
    [Serializable]
    [JsonObject]
    public class SampleConfig
    {
        [JsonProperty("audience")]
        public string Audience
        {
            get; set;
        }

        [JsonProperty("dns")]
        public string DnsName
        {
            get; set;
        }

        [JsonProperty("identityClaimType")]
        public string IdentityNameClaimType
        {
            get; set;
        }

        [JsonProperty("issuer")]
        public string Issuer
        {
            get; set;
        }

        [JsonProperty("location")]
        public string Location
        {
            get; set;
        }

        [JsonProperty("symmetricKey")]
        public string SymmetricKey
        {
            get; set;
        }
    }
}