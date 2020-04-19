using System;

namespace SkunkLab.Protocols.Coap
{
    public static class MediaTypeExtensions
    {
        //public static MediaType ConvertFromContentType(this MediaType mediaType, string contentType)
        //{
        //    return contentType.ToLower(CultureInfo.InvariantCulture) switch {
        //        "text/xml" => MediaType.Xml,
        //        "application/xml" => MediaType.Xml,
        //        "text/plain" => MediaType.TextPlain,
        //        "application/octet-stream" => MediaType.OctetStream,
        //        "text/json" => MediaType.Json,
        //        "application/json" => MediaType.Json,
        //        _ => throw new InvalidCastException("contentType")
        //    };
        //}

        public static string ConvertToContentType(this MediaType mediaType)
        {
            return mediaType switch
            {
                MediaType.Xml => "text/xml",
                MediaType.TextPlain => "text/plain",
                MediaType.OctetStream => "application/octet-stream",
                MediaType.Json => "application/json",
                _ => throw new InvalidCastException("MediaType content")
            };
        }
    }
}