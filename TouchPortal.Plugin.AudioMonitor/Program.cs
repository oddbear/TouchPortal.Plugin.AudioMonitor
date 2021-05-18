using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TouchPortal.Plugin.AudioMonitor.Capture;
using TouchPortal.Plugin.AudioMonitor.Meters;
using TouchPortal.Plugin.AudioMonitor.Models;
using TouchPortalSDK.Configuration;

namespace TouchPortal.Plugin.AudioMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var provider = BuildServiceProvider();
            
            var plugin = provider.GetRequiredService<AudioMonitorPlugin>();
            plugin.Run();
        }

        private static IServiceProvider BuildServiceProvider()
        {
            var configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            var services = new ServiceCollection();

            services.AddTouchPortalSdk(configurationRoot);
            services.AddSingleton<AudioMonitorPlugin>();
            services.AddSingleton<BarMeterGraphics>();
            services.AddSingleton<WindowsMultimediaDeviceFactory>();

            //Configuration:
            services.Configure<AppSettings.AppOptions>(configurationRoot);
            services.Configure<AppSettings.Capture>(configurationRoot.GetSection("Capture"));
            services.Configure<AppSettings.BarMeterSettings>(configurationRoot.GetSection("BarMeter"));

            //Logging:
            services.AddLogging(configure =>
            {
                configure.AddSimpleConsole(options => options.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] ");
                configure.AddConfiguration(configurationRoot.GetSection("Logging"));
            });

            return services.BuildServiceProvider(true);
        }
    }
}
