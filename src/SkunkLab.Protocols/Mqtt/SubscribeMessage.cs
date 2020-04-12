namespace SkunkLab.Protocols.Mqtt
{
    using System;
    using System.Collections.Generic;

    public class SubscribeMessage : MqttMessage
    {
        private readonly IDictionary<string, QualityOfServiceLevelType> _topics;

        public SubscribeMessage()
        {
            this._topics = new Dictionary<string, QualityOfServiceLevelType>();
        }

        public SubscribeMessage(ushort messageId, IDictionary<string, QualityOfServiceLevelType> topics)
        {
            this.MessageId = messageId;
            this._topics = topics;
        }

        public bool DupFlag
        {
            get => base.Dup;
            set => base.Dup = value;
        }

        public override bool HasAck => true;

        public override MqttMessageType MessageType
        {
            get => MqttMessageType.SUBSCRIBE;

            internal set => base.MessageType = value;
        }

        public IDictionary<string, QualityOfServiceLevelType> Topics => this._topics;

        public void AddTopic(string topic, QualityOfServiceLevelType qosLevel)
        {
            this._topics.Add(topic, qosLevel);
        }

        public override byte[] Encode()
        {
            byte qos = Convert.ToByte((int)Enum.Parse(typeof(QualityOfServiceLevelType), this.QualityOfService.ToString(), false));

            byte fixedHeader = (0x08 << Constants.Header.MessageTypeOffset) |
                   1 << Constants.Header.QosLevelOffset |
                   0x00 |
                   0x00;

            byte[] messageId = new byte[2];
            messageId[0] = (byte)((this.MessageId >> 8) & 0x00FF);
            messageId[1] = (byte)(this.MessageId & 0x00FF);

            ByteContainer payloadContainer = new ByteContainer();

            IEnumerator<KeyValuePair<string, QualityOfServiceLevelType>> en = this._topics.GetEnumerator();
            while (en.MoveNext())
            {
                string topic = en.Current.Key;
                QualityOfServiceLevelType qosLevel = this._topics[topic];
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
            if (this._topics.ContainsKey(topic))
            {
                this._topics.Remove(topic);
            }
        }

        internal override MqttMessage Decode(byte[] message)
        {
            SubscribeMessage subscribeMessage = new SubscribeMessage();

            int index = 0;
            byte fixedHeader = message[index];
            subscribeMessage.DecodeFixedHeader(fixedHeader);

            int remainingLength = base.DecodeRemainingLength(message);

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
                this._topics.Add(topic, topicQosLevel);
            }

            return subscribeMessage;
        }
    }
}