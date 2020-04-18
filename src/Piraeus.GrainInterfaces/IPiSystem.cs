using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;

namespace Piraeus.GrainInterfaces
{
    public interface IPiSystem : IGrainWithStringKey
    {
        [AlwaysInterleave]
        Task<string> AddObserverAsync(TimeSpan lifetime, IMetricObserver observer);

        [AlwaysInterleave]
        Task<string> AddObserverAsync(TimeSpan lifetime, IErrorObserver observer);

        Task ClearAsync();

        [AlwaysInterleave]
        Task<EventMetadata> GetMetadataAsync();

        [AlwaysInterleave]
        Task<CommunicationMetrics> GetMetricsAsync();

        Task<IEnumerable<string>> GetSubscriptionListAsync();

        [AlwaysInterleave]
        Task PublishAsync(EventMessage message);

        [AlwaysInterleave]
        Task PublishAsync(EventMessage message, List<KeyValuePair<string, string>> indexes);

        [AlwaysInterleave]
        Task RemoveObserverAsync(string leaseKey);

        [AlwaysInterleave]
        Task<bool> RenewObserverLeaseAsync(string leaseKey, TimeSpan lifetime);

        [AlwaysInterleave]
        Task SubscribeAsync(ISubscription subscription);

        [AlwaysInterleave]
        Task UnsubscribeAsync(string subscriptionUriString);

        [AlwaysInterleave]
        Task UnsubscribeAsync(string subscriptionUriString, string identity);

        [AlwaysInterleave]
        Task UpsertMetadataAsync(EventMetadata metadata);
    }
}