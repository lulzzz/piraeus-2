using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Tls;

namespace SkunkLab.Channels.Tcp
{
    public abstract class TcpChannel : IChannel
    {
        public abstract event EventHandler<ChannelCloseEventArgs> OnClose;

        public abstract event EventHandler<ChannelErrorEventArgs> OnError;

        public abstract event EventHandler<ChannelOpenEventArgs> OnOpen;

        public abstract event EventHandler<ChannelReceivedEventArgs> OnReceive;

        public abstract event EventHandler<ChannelStateEventArgs> OnStateChange;

        public abstract string Id
        {
            get; internal set;
        }

        public abstract bool IsAuthenticated
        {
            get; internal set;
        }

        public abstract bool IsConnected
        {
            get;
        }

        public abstract bool IsEncrypted
        {
            get; internal set;
        }

        public abstract int Port
        {
            get; internal set;
        }

        public abstract bool RequireBlocking
        {
            get;
        }

        public abstract ChannelState State
        {
            get; internal set;
        }

        public abstract string TypeId
        {
            get;
        }

        public static TcpChannel Create(bool usePrefixLength, TcpClient client, int blockSize = 0x4000,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpServerChannel(client, maxBufferSize, token);
            }

            return new TcpServerChannel2(client, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPAddress address, int port, IPEndPoint localEP,
            string pskIdentity, byte[] psk, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(address, port, localEP, pskIdentity, psk, maxBufferSize, token);
            }

            return new TcpClientChannel2(address, port, localEP, pskIdentity, psk, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, string hostname, int port, IPEndPoint localEP,
            string pskIdentity, byte[] psk, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(hostname, port, localEP, pskIdentity, psk, maxBufferSize, token);
            }

            return new TcpClientChannel2(hostname, port, localEP, pskIdentity, psk, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, string hostname, int port, string pskIdentity, byte[] psk,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(hostname, port, null, pskIdentity, psk, maxBufferSize, token);
            }

            return new TcpClientChannel2(hostname, port, null, pskIdentity, psk, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPAddress address, int port, string pskIdentity,
            byte[] psk, int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(address, port, null, pskIdentity, psk, maxBufferSize, token);
            }

            return new TcpClientChannel2(address, port, null, pskIdentity, psk, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPEndPoint remoteEndpoint, string pskIdentity, byte[] psk,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(remoteEndpoint, null, pskIdentity, psk, maxBufferSize, token);
            }

            return new TcpClientChannel2(remoteEndpoint, null, pskIdentity, psk, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPEndPoint remoteEndpoint, IPEndPoint localEP,
            string pskIdentity, byte[] psk, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(remoteEndpoint, localEP, pskIdentity, psk, maxBufferSize, token);
            }

            return new TcpClientChannel2(remoteEndpoint, localEP, pskIdentity, psk, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, TcpClient client, X509Certificate2 certificate,
            bool clientAuth, int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpServerChannel(client, certificate, clientAuth, maxBufferSize, token);
            }

            return new TcpServerChannel2(client, certificate, clientAuth, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, TcpClient client, TlsPskIdentityManager pskManager,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpServerChannel(client, pskManager, maxBufferSize, token);
            }

            return new TcpServerChannel2(client, pskManager, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, string hostname, int port, int blockSize = 0x4000,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(hostname, port, maxBufferSize, token);
            }

            return new TcpClientChannel2(hostname, port, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, string hostname, int port, IPEndPoint localEP,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(hostname, port, localEP, maxBufferSize, token);
            }

            return new TcpClientChannel2(hostname, port, localEP, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPEndPoint remoteEndpoint, int blockSize = 0x4000,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(remoteEndpoint, maxBufferSize, token);
            }

            return new TcpClientChannel2(remoteEndpoint, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPEndPoint remoteEndpoint, IPEndPoint localEP,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(remoteEndpoint, localEP, maxBufferSize, token);
            }

            return new TcpClientChannel2(remoteEndpoint, localEP, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPAddress address, int port, int blockSize = 0x4000,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(address, port, maxBufferSize, token);
            }

            return new TcpClientChannel2(address, port, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPAddress address, int port, IPEndPoint localEP,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(address, port, localEP, maxBufferSize, token);
            }

            return new TcpClientChannel2(address, port, localEP, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, string hostname, int port, X509Certificate2 certificate,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(hostname, port, certificate, maxBufferSize, token);
            }

            return new TcpClientChannel2(hostname, port, certificate, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, string hostname, int port, IPEndPoint localEP,
            X509Certificate2 certificate, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(hostname, port, localEP, certificate, maxBufferSize, token);
            }

            return new TcpClientChannel2(hostname, port, localEP, certificate, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPEndPoint remoteEndpoint, X509Certificate2 certificate,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(remoteEndpoint, certificate, maxBufferSize, token);
            }

            return new TcpClientChannel2(remoteEndpoint, certificate, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPEndPoint remoteEndpoint, IPEndPoint localEP,
            X509Certificate2 certificate, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(remoteEndpoint, localEP, certificate, maxBufferSize, token);
            }

            return new TcpClientChannel2(remoteEndpoint, localEP, certificate, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPAddress address, int port, X509Certificate2 certificate,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(address, port, certificate, maxBufferSize, token);
            }

            return new TcpClientChannel2(address, port, certificate, blockSize, maxBufferSize, token);
        }

        public static TcpChannel Create(bool usePrefixLength, IPAddress address, int port, IPEndPoint localEP,
            X509Certificate2 certificate, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            if (usePrefixLength)
            {
                return new TcpClientChannel(address, port, localEP, certificate, maxBufferSize, token);
            }

            return new TcpClientChannel2(address, port, localEP, certificate, blockSize, maxBufferSize, token);
        }

        public abstract Task AddMessageAsync(byte[] message);

        public abstract Task CloseAsync();

        public abstract void Dispose();

        public abstract Task OpenAsync();

        public abstract Task ReceiveAsync();

        public abstract Task SendAsync(byte[] message);
    }
}