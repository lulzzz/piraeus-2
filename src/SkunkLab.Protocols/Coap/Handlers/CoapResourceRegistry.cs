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

        /// <summary>
        /// Returns and action to execute for a known request.
        /// </summary>
        /// <param name="verb"></param>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Action<string, string, byte[]> GetAction(string verb, string parameter, string value)
        {
            string key = GetKey(verb, parameter, value);
            if (registry.ContainsKey(key))
            {
                return registry[key];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns an action to execute for a response.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Action<string, string, byte[]> GetTokenReference(string token)
        {
            if (tokenReference.ContainsKey(token) && registry.ContainsKey(tokenReference[token]))
            {
                return registry[tokenReference[token]];
            }
            else
            {
                return null;
            }
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

        /// <summary>
        /// Registers an action for received request for a client.
        /// </summary>
        /// <param name="verb"></param>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        /// <param name="action"></param>
        public void Register(string verb, string parameter, string value, Action<string, string, byte[]> action)
        {
            string key = GetKey(verb, parameter, value);
            registry.Add(key, action);
        }

        /// <summary>
        /// Remove a token reference for a request.  Called after the response action is found.
        /// </summary>
        /// <param name="token"></param>
        public void RemoveTokenReference(string token)
        {
            tokenReference.Remove(token);
        }

        /// <summary>
        /// Sets a token for a request to return an action for a response.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="verb"></param>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
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
            return string.Format("{0}-{1}-{2}", verb.ToLowerInvariant(), parameter.ToLowerInvariant(), value.ToLowerInvariant());
        }
    }
}