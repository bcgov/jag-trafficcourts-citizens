using DisputeApi.Web.Features.Disputes.Configuration;
using DisputeApi.Web.Health;
using DisputeApi.Web.Infrastructure;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using NSwag;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using DisputeApi.Web.Features.Tickets.Configuration;

namespace DisputeApi.Web
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private IConfiguration _configuration { get; }

        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

        public void ConfigureServices(IServiceCollection services)
        {
            // TODO - For now, prevent authentication this way
            if (!_env.IsDevelopment())
            {
                services.AddMvc(o =>
                {
#if USE_AUTHENTICATION
                    o.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
#endif
                }).AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                });
            }

            services.AddDbContext<ViolationContext>(opt => opt.UseInMemoryDatabase("DisputeApi"));
            services.AddControllers();
            ConfigureOpenApi(services);

#if USE_AUTHENTICATION
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(ConfigureJwtBearerAuthentication);

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .Build();
            });
#endif
            services.AddHealthChecks().AddCheck<DisputeApiHealthCheck>("service_health_check", failureStatus: HealthStatus.Degraded);
            services.AddTicketService();
            services.AddDisputeService();
        }

        internal void ConfigureJwtBearerAuthentication(JwtBearerOptions o)
        {
            string authority = _configuration["Jwt:Authority"];
            string audience = _configuration["Jwt:Audience"];
            if (string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(authority))
            {
                throw new ConfigurationErrorsException("One or more required configuration parameters are missing Jwt:Audience or Jwt:Authority");
            }
            if (!authority.EndsWith("/", StringComparison.InvariantCulture))
            {
                authority += "/";
            }
            string metadataAddress = authority + ".well-known/uma2-configuration";
            o.Authority = authority;
            o.Audience = audience;
            o.MetadataAddress = metadataAddress;
            if (_env.IsDevelopment())
            {
                o.RequireHttpsMetadata = false;
            }

            o.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = c =>
                {
                    c.NoResult();
                    c.Response.StatusCode = 401;
                    c.Response.ContentType = "text/plain";
                    return c.Response.WriteAsync("An error occurred processing your authentication.");
                }
            };
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Serilog middleware will not time or log components that appear before it in the pipeline
            // This can be utilized to exclude noisy handlers from logging, such as UseStaticFiles(), by placing UseSerilogRequestLogging() after them
            app.UseSerilogRequestLogging();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwaggerUi3();
            }

            app.Use(async (context, next) =>
            {
                context.Response.GetTypedHeaders().CacheControl =
                 new CacheControlHeaderValue
                 {
                     NoStore = true,
                     NoCache = true,
                     MustRevalidate = true,
                     MaxAge = TimeSpan.FromSeconds(0),
                     Private = true,
                 };
                context.Response.Headers.Add("Pragma", "no-cache");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                await next();
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseOpenApi();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapControllers();
            });
        }


        /// <summary>
        /// Configure Open Api using NSwag
        /// https://github.com/RicoSuter/NSwag
        /// </summary>
        /// <param name="services"></param>
        internal void ConfigureOpenApi(IServiceCollection services)
        {
            services.AddSwaggerDocument(config =>
            {
                // configure swagger properties
                config.PostProcess = document =>
                {
                    document.Info.Version = "V0.1";
                    document.Info.Description = "Dispute API";
                    document.Info.Title = "Dispute API";
                    document.Tags = new List<OpenApiTag>
                    {
                        new OpenApiTag
                        {
                            Name = "Dispute API",
                            Description = "Dispute API"
                        }
                    };
                };
            });
        }
    }
}
