using System;
using System.Collections.Generic;

namespace SkunkLab.Protocols.Mqtt
{
    public class SubscribeMessage : MqttMessage
    {
        public SubscribeMessage()
        {
            Topics = new Dictionary<string, QualityOfServiceLevelType>();
        }

        public SubscribeMessage(ushort messageId, IDictionary<string, QualityOfServiceLevelType> topics)
        {
            MessageId = messageId;
            Topics = topics;
        }

        public bool DupFlag
        {
            get => Dup;
            set => Dup = value;
        }

        public override bool HasAck => true;

        public override MqttMessageType MessageType
        {
            get => MqttMessageType.SUBSCRIBE;

            internal set => base.MessageType = value;
        }

        public IDictionary<string, QualityOfServiceLevelType> Topics
        {
            get;
        }

        public void AddTopic(string topic, QualityOfServiceLevelType qosLevel)
        {
            Topics.Add(topic, qosLevel);
        }

        public override byte[] Encode()
        {
            _ = Convert.ToByte((int)Enum.Parse(typeof(QualityOfServiceLevelType), QualityOfService.ToString(), false));

            byte fixedHeader = (0x08 << Constants.Header.MessageTypeOffset) |
                               (1 << Constants.Header.QosLevelOffset) |
                               0x00 |
                               0x00;

            byte[] messageId = new byte[2];
            messageId[0] = (byte)((MessageId >> 8) & 0x00FF);
            messageId[1] = (byte)(MessageId & 0x00FF);

            ByteContainer payloadContainer = new ByteContainer();

            IEnumerator<KeyValuePair<string, QualityOfServiceLevelType>> en = Topics.GetEnumerator();
            while (en.MoveNext())
            {
                string topic = en.Current.Key;
                QualityOfServiceLevelType qosLevel = Topics[topic];
                payloadContainer.Add(topic);
                byte topicQos = Convert.ToByte((int)qosLevel);
                payloadContainer.Add(topicQos);
            }

            byte[] payload = payloadContainer.ToBytes();

            byte[] remainingLengthBytes = EncodeRemainingLength(payload.Length + 2);

            ByteContainer container = new ByteContainer();
            container.Add(fixedHeader);
            container.Add(remainingLengthBytes);
            container.Add(messageId);
            container.Add(payload);

            return container.ToBytes();
        }

        public void RemoveTopic(string topic)
        {
            if (Topics.ContainsKey(topic))
            {
                Topics.Remove(topic);
            }
        }

        internal override MqttMessage Decode(byte[] message)
        {
            SubscribeMessage subscribeMessage = new SubscribeMessage();

            int index = 0;
            byte fixedHeader = message[index];
            subscribeMessage.DecodeFixedHeader(fixedHeader);

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

            subscribeMessage.MessageId = messageId;

            while (index < buffer.Length)
            {
                string topic = ByteContainer.DecodeString(buffer, index, out int length);
                index += length;
                QualityOfServiceLevelType topicQosLevel = (QualityOfServiceLevelType)buffer[index++];
                Topics.Add(topic, topicQosLevel);
            }

            return subscribeMessage;
        }
    }
}