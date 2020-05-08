using System;

namespace SkunkLab.Protocols.Utilities
{
    public static class UriExtensions
    {
        public static string ToCanonicalString(this Uri uri, bool trailingWhack, bool removeLastSegment = false)
        {
            string uriString = uri.ToString().ToLowerInvariant();
            string result;

            if (string.IsNullOrEmpty(uri.Query))
            {
                result = GetBase(uriString, uri, trailingWhack);
            }
            else
            {
                result = GetFromQuery(uriString, uri);
            }

            if (!removeLastSegment)
            {
                return result;
            }

            Uri uri2 = new Uri(result);
            return result.Replace("/" + uri2.Segments[^1], "");
        }

        private static string GetBase(string uriString, Uri uri, bool trailingWhack)
        {
            bool isTrailing = uri.Segments[^1] == "/";

            if (trailingWhack)
            {
                return isTrailing ? uriString : uriString + "/";
            }

            return !isTrailing ? uriString : uriString.Remove(uriString.Length - 1, 1);
        }

        private static string GetFromQuery(string uriString, Uri uri)
        {
            string resourceString = uriString.Replace(uri.Query, "");
            return GetBase(resourceString, new Uri(resourceString), false);
        }
    }
}