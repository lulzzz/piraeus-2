using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Piraeus.GrainInterfaces
{
    public interface ISubscriber : IGrainWithStringKey
    {
        Task AddSubscriptionAsync(string subscriptionUriString);

        Task ClearAsync();

        Task<IEnumerable<string>> GetSubscriptionsAsync();

        Task RemoveSubscriptionAsync(string subscriptionUriString);
    }
}