using System;
using System.Collections.Generic;
using SkunkLab.Protocols.Utilities;

namespace SkunkLab.Clients.Coap
{
    public class CoapClientRequestRegistry
    {
        private readonly Dictionary<string, Action<string, byte[]>> container;

        public CoapClientRequestRegistry()
        {
            container = new Dictionary<string, Action<string, byte[]>>();
        }

        public void Add(string verb, string resourceUriString, Action<string, byte[]> action)
        {
            Uri uri = new Uri(resourceUriString);
            string key = verb.ToUpperInvariant() + uri.ToCanonicalString(false);

            if (!container.ContainsKey(key))
            {
                container.Add(key, action);
            }
        }

        public void Clear()
        {
            container.Clear();
        }

        public Action<string, byte[]> GetAction(string verb, string resourceUriString)
        {
            Uri uri = new Uri(resourceUriString);
            string key = verb.ToUpperInvariant() + uri.ToCanonicalString(false);

            if (container.ContainsKey(key))
            {
                return container[key];
            }

            return null;
        }

        public void Remove(string verb, string resourceUriString)
        {
            Uri uri = new Uri(resourceUriString);
            string key = verb.ToUpperInvariant() + uri.ToCanonicalString(false);
            container.Remove(key);
        }
    }
}