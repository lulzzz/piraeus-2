﻿using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using System.Threading.Tasks;

namespace Piraeus.Grains.Notifications
{
    public abstract class EventSink
    {
        protected SubscriptionMetadata metadata;

        protected EventSink(SubscriptionMetadata metadata)
        {
            this.metadata = metadata;
        }

        public abstract Task SendAsync(EventMessage message);
    }
}