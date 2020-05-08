using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace SkunkLab.Storage
{
    public class KeyVaultPskStorage : PskStorageAdapter
    {
        internal KeyVaultClient client;

        private static string Authority;

        private static string ClientId;

        private static string ClientSecret;

        private static DateTime expiry;

        private static KeyVaultPskStorage instance;

        protected KeyVaultPskStorage()
        {
        }

        public static KeyVaultPskStorage CreateSingleton(string authority, string clientId, string clientSecret)
        {
            if (instance == null)
            {
                Authority = authority;
                ClientId = clientId;
                ClientSecret = clientSecret;
                instance = new KeyVaultPskStorage
                {
                    client = new KeyVaultClient(GetAccessToken)
                };
            }

            return instance;
        }

        public override async Task<string[]> GetKeys()
        {
            return await Task.FromResult<string[]>(null);
        }

        public override async Task<string> GetSecretAsync(string secretIdentifier)
        {
            if (DateTime.Now > expiry)
            {
                client = new KeyVaultClient(GetAccessToken);
            }

            SecretBundle sec = await client.GetSecretAsync(secretIdentifier);
            return sec.Value;
        }

        public override async Task RemoveSecretAsync(string key)
        {
            if (DateTime.Now > expiry)
            {
                client = new KeyVaultClient(GetAccessToken);
            }

            await client.DeleteKeyAsync(string.Format("https://{0}.vault.azure.net:443/", Authority), key);
        }

        public override async Task SetSecretAsync(string secretName, string value)
        {
            if (DateTime.Now > expiry)
            {
                client = new KeyVaultClient(GetAccessToken);
            }

            SecretBundle bundle =
                await client.SetSecretAsync(string.Format("https://{0}.vault.azure.net:443/", Authority), secretName,
                    value);
        }

        internal static async Task<string> GetAccessToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(ClientId, ClientSecret);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            expiry = result.ExpiresOn.DateTime;
            return result.AccessToken;
        }
    }
}