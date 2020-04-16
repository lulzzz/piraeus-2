using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Piraeus.Configuration;
using Piraeus.Core.Logging;
using Piraeus.Extensions.Configuration;
using System;

namespace Piraeus.SiloHost.Core
{
    public class Program
    {
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

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                    .ConfigureLogging((builder) =>
                    {
                        var orleansConfig = GetOrleansConfiguration();
                        LogLevel orleansLogLevel = Enum.Parse<LogLevel>(orleansConfig.LogLevel, true);
                        var loggers = orleansConfig.GetLoggerTypes();

                        if (loggers.HasFlag(LoggerType.Console))
                            builder.AddConsole();

                        if (loggers.HasFlag(LoggerType.Debug))
                            builder.AddDebug();

                        if (loggers.HasFlag(LoggerType.AppInsights) && !string.IsNullOrEmpty(orleansConfig.InstrumentationKey))
                            builder.AddApplicationInsights(orleansConfig.InstrumentationKey);

                        builder.SetMinimumLevel(orleansLogLevel);
                        builder.Services.AddSingleton<ILog, Logger>();

                    })
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddOrleansConfiguration();
                        services.AddSingleton<Logger>();    //add the logger
                        services.AddHostedService<SiloHostService>(); //start the silo host
                    });
        }

        private static OrleansConfig GetOrleansConfiguration()
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