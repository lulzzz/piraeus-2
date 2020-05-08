using System;
using System.Collections.Generic;
using SkunkLab.Protocols;

namespace Piraeus.Clients.Mqtt
{
    public class GenericMqttDispatcher : IMqttDispatch
    {
        private readonly Dictionary<string, Action<string, string, byte[]>> register;

        public GenericMqttDispatcher()
        {
            register = new Dictionary<string, Action<string, string, byte[]>>();
        }

        public void Dispatch(string key, string contentType, byte[] data)
        {
            if (register.ContainsKey(key))
            {
                register[key](key, contentType, data);
            }
        }

        public void Register(string key, Action<string, string, byte[]> action)
        {
            if (!register.ContainsKey(key))
            {
                register.Add(key, action);
            }
        }

        public void Unregister(string key)
        {
            register.Remove(key);
        }
    }
}