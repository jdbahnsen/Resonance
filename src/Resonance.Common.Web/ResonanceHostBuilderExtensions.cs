using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Resonance.Common.Web
{
    public static class ResonanceHostBuilderExtensions
    {
        public static IHostBuilder GetHostBuilder(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables();

            var configuration = configurationBuilder.Build();

            var addServerHeader = true;
            var httpEnabled = false;
            var httpAddress = IPAddress.Any;
            var httpPort = 5000;
            var httpsEnabled = false;
            var httpsAddress = IPAddress.Any;
            var httpsPort = 5001;
            var httpsConnectionAdapterOptions = new HttpsConnectionAdapterOptions();

            var appSettings = configuration.GetSection("AppSettings");

            if (appSettings != null)
            {
                var kestrelSettings = appSettings.GetSection("KestrelSettings");

                if (kestrelSettings != null)
                {
                    addServerHeader = kestrelSettings.GetValue("AddServerHeader", true);
                }

                var httpSettings = appSettings.GetSection("HttpSettings");

                if (httpSettings != null)
                {
                    var unsecuredSettings = httpSettings.GetSection("Unsecured");

                    if (unsecuredSettings != null)
                    {
                        httpEnabled = unsecuredSettings.GetValue("Enabled", false);

                        if (httpEnabled)
                        {
                            var listenerSettings = unsecuredSettings.GetSection("Listener");

                            if (listenerSettings != null)
                            {
                                var address = listenerSettings.GetValue("Address", "*");

                                if (address != "*")
                                {
                                    IPAddress.TryParse(address, out httpAddress);
                                }

                                httpPort = listenerSettings.GetValue("Port", 5000);
                            }
                        }
                    }

                    var securedSettings = httpSettings.GetSection("Secured");

                    if (securedSettings != null)
                    {
                        httpsEnabled = securedSettings.GetValue("Enabled", false);

                        if (httpsEnabled)
                        {
                            var listenerSettings = securedSettings.GetSection("Listener");

                            if (listenerSettings != null)
                            {
                                var address = listenerSettings.GetValue("Address", "*");

                                if (address != "*")
                                {
                                    IPAddress.TryParse(address, out httpsAddress);
                                }

                                httpsPort = listenerSettings.GetValue("Port", 5001);
                            }

                            var certificateFile = securedSettings.GetValue("CertificateFile", string.Empty);
                            var certificatePassword = securedSettings.GetValue("CertificatePassword", string.Empty);

                            httpsConnectionAdapterOptions = new HttpsConnectionAdapterOptions
                            {
                                ClientCertificateMode = ClientCertificateMode.NoCertificate,
                                ServerCertificate = new X509Certificate2(certificateFile, certificatePassword),
                                SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12
                            };
                        }
                    }
                }
            }

            var hostBuilder = Host.CreateDefaultBuilder(args);

            return hostBuilder.ConfigureWebHost(b =>
                b.UseKestrel(options =>
                {
                    options.AddServerHeader = addServerHeader;

                    if (httpEnabled)
                    {
                        options.Listen(httpAddress, httpPort);
                    }

                    if (httpsEnabled)
                    {
                        options.Listen(httpsAddress, httpsPort, listenOptions => listenOptions.UseHttps(httpsConnectionAdapterOptions));
                    }
                })
                .UseContentRoot(Directory.GetCurrentDirectory()));
        }
    }
}