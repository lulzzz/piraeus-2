﻿using Piraeus.Core.Messaging;
using Piraeus.GrainInterfaces;
using System;

namespace Piraeus.Grains
{
    public class MessageObserver : IMessageObserver
    {
        public MessageObserver()
        {
        }

        public event EventHandler<MessageNotificationArgs> OnNotify;

        public void Notify(EventMessage message)
        {
            OnNotify?.Invoke(this, new MessageNotificationArgs(message));
        }
    }
}