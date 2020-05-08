namespace SkunkLab.Protocols.Mqtt
{
    public class DisconnectMessage : MqttMessage
    {
        public override bool HasAck => false;

        public override byte[] Encode()
        {
            int index = 0;
            byte[] buffer = new byte[2];

            buffer[index++] = (0x0E << Constants.Header.MessageTypeOffset) |
                              0x00 |
                              0x00 |
                              0x00;

            buffer[index] = 0x00;

            return buffer;
        }

        internal override MqttMessage Decode(byte[] message)
        {
            DisconnectMessage disconnect = new DisconnectMessage();
            int index = 0;
            byte fixedHeader = message[index];
            DecodeFixedHeader(fixedHeader);

            int remainingLength = DecodeRemainingLength(message);

            if (remainingLength != 0)
            {
            }

            return disconnect;
        }
    }
}