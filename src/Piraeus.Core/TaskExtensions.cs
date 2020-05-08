using System;
using System.Threading.Tasks;
using Piraeus.Core.Logging;

namespace Piraeus.Core
{
    public static class TaskExtensions
    {
        public static void LogExceptions(this Task task)
        {
            task.ContinueWith(t =>
                {
                    var aggException = t.Exception.Flatten();
                    foreach (var exception in aggException.InnerExceptions)
                        Console.WriteLine(exception.Message);
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }

        public static Task LogExceptions(this Task task, ILog log = null)
        {
            return task.ContinueWith(t =>
                {
                    var aggException = t.Exception.Flatten();
                    foreach (var exception in aggException.InnerExceptions)
                        log?.LogErrorAsync(exception, exception.Message);
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}