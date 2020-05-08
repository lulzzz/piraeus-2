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
            lock (lockObj)
            {
                Func<Task, Task> continuationFunction = _ => taskFunc();
                Task task = lastQueuedTask
                    .ContinueWith(continuationFunction, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();
                lastQueuedTask = task;
                return task;
            }
        }
    }
}