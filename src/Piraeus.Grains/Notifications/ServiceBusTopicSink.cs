using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.ServiceBus;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Mqtt;

namespace Piraeus.Grains.Notifications
{
    public class ServiceBusTopicSink : EventSink
    {
        private readonly IAuditor auditor;

        private readonly string connectionString;

        private readonly string keyName;

        private readonly string topic;

        private readonly Uri uri;

        private TopicClient client;

        public ServiceBusTopicSink(SubscriptionMetadata metadata, ILog logger = null)
            : base(metadata, logger)
        {
            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);
            uri = new Uri(metadata.NotifyAddress);
            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
            keyName = nvc["keyname"];
            topic = nvc["topic"];
            string symmetricKey = metadata.SymmetricKey;
            connectionString =
                $"Endpoint=sb://{uri.Authority}/;SharedAccessKeyName={keyName};SharedAccessKey={symmetricKey}";
        }

        public override async Task SendAsync(EventMessage message)
        {
            AuditRecord record = null;

            try
            {
                byte[] payload = GetPayload(message);
                if (payload == null)
                {
                    Trace.TraceWarning(
                        "Subscription {0} could not write to service bus sink because payload was either null or unknown protocol type.");
                    return;
                }

                if (client == null)
                {
                    client = new TopicClient(connectionString, topic);
                }

                Message brokerMessage = new Message(payload)
                {
                    ContentType = message.ContentType,
                    MessageId = message.MessageId
                };
                await client.SendAsync(brokerMessage);
                record = new MessageAuditRecord(message.MessageId, string.Format("sb://{0}/{1}", uri.Authority, topic),
                    "ServiceBus", "ServiceBus", message.Message.Length, MessageDirectionType.Out, true,
                    DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Service bus failed to send to topic with error {0}", ex.Message);
                record = new MessageAuditRecord(message.MessageId, $"sb://{uri.Authority}/{topic}",
                    "ServiceBus", "ServiceBus", message.Message.Length, MessageDirectionType.Out, false,
                    DateTime.UtcNow, ex.Message);
            }
            finally
            {
                if (message.Audit && record != null)
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