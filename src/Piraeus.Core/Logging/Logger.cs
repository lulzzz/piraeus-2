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
            return logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            logger.Log(logLevel, eventId, state, exception, formatter);
        }

        public async Task LogCriticalAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = () => logger.LogCritical(msg, args);
            await Task.Run(action);
        }

        public async Task LogDebugAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = () => logger.LogDebug(msg, args);
            await Task.Run(action);
        }

        public async Task LogErrorAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = () => logger.LogError(msg, args);
            await Task.Run(action);
        }

        public async Task LogErrorAsync(Exception error, string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = () => logger.LogError(error, msg, args);
            await Task.Run(action);
        }

        public async Task LogInformationAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = () => logger.LogInformation(msg, args);
            await Task.Run(action);
        }

        public async Task LogTraceAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = () => logger.LogTrace(msg, args);
            await Task.Run(action);
        }

        public async Task LogWarningAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            Action action = () => logger.LogWarning(msg, args);
            await Task.Run(action);
        }

        private string AppendTimestamp(string message)
        {
            return $"{message} - {DateTime.Now:dd-MM-yyyyThh:mm:ss.ffff}";
        }
    }
}