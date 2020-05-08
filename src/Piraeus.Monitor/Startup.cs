using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Piraeus.Configuration;
using Piraeus.Extensions.Configuration;
using Piraeus.Extensions.Orleans;
using Piraeus.Monitor.Extensions;
using Piraeus.Monitor.Hubs;

namespace Piraeus.Monitor
{
    public class Startup
    {
        private MonitorConfig config;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration
        {
            get;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            if (!string.IsNullOrEmpty(config.TenantId))
            {
                app.UseCookiePolicy();
                app.UseAuthentication();
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(ac =>
            {
                ac.MapHub<PiSystemHub>("/pisystemHub");
                ac.MapRazorPages();
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMonitorConfiguration(out config);
            services.AddOrleansConfiguration(out OrleansConfig oconfig);
            services.AddSingletonOrleansClusterClient(oconfig);
            services.AddScoped<HubAdapter>();
            services.AddScoped<IMetricStream, MetricStream>();

            if (string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_FORWARDEDHEADERS_ENABLED"),
                "true", StringComparison.OrdinalIgnoreCase))
            {
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                               ForwardedHeaders.XForwardedProto;
                    // Only loopback proxies are allowed by default.
                    // Clear that restriction because forwarders are enabled by explicit
                    // configuration.
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });

                Console.WriteLine("Forward headers IS CONFIGURED");
            }
            else
            {
                Console.WriteLine("Forward headers NOT CONFIGURED");
            }

            if (!string.IsNullOrEmpty(config.TenantId))
            {
                Console.WriteLine($"Using tenantId = {config.TenantId}");
                //----------------------------------------------------------------
                services.Configure<CookiePolicyOptions>(options =>
                {
                    options.CheckConsentNeeded = context => true;
                    options.MinimumSameSitePolicy = SameSiteMode.None;
                });

                services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                    .AddAzureAD(options =>
                    {
                        options.CallbackPath = "/signin-oidc";
                        options.ClientId = config.ClientId;
                        options.Domain = $"{config.Domain}.onmicrosoft.com";
                        options.TenantId = config.TenantId;
                        options.SignedOutCallbackPath = "/signout-oidc";
                        options.Instance = "https://login.microsoftonline.com/";
                    });

                services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
                {
                    options.Authority += "/v2.0/";
                    options.TokenValidationParameters.ValidateIssuer = true;
                    options.TokenValidationParameters.ValidateAudience = true;
                    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    options.TokenValidationParameters.ValidateLifetime = true;
                    options.TokenValidationParameters.ValidateTokenReplay = true;
                    options.TokenValidationParameters.ValidateActor = true;
                });
                //----------------------------------------------------------------
            }

            services.AddSignalR();
            services.AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;

                    if (!string.IsNullOrEmpty(config.TenantId))
                    {
                        var policy = new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .Build();
                        options.Filters.Add(new AuthorizeFilter(policy));
                    }
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddRazorPages();
        }
    }
}