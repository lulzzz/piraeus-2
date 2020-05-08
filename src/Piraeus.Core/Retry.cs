using System;
using System.Threading.Tasks;

namespace Piraeus.Core
{
    public static class Retry
    {
        public static void Execute(Action retryOperation)
        {
            Execute(retryOperation, TimeSpan.FromMilliseconds(250), 3);
        }

        public static void Execute(Action retryOperation, TimeSpan deltaBackoff, int maxRetries)
        {
            int delayMilliseconds = Convert.ToInt32(deltaBackoff.TotalMilliseconds);
            if (maxRetries < 1)
            {
                throw new ArgumentOutOfRangeException("Retry maxRetries must be >= 1.");
            }

            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    retryOperation();
                    return;
                }
                catch
                {
                    if (attempt == maxRetries)
                    {
                        throw;
                    }

                    Task.Delay(delayMilliseconds).GetAwaiter();
                    attempt++;
                }
            }

            throw new OperationCanceledException("Operation cancelled due to retry failure.");
        }

        public static async Task ExecuteAsync(Action retryOperation)
        {
            await ExecuteAsync(retryOperation, TimeSpan.FromMilliseconds(250), 3);
        }

        public static async Task ExecuteAsync(Action retryOperation, TimeSpan deltaBackoff, int maxRetries)
        {
            int delayMilliseconds = Convert.ToInt32(deltaBackoff.TotalMilliseconds);

            if (maxRetries < 1)
            {
                throw new ArgumentOutOfRangeException("Retry maxRetries must be >= 1.");
            }

            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    await Task.Run(retryOperation);
                    break;
                }
                catch
                {
                    if (attempt == maxRetries)
                    {
                        throw;
                    }

                    await Task.Delay(delayMilliseconds);
                    attempt++;
                }
            }

            throw new OperationCanceledException("Operation cancelled due to retry failure.");
        }
    }
}