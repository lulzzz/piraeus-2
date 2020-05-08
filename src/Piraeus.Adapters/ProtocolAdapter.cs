using System;
using SkunkLab.Channels;

namespace Piraeus.Adapters
{
    public abstract class ProtocolAdapter : IDisposable
    {
        public abstract event EventHandler<ProtocolAdapterCloseEventArgs> OnClose;

        public abstract event EventHandler<ProtocolAdapterErrorEventArgs> OnError;

        public abstract event EventHandler<ChannelObserverEventArgs> OnObserve;

        public abstract IChannel Channel
        {
            get; set;
        }

        public abstract void Dispose();

        public abstract void Init();
    }
}