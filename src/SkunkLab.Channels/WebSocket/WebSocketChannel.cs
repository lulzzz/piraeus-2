using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SkunkLab.Channels.WebSocket
{
    public abstract class WebSocketChannel : IChannel
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

        public static WebSocketChannel Create(HttpContext context, WebSocketConfig config, CancellationToken token)
        {
            return new WebSocketServerChannel(context, config, token);
        }

        public static WebSocketChannel Create(HttpContext context, System.Net.WebSockets.WebSocket socket,
            WebSocketConfig config, CancellationToken token)
        {
            return new WebSocketServerChannel(context, socket, config, token);
        }

        public static WebSocketChannel Create(Uri endpointUri, WebSocketConfig config, CancellationToken token)
        {
            return new WebSocketClientChannel(endpointUri, config, token);
        }

        public static WebSocketChannel Create(Uri endpointUri, string subProtocol, WebSocketConfig config,
            CancellationToken token)
        {
            return new WebSocketClientChannel(endpointUri, subProtocol, config, token);
        }

        public static WebSocketChannel Create(Uri endpointUri, string securityToken, string subProtocol,
            WebSocketConfig config, CancellationToken token)
        {
            return new WebSocketClientChannel(endpointUri, securityToken, subProtocol, config, token);
        }

        public static WebSocketChannel Create(Uri endpointUri, X509Certificate2 certificate, string subProtocol,
            WebSocketConfig config, CancellationToken token)
        {
            return new WebSocketClientChannel(endpointUri, certificate, subProtocol, config, token);
        }

        public abstract Task AddMessageAsync(byte[] message);

        public abstract Task CloseAsync();

        public abstract void Dispose();

        public abstract void Open();

        public abstract Task OpenAsync();

        public abstract Task ReceiveAsync();

        public abstract void Send(byte[] message);

        public abstract Task SendAsync(byte[] message);
    }
}