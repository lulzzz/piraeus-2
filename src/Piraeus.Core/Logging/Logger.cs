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
            await Task.Run(() => logger.LogCritical(msg, args));
        }

        public async Task LogDebugAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            await Task.Run(() => logger.LogDebug(msg, args));
        }

        public async Task LogErrorAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            await Task.Run(() => logger.LogError(msg, args));
        }

        public async Task LogErrorAsync(Exception error, string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            await Task.Run(() => logger.LogError(error, msg, args));
        }

        public async Task LogInformationAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            await Task.Run(() => logger.LogInformation(msg, args));
        }

        public async Task LogTraceAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            await Task.Run(() => logger.LogTrace(msg, args));
        }

        public async Task LogWarningAsync(string message, params object[] args)
        {
            string msg = AppendTimestamp(message);
            await Task.Run(() => logger.LogWarning(msg, args));
        }

        private string AppendTimestamp(string message)
        {
            return $"{message} - {DateTime.Now:dd-MM-yyyyThh:mm:ss.ffff}";
        }
    }
}