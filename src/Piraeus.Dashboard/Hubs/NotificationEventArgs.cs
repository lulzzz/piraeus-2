using Piraeus.Core.Messaging;
using System;

namespace Piraeus.Dashboard.Hubs
{
    public class NotificationEventArgs : EventArgs
    {
        public NotificationEventArgs(CommunicationMetrics metrics)
        {
            Metrics = metrics;
        }

        public CommunicationMetrics Metrics { get; internal set; }
    }
}
