using System;
using Newtonsoft.Json;

namespace Piraeus.Auditing
{
    [Serializable]
    [JsonObject]
    public class UserAuditRecord : AuditRecord
    {
        public UserAuditRecord()
        {
        }

        public UserAuditRecord(string channelId, string identity, string claimType, string channel, string protocol,
            string status, DateTime loginTime)
        {
            ChannelId = channelId;
            Identity = identity;
            ClaimType = claimType;
            Channel = channel;
            Protocol = protocol;
            Status = status;
            LoginTime = loginTime;
        }

        public UserAuditRecord(string channelId, string identity, DateTime logoutTime)
        {
            ChannelId = channelId;
            Identity = identity;
            LogoutTime = logoutTime;
        }

        [JsonProperty("channel")]
        public string Channel
        {
            get; set;
        }

        [JsonProperty("channelId")]
        public string ChannelId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        [JsonProperty("claimType")]
        public string ClaimType
        {
            get; set;
        }

        [JsonProperty("identity")]
        public string Identity
        {
            get => RowKey;
            set => RowKey = value;
        }

        [JsonProperty("loginTime")]
        public DateTime? LoginTime
        {
            get; set;
        }

        [JsonProperty("logoutTime")]
        public DateTime? LogoutTime
        {
            get; set;
        }

        [JsonProperty("protocol")]
        public string Protocol
        {
            get; set;
        }

        [JsonProperty("status")]
        public string Status
        {
            get; set;
        }

        public override string ConvertToCsv()
        {
            return string.Format(
                $"{ChannelId},{Identity},{Channel},{Protocol},{ClaimType},{Status},{LoginTime},{LogoutTime}");
        }

        public override string ConvertToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}