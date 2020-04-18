using Microsoft.Extensions.Configuration;
using Piraeus.Configuration;

namespace Piraeus.WebApi
{
    public class WebApiHelpers
    {
        internal static OrleansConfig GetOrleansConfig()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("./orleansconfig.json")
                .AddEnvironmentVariables("OR_");

            IConfigurationRoot root = builder.Build();
            OrleansConfig config = new OrleansConfig();
            root.Bind(config);

            return config;
        }

        internal static PiraeusConfig GetPiraeusConfig()
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