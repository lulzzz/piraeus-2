using System;

namespace SkunkLab.Protocols.Mqtt
{
    public class ConnectAckMessage : MqttMessage
    {
        public ConnectAckMessage()
        {
        }

        public ConnectAckMessage(bool sessionPresent, ConnectAckCode returnCode)
        {
            SessionPresent = sessionPresent;
            ReturnCode = returnCode;
        }

        public override bool HasAck => false;

        public ConnectAckCode ReturnCode
        {
            get; set;
        }

        public bool SessionPresent
        {
            get; set;
        }

        public override byte[] Encode()
        {
            int index = 0;
            byte[] buffer = new byte[4];

            buffer[index++] = (0x02 << Constants.Header.MessageTypeOffset) |
                              0x00 |
                              0x00 |
                              0x00;

            buffer[index++] = 0x02;

            buffer[index++] = SessionPresent ? (byte)0x01 : (byte)0x00;
            buffer[index++] = (byte)(int)ReturnCode;

            return buffer;
        }

        internal override MqttMessage Decode(byte[] message)
        {
            ConnectAckMessage connackMessage = new ConnectAckMessage();

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

            index = 0;
            byte reserved = buffer[index++];

            if (reserved != 0x00)
            {
                SessionPresent = Convert.ToBoolean(reserved);
            }

            byte code = buffer[index++];

            ReturnCode = (ConnectAckCode)code;

            return connackMessage;
        }
    }
}