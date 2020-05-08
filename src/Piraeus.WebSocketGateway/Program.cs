using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Piraeus.Configuration;
using Piraeus.Core.Logging;

namespace Piraeus.WebSocketGateway
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(builder =>
                {
                    PiraeusConfig pconfig = GetPiraeusConfig();
                    LogLevel logLevel = Enum.Parse<LogLevel>(pconfig.LogLevel);
                    var loggers = pconfig.GetLoggerTypes();

                    if (loggers.HasFlag(LoggerType.Console))
                    {
                        builder.AddConsole();
                    }

                    if (loggers.HasFlag(LoggerType.Debug))
                    {
                        builder.AddDebug();
                    }

                    if (loggers.HasFlag(LoggerType.AppInsights) && !string.IsNullOrEmpty(pconfig.InstrumentationKey))
                    {
                        builder.AddApplicationInsights(pconfig.InstrumentationKey);
                    }

                    builder.SetMinimumLevel(logLevel);
                    builder.Services.AddSingleton<ILog, Logger>();
                })
                .ConfigureWebHost(options =>
                {
                    options.UseStartup<Startup>();
                    options.UseKestrel();
                    options.ConfigureKestrel(options =>
                    {
                        PiraeusConfig config = GetPiraeusConfig();
                        options.Limits.MaxConcurrentConnections = config.MaxConnections;
                        options.Limits.MaxConcurrentUpgradedConnections = config.MaxConnections;
                        options.Limits.MaxRequestBodySize = config.MaxBufferSize;
                        options.Limits.MinRequestBodyDataRate =
                            new MinDataRate(100, TimeSpan.FromSeconds(10));
                        options.Limits.MinResponseDataRate =
                            new MinDataRate(100, TimeSpan.FromSeconds(10));

                        if (!string.IsNullOrEmpty(config.ServerCertificateFilename))
                        {
                            Console.WriteLine("Port for cert with filename");
                            options.ListenAnyIP(config.GetPorts()[0],
                                a => a.UseHttps(config.ServerCertificateFilename, config.ServerCertificatePassword));
                        }
                        else if (!string.IsNullOrEmpty(config.ServerCertificateStore))
                        {
                            Console.WriteLine("Port for cert with store");
                            X509Certificate2 cert = config.GetServerCerticate();
                            options.ListenAnyIP(config.GetPorts()[0], a => a.UseHttps(cert));
                        }
                        else
                        {
                            Console.WriteLine("Hard coded port 8081");
                            options.ListenAnyIP(8081);
                        }
                    });
                });
        }

        private static PiraeusConfig GetPiraeusConfig()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("./piraeusconfig.json")
                .AddEnvironmentVariables("PI_");

            IConfigurationRoot root = builder.Build();
            PiraeusConfig config = new PiraeusConfig();
            root.Bind(config);

            return config;
        }
    }
}