using System;
using Piraeus.Core.Messaging;

namespace Piraeus.Grains
{
    [Serializable]
    public class MetricNotificationEventArgs
    {
        public MetricNotificationEventArgs()
        {
        }

        public MetricNotificationEventArgs(CommunicationMetrics metrics)
        {
            Metrics = metrics;
        }

        public CommunicationMetrics Metrics
        {
            get; internal set;
        }
    }
}