namespace SkunkLab.Channels.WebSocket
{
    using System;
    using System.ComponentModel;
    using System.Net.WebSockets;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public delegate void WebSocketCloseHandler(object sender, WebSocketCloseEventArgs args);

    public delegate void WebSocketErrorHandler(object sender, WebSocketErrorEventArgs args);

    public delegate void WebSocketOpenHandler(object sender, WebSocketOpenEventArgs args);

    public delegate void WebSocketReceiveHandler(object sender, WebSocketReceiveEventArgs args);

    public class WebSocketHandler
    {
        private readonly TaskQueue _sendQueue = new TaskQueue();

        private readonly WebSocketConfig config;

        //public WebSocketContext WebSocketContext { get; set; }
        private readonly CancellationToken token;

        public WebSocketHandler(WebSocketConfig config, CancellationToken token)
        {
            this.config = config;
            this.token = token;
        }

        public event WebSocketCloseHandler OnClose;

        public event WebSocketErrorHandler OnError;

        public event WebSocketOpenHandler OnOpen;

        public event WebSocketReceiveHandler OnReceive;

        public WebSocket Socket { get; set; }

        public void Close()
        {
            CloseAsync();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task ProcessWebSocketRequestAsync(WebSocket socket)
        {
            _ = socket ?? throw new ArgumentNullException(nameof(socket));

            byte[] buffer = new byte[config.ReceiveLoopBufferSize];

            return ProcessWebSocketRequestAsync(socket, () => WebSocketMessageReader.ReadMessageAsync(socket, buffer, config.MaxIncomingMessageSize, CancellationToken.None));
        }

        public void Send(string message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            SendAsync(message).GetAwaiter();
            //Task task = SendAsync(message);
            //Task.WaitAll(task);
        }

        //        while (!token.IsCancellationRequested && WebSocketContext.WebSocket.State == WebSocketState.Open)
        //        {
        //            WebSocketMessage message = await messageRetriever();
        //            if (message.MessageType == WebSocketMessageType.Binary)
        //            {
        //                OnReceive?.Invoke(this, new WebSocketReceiveEventArgs(message.Data as byte[]));
        //            }
        //            else if (message.MessageType == WebSocketMessageType.Text)
        //            {
        //                OnReceive?.Invoke(this, new WebSocketReceiveEventArgs(Encoding.UTF8.GetBytes(message.Data as string)));
        //            }
        //            else
        //            {
        //                //close received
        //                OnClose?.Invoke(this, new WebSocketCloseEventArgs(WebSocketCloseStatus.NormalClosure));
        //                break;
        //            }
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        if (!(WebSocketContext.WebSocket.State == WebSocketState.CloseReceived ||
        //            WebSocketContext.WebSocket.State == WebSocketState.CloseSent))
        //        {
        //            if (IsFatalException(exception))
        //            {
        //                OnError?.Invoke(this, new WebSocketErrorEventArgs(exception));
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
        public void Send(byte[] message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            SendAsync(message, WebSocketMessageType.Binary).GetAwaiter();
            //Task task = SendAsync(message, WebSocketMessageType.Binary);
            //Task.WaitAll(task);
        }

        internal Task CloseAsync()
        {
            TaskCompletionSource<Task> tcs = new TaskCompletionSource<Task>();

            if (Socket != null && Socket.State == WebSocketState.Open)
            {
                Task task = this._sendQueue.Enqueue(() => Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", token));
                tcs.SetResult(task);
            }

            return tcs.Task;
        }

        internal async Task ProcessWebSocketRequestAsync(WebSocket socket, Func<Task<WebSocketMessage>> messageRetriever)
        {
            try
            {
                Socket = socket;
                OnOpen?.Invoke(this, new WebSocketOpenEventArgs());

                while (!token.IsCancellationRequested && Socket.State == WebSocketState.Open)
                {
                    WebSocketMessage message = await messageRetriever();
                    if (message.MessageType == WebSocketMessageType.Binary)
                    {
                        OnReceive?.Invoke(this, new WebSocketReceiveEventArgs(message.Data as byte[]));
                    }
                    else if (message.MessageType == WebSocketMessageType.Text)
                    {
                        OnReceive?.Invoke(this, new WebSocketReceiveEventArgs(Encoding.UTF8.GetBytes(message.Data as string)));
                    }
                    else
                    {
                        //close received
                        OnClose?.Invoke(this, new WebSocketCloseEventArgs(WebSocketCloseStatus.NormalClosure));
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                if (!(Socket.State == WebSocketState.CloseReceived ||
                    Socket.State == WebSocketState.CloseSent))
                {
                    if (IsFatalException(exception))
                    {
                        OnError?.Invoke(this, new WebSocketErrorEventArgs(exception));
                    }
                }
            }
            finally
            {
                try
                {
                    await CloseAsync();
                }
                finally
                {
                    IDisposable disposable = this as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        //internal async Task ProcessWebSocketRequestAsync(HttpListenerWebSocketContext webSocketContext, Func<Task<WebSocketMessage>> messageRetriever)
        //{
        //    try
        //    {
        //        WebSocketContext = webSocketContext;
        //        OnOpen?.Invoke(this, new WebSocketOpenEventArgs());
        internal Task SendAsync(string message)
        {
            return SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text);
        }

        //    return ProcessWebSocketRequestAsync(webSocketContext, () => WebSocketMessageReader.ReadMessageAsync(webSocket, buffer, config.MaxIncomingMessageSize,CancellationToken.None));
        //    //source.SetResult(ProcessWebSocketRequestAsync(webSocketContext, () => WebSocketMessageReader.ReadMessageAsync(webSocket, buffer, config.MaxIncomingMessageSize, CancellationToken.None)));
        //    //return source.Task;
        //}
        internal Task SendAsync(byte[] message, WebSocketMessageType messageType)
        {
            TaskCompletionSource<Task> tcs = new TaskCompletionSource<Task>();
            try
            {
                if (Socket != null && Socket.State == WebSocketState.Open)
                {
                    _sendQueue.Enqueue(() => this.Socket.SendAsync(new ArraySegment<byte>(message), messageType, true, token));
                }
                tcs.SetResult(null);
            }
            catch (Exception exc) { tcs.SetException(exc); }

            return tcs.Task;
        }

        private static bool IsFatalException(Exception ex)
        {
            COMException exception = ex as COMException;
            if (exception != null)
            {
                switch (((uint)exception.ErrorCode))
                {
                    case 0x80070026:
                    case 0x800703e3:
                    case 0x800704cd:
                        return false;
                }
            }
            return true;
        }

        //[EditorBrowsable(EditorBrowsableState.Never)]
        //public Task ProcessWebSocketRequestAsync(HttpListenerWebSocketContext webSocketContext)
        //{
        //    if (webSocketContext == null)
        //    {
        //        throw new ArgumentNullException("webSocketContext");
        //    }

        //    byte[] buffer = new byte[config.ReceiveLoopBufferSize];
        //    WebSocket webSocket = webSocketContext.WebSocket;
    }
}