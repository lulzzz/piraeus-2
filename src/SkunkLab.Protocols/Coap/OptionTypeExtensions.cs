using System;
using System.Text;

namespace SkunkLab.Protocols.Coap
{
    internal static class OptionTypeExtensions
    {
        public static object DecodeOptionValue(this OptionType type, byte[] value)
        {
            int typeValue = (int)type;

            if (typeValue == 5)
            {
                return null;
            }

            if (typeValue == 1)
            {
                return value ?? null;
            }

            if (typeValue == 4)
            {
                return value;
            }

            if (typeValue == 6 || typeValue == 7 || typeValue == 12 || typeValue == 14 || typeValue == 17 ||
                typeValue == 60)
            {
                if (value.Length == 1)
                {
                    return (uint)value[0];
                }

                if (value.Length == 2)
                {
                    return (uint)value[0] | value[1];
                }

                return null;
            }

            if (typeValue == 3 || typeValue == 35 || typeValue == 39)
            {
                return value == null ? null : Encoding.UTF8.GetString(value);
            }

            if (typeValue == 8 || typeValue == 11 || typeValue == 15 || typeValue == 20)
            {
                return Encoding.UTF8.GetString(value);
            }

            throw new InvalidOperationException();
        }

        public static byte[] EncodeOptionValue(this OptionType type, object value)
        {
            int typeValue = (int)type;
            if (typeValue == 5)
            {
                return null;
            }

            if (typeValue == 6)
            {
                byte[] b = { Convert.ToByte(value) };
                return b;
            }

            if (typeValue == 1)
            {
                return value == null ? null : (byte[])value;
            }

            if (typeValue == 4)
            {
                return (byte[])value;
            }

            if (typeValue == 7 || typeValue == 12 || typeValue == 14 || typeValue == 17 || typeValue == 60)
            {
                uint val = (uint)value;
                if (val == 0)
                {
                    if (typeValue == 12)
                    {
                        return new[] { (byte)val };
                    }

                    return null;
                }

                return new[] { (byte)val };
            }

            if (typeValue == 3 || typeValue == 35 || typeValue == 39)
            {
                return value == null ? null : Encoding.UTF8.GetBytes((string)value);
            }

            if (typeValue == 8 || typeValue == 11 || typeValue == 15 || typeValue == 20)
            {
                return Encoding.UTF8.GetBytes((string)value);
            }

            throw new InvalidOperationException();
        }
    }
}