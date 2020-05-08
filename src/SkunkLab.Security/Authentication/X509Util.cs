using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace SkunkLab.Security.Authentication
{
    public class X509Util
    {
        public static X509Certificate2 GetCertificate(StoreName name, StoreLocation location, string thumbprint)
        {
            X509Store store = new X509Store(name, location);

            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection collection = store.Certificates;
                foreach (var item in collection)
                {
                    if (item.Thumbprint == thumbprint)
                    {
                        return item;
                    }
                }

                return null;
            }
            finally
            {
                store.Close();
            }
        }

        public static List<Claim> GetClaimSet(X509Certificate2 certificate)
        {
            List<Claim> list = new List<Claim> {
                new Claim(ClaimTypes.SerialNumber, certificate.SerialNumber, null, certificate.Issuer),
                new Claim(ClaimTypes.Thumbprint, certificate.Thumbprint, null, certificate.Issuer),
                new Claim(ClaimTypes.X500DistinguishedName, GetParsedClaimValue(certificate.Subject), null,
                    certificate.Issuer),
                new Claim(ClaimTypes.Name, GetParsedClaimValue(certificate.Subject), null, certificate.Issuer),
                new Claim(ClaimTypes.Dns, GetParsedClaimValue(certificate.Subject), null, certificate.Issuer)
            };

            return list;
        }

        public static string GetParsedClaimValue(string value)
        {
            string[] parts = value.Split(new[] { ',' });
            string[] item = parts[0].Split(new[] { '=' });
            return item[1];
        }

        public static bool Validate(StoreName name, StoreLocation location, X509RevocationMode mode,
            X509RevocationFlag flag, X509Certificate2 clientCertificate, string thumbprint)
        {
            X509Certificate2 chainedCertificate = GetCertificate(name, location, thumbprint);

            if (clientCertificate == null || chainedCertificate == null)
            {
                return false;
            }

            X509Store store = new X509Store(name, location);

            try
            {
                X509Chain chain = new X509Chain();
                X509ChainPolicy policy = new X509ChainPolicy { RevocationMode = mode, RevocationFlag = flag };
                chain.ChainPolicy = policy;

                if (!chain.Build(clientCertificate))
                {
                    return false;
                }

                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection collection = store.Certificates;

                foreach (var item in chain.ChainElements)
                {
                    X509Certificate2Collection certs = collection.Find(X509FindType.FindByThumbprint,
                        item.Certificate.Thumbprint, true);

                    if (certs == null || certs.Count == 0)
                    {
                        return false;
                    }

                    foreach (X509Certificate2 cert in certs)
                    {
                        if (cert.Thumbprint == chainedCertificate.Thumbprint && cert.NotAfter < DateTime.Now &&
                            cert.NotBefore > DateTime.Now)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            finally
            {
                store.Close();
            }
        }
    }
}