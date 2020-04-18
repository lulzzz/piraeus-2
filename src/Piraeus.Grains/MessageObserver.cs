using System;
using Piraeus.Core.Messaging;
using Piraeus.GrainInterfaces;

namespace Piraeus.Grains
{
    public class MessageObserver : IMessageObserver
    {
        public void Notify(EventMessage message)
        {
            OnNotify?.Invoke(this, new MessageNotificationArgs(message));
        }

        public event EventHandler<MessageNotificationArgs> OnNotify;
    }
}