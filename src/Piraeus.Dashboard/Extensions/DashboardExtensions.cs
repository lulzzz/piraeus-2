using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piraeus.Dashboard.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Clustering.Redis;
using Orleans.Messaging;
using Piraeus.Configuration;
using System;

namespace Piraeus.Dashboard.Extensions
{
    public static class DashboardExtensions
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection services, out DashboardConfig config)
        {
            var builder = new ConfigurationBuilder()
                    .AddJsonFile("./secrets.json")
                    .AddEnvironmentVariables("DB_");

            IConfigurationRoot root = builder.Build();

            var dbconfig = new DashboardConfig();
            ConfigurationBinder.Bind(root, dbconfig);

            services.AddSingleton<DashboardConfig>(dbconfig);
            config = dbconfig;
            return services;
        }
    }
}
