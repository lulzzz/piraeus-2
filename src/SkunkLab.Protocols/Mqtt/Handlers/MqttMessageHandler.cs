using System;
using System.Threading.Tasks;

namespace SkunkLab.Protocols.Mqtt.Handlers
{
    public abstract class MqttMessageHandler
    {
        protected MqttMessageHandler(MqttSession session, MqttMessage message, IMqttDispatch dispatcher = null)
        {
            Session = session;
            Message = message;
            Dispatcher = dispatcher;
        }

        protected IMqttDispatch Dispatcher
        {
            get; set;
        }

        protected MqttMessage Message
        {
            get; set;
        }

        protected MqttSession Session
        {
            get; set;
        }

        public static MqttMessageHandler Create(MqttSession session, MqttMessage message,
            IMqttDispatch dispatcher = null)
        {
            return message.MessageType switch
            {
                MqttMessageType.CONNACK => new MqttConnackHandler(session, message),
                MqttMessageType.CONNECT => new MqttConnectHandler(session, message),
                MqttMessageType.DISCONNECT => new MqttDisconnectHandler(session, message),
                MqttMessageType.PINGREQ => new MqttPingReqHandler(session, message),
                MqttMessageType.PINGRESP => new MqttPingRespHandler(session, message),
                MqttMessageType.PUBACK => new MqttPubAckHandler(session, message),
                MqttMessageType.PUBCOMP => new MqttPubCompHandler(session, message),
                MqttMessageType.PUBLISH => new MqttPublishHandler(session, message, dispatcher),
                MqttMessageType.PUBREC => new MqttPubRecHandler(session, message),
                MqttMessageType.PUBREL => new MqttPubRelHandler(session, message, dispatcher),
                MqttMessageType.SUBACK => new MqttSubAckHandler(session, message),
                MqttMessageType.SUBSCRIBE => new MqttSubscribeHandler(session, message),
                MqttMessageType.UNSUBACK => new MqttUnsubAckHandler(session, message),
                MqttMessageType.UNSUBSCRIBE => new MqttUnsubscribeHandler(session, message),
                _ => throw new InvalidCastException("MqttMessageType")
            };
        }

        public abstract Task<MqttMessage> ProcessAsync();
    }
}