using System;
using System.Threading.Tasks;

namespace Piraeus.Grains.Notifications
{
    internal sealed class TaskQueue
    {
        private readonly object lockObj = new object();

        private Task lastQueuedTask = Task.FromResult(0);

        public Task Enqueue(Func<Task> taskFunc)
        {
            Func<Task, Task> continuationFunction = null;
            lock (lockObj) {
                continuationFunction = _ => taskFunc();
                Task task = lastQueuedTask
                    .ContinueWith(continuationFunction, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();
                lastQueuedTask = task;
                return task;
            }
        }
    }
}