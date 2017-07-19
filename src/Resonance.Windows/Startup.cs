﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Newtonsoft.Json;
using Resonance.Data.Storage;
using Resonance.SubsonicCompat;

namespace Resonance.Windows
{
    public class Startup
    {
        private const string CorsPolicyName = "CorsPolicy";

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        private IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsProduction())
            {
                loggerFactory.AddEventLog(new EventLogSettings { LogName = "Resonance", SourceName = "Resonance" });
            }

            app.UseCors(CorsPolicyName);
            app.UseMvc();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services
                .AddMvc()
                .AddApplicationPart(typeof(Resonance.Common.Web.Controllers.MediaLibraryController).Assembly)
                .AddApplicationPart(typeof(Resonance.Common.Web.Controllers.SettingsController).Assembly)
                .AddApplicationPart(typeof(SubsonicCompat.Controllers.SubsonicRestController).Assembly)
                .AddXmlSerializerFormatters()
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    opt.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    opt.SerializerSettings.Formatting = Formatting.None;
                    opt.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                    opt.SerializerSettings.StringEscapeHandling = StringEscapeHandling.Default;
                });

            services.Configure<MetadataRepositorySettings>(Configuration.GetSection("MetadataRepository"));
            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, builder => builder.AllowAnyOrigin());
            });

            services.AddSingleton<SubsonicAsyncAuthorizationFilter>();
            services.AddSingleton<SubsonicAsyncResultFilter>();
        }
    }
}