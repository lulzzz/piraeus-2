using System;
using System.Collections.Generic;
using System.Linq;

namespace SkunkLab.Protocols.Coap
{
    public class CoapMessage
    {
        internal CoapOptionCollection options;

        protected List<byte[]> eTag;

        protected List<byte[]> ifMatch;

        protected List<string> locationPath;

        protected List<string> locationQuery;

        protected uint maxAge = 60;

        protected byte[] token;

        protected byte tokenLength;

        protected byte version = 1;

        public CoapMessage()
        {
            options = new CoapOptionCollection();
            locationPath = new List<string>();
            locationQuery = new List<string>();
            ifMatch = new List<byte[]>();
            eTag = new List<byte[]>();
        }

        public virtual MediaType? Accept
        {
            get; set;
        }

        public virtual CodeType Code
        {
            get; set;
        }

        public virtual MediaType? ContentType
        {
            get; set;
        }

        public virtual List<byte[]> ETag
        {
            get => eTag;
            internal set => eTag = value;
        }

        public virtual bool HasContentFormat
        {
            get; internal set;
        }

        public virtual List<byte[]> IfMatch => ifMatch;

        public virtual bool IfNoneMatch
        {
            get; set;
        }

        public virtual List<string> LocationPath => locationPath;

        public virtual List<string> LocationQuery => locationQuery;

        public virtual uint MaxAge
        {
            get => maxAge;
            set => maxAge = value;
        }

        public virtual byte[] MessageBytes
        {
            get; internal set;
        }

        public virtual ushort MessageId
        {
            get; set;
        }

        public virtual CoapMessageType MessageType
        {
            get; set;
        }

        public NoResponseType? NoResponse
        {
            get; set;
        }

        public virtual bool? Observe
        {
            get; set;
        }

        public virtual CoapOptionCollection Options => options;

        public virtual byte[] Payload
        {
            get; set;
        }

        public virtual string ProxyScheme
        {
            get; set;
        }

        public virtual string ProxyUri
        {
            get; set;
        }

        public virtual Uri ResourceUri
        {
            get; set;
        }

        public virtual uint Size1
        {
            get; set;
        }

        public virtual byte[] Token
        {
            get => token;
            set
            {
                if (value == null)
                {
                    return;
                }

                TokenLength = (byte)value.Length;
                token = value;
            }
        }

        protected virtual byte TokenLength
        {
            get => tokenLength;
            set
            {
                if (value > 8)
                {
                    throw new IndexOutOfRangeException("Token length is between 0 and 8 inclusive.");
                }

                tokenLength = value;
            }
        }

        public static CoapMessage DecodeMessage(byte[] message)
        {
            CoapMessage CoapMessage = new CoapMessage();
            CoapMessage.Decode(message);
            CoapMessage.MessageBytes = message;
            return CoapMessage;
        }

        public virtual void Decode(byte[] message)
        {
            int index = 0;
            byte header = message[index++];

            if (header >> 0x06 != 1)
            {
                throw new CoapVersionMismatchException("Coap Version 1 is only supported version for Coap response.");
            }

            MessageType = (CoapMessageType)Convert.ToInt32((header >> 0x04) & 0x03);
            TokenLength = (byte)(header & 0x0F);

            byte code = message[index++];
            Code = (CodeType)((code >> 0x05) * 100 + (code & 0x1F));

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
                if (index < message.Length)
                {
                    marker = (message[index] & 0xFF) == 0xFF;
                }
                else
                {
                    break;
                }
            }

            if (marker)
            {
                index++;
                Payload = new byte[message.Length - index];
                Buffer.BlockCopy(message, index, Payload, 0, Payload.Length);
            }

            HasContentFormat = Options.ContainsContentFormat();

            ReadOptions(this);
        }

        public virtual byte[] Encode()
        {
            LoadOptions();
            int length = 0;

            byte[] header = new byte[4 + TokenLength];

            int index = 0;

            header[index++] = (byte)((0x01 << 0x06) | (byte)(Convert.ToByte((int)MessageType) << 0x04) | TokenLength);

            int code = (int)Code;
            header[index++] = code < 10
                ? (byte)code
                : (byte)((byte)(Convert.ToByte(Convert.ToString((int)Code).Substring(0, 1)) << 0x05) |
                         Convert.ToByte(Convert.ToString((int)Code).Substring(1, 2)));
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

        protected static void ReadOptions(CoapMessage message)
        {
            object[] ifmatch = message.Options.GetOptionValues(OptionType.IfMatch);
            object[] etag = message.Options.GetOptionValues(OptionType.ETag);
            object[] locationpath = message.Options.GetOptionValues(OptionType.LocationPath);
            _ = message.Options.GetOptionValues(OptionType.UriPath);
            _ = message.Options.GetOptionValues(OptionType.UriQuery);
            object[] locationquery = message.Options.GetOptionValues(OptionType.LocationQuery);
            object observe = message.Options.GetOptionValue(OptionType.Observe);

            message.ResourceUri = message.Options.GetResourceUri();

            message.ifMatch = ifmatch == null ? new List<byte[]>() : new List<byte[]>(ifmatch as byte[][]);
            message.eTag = etag == null ? new List<byte[]>() : new List<byte[]>(etag as byte[][]);
            message.IfNoneMatch = message.Options.Contains(new CoapOption(OptionType.IfNoneMatch, null));
            message.locationPath =
                locationpath == null ? new List<string>() : new List<string>(locationpath as string[]);

            if (observe != null)
            {
                message.Observe = (uint)observe == 0 ? true : false;
            }

            object contentType = message.Options.GetOptionValue(OptionType.ContentFormat);
            if (contentType != null)
            {
                message.ContentType = (MediaType)Convert.ToInt32(contentType);
            }

            message.MaxAge = message.Options.GetOptionValue(OptionType.MaxAge) != null
                ? (uint)message.Options.GetOptionValue(OptionType.MaxAge)
                : 0;

            object accept = message.Options.GetOptionValue(OptionType.Accept);
            if (accept != null)
            {
                message.Accept = (MediaType)Convert.ToInt32(accept);
            }

            message.locationQuery =
                locationquery == null ? new List<string>() : new List<string>(locationquery as string[]);
            message.ProxyUri = message.Options.GetOptionValue(OptionType.ProxyUri) as string;
            message.ProxyScheme = message.Options.GetOptionValue(OptionType.ProxyScheme) as string;
            message.Size1 = message.Options.GetOptionValue(OptionType.Size1) != null
                ? (uint)message.Options.GetOptionValue(OptionType.Size1)
                : 0;
        }

        protected void LoadOptions()
        {
            void LoadByteArray(OptionType type, byte[] array)
            {
                Options.Add(new CoapOption(type, array));
            }

            void LoadString(OptionType type, string value)
            {
                if (value != null)
                {
                    Options.Add(new CoapOption(type, value));
                }
            }

            void LoadBool(OptionType type, bool value)
            {
                if (value)
                {
                    Options.Add(new CoapOption(type, null));
                }
            }

            void LoadUint(OptionType type, uint value, bool includeZero)
            {
                if (value > 0)
                {
                    Options.Add(new CoapOption(type, value));
                }

                if (value == 0 && includeZero)
                {
                    Options.Add(new CoapOption(type, value));
                }
            }

            Options.Clear();

            if (ResourceUri != null)
            {
                IEnumerable<CoapOption> resourceOptions = ResourceUri.DecomposeCoapUri();
                foreach (CoapOption co in resourceOptions)
                    Options.Add(co);
            }

            if (Observe.HasValue)
            {
                LoadUint(OptionType.Observe, Convert.ToUInt32(!Observe.Value), true);
            }

            if (NoResponse.HasValue)
            {
                LoadUint(OptionType.NoResponse, Convert.ToUInt32(NoResponse.Value), false);
            }

            if (IfMatch != null)
            {
                IfMatch.ForEach(s => LoadByteArray(OptionType.IfMatch, s));
            }

            if (ETag != null)
            {
                ETag.ForEach(s => LoadByteArray(OptionType.ETag, s));
            }

            LoadBool(OptionType.IfNoneMatch, IfNoneMatch);

            LocationPath.ForEach(s => LoadString(OptionType.LocationPath, s));

            if (ContentType.HasValue)
            {
                LoadUint(OptionType.ContentFormat, (uint)ContentType.Value, true);
            }

            LoadUint(OptionType.MaxAge, MaxAge, false);
            if (Accept.HasValue)
            {
                LoadUint(OptionType.Accept, (uint)Accept.Value, false);
            }

            LocationQuery.ForEach(s => LoadString(OptionType.LocationQuery, s));
            LoadString(OptionType.ProxyUri, ProxyUri);
            LoadString(OptionType.ProxyScheme, ProxyScheme);
            LoadUint(OptionType.Size1, Size1, false);
        }
    }
}