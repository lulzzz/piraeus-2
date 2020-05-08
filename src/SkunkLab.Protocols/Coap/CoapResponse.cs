using System;
using System.Linq;

namespace SkunkLab.Protocols.Coap
{
    public sealed class CoapResponse : CoapMessage
    {
        public CoapResponse()
        {
        }

        public CoapResponse(ushort messageId, ResponseMessageType type, ResponseCodeType code)
            : this(messageId, type, code, null, null, null)
        {
        }

        public CoapResponse(ushort messageId, ResponseMessageType type, ResponseCodeType code, byte[] token)
            : this(messageId, type, code, token, null, null)
        {
        }

        public CoapResponse(ushort messageId, ResponseMessageType type, ResponseCodeType code, byte[] token,
            MediaType? contentType, byte[] payload)
        {
            MessageId = messageId;
            ResponseType = type;
            ResponseCode = code;
            Code = (CodeType)code;

            if (token != null)
            {
                Token = token;
            }

            if (contentType.HasValue)
            {
                ContentType = contentType;
            }

            Payload = payload;
            options = new CoapOptionCollection();
        }

        public bool Error
        {
            get; internal set;
        }

        public ResponseCodeType ResponseCode
        {
            get; set;
        }

        public ResponseMessageType ResponseType
        {
            get; set;
        }

        public override void Decode(byte[] message)
        {
            int index = 0;
            byte header = message[index++];
            if (header >> 0x06 != 1)
            {
                throw new CoapVersionMismatchException("Coap Version 1 is only supported version for Coap response.");
            }

            ResponseType = (ResponseMessageType)Convert.ToInt32((header >> 0x04) & 0x03);

            TokenLength = Convert.ToByte(header & 0x0F);

            byte code = message[index++];
            ResponseCode = (ResponseCodeType)((code >> 0x05) * 100 + (code & 0x1F));

            MessageId = (ushort)((message[index++] << 0x08) | message[index++]);
            byte[] tokenBytes = new byte[TokenLength];
            Buffer.BlockCopy(message, index, tokenBytes, 0, TokenLength);
            Token = tokenBytes;

            index += TokenLength;
            int previous = 0;
            bool marker = (message[index] & 0xFF) == 0xFF;

            while (!marker)
            {
                int delta = message[index] >> 0x04;
                CoapOption CoapOption = CoapOption.Decode(message, index, previous, out index);
                Options.Add(CoapOption);
                previous += delta;
                marker = (message[index] & 0xFF) == 0xFF;
            }

            if (marker)
            {
                index++;
                Payload = new byte[message.Length - index];
                Buffer.BlockCopy(message, index, Payload, 0, Payload.Length);
            }

            Error = !Options.ContainsContentFormat();

            ReadOptions(this);
        }

        public override byte[] Encode()
        {
            LoadOptions();
            int length = 0;

            byte[] header = new byte[4 + TokenLength];

            int index = 0;

            header[index++] = (byte)((0x01 << 0x06) | (byte)(Convert.ToByte((int)ResponseType) << 0x04) | TokenLength);

            int code = (int)Code;
            header[index++] = code < 10
                ? (byte)code
                : (byte)((byte)(Convert.ToByte(Convert.ToString((int)ResponseCode).Substring(0, 1)) << 0x05) |
                         Convert.ToByte(Convert.ToString((int)ResponseCode).Substring(1, 2)));

            header[index++] = (byte)((MessageId >> 8) & 0x00FF);
            header[index++] = (byte)(MessageId & 0x00FF);

            if (TokenLength > 0)
            {
                Buffer.BlockCopy(Token, 0, header, 4, TokenLength);
            }

            length += header.Length;

            byte[] options = null;

            if (Options.Count > 0)
            {
                OptionBuilder builder = new OptionBuilder(Options.ToArray());
                options = builder.Encode();
                length += options.Length;
            }

            byte[] buffer;
            if (Payload != null)
            {
                length += Payload.Length + 1;
                buffer = new byte[length];
                Buffer.BlockCopy(header, 0, buffer, 0, header.Length);
                if (options != null)
                {
                    Buffer.BlockCopy(options, 0, buffer, header.Length, options.Length);
                    Buffer.BlockCopy(new byte[] { 0xFF }, 0, buffer, header.Length + options.Length, 1);
                    Buffer.BlockCopy(Payload, 0, buffer, header.Length + options.Length + 1, Payload.Length);
                }
                else
                {
                    Buffer.BlockCopy(new byte[] { 0xFF }, 0, buffer, header.Length, 1);
                    Buffer.BlockCopy(Payload, 0, buffer, header.Length + 1, Payload.Length);
                }
            }
            else
            {
                buffer = new byte[length];
                Buffer.BlockCopy(header, 0, buffer, 0, header.Length);
                if (options != null)
                {
                    Buffer.BlockCopy(options, 0, buffer, header.Length, options.Length);
                }
            }

            return buffer;
        }
    }
}