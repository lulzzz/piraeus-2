using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SkunkLab.Channels.Udp
{
    public class UdpServerChannel : UdpChannel
    {
        private readonly UdpClient client;

        private readonly IPEndPoint remoteEP;

        private readonly CancellationToken token;

        private bool disposedValue;

        private ChannelState state;

        public UdpServerChannel(UdpClient listener, IPEndPoint remoteEP, CancellationToken token)
        {
            Id = "udp-" + Guid.NewGuid();
            client = listener;
            this.remoteEP = remoteEP;
            this.token = token;
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

        public override bool IsConnected => ChannelState.Open == State;

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
            State = ChannelState.Closed;
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
            try
            {
                State = ChannelState.Open;

                OnOpen?.Invoke(this, new ChannelOpenEventArgs(Id, null));
            }
            catch (Exception ex)
            {
                Trace.TraceError("UDP server channel {0} open error {1}", Id, ex.Message);
                State = ChannelState.Aborted;
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
            }

            await Task.CompletedTask;
        }

        public override async Task ReceiveAsync()
        {
            await Task.CompletedTask;
        }

        public override async Task SendAsync(byte[] message)
        {
            try
            {
                await client.SendAsync(message, message.Length, remoteEP);
            }
            catch (Exception ex)
            {
                Trace.TraceError("UDP server channel {0} send error {1}", Id, ex.Message);
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
            }
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposedValue)
            {
                disposedValue = true;
            }
        }
    }
}