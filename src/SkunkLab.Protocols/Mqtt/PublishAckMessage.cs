using System;

namespace SkunkLab.Protocols.Mqtt
{
    public class PublishAckMessage : MqttMessage
    {
        public PublishAckMessage()
        {
        }

        public PublishAckMessage(PublishAckType ackType, ushort messageId)
        {
            AckType = ackType;
            MessageId = messageId;
        }

        public PublishAckType AckType
        {
            get; set;
        }

        public override bool HasAck => AckType == PublishAckType.PUBREC || AckType == PublishAckType.PUBREL;

        public override byte[] Encode()
        {
            byte ackType = Convert.ToByte((int)AckType);
            byte reserved = AckType != PublishAckType.PUBREL ? (byte)0x00 : (byte)0x02;
            byte fixedHeader = (byte)((ackType << Constants.Header.MessageTypeOffset) |
                                      0x00 |
                                      reserved |
                                      0x00);
            byte[] remainingLength = EncodeRemainingLength(2);

            byte[] buffer = new byte[4];
            buffer[0] = fixedHeader;
            buffer[1] = remainingLength[0];
            buffer[2] = (byte)((MessageId >> 8) & 0x00FF);
            buffer[3] = (byte)(MessageId & 0x00FF);

            return buffer;
        }

        internal override MqttMessage Decode(byte[] message)
        {
            MqttMessage pubackMessage = new PublishAckMessage();

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

            return pubackMessage;
        }
    }
}