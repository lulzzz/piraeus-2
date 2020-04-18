namespace Piraeus.Grains.Notifications
{
    using System;
    using System.Threading.Tasks;

    internal sealed class TaskQueue
    {
        private readonly object lockObj = new object();

        private Task lastQueuedTask = Task.FromResult<int>(0);

        public Task Enqueue(Func<Task> taskFunc)
        {
            Func<Task, Task> continuationFunction = null;
            lock (this.lockObj)
            {
                continuationFunction = _ => taskFunc();
                Task task = this.lastQueuedTask.ContinueWith<Task>(continuationFunction, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();
                this.lastQueuedTask = task;
                return task;
            }
        }
    }
}