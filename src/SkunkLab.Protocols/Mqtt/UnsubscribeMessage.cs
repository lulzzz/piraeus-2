namespace SkunkLab.Protocols.Mqtt
{
    using System;
    using System.Collections.Generic;

    public class UnsubscribeMessage : MqttMessage
    {
        public UnsubscribeMessage()
        {
            this.Topics = new List<string>();
        }

        public UnsubscribeMessage(ushort messageId, IEnumerable<string> topics)
        {
            this.MessageId = messageId;
            this.Topics = new List<string>(topics);
        }

        public override bool HasAck => true;

        public List<string> Topics { get; set; }

        public override byte[] Encode()
        {
            byte fixedHeader = (0x0A << Constants.Header.MessageTypeOffset) |
                   0x00 |
                   0x02 |
                   0x00;

            byte[] messageId = new byte[2];
            messageId[0] = (byte)((this.MessageId >> 8) & 0x00FF);
            messageId[1] = (byte)(this.MessageId & 0x00FF);

            ByteContainer topicContainer = new ByteContainer();
            int index = 0;
            while (index < this.Topics.Count)
            {
                topicContainer.Add(this.Topics[index]);
                index++;
            }

            byte[] payload = topicContainer.ToBytes();

            byte[] remainingLengthBytes = base.EncodeRemainingLength(2 + payload.Length);

            ByteContainer container = new ByteContainer();
            container.Add(fixedHeader);
            container.Add(remainingLengthBytes);
            container.Add(messageId);
            container.Add(payload);

            return container.ToBytes();
        }

        internal override MqttMessage Decode(byte[] message)
        {
            UnsubscribeMessage unsubscribeMessage = new UnsubscribeMessage();

            int index = 0;
            byte fixedHeader = message[index];
            base.DecodeFixedHeader(fixedHeader);

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

            this.MessageId = messageId;

            while (index < buffer.Length)
            {
                string topic = ByteContainer.DecodeString(buffer, index, out int length);
                index += length;
                this.Topics.Add(topic);
            }

            return unsubscribeMessage;
        }
    }
}