using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using System.Threading.Tasks;

namespace Piraeus.Grains.Notifications
{
    public abstract class EventSink
    {
        protected ILog logger;

        protected SubscriptionMetadata metadata;

        protected EventSink(SubscriptionMetadata metadata, ILog logger = null)
        {
            this.metadata = metadata;
            this.logger = logger;
        }

        public event System.EventHandler<EventSinkResponseArgs> OnResponse;

        public abstract Task SendAsync(EventMessage message);

        protected virtual void RaiseOnResponse(EventSinkResponseArgs e)
        {
            System.EventHandler<EventSinkResponseArgs> handler = OnResponse;
            handler?.Invoke(this, e);
        }
    }
}