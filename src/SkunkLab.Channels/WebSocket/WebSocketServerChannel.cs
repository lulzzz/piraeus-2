using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SkunkLab.Channels.WebSocket
{
    public class WebSocketServerChannel : WebSocketChannel
    {
        private readonly WebSocketConfig config;

        private readonly WebSocketHandler handler;

        private readonly TaskQueue sendQueue = new TaskQueue();

        private readonly CancellationToken token;

        private bool disposed;

        private System.Net.WebSockets.WebSocket socket;

        private ChannelState state;

        public WebSocketServerChannel(HttpContext context, WebSocketConfig config, CancellationToken token)
        {
            Id = "ws-" + Guid.NewGuid();
            this.config = config;
            this.token = token;
            IsEncrypted = context.Request.Scheme == "wss";
            IsAuthenticated = context.User.Identity.IsAuthenticated;

            handler = new WebSocketHandler(config, token);
            handler.OnReceive += Handler_OnReceive;
            handler.OnError += Handler_OnError;
            handler.OnOpen += Handler_OnOpen;
            handler.OnClose += Handler_OnClose;

            Task task = Task.Factory.StartNew(async () =>
            {
                await Task.Delay(100);
                socket = await context.AcceptWebSocketRequestAsync(handler);
            });

            Task.WhenAll(task);
        }

        public WebSocketServerChannel(HttpContext context, System.Net.WebSockets.WebSocket socket,
            WebSocketConfig config, CancellationToken token)
        {
            Id = "ws-" + Guid.NewGuid();
            this.config = config;
            this.token = token;

            IsEncrypted = context.Request.Scheme == "wss";
            IsAuthenticated = context.User.Identity.IsAuthenticated;

            handler = new WebSocketHandler(config, token);
            handler.OnReceive += Handler_OnReceive;
            handler.OnError += Handler_OnError;
            handler.OnOpen += Handler_OnOpen;
            handler.OnClose += Handler_OnClose;
            this.socket = socket;
        }

        public override event EventHandler<ChannelCloseEventArgs> OnClose;

        public override event EventHandler<ChannelErrorEventArgs> OnError;

        public override event EventHandler<ChannelOpenEventArgs> OnOpen;

        public override event EventHandler<ChannelReceivedEventArgs> OnReceive;

        public override event EventHandler<ChannelStateEventArgs> OnStateChange;

        public override string Id
        {
            get; internal set;
        }

        public override bool IsAuthenticated
        {
            get; internal set;
        }

        public override bool IsConnected => State == ChannelState.Open;

        public override bool IsEncrypted
        {
            get; internal set;
        }

        public override int Port
        {
            get; internal set;
        }

        public override bool RequireBlocking => false;

        public override ChannelState State
        {
            get => state;
            internal set
            {
                if (state != value)
                {
                    OnStateChange?.Invoke(this, new ChannelStateEventArgs(Id, value));
                }

                state = value;
            }
        }

        public override string TypeId => "WebSocket";

        public override async Task AddMessageAsync(byte[] message)
        {
            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, message));
            await Task.CompletedTask;
        }

        public override async Task CloseAsync()
        {
            if (IsConnected)
            {
                State = ChannelState.ClosedReceived;
            }

            if (socket != null && (socket.State == WebSocketState.Open || socket.State == WebSocketState.Connecting))
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fault closing Web socket server socket - {ex.Message}");
                }
            }

            OnClose?.Invoke(this, new ChannelCloseEventArgs(Id));
            await Task.CompletedTask;
        }

        public override void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }

        public override void Open()
        {
            State = ChannelState.Open;
            handler.ProcessWebSocketRequestAsync(socket);
        }

        public override async Task OpenAsync()
        {
            await handler.ProcessWebSocketRequestAsync(socket);
        }

        public override async Task ReceiveAsync()
        {
            await Task.CompletedTask;
        }

        public override void Send(byte[] message)
        {
            handler.SendAsync(message, WebSocketMessageType.Binary).GetAwaiter();
        }

        public override async Task SendAsync(byte[] message)
        {
            await handler.SendAsync(message, WebSocketMessageType.Binary);
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;

                if (State == ChannelState.Open)
                {
                    handler.Close();
                }

                if (socket != null)
                {
                    socket.Dispose();
                }
            }
        }

        #region Handler Events

        private void Handler_OnClose(object sender, WebSocketCloseEventArgs args)
        {
            State = ChannelState.Closed;
            OnClose?.Invoke(this, new ChannelCloseEventArgs(Id));
        }

        private void Handler_OnError(object sender, WebSocketErrorEventArgs args)
        {
            OnError?.Invoke(this, new ChannelErrorEventArgs(Id, args.Error));
        }

        private void Handler_OnOpen(object sender, WebSocketOpenEventArgs args)
        {
            State = ChannelState.Open;
            OnOpen?.Invoke(this, new ChannelOpenEventArgs(Id, null));
        }

        private void Handler_OnReceive(object sender, WebSocketReceiveEventArgs args)
        {
            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, args.Message));
        }

        #endregion Handler Events
    }
}