using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SkunkLab.Channels.Udp
{
    public class UdpClientChannel : UdpChannel
    {
        private readonly string hostname;

        private readonly int port;

        private readonly IPEndPoint remoteEP;

        private readonly CancellationToken token;

        private UdpClient client;

        private bool disposedValue;

        private ChannelState state;

        public UdpClientChannel(int localPort, IPEndPoint remoteEP, CancellationToken token)
        {
            Port = localPort;
            this.remoteEP = remoteEP;
            this.token = token;
            Id = "udp-" + Guid.NewGuid();
        }

        public UdpClientChannel(int localPort, string hostname, int port, CancellationToken token)
        {
            Port = localPort;
            this.hostname = hostname;
            this.port = port;
            this.token = token;
            Id = "udp-" + Guid.NewGuid();
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

        public override bool IsConnected
        {
            get
            {
                if (disposedValue || client == null || client.Client == null)
                {
                    return false;
                }

                return client.Client.Connected;
            }
        }

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
                if (value != state)
                {
                    OnStateChange?.Invoke(this, new ChannelStateEventArgs(Id, value));
                }

                state = value;
            }
        }

        public override string TypeId => "UDP";

        public override async Task AddMessageAsync(byte[] message)
        {
            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, message));
            await Task.CompletedTask;
        }

        public override async Task CloseAsync()
        {
            client.Close();
            OnClose?.Invoke(this, new ChannelCloseEventArgs(Id));
            await Task.CompletedTask;
        }

        public override void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }

        public override async Task OpenAsync()
        {
            State = ChannelState.Connecting;
            client = new UdpClient(Port)
            {
                DontFragment = true
            };

            try
            {
                if (!string.IsNullOrEmpty(hostname))
                {
                    client.Connect(hostname, port);
                }
                else
                {
                    client.Connect(remoteEP);
                }

                State = ChannelState.Open;

                OnOpen?.Invoke(this, new ChannelOpenEventArgs(Id, null));
            }
            catch (Exception ex)
            {
                client = null;
                Trace.TraceError("UDP client channel {0} open error {1}", Id, ex.Message);
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
            }

            await Task.CompletedTask;
        }

        public override async Task ReceiveAsync()
        {
            while (IsConnected && !token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await client.ReceiveAsync();
                    OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, result.Buffer));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("UDP client channel {0} receive error {1}", Id, ex.Message);
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                    break;
                }
            }

            await CloseAsync();
        }

        public override async Task SendAsync(byte[] message)
        {
            try
            {
                if (remoteEP == null)
                {
                    await client.SendAsync(message, message.Length);
                }
                else
                {
                    await client.SendAsync(message, message.Length);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("UDP client channel {0} send error {1}", Id, ex.Message);
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
            }
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposedValue)
            {
                if (client != null && IsConnected)
                {
                    client.Close();
                }

                client.Dispose();
                disposedValue = true;
            }
        }
    }
}