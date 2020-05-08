using System;
using Piraeus.Core.Messaging;

namespace Piraeus.Grains.Notifications
{
    public class EventSinkResponseArgs : EventArgs
    {
        public EventSinkResponseArgs(EventMessage message)
        {
            Message = message;
        }

        public EventMessage Message
        {
            get; internal set;
        }
    }
}