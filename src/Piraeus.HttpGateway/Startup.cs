using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Piraeus.Configuration;
using Piraeus.Core.Logging;
using Piraeus.Extensions.Configuration;
using Piraeus.Extensions.Logging;
using Piraeus.Extensions.Orleans;
using Piraeus.HttpGateway.Formatters;
using Piraeus.HttpGateway.Middleware;

namespace Piraeus.HttpGateway
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseRouting();

            app.UseAuthentication();
            //app.UseMiddleware<PiraeusHttpMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("Connect", "{controller=Connect}/{id}");
                //endpoints.MapControllerRoute("AccessControl", "accesscontrol/{controller=AccessControl}/{action}");
                //endpoints.MapControllerRoute("Resource", "resource/{controller=Resource}/{action}");
                //endpoints.MapControllerRoute("Subscription", "subscription/{controller=Subscription}/{action}");
                //endpoints.MapControllerRoute("Psk", "psk/{controller=Psk}/{action}");
            });

            //app.UseMvc();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //PiraeusConfig config;
            //OrleansConfig orleansConfig;
            services.AddPiraeusConfiguration(out PiraeusConfig config);
            services.AddOrleansConfiguration(out OrleansConfig orleansConfig);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingletonOrleansClusterClient(orleansConfig);
            LoggerType loggers = config.GetLoggerTypes();

            if (loggers.HasFlag(LoggerType.AppInsights))
            {
                services.AddApplicationInsightsTelemetry(op =>
                {
                    op.InstrumentationKey = config.InstrumentationKey;
                    op.AddAutoCollectedMetricExtractor = true;
                    op.EnableHeartbeat = true;
                });
            }

            services.AddLogging(builder => builder.AddLogging(config));
            services.AddSingleton<Logger>();
            services.AddTransient<PiraeusHttpMiddleware>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = !string.IsNullOrEmpty(config.ClientIssuer),
                        ValidateAudience = !string.IsNullOrEmpty(config.ClientAudience),
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = config.ClientIssuer,
                        ValidAudience = config.ClientAudience,
                        ClockSkew = TimeSpan.FromMinutes(5.0),
                        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(config.ClientSymmetricKey))
                    };
                });

            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
                options.InputFormatters.Add(new BinaryInputFormatter());
                options.InputFormatters.Add(new PlainTextInputFormatter());
                options.InputFormatters.Add(new XmlSerializerInputFormatter(options));
                options.OutputFormatters.Add(new BinaryOutputFormatter());
                options.OutputFormatters.Add(new PlainTextOutputFormatter());
                options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
            });
            services.AddRouting();
            services.AddMvcCore();
        }
    }
}