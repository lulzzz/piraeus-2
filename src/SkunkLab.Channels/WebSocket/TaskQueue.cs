using System;
using System.Threading.Tasks;

namespace SkunkLab.Channels.WebSocket
{
    internal sealed class TaskQueue
    {
        private readonly object lockObj = new object();

        private Task lastQueuedTask = Task.FromResult(0);

        public Task Enqueue(Func<Task> taskFunc)
        {
            Func<Task, Task> continuationFunction = null;
            lock (lockObj)
            {
                if (continuationFunction == null)
                {
                    continuationFunction = _ => taskFunc();
                }

                Task task = lastQueuedTask
                    .ContinueWith(continuationFunction, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();
                lastQueuedTask = task;
                return task;
            }
        }
    }
}