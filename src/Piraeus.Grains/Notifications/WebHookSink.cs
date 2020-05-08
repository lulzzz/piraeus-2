using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Mqtt;

namespace Piraeus.Grains.Notifications
{
    public class WebHookSink : EventSink
    {
        private readonly string address;

        private readonly IAuditor auditor;

        public WebHookSink(SubscriptionMetadata metadata, ILog logger)
            : base(metadata, logger)
        {
            this.logger = logger;
            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);

            address = new Uri(metadata.NotifyAddress).ToString();
        }

        public override async Task SendAsync(EventMessage message)
        {
            AuditRecord record = null;
            byte[] payload = null;

            try
            {
                payload = GetPayload(message);
                if (payload == null)
                {
                    await logger?.LogWarningAsync(
                        "Subscription {0} could not write to web hook sink because payload was null.");
                    return;
                }

                HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;
                request.ContentType = message.ContentType;
                request.Method = "POST";

                byte[] signature = SignPayload(payload);
                request.Headers.Add("Authorization", $"Bearer {Convert.ToBase64String(signature)}");

                request.ContentLength = payload.Length;
                Stream stream = await request.GetRequestStreamAsync();
                await stream.WriteAsync(payload, 0, payload.Length);

                using HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;

                if (response.StatusCode == HttpStatusCode.Accepted ||
                    response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.NoContent ||
                    response.StatusCode == HttpStatusCode.Created)
                {
                    await logger?.LogInformationAsync(
                        $"Subscription {metadata.SubscriptionUriString} web hook request is success.");
                    record = new MessageAuditRecord(message.MessageId, address, "WebHook", "HTTP", payload.Length,
                        MessageDirectionType.Out, true, DateTime.UtcNow);
                }
                else
                {
                    await logger?.LogWarningAsync(
                        $"Subscription {metadata.SubscriptionUriString} web hook request returned an expected status code {response.StatusCode}");
                    record = new MessageAuditRecord(message.MessageId, address, "WebHook", "HTTP", payload.Length,
                        MessageDirectionType.Out, false, DateTime.UtcNow,
                        $"Rest request returned an expected status code {response.StatusCode}");
                }
            }
            catch (WebException we)
            {
                await logger?.LogErrorAsync(we,
                    $"Subscription {metadata.SubscriptionUriString} web hook request sink.");
                record = new MessageAuditRecord(message.MessageId, address, "WebHook", "HTTP", payload.Length,
                    MessageDirectionType.Out, false, DateTime.UtcNow, we.Message);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, $"Subscription {metadata.SubscriptionUriString} Web hook event sink.");
                record = new MessageAuditRecord(message.MessageId, address, "WebHook", "HTTP", payload.Length,
                    MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
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

        private byte[] SignPayload(byte[] payload)
        {
            byte[] key = Convert.FromBase64String(metadata.SymmetricKey);

            using HMAC hmac = HMAC.Create();
            hmac.Key = key;
            return hmac.ComputeHash(payload);
        }
    }
}