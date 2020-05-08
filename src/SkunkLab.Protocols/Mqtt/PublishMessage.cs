using System;

namespace SkunkLab.Protocols.Mqtt
{
    public class PublishMessage : MqttMessage
    {
        public PublishMessage()
        {
        }

        public PublishMessage(bool dupFlag, QualityOfServiceLevelType qosLevel, bool retainFlag, ushort messageId,
            string topic, byte[] data)
        {
            DupFlag = dupFlag;
            QualityOfServiceLevel = qosLevel;
            RetainFlag = retainFlag;

            MessageId = messageId;
            Topic = topic;
            Payload = data;
        }

        public bool DupFlag
        {
            get => Dup;
            set => Dup = value;
        }

        public override bool HasAck => QualityOfServiceLevel != QualityOfServiceLevelType.AtMostOnce;

        public override MqttMessageType MessageType
        {
            get => MqttMessageType.PUBLISH;
            internal set => base.MessageType = value;
        }

        public QualityOfServiceLevelType QualityOfServiceLevel
        {
            get => QualityOfService;
            set => QualityOfService = value;
        }

        public bool RetainFlag
        {
            get => Retain;
            set => Retain = value;
        }

        public string Topic
        {
            get; set;
        }

        public override byte[] Encode()
        {
            byte qos = (byte)(int)QualityOfServiceLevel;

            byte fixedHeader = (byte)((0x03 << Constants.Header.MessageTypeOffset) |
                                      (byte)(qos << Constants.Header.QosLevelOffset) |
                                      (Dup ? 0x01 << Constants.Header.DupFlagOffset : 0x00) |
                                      (Retain ? 0x01 : 0x00));

            ByteContainer vhContainer = new ByteContainer();
            vhContainer.Add(Topic);

            if (qos > 0)
            {
                byte[] messageId = new byte[2];
                messageId[0] = (byte)((MessageId >> 8) & 0x00FF);
                messageId[1] = (byte)(MessageId & 0x00FF);
                vhContainer.Add(messageId);
            }

            byte[] variableHeaderBytes = vhContainer.ToBytes();

            byte[] lengthSB = new byte[2];
            lengthSB[0] = (byte)((Payload.Length >> 8) & 0x00FF);
            lengthSB[1] = (byte)(Payload.Length & 0x00FF);

            ByteContainer payloadContainer = new ByteContainer();
            payloadContainer.Add(Payload);

            byte[] payloadBytes = payloadContainer.ToBytes();

            int remainingLength = variableHeaderBytes.Length + payloadBytes.Length;
            byte[] remainingLengthBytes = EncodeRemainingLength(remainingLength);

            ByteContainer container = new ByteContainer();
            container.Add(fixedHeader);
            container.Add(remainingLengthBytes);
            container.Add(variableHeaderBytes);
            container.Add(payloadBytes);

            return container.ToBytes();
        }

        internal override MqttMessage Decode(byte[] message)
        {
            MqttMessage publishMessage = new PublishMessage();

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
            Topic = ByteContainer.DecodeString(buffer, index, out int length);
            index += length;

            if (QualityOfServiceLevel > 0)
            {
                ushort messageId = (ushort)((buffer[index++] << 8) & 0xFF00);
                messageId |= buffer[index++];

                MessageId = messageId;
                length += 2;
            }

            byte[] data = new byte[remainingLength - length];
            Buffer.BlockCopy(buffer, index, data, 0, data.Length);

            Payload = data;
            return publishMessage;
        }
    }
}