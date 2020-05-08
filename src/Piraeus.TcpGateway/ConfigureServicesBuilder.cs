using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Piraeus.TcpGateway
{
    public class ConfigureServicesBuilder
    {
        public ConfigureServicesBuilder(MethodInfo configureServices)
        {
            if (configureServices == null)
            {
                throw new ArgumentNullException(nameof(configureServices));
            }

            var parameters = configureServices.GetParameters();
            if (parameters.Length > 1 ||
                parameters.Any(p => p.ParameterType != typeof(IServiceCollection)))
            {
                throw new InvalidOperationException(
                    "ConfigureServices can take at most a single IServiceCollection parameter.");
            }

            MethodInfo = configureServices;
        }

        public MethodInfo MethodInfo
        {
            get;
        }

        public IServiceProvider Build(object instance, IServiceCollection services)
        {
            _ = instance ?? throw new ArgumentNullException(nameof(instance));
            _ = services ?? throw new ArgumentNullException(nameof(services));

            return Invoke(instance, services);
        }

        private IServiceProvider Invoke(object instance, IServiceCollection exportServices)
        {
            var parameters = new object[MethodInfo.GetParameters().Length];

            if (parameters.Length > 0)
            {
                parameters[0] = exportServices;
            }

            IServiceProvider serviceProvider = MethodInfo.Invoke(instance, parameters) as IServiceProvider;
            _ = serviceProvider ??
                throw new InvalidOperationException(
                    "The ConfigureServices method did not returned a configured IServiceProvider instance.");

            return serviceProvider;
        }
    }
}