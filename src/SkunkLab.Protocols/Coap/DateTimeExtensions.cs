using System;
using System.Text;
using System.Xml;

namespace SkunkLab.Protocols.Coap
{
    internal static class DateTimeExtensions
    {
        public static byte[] Convert(this DateTime? expires, string contentType)
        {
            MediaType media = MediaTypeConverter.ConvertToMediaType(contentType);

            switch (media)
            {
                case MediaType.TextPlain:
                    return expires.HasValue ? Encoding.UTF8.GetBytes(expires.Value.ToString()) : null;

                case MediaType.Json:
                    return expires.HasValue
                        ? Encoding.UTF8.GetBytes($"{{\"Expires\":\"{expires.Value}\"}}")
                        : Encoding.UTF8.GetBytes("{\"Expires\":\"\"}");

                case MediaType.Xml:
                    return expires.HasValue
                        ? Encoding.UTF8.GetBytes(
                            $"<Expires>{XmlConvert.ToString(expires.Value, XmlDateTimeSerializationMode.Utc)}</Expires>")
                        : Encoding.UTF8.GetBytes("<Expires/>");

                case MediaType.OctetStream:
                    return expires.HasValue
                        ? Encoding.UTF8.GetBytes($"Expires={expires.Value}")
                        : Encoding.UTF8.GetBytes("Expires=\"\"");

                default:
                    return null;
            }
        }
    }
}