using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Piraeus.Core.Logging
{
    public class Logger : ILog
    {
        private readonly ILogger<Logger> logger;

        public Logger(ILogger<Logger> logger)
        {
            this.logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return logger.BeginScope<TState>(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log<TState>(logLevel, eventId, state, exception, formatter);
        }

        public async Task LogCriticalAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = new Action(() => logger.LogCritical(msg, args));
            await Task.Run(action);
        }

        public async Task LogDebugAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = new Action(() => logger.LogDebug(msg, args));
            await Task.Run(action);
        }

        public async Task LogErrorAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = new Action(() => logger.LogError(msg, args));
            await Task.Run(action);
        }

        public async Task LogErrorAsync(Exception error, string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = new Action(() => logger.LogError(error, msg, args));
            await Task.Run(action);
        }

        public async Task LogInformationAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = new Action(() => logger.LogInformation(msg, args));
            await Task.Run(action);
        }

        public async Task LogTraceAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = new Action(() => logger.LogTrace(msg, args));
            await Task.Run(action);
        }

        public async Task LogWarningAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = new Action(() => logger.LogWarning(msg, args));
            await Task.Run(action);
        }

        private string AppendTimestamp(string message)
        {
            return $"{message} - {DateTime.Now:dd-MM-yyyyThh:mm:ss.ffff}";
        }
    }
}