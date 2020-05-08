using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Piraeus.Configuration;
using Piraeus.Core.Logging;
using Piraeus.Extensions.Configuration;
using Piraeus.Extensions.Logging;

namespace Piraeus.TcpGateway
{
    internal class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddPiraeusConfiguration(out PiraeusConfig config);
                    if (!string.IsNullOrEmpty(config.InstrumentationKey))
                    {
                        services.AddApplicationInsightsTelemetry(op =>
                        {
                            op.InstrumentationKey = config.InstrumentationKey;
                            op.AddAutoCollectedMetricExtractor = true;
                            op.EnableHeartbeat = true;
                        });
                    }

                    services.AddOrleansConfiguration();
                    services.AddLogging(builder => builder.AddLogging(config));
                    services.AddSingleton<Logger>();
                    services.AddHostedService<TcpGatewayHost>();
                });
        }

        private static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
    }
}