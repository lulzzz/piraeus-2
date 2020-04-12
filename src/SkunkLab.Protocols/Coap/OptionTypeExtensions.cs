namespace SkunkLab.Protocols.Coap
{
    using System;
    using System.Text;

    internal static class OptionTypeExtensions
    {
        public static object DecodeOptionValue(this OptionType type, byte[] value)
        {
            int typeValue = (int)type;

            if (typeValue == 5)
            {
                return null;
            }
            else if (typeValue == 1)
            {
                return value ?? null;
            }
            else if (typeValue == 4)
            {
                return value;
            }
            else if (typeValue == 6 || typeValue == 7 || typeValue == 12 || typeValue == 14 || typeValue == 17 || typeValue == 60)
            {
                if (value.Length == 1)
                {
                    return (uint)value[0];
                }

                if (value.Length == 2)
                {
                    return (uint)value[0] | value[1];
                }
                else
                {
                    return null;
                }
            }
            else if (typeValue == 3 || typeValue == 35 || typeValue == 39)
            {
                return value == null ? null : Encoding.UTF8.GetString(value);
            }
            else if (typeValue == 8 || typeValue == 11 || typeValue == 15 || typeValue == 20)
            {
                return Encoding.UTF8.GetString(value);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static byte[] EncodeOptionValue(this OptionType type, object value)
        {
            int typeValue = (int)type;
            if (typeValue == 5)
            {
                return null;
            }
            else if (typeValue == 6)
            {
                byte[] b = new byte[] { Convert.ToByte(value) };
                return b;
            }
            else if (typeValue == 1)
            {
                return value == null ? null : (byte[])value;
            }
            else if (typeValue == 4)
            {
                return (byte[])value;
            }
            else if (typeValue == 7 || typeValue == 12 || typeValue == 14 || typeValue == 17 || typeValue == 60)
            {
                uint val = (uint)value;
                if (val == 0)
                {
                    if (typeValue == 12)
                    {
                        return new byte[] { (byte)val };
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return new byte[] { (byte)val };
                }
            }
            else if (typeValue == 3 || typeValue == 35 || typeValue == 39)
            {
                return value == null ? null : Encoding.UTF8.GetBytes((string)value);
            }
            else if (typeValue == 8 || typeValue == 11 || typeValue == 15 || typeValue == 20)
            {
                return Encoding.UTF8.GetBytes((string)value);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}