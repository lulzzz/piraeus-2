using System;
using Piraeus.Core.Messaging;

namespace Piraeus.Monitor.Hubs
{
    public class NotificationEventArgs : EventArgs
    {
        public NotificationEventArgs(CommunicationMetrics metrics)
        {
            Metrics = metrics;
        }

        public CommunicationMetrics Metrics
        {
            get; internal set;
        }
    }
}