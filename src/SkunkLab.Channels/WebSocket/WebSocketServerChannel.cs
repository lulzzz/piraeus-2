﻿using Microsoft.AspNetCore.Http;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SkunkLab.Channels.WebSocket
{
    public class WebSocketServerChannel : WebSocketChannel
    {
        //public WebSocketServerChannel(HttpListenerWebSocketContext context, WebSocketConfig config, CancellationToken token)
        //{
        //    Id = "ws-" + Guid.NewGuid().ToString();
        //    this.token = token;
        //    this.IsEncrypted = context.RequestUri.Scheme == "wss";
        //    this.IsAuthenticated = HttpHelper.HttpContext.User.Identity.IsAuthenticated;
        //    this.handler = new WebSocketHandler(config, token);
        //    this.handler.OnReceive += Handler_OnReceive;
        //    this.handler.OnError += Handler_OnError;
        //    this.handler.OnOpen += Handler_OnOpen;
        //    this.handler.OnClose += Handler_OnClose;
        //}

        private readonly TaskQueue _sendQueue = new TaskQueue();

        private readonly WebSocketConfig config;

        private readonly WebSocketHandler handler;

        private readonly CancellationToken token;

        private ChannelState _state;

        private bool disposed;

        private System.Net.WebSockets.WebSocket socket;

        public WebSocketServerChannel(HttpContext context, WebSocketConfig config, CancellationToken token)
        {
            Id = "ws-" + Guid.NewGuid().ToString();
            this.config = config;
            this.token = token;
            this.IsEncrypted = context.Request.Scheme == "wss";
            this.IsAuthenticated = context.User.Identity.IsAuthenticated;

            this.handler = new WebSocketHandler(config, token);
            this.handler.OnReceive += Handler_OnReceive;
            this.handler.OnError += Handler_OnError;
            this.handler.OnOpen += Handler_OnOpen;
            this.handler.OnClose += Handler_OnClose;

            Task task = Task.Factory.StartNew(async () =>
            {
                await Task.Delay(100);
                this.socket = await context.AcceptWebSocketRequestAsync(this.handler);
            });

            Task.WhenAll(task);
        }

        public WebSocketServerChannel(HttpContext context, System.Net.WebSockets.WebSocket socket, WebSocketConfig config, CancellationToken token)
        {
            Id = "ws-" + Guid.NewGuid().ToString();
            this.config = config;
            this.token = token;

            this.IsEncrypted = context.Request.Scheme == "wss";
            this.IsAuthenticated = context.User.Identity.IsAuthenticated;

            this.handler = new WebSocketHandler(config, token);
            this.handler.OnReceive += Handler_OnReceive;
            this.handler.OnError += Handler_OnError;
            this.handler.OnOpen += Handler_OnOpen;
            this.handler.OnClose += Handler_OnClose;
            this.socket = socket;
        }

        public override event EventHandler<ChannelCloseEventArgs> OnClose;

        public override event EventHandler<ChannelErrorEventArgs> OnError;

        public override event EventHandler<ChannelOpenEventArgs> OnOpen;

        public override event EventHandler<ChannelReceivedEventArgs> OnReceive;

        public override event EventHandler<ChannelStateEventArgs> OnStateChange;

        public override string Id { get; internal set; }

        public override bool IsAuthenticated { get; internal set; }

        public override bool IsConnected => State == ChannelState.Open;

        public override bool IsEncrypted { get; internal set; }

        public override int Port { get; internal set; }

        public override bool RequireBlocking => false;

        public override ChannelState State
        {
            get => _state;
            internal set
            {
                if (_state != value)
                {
                    OnStateChange?.Invoke(this, new ChannelStateEventArgs(Id, value));
                }

                _state = value;
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
            this.handler.ProcessWebSocketRequestAsync(this.socket);
        }

        #region Handler Events

        private void Handler_OnClose(object sender, WebSocketCloseEventArgs args)
        {
            State = ChannelState.Closed;
            OnClose?.Invoke(this, new ChannelCloseEventArgs(this.Id));
        }

        private void Handler_OnError(object sender, WebSocketErrorEventArgs args)
        {
            OnError?.Invoke(this, new ChannelErrorEventArgs(this.Id, args.Error));
        }

        private void Handler_OnOpen(object sender, WebSocketOpenEventArgs args)
        {
            State = ChannelState.Open;
            OnOpen?.Invoke(this, new ChannelOpenEventArgs(this.Id, null));
        }

        private void Handler_OnReceive(object sender, WebSocketReceiveEventArgs args)
        {
            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(this.Id, args.Message));
        }

        #endregion Handler Events

        public override async Task OpenAsync()
        {
            await this.handler.ProcessWebSocketRequestAsync(this.socket);
        }

        public override async Task ReceiveAsync()
        {
            await Task.CompletedTask;
        }

        public override void Send(byte[] message)
        {
            handler.SendAsync(message, WebSocketMessageType.Binary).GetAwaiter();

            //Task task = Task.Factory.StartNew(async () =>
            //{
            //    await handler.SendAsync(message, WebSocketMessageType.Binary);
            //});

            //Task.WaitAll(task);
        }

        public override async Task SendAsync(byte[] message)
        {
            await this.handler.SendAsync(message, WebSocketMessageType.Binary);
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;

                if (State == ChannelState.Open)
                {
                    this.handler.Close();
                }

                if (socket != null)
                {
                    socket.Dispose();
                }
            }
        }

        //private static bool IsFatalException(Exception ex)
        //{
        //    COMException exception = ex as COMException;
        //    if (exception != null)
        //    {
        //        switch (((uint)exception.ErrorCode))
        //        {
        //            case 0x80070026:
        //            case 0x800703e3:
        //            case 0x800704cd:
        //                return false;
        //        }
        //    }
        //    return true;
        //}

        //[EditorBrowsable(EditorBrowsableState.Never)]
        //public Task ProcessWebSocketRequestAsync(WebSocketContext webSocketContext)
        //{
        //    if (webSocketContext == null)
        //    {
        //        throw new ArgumentNullException("webSocketContext");
        //    }

        //    byte[] buffer = new byte[config.ReceiveLoopBufferSize];
        //    System.Net.WebSockets.WebSocket webSocket = webSocketContext.WebSocket;
        //    return ProcessWebSocketRequestAsync(webSocketContext, () => WebSocketMessageReader.ReadMessageAsync(webSocket, buffer, config.MaxIncomingMessageSize, token));
        //}

        //internal async Task ProcessWebSocketRequestAsync(WebSocketContext webSocketContext, Func<Task<WebSocketMessage>> messageRetriever)
        //{
        //    try
        //    {
        //        WebSocketContext = webSocketContext;
        //        OnOpen?.Invoke(this, new ChannelOpenEventArgs(Id, null));

        //        while (!token.IsCancellationRequested && WebSocketContext.WebSocket.State == WebSocketState.Open)
        //        {
        //            WebSocketMessage message = await messageRetriever();
        //            if (message.MessageType == WebSocketMessageType.Binary)
        //            {
        //                OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, message.Data as byte[]));
        //            }
        //            else if (message.MessageType == WebSocketMessageType.Text)
        //            {
        //                OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, Encoding.UTF8.GetBytes(message.Data as string)));
        //            }
        //            else
        //            {
        //                //close received
        //                OnClose?.Invoke(this, new ChannelCloseEventArgs(Id));
        //                break;
        //            }
        //        }
        //    }
        //    catch (AggregateException ae)
        //    {
        //        if (!(WebSocketContext.WebSocket.State == WebSocketState.CloseReceived ||
        //            WebSocketContext.WebSocket.State == WebSocketState.CloseSent))
        //        {
        //            OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ae.Flatten()));
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        if (!(WebSocketContext.WebSocket.State == WebSocketState.CloseReceived ||
        //            WebSocketContext.WebSocket.State == WebSocketState.CloseSent))
        //        {
        //            if (IsFatalException(exception))
        //            {
        //                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, exception));
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        try
        //        {
        //            await CloseAsync();
        //        }
        //        finally
        //        {
        //            IDisposable disposable = this as IDisposable;
        //            if (disposable != null)
        //            {
        //                disposable.Dispose();
        //            }
        //        }
        //    }

        //}
    }
}