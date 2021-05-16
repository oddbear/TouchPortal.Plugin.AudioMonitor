﻿using System;
using Microsoft.Extensions.DependencyInjection;
using TouchPortal.Plugin.AudioMonitor.Settings;

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
            var services = new ServiceCollection();

            services.AddSingleton<AudioMonitorPlugin>();
            services.AddSingleton<AppSettings>();

            return services.BuildServiceProvider(true);
        }
    }
}
