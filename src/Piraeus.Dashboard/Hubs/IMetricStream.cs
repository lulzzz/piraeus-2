using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Piraeus.Dashboard.Hubs
{
    public interface IMetricStream
    {
        Task SubscribeAsync(string resourceUriString);
        Task UnsubscribeAsync(string resourceUriString);
    }
}
