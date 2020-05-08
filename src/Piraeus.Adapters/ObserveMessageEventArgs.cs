using System;
using Piraeus.Core.Messaging;

namespace Piraeus.Adapters
{
    public class ObserveMessageEventArgs : EventArgs
    {
        public ObserveMessageEventArgs(EventMessage message)
        {
            Message = message;
        }

        public EventMessage Message
        {
            get; internal set;
        }
    }
}