using System;
using Piraeus.Core.Messaging;
using Piraeus.GrainInterfaces;

namespace Piraeus.Grains
{
    public class MetricObserver : IMetricObserver
    {
        public void NotifyMetrics(CommunicationMetrics metrics)
        {
            OnNotify?.Invoke(this, new MetricNotificationEventArgs(metrics));
        }

        public event EventHandler<MetricNotificationEventArgs> OnNotify;
    }
}