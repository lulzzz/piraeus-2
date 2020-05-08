using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Piraeus.Core.Utilities
{
    public class MessageUri : Uri
    {
        private readonly IEnumerable<KeyValuePair<string, string>> items;

        public MessageUri(HttpRequest request)
            : this(HttpUtility.HtmlDecode(request.GetEncodedUrl()))
        {
            if (request.QueryString.HasValue)
            {
                var query = QueryHelpers.ParseQuery(request.QueryString.Value);
                items = query.SelectMany(x => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value))
                    .ToList();
            }

            Read(request);
        }

        public MessageUri(HttpRequestMessage request)
            : this(request.RequestUri.ToString())
        {
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            items = query.SelectMany(x => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value))
                .ToList();
            Read(request);
        }

        public MessageUri(string uriString)
            : base(uriString)
        {
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            NameValueCollection nvc = HttpUtility.ParseQueryString(HttpUtility.UrlDecode(Query));
            for (int i = 0; i < nvc.Count; i++)
            {
                string key = nvc.Keys[i];
                string[] values = nvc.GetValues(i);
                foreach (string val in values)
                    list.Add(new KeyValuePair<string, string>(key, val));
            }

            items = list.ToArray();
            Read();
        }

        public string CacheKey
        {
            get; internal set;
        }

        public string ContentType
        {
            get; internal set;
        }

        public IEnumerable<KeyValuePair<string, string>> Indexes
        {
            get; internal set;
        }

        public string MessageId
        {
            get; internal set;
        }

        public string Resource
        {
            get; internal set;
        }

        public string SecurityToken
        {
            get; internal set;
        }

        public IEnumerable<string> Subscriptions
        {
            get; internal set;
        }

        public string TokenType
        {
            get; internal set;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal KeyValuePair<string, string>[] BuildIndexes(IEnumerable<string> indexes)
        {
            if (indexes == null)
            {
                return null;
            }

            List<KeyValuePair<string, string>> indexList = new List<KeyValuePair<string, string>>();
            foreach (string index in indexes)
            {
                string[] parts = index.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    throw new IndexOutOfRangeException("indexes");
                }

                indexList.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
            }

            return indexList.Count > 0 ? indexList.ToArray() : null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal void CheckUri(IEnumerable<string> uriStrings)
        {
            if (uriStrings == null)
            {
                return;
            }

            foreach (string uriString in uriStrings)
                CheckUri(uriString);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal void CheckUri(string uriString)
        {
            if (string.IsNullOrEmpty(uriString))
            {
                return;
            }

            if (!IsWellFormedUriString(uriString, UriKind.Absolute))
            {
                throw new UriFormatException("uriString");
            }
        }

        private IEnumerable<string> GetEnumerableHeaders(string key, HttpRequestMessage request)
        {
            if (request.Headers.Contains(key))
            {
                return request.Headers.GetValues(key);
            }

            return null;
        }

        private IEnumerable<string> GetEnumerableParameters(string key)
        {
            return from kv in items
                   where kv.Key.ToLower(CultureInfo.InvariantCulture) == key.ToLower(CultureInfo.InvariantCulture)
                   select kv.Value.ToLower(CultureInfo.InvariantCulture);
        }

        private string GetSingleParameter(string key)
        {
            IEnumerable<string> parameters = GetEnumerableParameters(key);

            if (parameters.Count() > 1)
            {
                throw new IndexOutOfRangeException(key);
            }

            return parameters.Count() == 0 ? null : parameters.First();
        }

        private void Read()
        {
            ContentType = GetSingleParameter(QueryStringConstants.CONTENT_TYPE);
            Resource = GetSingleParameter(QueryStringConstants.RESOURCE);
            TokenType = GetSingleParameter(QueryStringConstants.TOKEN_TYPE);
            SecurityToken = GetSingleParameter(QueryStringConstants.SECURITY_TOKEN);
            MessageId = GetSingleParameter(QueryStringConstants.MESSAGE_ID);
            CacheKey = GetSingleParameter(QueryStringConstants.CACHE_KEY);
            Subscriptions = GetEnumerableParameters(QueryStringConstants.SUBSCRIPTION);
            Indexes = BuildIndexes(GetEnumerableParameters(QueryStringConstants.INDEX));
        }

        private void Read(HttpRequest request)
        {
            ContentType ??= request.ContentType?.ToLowerInvariant();
            Resource ??= request.Headers[HttpHeaderConstants.RESOURCE_HEADER];
            if (request.Headers.ContainsKey(HttpHeaderConstants.SUBSCRIBE_HEADER))
            {
                string[] arr = request.Headers[HttpHeaderConstants.SUBSCRIBE_HEADER].ToArray();
                string[] subs = arr[0].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (subs != null && subs.Count() > 0)
                {
                    Subscriptions = new List<string>(subs);
                }
            }

            CacheKey ??= request.Headers[HttpHeaderConstants.CACHE_KEY];
            if (request.Headers.ContainsKey(HttpHeaderConstants.INDEX_HEADER))
            {
                StringValues vals = request.Headers[HttpHeaderConstants.INDEX_HEADER];
                List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

                foreach (string val in vals)
                {
                    string[] parts = val.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    list.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
                }

                Indexes = list;
            }
        }

        private void Read(HttpRequestMessage request)
        {
            ContentType = request.Content.Headers.ContentType != null
                ? request.Content.Headers.ContentType.MediaType
                : "application/octet-stream";
            Subscriptions = GetEnumerableHeaders(HttpHeaderConstants.SUBSCRIBE_HEADER, request);
            CheckUri(Subscriptions);
            IEnumerable<string> resources = GetEnumerableHeaders(HttpHeaderConstants.RESOURCE_HEADER, request);
            CheckUri(resources);
            SetResource(resources);
            IEnumerable<string> indexes = GetEnumerableHeaders(HttpHeaderConstants.INDEX_HEADER, request);
            Indexes = BuildIndexes(indexes);
            IEnumerable<string> messageIds = GetEnumerableHeaders(HttpHeaderConstants.MESSAGEID_HEADER, request);
            IEnumerable<string> cachekeys = GetEnumerableHeaders(HttpHeaderConstants.CACHE_KEY, request);

            if (resources != null && resources.Count() == 1)
            {
                Resource = resources.First();
            }

            if (messageIds != null && messageIds.Count() == 1)
            {
                MessageId = messageIds.First();
            }

            if (cachekeys != null && cachekeys.Count() == 1)
            {
                CacheKey = cachekeys.First();
            }

            string contentType = GetSingleParameter(QueryStringConstants.CONTENT_TYPE);
            string resource = GetSingleParameter(QueryStringConstants.RESOURCE);
            string tokenType = GetSingleParameter(QueryStringConstants.TOKEN_TYPE);
            string securityToken = GetSingleParameter(QueryStringConstants.SECURITY_TOKEN);
            string messageId = GetSingleParameter(QueryStringConstants.MESSAGE_ID);
            IEnumerable<string> subscriptions = GetEnumerableParameters(QueryStringConstants.SUBSCRIPTION);
            KeyValuePair<string, string>[] queryStringIndexes =
                BuildIndexes(GetEnumerableParameters(QueryStringConstants.INDEX));

            Resource ??= resource;
            Indexes ??= queryStringIndexes;
            MessageId ??= messageId;
            Subscriptions ??= subscriptions;
            TokenType ??= tokenType;
            SecurityToken ??= securityToken;
            ContentType ??= contentType;
        }

        private void SetResource(IEnumerable<string> resources)
        {
            if (resources == null)
            {
            }
            else if (resources.Count() > 1)
            {
                throw new IndexOutOfRangeException("Number of resources specified in request header must be 0 or 1.");
            }
            else
            {
                Resource = resources.First();
            }
        }
    }
}