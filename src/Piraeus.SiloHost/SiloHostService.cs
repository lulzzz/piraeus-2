using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Clustering.Redis;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Storage.Redis;
using Piraeus.Configuration;
using Piraeus.Core.Logging;

namespace Piraeus.SiloHost
{
    public class SiloHostService : IHostedService
    {
        private readonly OrleansConfig orleansConfig;

        private ISiloHost host;

        public SiloHostService(OrleansConfig orleansConfig)
        {
            this.orleansConfig = orleansConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
#if DEBUG
            host = AddLocalSiloHost();
#else
            host = AddClusteredSiloHost();
#endif

            await host.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (host != null)
            {
                await host.StopAsync(cancellationToken);
            }
        }

        private ISiloHost AddLocalSiloHost()
        {
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = orleansConfig.ClusterId;
                    options.ServiceId = orleansConfig.ServiceId;
                })
                .AddMemoryGrainStorage("store")
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.Services.TryAddSingleton<ILog, Logger>();
                });

            return builder.Build();
        }

        private ISiloHost AddClusteredSiloHost()
        {
            var silo = new SiloHostBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = orleansConfig.ClusterId;
                    options.ServiceId = orleansConfig.ServiceId;
                });

            if (string.IsNullOrEmpty(orleansConfig.DataConnectionString))
            {
                silo.AddMemoryGrainStorage("store");
            }
            else if (orleansConfig.DataConnectionString.Contains("6379") ||
                     orleansConfig.DataConnectionString.Contains("6380"))
            {
                silo.UseRedisMembership(options => options.ConnectionString = orleansConfig.DataConnectionString);
                silo.AddRedisGrainStorage("store",
                    options => options.ConnectionString = orleansConfig.DataConnectionString);
            }
            else
            {
                silo.UseAzureStorageClustering(options =>
                    options.ConnectionString = orleansConfig.DataConnectionString);
                silo.AddAzureBlobGrainStorage("store",
                    options => options.ConnectionString = orleansConfig.DataConnectionString);
            }

            silo.ConfigureEndpoints(11111, 30000);

            LogLevel orleansLogLevel = Enum.Parse<LogLevel>(orleansConfig.LogLevel);
            var loggers = orleansConfig.GetLoggerTypes();
            silo.ConfigureLogging(builder =>
            {
                if (loggers.HasFlag(LoggerType.Console))
                {
                    builder.AddConsole();
                }

                if (loggers.HasFlag(LoggerType.Debug))
                {
                    builder.AddDebug();
                }

                if (loggers.HasFlag(LoggerType.AppInsights) &&
                    !string.IsNullOrEmpty(orleansConfig.InstrumentationKey))
                {
                    builder.AddApplicationInsights(orleansConfig.InstrumentationKey);
                }

                builder.SetMinimumLevel(orleansLogLevel);
                builder.Services.TryAddSingleton<ILog, Logger>();
            });

            if (!string.IsNullOrEmpty(orleansConfig.InstrumentationKey))
            {
                silo.AddApplicationInsightsTelemetryConsumer(orleansConfig.InstrumentationKey);
            }

            return silo.Build();
        }
    }
}