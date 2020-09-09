using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Resonance.Common.Web;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace Resonance
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 12;

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var hostBuilder = ResonanceHostBuilderExtensions.GetHostBuilder(args);

            hostBuilder.ConfigureWebHost(c => c.UseStartup<Startup>());

            if (isService)
            {
                hostBuilder.UseWindowsService();
            }

            hostBuilder.Build().Run();
        }
    }
}