using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Mqtt;
using Message = Microsoft.Azure.Devices.Message;

namespace Piraeus.Grains.Notifications
{
    public class IoTHubSink : EventSink
    {
        private readonly IAuditor auditor;

        private readonly DeviceClient deviceClient;

        private readonly string deviceId;

        private readonly string methodName;

        private readonly string propertyName;

        private readonly string propertyValue;

        private readonly ServiceClient serviceClient;

        private readonly Uri uri;

        public IoTHubSink(SubscriptionMetadata metadata, ILog logger = null)
            : base(metadata, logger)
        {
            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);
            uri = new Uri(metadata.NotifyAddress);
            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
            string keyName = nvc["keyname"];
            deviceId = nvc["deviceid"];
            methodName = nvc["method"];
            propertyName = nvc["propname"];
            propertyValue = nvc["propvalue"];

            if (string.IsNullOrEmpty(methodName))
            {
                deviceClient = DeviceClient.CreateFromConnectionString(string.Format(
                    "HostName={0};DeviceId={1};SharedAccessKey={2}", uri.Authority, deviceId, metadata.SymmetricKey));
            }
            else
            {
                serviceClient = ServiceClient.CreateFromConnectionString(string.Format(
                    "HostName={0};SharedAccessKeyName={1};SharedAccessKey={2}", uri.Authority, keyName,
                    metadata.SymmetricKey));
            }
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
                        $"Subscription '{metadata.SubscriptionUriString}' message not written to iot hub sink because message is null.");
                    return;
                }

                if (serviceClient != null)
                {
                    if (!string.IsNullOrEmpty(methodName))
                    {
                        if (message.ContentType == "application/json")
                        {
                            CloudToDeviceMethod method = new CloudToDeviceMethod(methodName);
                            method.SetPayloadJson(Encoding.UTF8.GetString(payload));
                            await serviceClient.InvokeDeviceMethodAsync(deviceId, method);
                            record = new MessageAuditRecord(message.MessageId,
                                $"iothub://{uri.Authority}", "IoTHub", "IoTHub", payload.Length,
                                MessageDirectionType.Out, true, DateTime.UtcNow);
                        }
                        else
                        {
                            await logger?.LogWarningAsync(
                                $"Subscription '{metadata.SubscriptionUriString}' cannot send IoTHub direct method sink because content-type is not JSON.");
                            record = new MessageAuditRecord(message.MessageId,
                                string.Format("iothub://{0}", uri.Authority), "IoTHub", "IoTHub", payload.Length,
                                MessageDirectionType.Out, false, DateTime.UtcNow,
                                string.Format(
                                    "Cannot send IoTHub device {0} direct message because content-type is not JSON.",
                                    deviceId));
                        }
                    }
                    else
                    {
                        Message serviceMessage = new Message(payload)
                        {
                            ContentType = message.ContentType,
                            MessageId = message.MessageId
                        };

                        if (!string.IsNullOrEmpty(propertyName))
                        {
                            serviceMessage.Properties.Add(propertyName, propertyValue);
                        }

                        await serviceClient.SendAsync(deviceId, serviceMessage);
                        record = new MessageAuditRecord(message.MessageId, string.Format("iothub://{0}", uri.Authority),
                            "IoTHub", "IoTHub", payload.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
                    }
                }
                else if (deviceClient != null)
                {
                    Microsoft.Azure.Devices.Client.Message msg = new Microsoft.Azure.Devices.Client.Message(payload)
                    {
                        ContentType = message.ContentType,
                        MessageId = message.MessageId
                    };

                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        msg.Properties.Add(propertyName, propertyValue);
                    }

                    await deviceClient.SendEventAsync(msg);
                    record = new MessageAuditRecord(message.MessageId, $"iothub://{uri.Authority}",
                        "IoTHub", "IoTHub", payload.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
                }
                else
                {
                    await logger?.LogWarningAsync(
                        $"Subscription '{metadata.SubscriptionUriString}' IoTHub sink has neither service or device client.");

                    record = new MessageAuditRecord(message.MessageId, $"iothub://{uri.Authority}",
                        "IoTHub", "IoTHub", payload.Length, MessageDirectionType.Out, false, DateTime.UtcNow,
                        "IoTHub subscription has neither service or device client");
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex,
                    $"Subscription '{metadata.SubscriptionUriString}' message not written to IoTHub sink.");
                record = new MessageAuditRecord(message.MessageId, $"iothub://{uri.Authority}",
                    "IoTHub", "IoTHub", payload.Length, MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
            }
            finally
            {
                if (record != null && message.Audit)
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