using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piraeus.Extensions.Configuration;
using System;

namespace Piraeus.SiloHost
{
    public class Startup
    {
        private Host host;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddOrleansConfiguration(out _);
            ServiceDescriptor sd = new ServiceDescriptor(typeof(Host), new Host());
            services.Add(sd);

            IServiceProvider sp = services.BuildServiceProvider();

            host = sp.GetRequiredService<Host>();
            host.Init();
        }
    }
}