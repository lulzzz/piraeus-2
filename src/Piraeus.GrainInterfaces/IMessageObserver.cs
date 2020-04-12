using Orleans;
using Piraeus.Core.Messaging;

namespace Piraeus.GrainInterfaces
{
    public interface IMessageObserver : IGrainObserver
    {
        void Notify(EventMessage message);
    }
}