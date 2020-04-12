﻿using Piraeus.Core.Messaging;
using Piraeus.GrainInterfaces;
using System;

namespace Piraeus.Grains
{
    public class MetricObserver : IMetricObserver
    {
        public MetricObserver()
        {
        }

        public event EventHandler<MetricNotificationEventArgs> OnNotify;

        public void NotifyMetrics(CommunicationMetrics metrics)
        {
            OnNotify?.Invoke(this, new MetricNotificationEventArgs(metrics));
        }
    }
}