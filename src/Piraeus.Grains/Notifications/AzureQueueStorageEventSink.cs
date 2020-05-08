using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Mqtt;
using SkunkLab.Storage;

namespace Piraeus.Grains.Notifications
{
    public class AzureQueueStorageSink : EventSink
    {
        private readonly IAuditor auditor;

        private readonly ConcurrentQueue<EventMessage> loadQueue;

        private readonly string queue;

        private readonly QueueStorage storage;

        private readonly TimeSpan? ttl;

        private readonly Uri uri;

        public AzureQueueStorageSink(SubscriptionMetadata metadata, ILog logger = null)
            : base(metadata, logger)
        {
            loadQueue = new ConcurrentQueue<EventMessage>();

            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);
            uri = new Uri(metadata.NotifyAddress);
            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
            queue = nvc["queue"];

            string ttlString = nvc["ttl"];
            if (!string.IsNullOrEmpty(ttlString))
            {
                ttl = TimeSpan.Parse(ttlString);
            }

            Uri.TryCreate(metadata.SymmetricKey, UriKind.Absolute, out Uri sasUri);

            if (sasUri == null)
            {
                storage = QueueStorage.New(
                    $"DefaultEndpointsProtocol=https;AccountName={uri.Authority.Split(new[] { '.' })[0]};AccountKey={metadata.SymmetricKey};",
                    10000, 1000);
            }
            else
            {
                string connectionString = $"BlobEndpoint={queue};SharedAccessSignature={metadata.SymmetricKey}";
                storage = QueueStorage.New(connectionString, 1000, 5120000);
            }
        }

        public override async Task SendAsync(EventMessage message)
        {
            AuditRecord record = null;
            byte[] payload = null;
            EventMessage msg = null;
            loadQueue.Enqueue(message);

            try
            {
                while (!loadQueue.IsEmpty)
                {
                    bool isdequeued = loadQueue.TryDequeue(out msg);
                    if (!isdequeued)
                    {
                        continue;
                    }

                    payload = GetPayload(msg);
                    if (payload == null)
                    {
                        await logger?.LogWarningAsync(
                            $"Subscription '{metadata.SubscriptionUriString}' message not written to queue sink because message is null.");
                        return;
                    }

                    await storage.EnqueueAsync(queue, payload, ttl);

                    if (message.Audit)
                    {
                        record = new MessageAuditRecord(msg.MessageId,
                            uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(),
                            "AzureQueue", "AzureQueue", payload.Length, MessageDirectionType.Out, true,
                            DateTime.UtcNow);
                    }
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex,
                    $"Subscription '{metadata.SubscriptionUriString}' message not written to queue sink.");
                record = new MessageAuditRecord(msg.MessageId,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(), "AzureQueue",
                    "AzureQueue", payload.Length, MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
                throw;
            }
            finally
            {
                if (record != null && msg.Audit)
                {
                    await auditor?.WriteAuditRecordAsync(record);
                }
            }
        }

        private byte[] GetPayload(EventMessage message)
        {
            switch (message.Protocol)
            {
                case ProtocolType.COAP:
                    CoapMessage coap = CoapMessage.DecodeMessage(message.Message);
                    return coap.Payload;

                case ProtocolType.MQTT:
                    MqttMessage mqtt = MqttMessage.DecodeMessage(message.Message);
                    return mqtt.Payload;

                case ProtocolType.REST:
                    return message.Message;

                case ProtocolType.WSN:
                    return message.Message;

                default:
                    return null;
            }
        }
    }
}