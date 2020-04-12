using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Piraeus.Monitor.Extensions
{
    public static class MonitorExtensions
    {
        public static IServiceCollection AddMonitorConfiguration(this IServiceCollection services, out MonitorConfig config)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile("./secrets.json")
                .AddEnvironmentVariables("PM_");
            IConfigurationRoot root = builder.Build();
            config = new MonitorConfig();
            ConfigurationBinder.Bind(root, config);
            services.AddSingleton<MonitorConfig>(config);

            return services;
        }
    }
}