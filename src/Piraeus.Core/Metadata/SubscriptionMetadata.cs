using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Piraeus.Core.Metadata
{
    [Serializable]
    [JsonObject]
    public class SubscriptionMetadata
    {
        public SubscriptionMetadata()
        {
        }

        public SubscriptionMetadata(string identity, string subscriptionUriString, string address, string symmetricKey,
            string description = null, TimeSpan? ttl = null, DateTime? expires = null, TimeSpan? spoolRate = null,
            bool durableMessaging = false)
        {
            Identity = identity;
            SubscriptionUriString = subscriptionUriString;
            NotifyAddress = address;
            SymmetricKey = symmetricKey;
            Description = description;
            TTL = ttl;
            Expires = expires;
            SpoolRate = spoolRate;
            DurableMessaging = durableMessaging;
            IsEphemeral = false;
        }

        public SubscriptionMetadata(string subscriptionUriString)
        {
            SubscriptionUriString = subscriptionUriString;
            Description = "Ephemeral subscription.";
            IsEphemeral = true;
        }

        public SubscriptionMetadata(string subscriptionUriString, string identity)
        {
            SubscriptionUriString = subscriptionUriString;
            Identity = identity;
            Description = "Ephemeral subscription.";
            IsEphemeral = true;
        }

        [JsonProperty("claimKey")]
        public string ClaimKey
        {
            get; set;
        }

        [JsonProperty("description")]
        public string Description
        {
            get; set;
        }

        [JsonProperty("durableMessaging")]
        public bool DurableMessaging
        {
            get; set;
        }

        [JsonProperty("expires")]
        public DateTime? Expires
        {
            get; set;
        }

        [JsonProperty("identity")]
        public string Identity
        {
            get; set;
        }

        [JsonProperty("indexes")]
        public List<KeyValuePair<string, string>> Indexes
        {
            get; set;
        }

        [JsonProperty("isEphemeral")]
        public bool IsEphemeral
        {
            get; set;
        }

        [JsonProperty("notifyAddress")]
        public string NotifyAddress
        {
            get; set;
        }

        [JsonProperty("spoolRate")]
        public TimeSpan? SpoolRate
        {
            get; set;
        }

        [JsonProperty("subscriptionUriString")]
        public string SubscriptionUriString
        {
            get; set;
        }

        [JsonProperty("symmetricKey")]
        public string SymmetricKey
        {
            get; set;
        }

        [JsonProperty("securityTokenType")]
        public SecurityTokenType? TokenType
        {
            get; set;
        }

        [JsonProperty("ttl")]
        public TimeSpan? TTL
        {
            get; set;
        }
    }
}