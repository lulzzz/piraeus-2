using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Clustering.Redis;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Storage.Redis;
using Piraeus.Configuration;
using System;
using System.Net;

namespace Piraeus.SiloHost
{
    public class Host
    {
        private OrleansConfig orleansConfig;

        private ISiloHost host;

        public Host()
        {
        }

        public void Init()
        {
            orleansConfig = GetOrleansConfiguration();

#if DEBUG
            CreateClusteredSiloHost();
#else
            CreateLocalSiloHost();
#endif
            host.StartAsync().GetAwaiter();
        }

        private void CreateLocalSiloHost()
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

            .ConfigureLogging(logging => logging.AddConsole());

            host = builder.Build();
        }

        private void CreateClusteredSiloHost()
        {
            var silo = new SiloHostBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = orleansConfig.ClusterId;
                    options.ServiceId = orleansConfig.ServiceId;
                });

            string storageType = GetStorageType(orleansConfig.DataConnectionString);
            if (storageType.ToLowerInvariant() == "redis")
            {
                silo.UseRedisMembership(options => options.ConnectionString = orleansConfig.DataConnectionString);
                silo.AddRedisGrainStorage("store", options => options.ConnectionString = orleansConfig.DataConnectionString);
            }
            else if (storageType.ToLowerInvariant() == "azurestorage")
            {
                silo.UseAzureStorageClustering(options => options.ConnectionString = orleansConfig.DataConnectionString);
                silo.AddAzureBlobGrainStorage("store", options => options.ConnectionString = orleansConfig.DataConnectionString);
            }
            else
            {
            }

            silo.ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000);

            LogLevel orleansLogLevel = GetLogLevel();
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

                builder.SetMinimumLevel(orleansLogLevel);
            });

            if (loggers.HasFlag(LoggerType.AppInsights) && !string.IsNullOrEmpty(orleansConfig.InstrumentationKey))
            {
                silo.AddApplicationInsightsTelemetryConsumer(orleansConfig.InstrumentationKey);
            }
            else
            {
                if (!string.IsNullOrEmpty(orleansConfig.InstrumentationKey))
                {
                    silo.AddApplicationInsightsTelemetryConsumer(orleansConfig.InstrumentationKey);
                }
            }
            host = silo.Build();
        }

        private LogLevel GetLogLevel()
        {
            return Enum.Parse<LogLevel>(orleansConfig.LogLevel, true);
        }

        private string GetStorageType(string connectionString)
        {
            string cs = connectionString.ToLowerInvariant();
            if (cs.Contains(":6380") || cs.Contains(":6379"))
            {
                return "Redis";
            }
            else if (cs.Contains("defaultendpointsprotocol="))
            {
                return "AzureStorage";
            }
            else
            {
                throw new ArgumentException("Invalid connection string");
            }
        }

        private OrleansConfig GetOrleansConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("./orleansconfig.json")
                .AddEnvironmentVariables("OR_");
            IConfigurationRoot root = builder.Build();
            OrleansConfig config = new OrleansConfig();
            root.Bind(config);
            return config;
        }
    }
}