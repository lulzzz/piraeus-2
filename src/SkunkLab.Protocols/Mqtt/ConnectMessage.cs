using System;
using System.Diagnostics;
using System.Text;

namespace SkunkLab.Protocols.Mqtt
{
    public class ConnectMessage : MqttMessage
    {
        private byte connectFlags;

        private bool passwordFlag;

        private bool usernameFlag;

        private byte willQoS;

        public ConnectMessage()
        {
        }

        public ConnectMessage(string clientId, bool cleanSession)
        {
            ClientId = clientId;
            CleanSession = cleanSession;
        }

        public ConnectMessage(string clientId, int keepAliveSeconds, bool cleanSession)
        {
            ClientId = clientId;
            KeepAlive = keepAliveSeconds;
            CleanSession = cleanSession;
        }

        public ConnectMessage(string clientId, string username, string password, int keepAliveSeconds,
            bool cleanSession)
        {
            ClientId = clientId;
            Username = username;
            Password = password;
            KeepAlive = keepAliveSeconds;
            CleanSession = cleanSession;
        }

        public ConnectMessage(string clientId,
            QualityOfServiceLevelType willQualityOfServiceLevel,
            string willTopic, string willMessage, bool willRetain, bool cleanSession)
            : this(clientId, null, null, 0, willQualityOfServiceLevel, willTopic, willMessage, willRetain, cleanSession)
        {
        }

        public ConnectMessage(string clientId, int keepAliveSeconds,
            QualityOfServiceLevelType willQualityOfServiceLevel,
            string willTopic, string willMessage, bool willRetain, bool cleanSession)
            : this(clientId, null, null, keepAliveSeconds, willQualityOfServiceLevel, willTopic, willMessage,
                willRetain, cleanSession)
        {
        }

        public ConnectMessage(string clientId, string username, string password, int keepAliveSeconds,
            QualityOfServiceLevelType willQualityOfServiceLevel,
            string willTopic, string willMessage, bool willRetain, bool cleanSession)
        {
            ClientId = clientId;
            Username = username;
            Password = password;
            KeepAlive = keepAliveSeconds;
            WillFlag = true;
            WillQualityOfServiceLevel = willQualityOfServiceLevel;
            WillTopic = willTopic;
            WillMessage = willMessage;
            WillRetain = willRetain;
            CleanSession = cleanSession;
        }

        public bool CleanSession
        {
            get; internal set;
        }

        public string ClientId
        {
            get; internal set;
        }

        public override bool HasAck => true;

        public int KeepAlive
        {
            get; internal set;
        }

        public override MqttMessageType MessageType
        {
            get => MqttMessageType.CONNECT;

            internal set => base.MessageType = value;
        }

        public string Password
        {
            get; internal set;
        }

        public string ProtocolName { get; set; } = "MQTT";

        public int ProtocolVersion { get; set; } = 0x04;

        public string Username
        {
            get; internal set;
        }

        public bool WillFlag
        {
            get; internal set;
        }

        public string WillMessage
        {
            get; internal set;
        }

        public QualityOfServiceLevelType? WillQualityOfServiceLevel
        {
            get; internal set;
        }

        public bool WillRetain
        {
            get; internal set;
        }

        public string WillTopic
        {
            get; internal set;
        }

        public override byte[] Encode()
        {
            byte fixedHeader = (0x01 << Constants.Header.MessageTypeOffset) |
                               (0x00 << Constants.Header.QosLevelOffset) |
                               (0x00 << Constants.Header.DupFlagOffset) |
                               0x00;

            SetConnectFlags();
            ByteContainer vhContainer = new ByteContainer();
            vhContainer.Add(ProtocolName);
            vhContainer.Add((byte)ProtocolVersion);
            vhContainer.Add(connectFlags);

            byte[] keepAlive = new byte[2];
            keepAlive[0] = (byte)((KeepAlive >> 8) & 0x00FF);
            keepAlive[1] = (byte)(KeepAlive & 0x00FF);

            vhContainer.Add(keepAlive);

            byte[] variableHeaderBytes = vhContainer.ToBytes();

            ByteContainer payloadContainer = new ByteContainer();
            payloadContainer.Add(ClientId);
            payloadContainer.Add(WillTopic);
            payloadContainer.Add(WillMessage);
            payloadContainer.Add(Username);
            payloadContainer.Add(Password);

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
            MqttMessage connectMessage = new ConnectMessage();

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

            int protocolNameLength = (buffer[index++] << 8) & 0xFF00;
            protocolNameLength |= buffer[index++];

            byte[] protocolName = new byte[protocolNameLength];
            try
            {
                Buffer.BlockCopy(buffer, index, protocolName, 0, protocolNameLength);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }

            ProtocolName = Encoding.UTF8.GetString(protocolName);

            index += protocolNameLength;
            ProtocolVersion = buffer[index++];
            byte connectFlags = buffer[index++];
            usernameFlag = connectFlags >> 0x07 == 0x01 ? true : false;
            passwordFlag = (connectFlags & 0x64) >> 0x06 == 0x01 ? true : false;
            WillRetain = (connectFlags & 0x032) >> 0x05 == 0x01 ? true : false;

            WillQualityOfServiceLevel =
                (QualityOfServiceLevelType)Convert.ToByte(((connectFlags & 0x1F) >> 0x03) |
                                                          (connectFlags & (0x08 >> 0x03)));
            WillFlag = (connectFlags & 0x04) >> 0x02 == 0x01 ? true : false;
            CleanSession = (connectFlags & 0x02) >> 0x01 == 0x01 ? true : false;

            int keepAliveSec = (buffer[index++] << 8) & 0xFF00;
            keepAliveSec |= buffer[index++];

            KeepAlive = keepAliveSec;

            ClientId = ByteContainer.DecodeString(buffer, index, out int length);
            index += length;

            if (WillFlag)
            {
                WillTopic = ByteContainer.DecodeString(buffer, index, out length);
                index += length;
                WillMessage = ByteContainer.DecodeString(buffer, index, out length);
                index += length;
            }

            if (usernameFlag)
            {
                Username = ByteContainer.DecodeString(buffer, index, out length);
                index += length;
            }

            if (passwordFlag)
            {
                Password = ByteContainer.DecodeString(buffer, index, out _);
            }

            return connectMessage;
        }

        private void SetConnectFlags()
        {
            usernameFlag = !string.IsNullOrEmpty(Username);
            passwordFlag = !string.IsNullOrEmpty(Password);

            if (passwordFlag && !usernameFlag)
            {
            }

            if (WillFlag && (string.IsNullOrEmpty(WillTopic) || string.IsNullOrEmpty(WillMessage) ||
                             !WillQualityOfServiceLevel.HasValue))
            {
            }

            willQoS = 0x00;
            if (WillQualityOfServiceLevel.HasValue)
            {
                willQoS = (byte)(int)WillQualityOfServiceLevel;
            }

            connectFlags = 0x00;

            connectFlags |= usernameFlag ? (byte)(0x01 << 0x07) : (byte)0x00;
            connectFlags |= passwordFlag ? (byte)(0x01 << 0x06) : (byte)0x00;
            connectFlags |= WillRetain ? (byte)(0x01 << 5) : (byte)0x00;
            connectFlags |= (byte)(willQoS << 0x03);
            connectFlags |= WillFlag ? (byte)(0x01 << 0x02) : (byte)0x00;
            connectFlags |= CleanSession ? (byte)(0x01 << 0x01) : (byte)0x00;
        }
    }
}