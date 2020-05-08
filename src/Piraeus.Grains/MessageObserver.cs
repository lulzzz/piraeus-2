using System;
using Piraeus.Core.Messaging;
using Piraeus.GrainInterfaces;

namespace Piraeus.Grains
{
    public class MessageObserver : IMessageObserver
    {
        public event EventHandler<MessageNotificationArgs> OnNotify;

        public void Notify(EventMessage message)
        {
            OnNotify?.Invoke(this, new MessageNotificationArgs(message));
        }
    }
}