using Piraeus.Core.Metadata;
using System;
using System.Collections.Generic;

namespace Piraeus.GrainInterfaces
{
    [Serializable]
    public class PiSystemState
    {
        public Dictionary<string, ISubscription> Subscriptions;

        public PiSystemState()
        {
        }

        #region Metrics

        public long ByteCount { get; set; }
        public long ErrorCount { get; set; }
        public DateTime? LastErrorTimestamp { get; set; }
        public DateTime? LastMessageTimestamp { get; set; }
        public long MessageCount { get; set; }

        #endregion Metrics

        public Dictionary<string, IErrorObserver> ErrorLeases { get; set; }
        public Dictionary<string, Tuple<DateTime, string>> LeaseExpiry { get; set; }
        public EventMetadata Metadata { get; set; }
        public Dictionary<string, IMetricObserver> MetricLeases { get; set; }
    }
}