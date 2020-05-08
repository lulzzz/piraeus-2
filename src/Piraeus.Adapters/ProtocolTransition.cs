using System;
using Piraeus.Core.Messaging;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Coap.Handlers;
using SkunkLab.Protocols.Mqtt;
using SkunkLab.Protocols.Mqtt.Handlers;

namespace Piraeus.Adapters
{
    public abstract class ProtocolTransition
    {
        public static bool IsEncryptedChannel
        {
            get; set;
        }

        public static byte[] ConvertToCoap(CoapSession session, EventMessage message, byte[] observableToken = null)
        {
            CoapMessage coapMessage = null;
            CoapToken token = CoapToken.Create();

            ushort id = observableToken == null
                ? session.CoapSender.NewId(token.TokenBytes)
                : session.CoapSender.NewId(observableToken);

            string uriString = CoapUri.Create(session.Config.Authority, message.ResourceUri, IsEncryptedChannel);

            if (message.Protocol == ProtocolType.MQTT)
            {
                MqttMessage msg = MqttMessage.DecodeMessage(message.Message);
                PublishMessage pub = msg as PublishMessage;
                MqttUri uri = new MqttUri(pub.Topic);
                if (observableToken == null)
                {
                    RequestMessageType messageType = msg.QualityOfService == QualityOfServiceLevelType.AtMostOnce
                        ? RequestMessageType.NonConfirmable
                        : RequestMessageType.Confirmable;
                    coapMessage = new CoapRequest(id, messageType, MethodType.POST, new Uri(uriString),
                        MediaTypeConverter.ConvertToMediaType(message.ContentType));
                }
                else
                {
                    coapMessage = new CoapResponse(id, ResponseMessageType.NonConfirmable, ResponseCodeType.Content,
                        observableToken, MediaTypeConverter.ConvertToMediaType(uri.ContentType), msg.Payload);
                }
            }
            else if (message.Protocol == ProtocolType.COAP)
            {
                CoapMessage msg = CoapMessage.DecodeMessage(message.Message);
                if (observableToken == null)
                {
                    coapMessage = new CoapRequest(id,
                        msg.MessageType == CoapMessageType.Confirmable
                            ? RequestMessageType.Confirmable
                            : RequestMessageType.NonConfirmable, MethodType.POST, new Uri(uriString),
                        MediaTypeConverter.ConvertToMediaType(message.ContentType), msg.Payload);
                }
                else
                {
                    coapMessage = new CoapResponse(id, ResponseMessageType.NonConfirmable, ResponseCodeType.Content,
                        observableToken, MediaTypeConverter.ConvertToMediaType(message.ContentType), msg.Payload);
                }
            }
            else
            {
                if (observableToken == null)
                {
                    coapMessage = new CoapRequest(id, RequestMessageType.NonConfirmable, MethodType.POST,
                        new Uri(uriString), MediaTypeConverter.ConvertToMediaType(message.ContentType),
                        message.Message);
                }
                else
                {
                    coapMessage = new CoapResponse(id, ResponseMessageType.NonConfirmable, ResponseCodeType.Content,
                        observableToken, MediaTypeConverter.ConvertToMediaType(message.ContentType), message.Message);
                }
            }

            return coapMessage.Encode();
        }

        public static byte[] ConvertToHttp(EventMessage message)
        {
            if (message.Protocol == ProtocolType.MQTT)
            {
                MqttMessage mqtt = MqttMessage.DecodeMessage(message.Message);
                return mqtt.Payload;
            }

            if (message.Protocol == ProtocolType.COAP)
            {
                CoapMessage coap = CoapMessage.DecodeMessage(message.Message);
                return coap.Payload;
            }

            return message.Message;
        }

        public static byte[] ConvertToMqtt(MqttSession session, EventMessage message)
        {
            if (message.Protocol == ProtocolType.MQTT)
            {
                return MqttConversion(session, message.Message);
            }

            if (message.Protocol == ProtocolType.COAP)
            {
                CoapMessage msg = CoapMessage.DecodeMessage(message.Message);
                CoapUri curi = new CoapUri(msg.ResourceUri.ToString());
                QualityOfServiceLevelType qos = QualityOfServiceLevelType.AtLeastOnce;

                QualityOfServiceLevelType? qosType = session.GetQoS(curi.Resource);
                qos = qosType ?? QualityOfServiceLevelType.AtLeastOnce;

                PublishMessage pub = new PublishMessage(false, qos, false, session.NewId(), curi.Resource, msg.Payload);
                return pub.Encode();
            }

            if (message.Protocol == ProtocolType.REST)
            {
                PublishMessage pubm = new PublishMessage(false, session.GetQoS(message.ResourceUri).Value, false,
                    session.NewId(), message.ResourceUri, message.Message);
                return pubm.Encode();
            }

            return MqttConversion(session, message.Message);
        }

        private static byte[] MqttConversion(MqttSession session, byte[] message)
        {
            PublishMessage msg = MqttMessage.DecodeMessage(message) as PublishMessage;
            MqttUri uri = new MqttUri(msg.Topic);
            QualityOfServiceLevelType? qos = session.GetQoS(uri.Resource);

            PublishMessage pm = new PublishMessage(false, qos ?? QualityOfServiceLevelType.AtMostOnce, false,
                session.NewId(), uri.Resource, msg.Payload);

            if (pm.QualityOfService != QualityOfServiceLevelType.AtMostOnce)
            {
                session.Quarantine(pm, DirectionType.Out);
            }

            return pm.Encode();
        }
    }
}