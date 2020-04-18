using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;

namespace Orleans.Storage.Redis
{
    public static class RedisStorageExtensions
    {
        public const string DEFAULT_STORAGE_PROVIDER_NAME = "Default";

        public static ISiloHostBuilder AddRedisGrainStorage(this ISiloHostBuilder builder, string name, ILogger logger,
            Action<RedisStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.AddRedisGrainStorage(name, logger, configureOptions));
        }

        public static ISiloHostBuilder AddRedisGrainStorage(this ISiloHostBuilder builder, string name,
            Action<RedisStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.AddRedisGrainStorage(name, configureOptions));
        }

        public static IServiceCollection AddRedisGrainStorage(this IServiceCollection services, string name,
            ILogger logger, Action<RedisStorageOptions> configureOptions)
        {
            return services.AddRedisGrainStorage(name, logger, ob => ob.Configure(configureOptions));
        }

        public static IServiceCollection AddRedisGrainStorage(this IServiceCollection services, string name,
            Action<RedisStorageOptions> configureOptions)
        {
            return services.AddRedisGrainStorage(name, ob => ob.Configure(configureOptions));
        }

        public static IServiceCollection AddRedisGrainStorage(this IServiceCollection services, string name,
            ILogger logger,
            Action<OptionsBuilder<RedisStorageOptions>> configureOptions = null)
        {
            configureOptions?.Invoke(services.AddOptions<RedisStorageOptions>(name));
            services.ConfigureNamedOptionForLogging<RedisStorageOptions>(name);
            services.TryAddSingleton(sp => sp.GetServiceByName<IGrainStorage>(DEFAULT_STORAGE_PROVIDER_NAME));
            services.TryAddSingleton(logger);
            return services.AddSingletonNamedService(name, RedisGrainStorageFactory.Create)
                .AddSingletonNamedService(name,
                    (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));
        }

        public static IServiceCollection AddRedisGrainStorage(this IServiceCollection services, string name,
            Action<OptionsBuilder<RedisStorageOptions>> configureOptions = null)
        {
            configureOptions?.Invoke(services.AddOptions<RedisStorageOptions>(name));
            services.ConfigureNamedOptionForLogging<RedisStorageOptions>(name);
            services.TryAddSingleton(sp => sp.GetServiceByName<IGrainStorage>(DEFAULT_STORAGE_PROVIDER_NAME));
            return services.AddSingletonNamedService(name, RedisGrainStorageFactory.Create)
                .AddSingletonNamedService(name,
                    (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));
        }
    }
}