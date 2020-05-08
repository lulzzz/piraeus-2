using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SkunkLab.Storage
{
    public class EnvironmentVariablePskStorage : PskStorageAdapter
    {
        private static EnvironmentVariablePskStorage instance;

        private readonly Dictionary<string, string> container;

        protected EnvironmentVariablePskStorage(string keys, string values)
        {
            string[] keyParts = keys.Split(keys, StringSplitOptions.RemoveEmptyEntries);
            string[] valueParts = values.Split(values, StringSplitOptions.RemoveEmptyEntries);

            if (keyParts.Length != valueParts.Length)
            {
                throw new IndexOutOfRangeException("Number of PSK identities does not match number of keys.");
            }

            container = new Dictionary<string, string>();
            int index = 0;
            while (index < keyParts.Length)
            {
                container.Add(keyParts[index], valueParts[index]);
                index++;
            }
        }

        public static EnvironmentVariablePskStorage CreateSingleton(string keys, string values)
        {
            if (instance == null)
            {
                instance = new EnvironmentVariablePskStorage(keys, values);
            }

            return instance;
        }

        public override async Task<string[]> GetKeys()
        {
            Dictionary<string, string>.KeyCollection coll = container.Keys;
            string[] keys = new string[container.Count];
            coll.CopyTo(keys, 0);
            return await Task.FromResult(keys);
        }

        public override async Task<string> GetSecretAsync(string key)
        {
            string result = null;

            if (container.ContainsKey(key))
            {
                Dictionary<string, string> clone = DeepClone(container);
                result = clone[key];
            }

            return await Task.FromResult(result);
        }

        public override async Task RemoveSecretAsync(string key)
        {
            if (container.ContainsKey(key))
            {
                container.Remove(key);
            }

            await Task.CompletedTask;
        }

        public override async Task SetSecretAsync(string key, string value)
        {
            if (!container.ContainsKey(key))
            {
                container.Add(key, value);
            }

            await Task.CompletedTask;
        }

        private T DeepClone<T>(T obj)
        {
            if (obj == null)
            {
                return default;
            }

            using MemoryStream ms = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);
            ms.Position = 0;

            return (T)formatter.Deserialize(ms);
        }
    }
}