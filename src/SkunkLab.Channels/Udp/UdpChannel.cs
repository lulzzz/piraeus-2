using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SkunkLab.Channels.Udp
{
    public abstract class UdpChannel : IChannel
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

        public static UdpChannel Create(UdpClient client, IPEndPoint remoteEP, CancellationToken token)
        {
            return new UdpServerChannel(client, remoteEP, token);
        }

        public static UdpChannel Create(int localPort, string hostname, int port, CancellationToken token)
        {
            return new UdpClientChannel(localPort, hostname, port, token);
        }

        public static UdpChannel Create(int localPort, IPEndPoint remoteEP, CancellationToken token)
        {
            return new UdpClientChannel(localPort, remoteEP, token);
        }

        public abstract Task AddMessageAsync(byte[] message);

        public abstract Task CloseAsync();

        public abstract void Dispose();

        public abstract Task OpenAsync();

        public abstract Task ReceiveAsync();

        public abstract Task SendAsync(byte[] message);
    }
}