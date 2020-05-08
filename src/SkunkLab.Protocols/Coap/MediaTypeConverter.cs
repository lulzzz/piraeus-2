using System;
using System.Globalization;

namespace SkunkLab.Protocols.Coap
{
    public static class MediaTypeConverter
    {
        public static string ConvertFromMediaType(MediaType? mediaType)
        {
            if (!mediaType.HasValue)
            {
                return "application/octet-stream";
            }

            if (mediaType == MediaType.TextPlain)
            {
                return "text/plain";
            }

            if (mediaType == MediaType.Json)
            {
                return "application/json";
            }

            if (mediaType == MediaType.Xml)
            {
                return "application/xml";
            }

            if (mediaType == MediaType.OctetStream)
            {
                return "application/octet-stream";
            }

            throw new UnsupportedMediaTypeException(string.Format("Media type of '{0}' is not supported.",
                mediaType.ToString()));
        }

        public static MediaType ConvertToMediaType(string contentType)
        {
            _ = contentType ?? throw new ArgumentNullException(nameof(contentType));

            string lower = contentType.ToLower(CultureInfo.InvariantCulture);

            if (lower == "text/plain")
            {
                return MediaType.TextPlain;
            }

            if (lower == "application/json" || lower == "text/json")
            {
                return MediaType.Json;
            }

            if (lower == "application/xml" || lower == "text/xml")
            {
                return MediaType.Xml;
            }

            if (lower == "application/octet-stream")
            {
                return MediaType.OctetStream;
            }

            throw new UnsupportedMediaTypeException(string.Format("Content-Type of '{0}' is not supported.",
                contentType));
        }
    }
}