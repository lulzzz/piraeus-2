using System;
using Newtonsoft.Json;

namespace Piraeus.Auditing
{
    [Serializable]
    [JsonObject]
    public class MessageAuditRecord : AuditRecord
    {
        public MessageAuditRecord()
        {
        }

        public MessageAuditRecord(string messageId, string identity, string channel, string protocol, int length,
            MessageDirectionType direction, bool success, DateTime messageTime, string error = null)
        {
            MessageId = messageId;
            Identity = identity;
            Channel = channel;
            Protocol = protocol;
            Length = length;
            Direction = direction.ToString();
            Error = error;
            Success = success;
            MessageTime = messageTime;
            Key = Guid.NewGuid().ToString();
        }

        [JsonProperty("channel")]
        public string Channel
        {
            get; set;
        }

        [JsonProperty("direction")]
        public string Direction
        {
            get; set;
        }

        [JsonProperty("error")]
        public string Error
        {
            get; set;
        }

        [JsonProperty("identity")]
        public string Identity
        {
            get; set;
        }

        [JsonProperty("key")]
        public string Key
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        [JsonProperty("length")]
        public int Length
        {
            get; set;
        }

        [JsonProperty("messageId")]
        public string MessageId
        {
            get => RowKey;
            set => RowKey = value;
        }

        [JsonProperty("messageTimestamp")]
        public DateTime MessageTime
        {
            get; set;
        }

        [JsonProperty("protocol")]
        public string Protocol
        {
            get; set;
        }

        [JsonProperty("success")]
        public bool Success
        {
            get; set;
        }

        public override string ConvertToCsv()
        {
            return string.Format(
                $"{Key},{MessageId},{Identity},{Direction},{Channel},{Protocol},{Length},{Error},{Success},{Error},{MessageTime}");
        }

        public override string ConvertToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}