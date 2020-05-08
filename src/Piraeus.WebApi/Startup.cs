using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Piraeus.Configuration;
using Piraeus.Core.Logging;
using Piraeus.Extensions.Configuration;
using Piraeus.Extensions.Orleans;
using SkunkLab.Storage;

namespace Piraeus.WebApi
{
    public class Startup
    {
        private readonly PiraeusConfig pconfig;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            pconfig = WebApiHelpers.GetPiraeusConfig();
        }

        public IConfiguration Configuration
        {
            get;
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("Manage", "{controller=Manage}/{id}");
                endpoints.MapControllerRoute("AccessControl", "accesscontrol/{controller=AccessControl}/{action}");
                endpoints.MapControllerRoute("Resource", "resource/{controller=Resource}/{action}");
                endpoints.MapControllerRoute("Subscription", "subscription/{controller=Subscription}/{action}");
                endpoints.MapControllerRoute("Psk", "psk/{controller=Psk}/{action}");
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddXmlSerializerFormatters();

            services.AddMvc(option => option.EnableEndpointRouting = false);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = pconfig.ManagementApiIssuer,
                        ValidAudience = pconfig.ManagementApiAudience,
                        ClockSkew = TimeSpan.FromMinutes(5.0),
                        IssuerSigningKey =
                            new SymmetricSecurityKey(Convert.FromBase64String(pconfig.ManagmentApiSymmetricKey))
                    };
                });

            PskStorageAdapter pskAdpater = GetPskAdapter();

            if (pskAdpater != null)
            {
                services.AddSingleton(pskAdpater);
            }

            services.AddPiraeusConfiguration();
            services.AddOrleansConfiguration();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<ILog, Logger>();
            services.AddLogging(log =>
            {
                log.AddConsole();
                log.SetMinimumLevel(Enum.Parse<LogLevel>(pconfig.LogLevel));
            });

            if (!string.IsNullOrEmpty(pconfig.InstrumentationKey))
            {
                services.AddApplicationInsightsTelemetry(pconfig.InstrumentationKey);
            }

            services.AddSingletonOrleansClusterClient(WebApiHelpers.GetOrleansConfig());
            services.AddRouting();
        }

        private PskStorageAdapter GetPskAdapter()
        {
            if (!string.IsNullOrEmpty(pconfig.PskRedisConnectionString))
            {
                return PskStorageAdapterFactory.Create(pconfig.PskRedisConnectionString);
            }

            if (!string.IsNullOrEmpty(pconfig.PskKeyVaultClientSecret) &&
                !string.IsNullOrEmpty(pconfig.PskKeyVaultClientId) &&
                !string.IsNullOrEmpty(pconfig.PskKeyVaultAuthority))
            {
                return PskStorageAdapterFactory.Create(pconfig.PskKeyVaultAuthority, pconfig.PskKeyVaultClientId,
                    pconfig.PskKeyVaultClientSecret);
            }

            if (!string.IsNullOrEmpty(pconfig.PskKeys))
            {
                return PskStorageAdapterFactory.Create(pconfig.PskIdentities, pconfig.PskKeys);
            }

            return null;
        }
    }
}