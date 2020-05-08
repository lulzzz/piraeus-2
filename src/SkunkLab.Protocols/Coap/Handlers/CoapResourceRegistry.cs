using System;
using System.Collections.Generic;

namespace SkunkLab.Protocols.Coap.Handlers
{
    public class CoapResourceRegistry
    {
        private readonly Dictionary<string, Action<string, string, byte[]>> registry;

        private readonly Dictionary<string, string> tokenReference;

        public CoapResourceRegistry()
        {
            registry = new Dictionary<string, Action<string, string, byte[]>>();
            tokenReference = new Dictionary<string, string>();
        }

        public Action<string, string, byte[]> GetAction(string verb, string parameter, string value)
        {
            string key = GetKey(verb, parameter, value);
            if (registry.ContainsKey(key))
            {
                return registry[key];
            }

            return null;
        }

        public Action<string, string, byte[]> GetTokenReference(string token)
        {
            if (tokenReference.ContainsKey(token) && registry.ContainsKey(tokenReference[token]))
            {
                return registry[tokenReference[token]];
            }

            return null;
        }

        public bool HasParameter(string verb, string parameter, string value)
        {
            string key = GetKey(verb, parameter, value);
            return registry.ContainsKey(key);
        }

        public bool HasTokenReference(string token)
        {
            return tokenReference.ContainsKey(token);
        }

        public void Register(string verb, string parameter, string value, Action<string, string, byte[]> action)
        {
            string key = GetKey(verb, parameter, value);
            registry.Add(key, action);
        }

        public void RemoveTokenReference(string token)
        {
            tokenReference.Remove(token);
        }

        public void SetTokenReference(string token, string verb, string parameter, string value)
        {
            string key = GetKey(verb, parameter, value);
            if (!tokenReference.ContainsKey(token))
            {
                tokenReference.Add(token, key);
            }
        }

        public void Unregistry(string verb, string parameter, string value)
        {
            string key = GetKey(verb, parameter, value);
            registry.Remove(key);
        }

        private string GetKey(string verb, string parameter, string value)
        {
            return string.Format("{0}-{1}-{2}", verb.ToLowerInvariant(), parameter.ToLowerInvariant(),
                value.ToLowerInvariant());
        }
    }
}