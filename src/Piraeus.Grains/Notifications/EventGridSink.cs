using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Rest;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Mqtt;

namespace Piraeus.Grains.Notifications
{
    public class EventGridSink : EventSink
    {
        private readonly IAuditor auditor;

        private readonly int clientCount;

        private readonly EventGridClient[] clients;

        private readonly string resourceUriString;

        private readonly string topicHostname;

        private readonly string topicKey;

        private readonly Uri uri;

        private int arrayIndex;

        public EventGridSink(SubscriptionMetadata metadata, ILog logger = null)
            : base(metadata, logger)
        {
            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);
            uri = new Uri(metadata.NotifyAddress);
            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
            topicHostname = uri.Authority;
            topicKey = metadata.SymmetricKey;
            string uriString = new Uri(metadata.SubscriptionUriString).ToString();
            resourceUriString = uriString.Replace("/" + uri.Segments[^1], "");
            if (!int.TryParse(nvc["clients"], out clientCount))
            {
                clientCount = 1;
            }

            ServiceClientCredentials credentials = new TopicCredentials(topicKey);

            clients = new EventGridClient[clientCount];
            for (int i = 0; i < clientCount; i++)
                clients[i] = new EventGridClient(credentials);
        }

        public override async Task SendAsync(EventMessage message)
        {
            AuditRecord record = null;
            byte[] payload = null;

            try
            {
                arrayIndex = arrayIndex.RangeIncrement(0, clientCount - 1);
                payload = GetPayload(message);
                if (payload == null)
                {
                    await logger?.LogWarningAsync(
                        $"Subscription '{metadata.SubscriptionUriString}' message not written to event grid sink because message is null.");
                    return;
                }

                EventGridEvent gridEvent = new EventGridEvent(message.MessageId, resourceUriString, payload,
                    resourceUriString, DateTime.UtcNow, "1.0");
                IList<EventGridEvent> events = new List<EventGridEvent>(new[] { gridEvent });
                Task task = clients[arrayIndex].PublishEventsAsync(topicHostname, events);
                Task innerTask =
                    task.ContinueWith(async a => { await FaultTask(message.MessageId, payload, message.Audit); },
                        TaskContinuationOptions.OnlyOnFaulted);
                await Task.WhenAll(task);

                record = new MessageAuditRecord(message.MessageId,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(), "EventGrid",
                    "EventGrid", payload.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex,
                    $"Subscription '{metadata.SubscriptionUriString}' message not written to event grid sink.");
                record = new MessageAuditRecord(message.MessageId,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(), "EventGrid",
                    "EventGrid", payload.Length, MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
            }
            finally
            {
                if (message.Audit && record != null)
                {
                    await auditor?.WriteAuditRecordAsync(record);
                }
            }
        }

        private async Task FaultTask(string id, byte[] payload, bool canAudit)
        {
            AuditRecord record = null;
            try
            {
                ServiceClientCredentials credentials = new TopicCredentials(topicKey);
                EventGridClient client = new EventGridClient(credentials);
                EventGridEvent gridEvent = new EventGridEvent(id, resourceUriString, payload, resourceUriString,
                    DateTime.UtcNow, "1.0");
                IList<EventGridEvent> events = new List<EventGridEvent>(new[] { gridEvent });
                await clients[arrayIndex].PublishEventsAsync(topicHostname, events);
                record = new MessageAuditRecord(id,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(), "EventGrid",
                    "EventGrid", payload.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex,
                    $"Subscription '{metadata.SubscriptionUriString}' message not written to event grid sink in fault task.");
                record = new MessageAuditRecord(id,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(), "EventGrid",
                    "EventGrid", payload.Length, MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
            }
            finally
            {
                if (canAudit && record != null)
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