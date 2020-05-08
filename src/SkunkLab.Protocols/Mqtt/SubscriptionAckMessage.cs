using System;
using System.Collections.Generic;

namespace SkunkLab.Protocols.Mqtt
{
    public class SubscriptionAckMessage : MqttMessage
    {
        public SubscriptionAckMessage()
        {
            QualityOfServiceLevels = new QualityOfServiceLevelCollection();
        }

        public SubscriptionAckMessage(ushort messageId, IEnumerable<QualityOfServiceLevelType> qosLevels)
        {
            MessageId = messageId;
            QualityOfServiceLevels = new QualityOfServiceLevelCollection(qosLevels);
        }

        public override bool HasAck => false;

        public override MqttMessageType MessageType
        {
            get => MqttMessageType.SUBACK;

            internal set => base.MessageType = value;
        }

        public QualityOfServiceLevelCollection QualityOfServiceLevels
        {
            get;
        }

        public override byte[] Encode()
        {
            byte fixedHeader = (0x09 << Constants.Header.MessageTypeOffset) |
                               0x00 |
                               0x00 |
                               0x00;

            byte[] messageId = new byte[2];
            messageId[0] = (byte)((MessageId >> 8) & 0x00FF);
            messageId[1] = (byte)(MessageId & 0x00FF);

            ByteContainer qosContainer = new ByteContainer();
            int index = 0;
            while (index < QualityOfServiceLevels.Count)
            {
                byte qos = (byte)(int)QualityOfServiceLevels[index];
                qosContainer.Add(qos);
                index++;
            }

            byte[] payload = qosContainer.ToBytes();

            byte[] remainingLengthBytes = EncodeRemainingLength(2 + payload.Length);

            ByteContainer container = new ByteContainer();
            container.Add(fixedHeader);
            container.Add(remainingLengthBytes);
            container.Add(messageId);
            container.Add(payload);

            return container.ToBytes();
        }

        internal override MqttMessage Decode(byte[] message)
        {
            SubscriptionAckMessage subackMessage = new SubscriptionAckMessage();

            int index = 0;
            byte fixedHeader = message[index];
            DecodeFixedHeader(fixedHeader);

            int remainingLength = DecodeRemainingLength(message);

            int temp = remainingLength;
            do
            {
                index++;
                temp /= 128;
            } while (temp > 0);

            index++;

            byte[] buffer = new byte[remainingLength];
            Buffer.BlockCopy(message, index, buffer, 0, buffer.Length);

            ushort messageId = (ushort)((buffer[0] << 8) & 0xFF00);
            messageId |= buffer[1];

            MessageId = messageId;

            while (index < buffer.Length)
            {
                QualityOfServiceLevelType qosLevel = (QualityOfServiceLevelType)buffer[index++];
                QualityOfServiceLevels.Add(qosLevel);
            }

            return subackMessage;
        }
    }
}