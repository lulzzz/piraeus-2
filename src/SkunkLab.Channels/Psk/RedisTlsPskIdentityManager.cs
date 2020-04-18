using System;
using System.Text;
using Org.BouncyCastle.Crypto.Tls;
using SkunkLab.Storage;

namespace SkunkLab.Channels.Psk
{
    public class RedisTlsPskIdentityManager : TlsPskIdentityManager
    {
        private readonly RedisPskStorage storage;

        public RedisTlsPskIdentityManager(string connectionString)
        {
            storage = RedisPskStorage.CreateSingleton(connectionString);
        }

        public byte[] GetHint()
        {
            return null;
        }

        public byte[] GetPsk(byte[] identity)
        {
            string key = Encoding.UTF8.GetString(identity);
            string value = storage.GetSecretAsync(key).GetAwaiter().GetResult();
            byte[] psk = Convert.FromBase64String(value);

            return psk;
        }
    }
}