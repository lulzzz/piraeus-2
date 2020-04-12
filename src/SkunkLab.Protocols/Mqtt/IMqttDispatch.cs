using System;

namespace SkunkLab.Protocols
{
    public interface IMqttDispatch
    {
        void Dispatch(string key, string contentType, byte[] data);

        void Register(string key, Action<string, string, byte[]> action);

        void Unregister(string key);
    }
}