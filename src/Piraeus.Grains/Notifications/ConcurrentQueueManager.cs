using System.Collections.Concurrent;
using System.Threading.Tasks;
using Piraeus.Core.Messaging;

namespace Piraeus.Grains.Notifications
{
    public class ConcurrentQueueManager
    {
        private readonly ConcurrentQueue<EventMessage> queue;

        public ConcurrentQueueManager()
        {
            queue = new ConcurrentQueue<EventMessage>();
        }

        public bool IsEmpty => queue.IsEmpty;

        public Task<EventMessage> DequeueAsync()
        {
            TaskCompletionSource<EventMessage> tcs = new TaskCompletionSource<EventMessage>();
            if (!queue.IsEmpty)
            {
                bool result = queue.TryDequeue(out EventMessage message);

                tcs.SetResult(result ? message : null);
            }

            return tcs.Task;
        }

        public Task EnqueueAsync(EventMessage message)
        {
            TaskCompletionSource<Task> tcs = new TaskCompletionSource<Task>();
            queue.Enqueue(message);
            tcs.SetResult(null);
            return tcs.Task;
        }
    }
}