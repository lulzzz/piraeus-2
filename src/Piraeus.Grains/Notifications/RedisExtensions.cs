using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using StackExchange.Redis;

namespace Piraeus.Grains.Notifications
{
    public static class RedisExtensions
    {
        public static T Get<T>(this IDatabase database, string key)
        {
            byte[] stream = database.StringGet(key);
            return Deserialize<T>(stream);
        }

        public static object Get(this IDatabase database, string key)
        {
            return Deserialize<object>(database.StringGet(key));
        }

        public static void Set(this IDatabase database, string key, object value)
        {
            byte[] serializedValue = Serialize(value);
            database.StringSet(key, serializedValue);
        }

        private static T Deserialize<T>(byte[] stream)
        {
            if (stream == null)
            {
                return default;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using MemoryStream memoryStream = new MemoryStream(stream);
            T result = (T)binaryFormatter.Deserialize(memoryStream);
            return result;
        }

        private static byte[] Serialize(object o)
        {
            if (o == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using MemoryStream memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, o);
            byte[] objectDataAsStream = memoryStream.ToArray();
            return objectDataAsStream;
        }
    }
}