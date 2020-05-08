using System;

namespace SkunkLab.Protocols.Mqtt
{
    public class UnsubscribeAckMessage : MqttMessage
    {
        public UnsubscribeAckMessage()
        {
        }

        public UnsubscribeAckMessage(ushort messageId)
        {
            MessageId = messageId;
        }

        public override bool HasAck => false;

        public override byte[] Encode()
        {
            byte fixedHeader = (0x0B << Constants.Header.MessageTypeOffset) |
                               0x00 |
                               0x00 |
                               0x00;

            byte[] messageId = new byte[2];
            messageId[0] = (byte)((MessageId >> 8) & 0x00FF);
            messageId[1] = (byte)(MessageId & 0x00FF);

            byte[] remainingLengthBytes = EncodeRemainingLength(2);

            ByteContainer container = new ByteContainer();
            container.Add(fixedHeader);
            container.Add(remainingLengthBytes);
            container.Add(messageId);

            return container.ToBytes();
        }

        internal override MqttMessage Decode(byte[] message)
        {
            UnsubscribeAckMessage unsubackMessage = new UnsubscribeAckMessage();

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

            return unsubackMessage;
        }
    }
}