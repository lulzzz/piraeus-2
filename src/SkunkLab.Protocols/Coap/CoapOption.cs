using System;

namespace SkunkLab.Protocols.Coap
{
    public class CoapOption
    {
        private OptionType type;

        public CoapOption()
        {
        }

        public CoapOption(OptionType type, object value)
        {
            this.type = type;
            Value = value;
        }

        public bool CacheKey
        {
            get; internal set;
        }

        public bool Critial
        {
            get; internal set;
        }

        public bool Safe
        {
            get; internal set;
        }

        public OptionType Type
        {
            get => type;
            set
            {
                byte t = (byte)value;
                CacheKey = ((t >> 0x02) & 0x07) == 0x07 ? false : true;
                Safe = ((t >> 0x02) & 0x01) == 0x01 ? false : true;
                Critial = (t & 0x01) == 0x01 ? true : false;
                type = value;
            }
        }

        public object Value
        {
            get; set;
        }

        public Type ValueType
        {
            get
            {
                int typeValue = (int)Type;
                if (typeValue == 1 || typeValue == 4)
                {
                    return typeof(byte[]);
                }

                if (typeValue == 5)
                {
                    return null;
                }

                if (typeValue == 7 || typeValue == 12 || typeValue == 14 || typeValue == 17 || typeValue == 60)
                {
                    return typeof(uint);
                }

                return typeof(string);
            }
        }

        public static CoapOption Decode(byte[] buffer, int index, int previousDelta, out int newIndex)
        {
            int deltaPart = buffer[index] >> 0x04;
            int deltaExtendedLength = deltaPart <= 12 ? 0 : deltaPart == 13 ? 1 : 2;
            int valuePart = buffer[index] & 0x0F;
            int valueExtendedLength = valuePart <= 12 ? 0 : valuePart == 13 ? 1 : 2;

            index++;

            int optionTypeValue = deltaExtendedLength == 0 ? deltaPart + previousDelta :
                deltaExtendedLength == 1 ? deltaPart + previousDelta + buffer[index++] :
                deltaPart + 255 + previousDelta + (buffer[index++] | buffer[index++]);
            OptionType option = (OptionType)optionTypeValue;

            int valueLength = valueExtendedLength == 0 ? valuePart :
                valueExtendedLength == 1 ? valuePart + buffer[index++] :
                valuePart + 255 + (buffer[index++] | buffer[index++]);
            byte[] encodedValue = new byte[valueLength];
            Buffer.BlockCopy(buffer, index, encodedValue, 0, valueLength);

            newIndex = index + valueLength;

            object value = option.DecodeOptionValue(encodedValue);
            return new CoapOption(option, value);
        }

        public byte[] Encode(int previousDelta)
        {
            int delta = (int)Type - previousDelta;
            byte[] encodedValue = Type.EncodeOptionValue(Value);
            int valueLength = encodedValue.Length;

            if (delta > ushort.MaxValue)
            {
                throw new InvalidOperationException("Option delta exceeds max length.");
            }

            if (valueLength > ushort.MaxValue)
            {
                throw new InvalidOperationException("Option value exceeds max length.");
            }

            int dv = delta <= 12 ? delta : delta <= byte.MaxValue - 13 ? 13 : 14;
            int vv = valueLength <= 12 ? valueLength : valueLength <= byte.MaxValue ? 13 : 14;
            byte[] deltaBuffer = new byte[dv > 12 ? dv - 12 : 0];
            byte[] valueBuffer = new byte[vv > 12 ? vv - 12 : 0];

            byte[] buffer = new byte[1 + deltaBuffer.Length + valueBuffer.Length + valueLength];

            int index = 0;
            buffer[index++] = (byte)((byte)(dv << 0x04) | (byte)vv);

            byte[] deltaArray = deltaBuffer.Length == 0 ? null :
                deltaBuffer.Length == 1 ? new[] { (byte)(delta - 13) } :
                new[] { (byte)(((delta - 269) >> 8) & 0x00FF), (byte)((delta - 269) & 0x00FF) };

            if (deltaArray != null)
            {
                Buffer.BlockCopy(deltaArray, 0, buffer, index, deltaArray.Length);
                index += deltaArray.Length;
            }

            byte[] valueArray = valueBuffer.Length == 0 ? null :
                valueBuffer.Length == 1 ? new[] { (byte)(valueLength - 13) } : new[]
                    {(byte)(((valueLength - 269) >> 8) & 0x00FF), (byte)((valueLength - 269) & 0x00FF)};

            if (valueArray != null)
            {
                Buffer.BlockCopy(valueArray, 0, buffer, index, valueArray.Length);
                index += valueArray.Length;
            }

            Buffer.BlockCopy(encodedValue, 0, buffer, index, encodedValue.Length);

            return buffer;
        }
    }
}