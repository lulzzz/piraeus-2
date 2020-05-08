using System;

namespace Piraeus.Core.Messaging
{
    [Serializable]
    public class EventMessage
    {
        public EventMessage()
        {
        }

        public EventMessage(string contentType, Uri resourceUri, ProtocolType protocol, byte[] message)
            : this(Guid.NewGuid().ToString(), contentType, resourceUri.ToString(), protocol, message, DateTime.UtcNow,
                null)
        {
        }

        public EventMessage(string contentType, string resourceUri, ProtocolType protocol, byte[] message)
            : this(Guid.NewGuid().ToString(), contentType, resourceUri, protocol, message, DateTime.UtcNow, null)
        {
        }

        public EventMessage(string contentType, Uri resourceUri, ProtocolType protocol, byte[] message,
            DateTime timestamp, bool audit = false)
            : this(Guid.NewGuid().ToString(), contentType, resourceUri.ToString(), protocol, message, timestamp, null,
                audit)
        {
        }

        public EventMessage(string contentType, string resourceUri, ProtocolType protocol, byte[] message,
            DateTime timestamp, bool audit = false)
            : this(Guid.NewGuid().ToString(), contentType, resourceUri, protocol, message, timestamp, null, audit)
        {
        }

        public EventMessage(string messageId, string contentType, Uri resourceUri, ProtocolType protocol,
            byte[] message, bool audit = false)
            : this(messageId, contentType, resourceUri.ToString(), protocol, message, DateTime.UtcNow, null, audit)
        {
        }

        public EventMessage(string messageId, string contentType, string resourceUri, ProtocolType protocol,
            byte[] message, bool audit = false)
            : this(messageId, contentType, resourceUri, protocol, message, DateTime.UtcNow, null, audit)
        {
        }

        public EventMessage(string messageId, string contentType, Uri resourceUri, ProtocolType protocol,
            byte[] message, DateTime timeStamp, bool audit = false)
            : this(messageId, contentType, resourceUri.ToString(), protocol, message, timeStamp, null, audit)
        {
        }

        public EventMessage(string messageId, string contentType, string resourceUri, ProtocolType protocol,
            byte[] message, DateTime timeStamp, string cacheKey, bool audit = false)
        {
            MessageId = messageId ?? Guid.NewGuid().ToString();
            ContentType = contentType;
            ResourceUri = resourceUri;
            Protocol = protocol;
            Message = message;
            Timestamp = timeStamp;
            Audit = audit;
            CacheKey = cacheKey;
        }

        public bool Audit
        {
            get; set;
        }

        public string CacheKey
        {
            get; set;
        }

        public string ContentType
        {
            get; set;
        }

        public byte[] Message
        {
            get; set;
        }

        public string MessageId
        {
            get; set;
        }

        public ProtocolType Protocol
        {
            get; set;
        }

        public string ResourceUri
        {
            get; set;
        }

        public DateTime Timestamp
        {
            get; set;
        }
    }
}