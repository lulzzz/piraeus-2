using System;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkunkLab.Channels.WebSocket
{
    public delegate void WebSocketCloseHandler(object sender, WebSocketCloseEventArgs args);

    public delegate void WebSocketErrorHandler(object sender, WebSocketErrorEventArgs args);

    public delegate void WebSocketOpenHandler(object sender, WebSocketOpenEventArgs args);

    public delegate void WebSocketReceiveHandler(object sender, WebSocketReceiveEventArgs args);

    public class WebSocketHandler
    {
        private readonly WebSocketConfig config;

        private readonly TaskQueue sendQueue = new TaskQueue();

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

        public System.Net.WebSockets.WebSocket Socket
        {
            get; set;
        }

        public void Close()
        {
            CloseAsync();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task ProcessWebSocketRequestAsync(System.Net.WebSockets.WebSocket socket)
        {
            _ = socket ?? throw new ArgumentNullException(nameof(socket));

            byte[] buffer = new byte[config.ReceiveLoopBufferSize];

            return ProcessWebSocketRequestAsync(socket,
                () => WebSocketMessageReader.ReadMessageAsync(socket, buffer, config.MaxIncomingMessageSize,
                    CancellationToken.None));
        }

        public void Send(string message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            SendAsync(message).GetAwaiter();
        }

        public void Send(byte[] message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            SendAsync(message, WebSocketMessageType.Binary).GetAwaiter();
        }

        internal Task CloseAsync()
        {
            TaskCompletionSource<Task> tcs = new TaskCompletionSource<Task>();

            if (Socket != null && Socket.State == WebSocketState.Open)
            {
                Task task = sendQueue.Enqueue(() =>
                    Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", token));
                tcs.SetResult(task);
            }

            return tcs.Task;
        }

        internal async Task ProcessWebSocketRequestAsync(System.Net.WebSockets.WebSocket socket,
            Func<Task<WebSocketMessage>> messageRetriever)
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
                        OnReceive?.Invoke(this,
                            new WebSocketReceiveEventArgs(Encoding.UTF8.GetBytes(message.Data as string)));
                    }
                    else
                    {
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
                    if (this is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        internal Task SendAsync(string message)
        {
            return SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text);
        }

        internal Task SendAsync(byte[] message, WebSocketMessageType messageType)
        {
            TaskCompletionSource<Task> tcs = new TaskCompletionSource<Task>();
            try
            {
                if (Socket != null && Socket.State == WebSocketState.Open)
                {
                    sendQueue.Enqueue(() =>
                        Socket.SendAsync(new ArraySegment<byte>(message), messageType, true, token));
                }

                tcs.SetResult(null);
            }
            catch (Exception exc) { tcs.SetException(exc); }

            return tcs.Task;
        }

        private static bool IsFatalException(Exception ex)
        {
            if (ex is COMException exception)
            {
                switch ((uint)exception.ErrorCode)
                {
                    case 0x80070026:
                    case 0x800703e3:
                    case 0x800704cd:
                        return false;
                }
            }

            return true;
        }
    }
}