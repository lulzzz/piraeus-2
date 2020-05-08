using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Piraeus.Configuration;
using Piraeus.Core.Logging;
using Piraeus.Extensions.Configuration;

namespace Piraeus.SiloHost.Core
{
    public static class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(builder =>
                {
                    OrleansConfig orleansConfig = GetOrleansConfiguration();
                    LogLevel orleansLogLevel = Enum.Parse<LogLevel>(orleansConfig.LogLevel, true);
                    LoggerType loggers = orleansConfig.GetLoggerTypes();

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
                    builder.Services.AddSingleton<ILog, Logger>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOrleansConfiguration();
                    services.AddSingleton<Logger>(); //add the logger
                    services.AddHostedService<SiloHostService>(); //start the silo host
                });
        }

        private static OrleansConfig GetOrleansConfiguration()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("./orleansconfig.json")
                .AddEnvironmentVariables("OR_");
            IConfigurationRoot root = builder.Build();
            OrleansConfig config = new OrleansConfig();
            root.Bind(config);
            return config;
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("  ******** **  **            **      **                    **");
            Console.WriteLine(" **////// //  /**           /**     /**                   /**");
            Console.WriteLine("/**        ** /**  ******   /**     /**  ******   ****** ******");
            Console.WriteLine("/*********/** /** **////**  /********** **////** **//// ///**/");
            Console.WriteLine("////////**/** /**/**   /**  /**//////**/**   /**//*****   /**");
            Console.WriteLine("       /**/** /**/**   /**  /**     /**/**   /** /////**  /**");
            Console.WriteLine(" ******** /** ***//******   /**     /**//******  ******   //**");
            Console.WriteLine("////////  // ///  //////    //      //  //////  //////     //");
            Console.WriteLine("");

            CreateHostBuilder(args).Build().Run();
        }
    }
}