using System;
using System.Collections.Generic;

namespace SkunkLab.Protocols.Mqtt
{
    public abstract class MqttMessage
    {
        public abstract bool HasAck
        {
            get;
        }

        public virtual ushort MessageId
        {
            get; set;
        }

        public static MqttMessage DecodeMessage(byte[] message)
        {
            byte fixedHeader = message[0];
            byte msgType = (byte)(fixedHeader >> 0x04);

            MqttMessageType messageType = (MqttMessageType)msgType;
            MqttMessage mqttMessage = messageType switch
            {
                MqttMessageType.CONNECT => new ConnectMessage(),
                MqttMessageType.CONNACK => new ConnectAckMessage(),
                MqttMessageType.PUBLISH => new PublishMessage(),
                MqttMessageType.PUBACK => new PublishAckMessage(),
                MqttMessageType.PUBREC => new PublishAckMessage(),
                MqttMessageType.PUBREL => new PublishAckMessage(),
                MqttMessageType.PUBCOMP => new PublishAckMessage(),
                MqttMessageType.SUBSCRIBE => new SubscribeMessage(),
                MqttMessageType.SUBACK => new SubscriptionAckMessage(),
                MqttMessageType.UNSUBSCRIBE => new UnsubscribeMessage(),
                MqttMessageType.UNSUBACK => new UnsubscribeAckMessage(),
                MqttMessageType.PINGREQ => new PingRequestMessage(),
                MqttMessageType.PINGRESP => new PingResponseMessage(),
                MqttMessageType.DISCONNECT => new DisconnectMessage(),
                _ => throw new InvalidOperationException("Unknown message type.")
            };
            mqttMessage.Decode(message);
            return mqttMessage;
        }

        public abstract byte[] Encode();

        internal abstract MqttMessage Decode(byte[] message);

        internal void DecodeFixedHeader(byte fixedHeader)
        {
            byte msgType = (byte)(fixedHeader >> 0x04);
            byte qosLevel = (byte)((fixedHeader & 0x06) >> 0x01);
            byte dupFlag = (byte)((fixedHeader & 0x08) >> 0x03);
            byte retainFlag = (byte)(fixedHeader & 0x01);

            MessageType = (MqttMessageType)msgType;
            QualityOfService = (QualityOfServiceLevelType)qosLevel;
            Dup = dupFlag == 0 ? false : true;
            Retain = retainFlag == 0 ? false : true;
        }

        internal int DecodeRemainingLength(byte[] buffer)
        {
            int index = 0;
            int multiplier = 1;
            int value = 0;
            index++;
            int digit;
            do
            {
                digit = buffer[index++];
                value += (digit & 127) * multiplier;
                multiplier *= 128;
            } while ((digit & 128) != 0);

            return value;
        }

        internal byte[] EncodeRemainingLength(int remainingLength)
        {
            List<byte> list = new List<byte>();
            do
            {
                int digit = remainingLength % 128;
                remainingLength /= 128;

                if (remainingLength > 0)
                {
                    digit |= 0x80;
                }

                list.Add((byte)digit);
            } while (remainingLength > 0);

            if (list.Count > 4)
            {
                throw new InvalidOperationException("Invalid remaining length;");
            }

            return list.ToArray();
        }

        #region Fixed Header

        public bool Dup
        {
            get; set;
        }

        public virtual MqttMessageType MessageType
        {
            get; internal set;
        }

        public byte[] Payload
        {
            get; set;
        }

        public QualityOfServiceLevelType QualityOfService
        {
            get; set;
        }

        protected bool Retain
        {
            get; set;
        }

        #endregion Fixed Header
    }
}