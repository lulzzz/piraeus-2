using System;
using System.Collections.Generic;
using System.IO;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;

namespace SkunkLab.Channels.Tcp
{
    public static class TlsClientUtil
    {
        public static TlsClientProtocol ConnectPskTlsClient(string identity, byte[] psk, Stream stream)
        {
            try
            {
                SimplePskIdentity pskIdentity = new SimplePskIdentity(identity, psk);
                PskTlsClient2 pskTlsClient = new PskTlsClient2(pskIdentity);
                TlsClientProtocol protocol = new TlsClientProtocol(stream, new SecureRandom());
                protocol.Connect(pskTlsClient);
                return protocol;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in TLS protocol connnection '{0}'", ex.Message);
                throw ex;
            }
        }

        public static TlsClientProtocol ConnectPskTlsClientNonBlocking(string identity, byte[] psk)
        {
            try
            {
                SimplePskIdentity pskIdentity = new SimplePskIdentity(identity, psk);
                PskTlsClient2 pskTlsClient = new PskTlsClient2(pskIdentity);
                TlsClientProtocol protocol = new TlsClientProtocol(new SecureRandom());
                protocol.Connect(pskTlsClient);
                return protocol;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in TLS protocol connnection '{0}'", ex.Message);
                throw ex;
            }
        }

        public static TlsServerProtocol ConnectPskTlsServer(TlsPskIdentityManager pskManager, Stream stream)
        {
            try
            {
                PskTlsServer server = new PskTlsServer2(pskManager);
                TlsServerProtocol protocol = new TlsServerProtocol(stream, new SecureRandom());
                protocol.Accept(server);
                return protocol;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in TLS protocol connnection '{0}'", ex.Message);
                throw ex;
            }
        }

        public static TlsServerProtocol ConnectPskTlsServerNonBlocking(Dictionary<string, byte[]> psks)
        {
            try
            {
                TlsPskIdentityManager pskTlsManager = new PskIdentityManager(psks);
                PskTlsServer2 server = new PskTlsServer2(pskTlsManager);
                TlsServerProtocol protocol = new TlsServerProtocol(new SecureRandom());
                protocol.Accept(server);

                return protocol;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in TLS protocol connnection '{0}'", ex.Message);
                throw ex;
            }
        }
    }
}