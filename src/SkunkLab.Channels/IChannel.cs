using System;
using System.Threading.Tasks;

namespace SkunkLab.Channels
{
    public interface IChannel : IDisposable
    {
        event EventHandler<ChannelCloseEventArgs> OnClose;

        event EventHandler<ChannelErrorEventArgs> OnError;

        event EventHandler<ChannelOpenEventArgs> OnOpen;

        event EventHandler<ChannelReceivedEventArgs> OnReceive;

        event EventHandler<ChannelStateEventArgs> OnStateChange;

        string Id
        {
            get;
        }

        bool IsAuthenticated
        {
            get;
        }

        bool IsConnected
        {
            get;
        }

        bool IsEncrypted
        {
            get;
        }

        int Port
        {
            get;
        }

        bool RequireBlocking
        {
            get;
        }

        ChannelState State
        {
            get;
        }

        string TypeId
        {
            get;
        }

        Task AddMessageAsync(byte[] message);

        Task CloseAsync();

        Task OpenAsync();

        Task ReceiveAsync();

        Task SendAsync(byte[] message);
    }
}