using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Org.BouncyCastle.Crypto.Tls;
using SkunkLab.Channels.Http;
using SkunkLab.Channels.Tcp;
using SkunkLab.Channels.Udp;
using SkunkLab.Channels.WebSocket;

namespace SkunkLab.Channels
{
    public abstract class ChannelFactory
    {
        #region TCP Channels

        #region TCP Server Channels

        public static IChannel Create(bool usePrefixLength, TcpClient client, int blockSize = 0x4000,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, client, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, TcpClient client, X509Certificate2 certificate,
            bool clientAuth, int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, client, certificate, clientAuth, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, TcpClient client, TlsPskIdentityManager pskManager,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, client, pskManager, blockSize, maxBufferSize, token);
        }

        #endregion TCP Server Channels

        #region TCP Client Channels

        public static IChannel Create(bool usePrefixLength, string hostname, int port, int blockSize = 0x4000,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, hostname, port, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, string hostname, int port, IPEndPoint localEP,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, hostname, port, localEP, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, IPEndPoint remoteEndpoint, int blockSize = 0x4000,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, remoteEndpoint, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, IPEndPoint remoteEndpoint, IPEndPoint localEP,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, remoteEndpoint, localEP, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, IPAddress address, int port, int blockSize = 0x4000,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, address, port, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, IPAddress address, int port, IPEndPoint localEP,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, address, port, localEP, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, string hostname, int port, X509Certificate2 certificate,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, hostname, port, certificate, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, string hostname, int port, IPEndPoint localEP,
            X509Certificate2 certificate, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, hostname, port, localEP, certificate, blockSize, maxBufferSize,
                token);
        }

        public static IChannel Create(bool usePrefixLength, IPEndPoint remoteEndpoint, X509Certificate2 certificate,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, remoteEndpoint, certificate, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, IPEndPoint remoteEndpoint, IPEndPoint localEP,
            X509Certificate2 certificate, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, remoteEndpoint, localEP, certificate, blockSize, maxBufferSize,
                token);
        }

        public static IChannel Create(bool usePrefixLength, IPAddress address, int port, X509Certificate2 certificate,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, address, port, certificate, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, IPAddress address, int port, IPEndPoint localEP,
            X509Certificate2 certificate, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, address, port, localEP, certificate, blockSize, maxBufferSize,
                token);
        }

        public static IChannel Create(bool usePrefixLength, IPAddress address, int port, IPEndPoint localEP,
            string pskIdentity, byte[] psk, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, address, port, localEP, pskIdentity, psk, blockSize,
                maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, IPAddress address, int port, string pskIdentity, byte[] psk,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, address, port, pskIdentity, psk, blockSize, maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, string hostname, int port, IPEndPoint localEP,
            string pskIdentity, byte[] psk, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, hostname, port, localEP, pskIdentity, psk, blockSize,
                maxBufferSize, token);
        }

        public static IChannel Create(bool usePrefixLength, string hostname, int port, string pskIdentity, byte[] psk,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, hostname, port, pskIdentity, psk, blockSize, maxBufferSize,
                token);
        }

        public static IChannel Create(bool usePrefixLength, IPEndPoint remoteEP, IPEndPoint localEP, string pskIdentity,
            byte[] psk, int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, remoteEP, localEP, pskIdentity, psk, blockSize, maxBufferSize,
                token);
        }

        public static IChannel Create(bool usePrefixLength, IPEndPoint remoteEP, string pskIdentity, byte[] psk,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            return TcpChannel.Create(usePrefixLength, remoteEP, pskIdentity, psk, blockSize, maxBufferSize, token);
        }

        #endregion TCP Client Channels

        #endregion TCP Channels

        #region HTTP Channels

        #region HTTP Server Channels

        public static IChannel Create(HttpContext context)
        {
            return HttpChannel.Create(context);
        }

        public static IChannel Create(string endpoint, string resourceUriString, string contentType)
        {
            return HttpChannel.Create(endpoint, resourceUriString, contentType);
        }

        public static IChannel Create(string endpoint, string resourceUriString, string contentType,
            string securityToken)
        {
            return HttpChannel.Create(endpoint, resourceUriString, contentType, securityToken);
        }

        #endregion HTTP Server Channels

        #region HTTP Client Channels

        public static IChannel Create(string endpoint, string securityToken)
        {
            return HttpChannel.Create(endpoint, securityToken);
        }

        public static IChannel Create(string endpoint, X509Certificate2 certficate)
        {
            return HttpChannel.Create(endpoint, certficate);
        }

        public static IChannel Create(string endpoint, string securityToken, IEnumerable<Observer> observers,
            CancellationToken token = default)
        {
            return HttpChannel.Create(endpoint, securityToken, observers, token);
        }

        public static IChannel Create(string endpoint, X509Certificate2 certificate, IEnumerable<Observer> observers,
            CancellationToken token = default)
        {
            return HttpChannel.Create(endpoint, certificate, observers, token);
        }

        #endregion HTTP Client Channels

        #endregion HTTP Channels

        #region Web Socket Channels

        #region Web Socket Server Channels

        public static IChannel Create(HttpContext context, WebSocketConfig config, CancellationToken token)
        {
            return WebSocketChannel.Create(context, config, token);
        }

        public static IChannel Create(WebSocketConfig config, HttpContext context,
            System.Net.WebSockets.WebSocket socket, CancellationToken token)
        {
            return WebSocketChannel.Create(context, socket, config, token);
        }

        #endregion Web Socket Server Channels

        #region Web Socket Client Channels

        public static IChannel Create(Uri endpointUri, WebSocketConfig config, CancellationToken token)
        {
            return WebSocketChannel.Create(endpointUri, config, token);
        }

        public static IChannel Create(Uri endpointUri, string subProtocol, WebSocketConfig config,
            CancellationToken token)
        {
            return WebSocketChannel.Create(endpointUri, subProtocol, config, token);
        }

        public static IChannel Create(Uri endpointUri, string securityToken, string subProtocol, WebSocketConfig config,
            CancellationToken token)
        {
            return WebSocketChannel.Create(endpointUri, securityToken, subProtocol, config, token);
        }

        public static IChannel Create(Uri endpointUri, X509Certificate2 certificate, string subProtocol,
            WebSocketConfig config, CancellationToken token)
        {
            return WebSocketChannel.Create(endpointUri, certificate, subProtocol, config, token);
        }

        #endregion Web Socket Client Channels

        #endregion Web Socket Channels

        #region UDP Channels

        #region UDP Server Channels

        public static IChannel Create(UdpClient client, IPEndPoint remoteEP, CancellationToken token)
        {
            return UdpChannel.Create(client, remoteEP, token);
        }

        #endregion UDP Server Channels

        #region UDP Client Channels

        public static IChannel Create(int localPort, string hostname, int port, CancellationToken token)
        {
            return UdpChannel.Create(localPort, hostname, port, token);
        }

        public static IChannel Create(int localPort, IPEndPoint remoteEP, CancellationToken token)
        {
            return UdpChannel.Create(localPort, remoteEP, token);
        }

        #endregion UDP Client Channels

        #endregion UDP Channels
    }
}