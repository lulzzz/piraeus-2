using System;
using System.Text;
using Org.BouncyCastle.Crypto.Tls;
using SkunkLab.Storage;

namespace SkunkLab.Channels.Psk
{
    public class KeyVaultTlsPskIdentityManager : TlsPskIdentityManager
    {
        private readonly KeyVaultPskStorage storage;

        public KeyVaultTlsPskIdentityManager(string authority, string clientId, string clientSecret)
        {
            storage = KeyVaultPskStorage.CreateSingleton(authority, clientId, clientSecret);
        }

        public byte[] GetHint()
        {
            return null;
        }

        public byte[] GetPsk(byte[] identity)
        {
            string key = Encoding.UTF8.GetString(identity);
            string value = storage.GetSecretAsync(key).GetAwaiter().GetResult();
            return Convert.FromBase64String(value);
        }
    }
}