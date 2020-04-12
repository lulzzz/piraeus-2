using System.Threading.Tasks;

namespace Piraeus.Monitor.Hubs
{
    public interface IMetricStream
    {
        Task SubscribeAsync(string resourceUriString);

        Task UnsubscribeAsync(string resourceUriString);
    }
}