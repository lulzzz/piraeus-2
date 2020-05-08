using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace Piraeus.Configuration
{
    [Serializable]
    [JsonObject]
    public class PiraeusConfig
    {
        private List<KeyValuePair<string, string>> clientIndexes;

        public X509Certificate2 GetClientCertificate()
        {
            string filename = ClientCertificateFilename ?? null;
            if (filename != null)
            {
                return new X509Certificate2(File.ReadAllBytes(filename));
            }

            string store = ClientCertificateStore ?? null;
            string location = ClientCertificateLocation ?? null;
            string thumbprint = ClientCertificateThumbprint ?? null;

            if (store != null && location != null && thumbprint != null)
            {
                return GetCertificateFromStore(store, location, thumbprint);
            }

            return null;
        }

        public List<KeyValuePair<string, string>> GetClientIndexes()
        {
            if (clientIndexes == null)
            {
                if (ClientIdentityClaimTypes == null || ClientIdentityClaimKeys == null)
                {
                    return null;
                }

                string[] claimTypes = ClientIdentityClaimTypes.Split(";", StringSplitOptions.RemoveEmptyEntries);
                string[] claimKeys = ClientIdentityClaimKeys.Split(";", StringSplitOptions.RemoveEmptyEntries);

                if (claimTypes == null && claimKeys == null)
                {
                    return null;
                }

                if (claimTypes != null && claimKeys != null && claimTypes.Length == claimKeys.Length)
                {
                    clientIndexes = new List<KeyValuePair<string, string>>();
                    for (int index = 0; index < claimTypes.Length; index++)
                        clientIndexes.Add(new KeyValuePair<string, string>(claimTypes[index], claimKeys[index]));
                }
                else
                {
                    throw new IndexOutOfRangeException("Client claim types and values for indexing out of range.");
                }
            }

            return clientIndexes;
        }

        public List<KeyValuePair<string, string>> GetClientIndexes(IEnumerable<Claim> claims)
        {
            List<KeyValuePair<string, string>> container = new List<KeyValuePair<string, string>>();

            List<KeyValuePair<string, string>> clientIndexes = GetClientIndexes();
            if (clientIndexes == null)
            {
                return null;
            }

            foreach (Claim claim in claims)
            {
                var query = clientIndexes.Where(c => c.Key == claim.Type.ToLowerInvariant());
                foreach (KeyValuePair<string, string> kvp in query)
                    container.Add(new KeyValuePair<string, string>(kvp.Value, claim.Value));
            }

            if (container.Count > 0)
            {
                return container;
            }

            return null;
        }

        public LoggerType GetLoggerTypes()
        {
            if (string.IsNullOrEmpty(LoggerTypes))
            {
                return default;
            }

            string loggerTypes = LoggerTypes.Replace(";", ",");
            return Enum.Parse<LoggerType>(loggerTypes, true);
        }

        public int[] GetPorts()
        {
            string[] parts = Ports.Split(";", StringSplitOptions.RemoveEmptyEntries);
            return parts != null ? Array.ConvertAll(parts, s => int.Parse(s)) : null;
        }

        public string[] GetSecurityCodes()
        {
            string code = ManagementApiSecurityCodes;
            string[] result;
            if (code.Contains(";"))
            {
                result = code.Split(";", StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                result = new string[1];
                result[0] = code;
            }

            return result;
        }

        public X509Certificate2 GetServerCerticate()
        {
            string filename = ServerCertificateFilename ?? null;
            if (!string.IsNullOrEmpty(filename))
            {
                return new X509Certificate2(filename, ServerCertificatePassword);
            }

            string store = ServerCertificateStore ?? null;
            string location = ServerCertificateLocation ?? null;
            string thumbprint = ServerCertificateThumbprint ?? null;

            if (!string.IsNullOrEmpty(store) && !string.IsNullOrEmpty(location) && !string.IsNullOrEmpty(thumbprint))
            {
                return GetCertificateFromStore(store, location, thumbprint);
            }

            return null;
        }

        public IEnumerable<Claim> GetServiceClaims()
        {
            string[] claimTypes = ServiceIdentityClaimTypes.Split(";", StringSplitOptions.RemoveEmptyEntries);
            string[] claimValues = ServiceIdentityClaimValues.Split(";", StringSplitOptions.RemoveEmptyEntries);

            if (claimTypes == null && claimValues == null)
            {
                return null;
            }

            if (claimTypes != null && claimValues != null && claimTypes.Length == claimTypes.Length)
            {
                List<Claim> list = new List<Claim>();
                for (int index = 0; index < claimTypes.Length; index++)
                    list.Add(new Claim(claimTypes[index], claimValues[index]));

                return list;
            }

            throw new IndexOutOfRangeException("Service claim types and values out of range.");
        }

        private X509Certificate2 GetCertificateFromStore(string store, string location, string thumbprint)
        {
            StoreName storeName = Enum.Parse<StoreName>(store, true);
            StoreLocation storeLocation = Enum.Parse<StoreLocation>(location, true);
            string thumb = thumbprint.ToUpperInvariant();

            X509Store x509Store = new X509Store(storeName, storeLocation);

            try
            {
                x509Store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection collection = x509Store.Certificates;
                foreach (var item in collection)
                {
                    if (item.Thumbprint == thumb)
                    {
                        return item;
                    }
                }

                return null;
            }
            finally
            {
                x509Store.Close();
            }
        }

        #region Client Certificate (Optional)

        [JsonProperty("clientCertificateFilename")]
        public string ClientCertificateFilename
        {
            get; set;
        }

        [JsonProperty("clientCertificateLocation")]
        public string ClientCertificateLocation
        {
            get; set;
        }

        [JsonProperty("clientCertificateStore")]
        public string ClientCertificateStore
        {
            get; set;
        }

        [JsonProperty("clientCertificateThumbprint")]
        public string ClientCertificateThumbprint
        {
            get; set;
        }

        #endregion Client Certificate (Optional)

        #region Service Certificate (Optional)

        [JsonProperty("serverCertificateFilename")]
        public string ServerCertificateFilename
        {
            get; set;
        }

        [JsonProperty("serverCertificateLocation")]
        public string ServerCertificateLocation
        {
            get; set;
        }

        [JsonProperty("serverCertificatePassword")]
        public string ServerCertificatePassword
        {
            get; set;
        }

        [JsonProperty("serverCertificateStore")]
        public string ServerCertificateStore
        {
            get; set;
        }

        [JsonProperty("serverCertificateThumbprint")]
        public string ServerCertificateThumbprint
        {
            get; set;
        }

        #endregion Service Certificate (Optional)

        #region Channels

        [JsonProperty("blockSize")] public int BlockSize { get; set; } = 0x4000;

        [JsonProperty("maxBufferSize")] public int MaxBufferSize { get; set; } = 0x400000;

        [JsonProperty("usePrefixLength")]
        public bool UsePrefixLength
        {
            get; set;
        }

        #endregion Channels

        #region Management API

        [JsonProperty("managementApiAudience")]
        public string ManagementApiAudience
        {
            get; set;
        }

        [JsonProperty("managementApiIssuer")]
        public string ManagementApiIssuer
        {
            get; set;
        }

        [JsonProperty("managementApiSecurityCodes")]
        public string ManagementApiSecurityCodes
        {
            get; set;
        }

        [JsonProperty("managmentApiSymmetricKey")]
        public string ManagmentApiSymmetricKey
        {
            get; set;
        }

        #endregion Management API

        #region Gateway

        [JsonProperty("tlsCertficateAuthentication")]
        public bool TlsCertficateAuthentication;

        [JsonProperty("auditConnectionString")]
        public string AuditConnectionString
        {
            get; set;
        }

        [JsonProperty("coapAuthority")]
        public string CoapAuthority
        {
            get; set;
        }

        [JsonProperty("maxConnections")] public int MaxConnections { get; set; } = 10000;

        [JsonProperty("ports")]
        public string Ports
        {
            get; set;
        }

        #endregion Gateway

        #region PSKs (Optional)

        [JsonProperty("pskIdentities")]
        public string PskIdentities
        {
            get; set;
        }

        [JsonProperty("pskKeys")]
        public string PskKeys
        {
            get; set;
        }

        [JsonProperty("pskKeyVaultAuthority")]
        public string PskKeyVaultAuthority
        {
            get; set;
        }

        [JsonProperty("pskKeyVaultClientId")]
        public string PskKeyVaultClientId
        {
            get; set;
        }

        [JsonProperty("pskKeyVaultClientSecret")]
        public string PskKeyVaultClientSecret
        {
            get; set;
        }

        [JsonProperty("pskRedisConnectionString")]
        public string PskRedisConnectionString
        {
            get; set;
        }

        [JsonProperty("pskStorageType")]
        public string PskStorageType
        {
            get; set;
        }

        #endregion PSKs (Optional)

        #region Client Identity

        [JsonProperty("clientIdentityClaimKeys")]
        public string ClientIdentityClaimKeys
        {
            get; set;
        }

        [JsonProperty("clientIdentityClaimTypes")]
        public string ClientIdentityClaimTypes
        {
            get; set;
        }

        [JsonProperty("clientIdentityNameClaimType")]
        public string ClientIdentityNameClaimType
        {
            get; set;
        }

        #endregion Client Identity

        #region Service Identity

        [JsonProperty("serviceIdentityClaimTypes")]
        public string ServiceIdentityClaimTypes
        {
            get; set;
        }

        [JsonProperty("serviceIdentityClaimValues")]
        public string ServiceIdentityClaimValues
        {
            get; set;
        }

        #endregion Service Identity

        #region Client Security

        [JsonProperty("clientAudience")]
        public string ClientAudience
        {
            get; set;
        }

        [JsonProperty("clientIssuer")]
        public string ClientIssuer
        {
            get; set;
        }

        [JsonProperty("clientSymmetricKey")]
        public string ClientSymmetricKey
        {
            get; set;
        }

        [JsonProperty("clientTokenType")]
        public string ClientTokenType
        {
            get; set;
        }

        #endregion Client Security

        #region Protocols

        [JsonProperty("ackRandomFactor")] public double AckRandomFactor { get; set; } = 1.5;

        [JsonProperty("ackTimeoutSeconds")] public double AckTimeoutSeconds { get; set; } = 2.0;

        [JsonProperty("autoRetry")]
        public bool AutoRetry
        {
            get; set;
        }

        [JsonProperty("defaultLeisure")] public double DefaultLeisure { get; set; } = 4.0;

        [JsonProperty("keepAliveSeconds")] public double KeepAliveSeconds { get; set; } = 180.0;

        [JsonProperty("maxLatencySeconds")] public double MaxLatencySeconds { get; set; } = 100.0;

        [JsonProperty("maxRetransmit")] public int MaxRetransmit { get; set; } = 4;

        [JsonProperty("noResponseOption")] public bool NoResponseOption { get; set; } = true;

        [JsonProperty("nstart")] public int NStart { get; set; } = 1;

        [JsonProperty("observeOption")] public bool ObserveOption { get; set; } = true;

        [JsonProperty("probingRate")] public double ProbingRate { get; set; } = 1.0;

        #endregion Protocols

        #region Logging

        [JsonProperty("instrumentationKey")]
        public string InstrumentationKey
        {
            get; set;
        }

        [JsonProperty("loggerTypes")] public string LoggerTypes { get; set; } = "Console";

        [JsonProperty("logLevel")] public string LogLevel { get; set; } = "Warning";

        #endregion Logging
    }
}