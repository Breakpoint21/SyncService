using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace SyncService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            if (isService)
            {
                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                Directory.SetCurrentDirectory(pathToContentRoot);
            }

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logFilePath = Path.Combine(appDataPath, "SyncService", "SyncService.log");

            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.File(logFilePath)
                .CreateLogger();

            try
            {
                var host = CreateWebHostBuilder(args.Where(arg => arg != "--console").ToArray()).Build();

                if (isService)
                {
                    host.RunAsService();
                }
                else
                {
                    host.Run();
                }
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Host terminated!");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json",
                        optional: false,
                        reloadOnChange: true);

                    if (hostingContext.HostingEnvironment.IsDevelopment())
                    {
                        config.AddUserSecrets<Startup>();
                    }
                    else
                    {
                        Log.Information("Using HiDriveConfig File");
                        config.AddXmlFile(
                            "hidriveSettings.xml", optional: true, reloadOnChange: true);
                    }
                })
                .UseStartup<Startup>().UseSerilog();
    }
}
