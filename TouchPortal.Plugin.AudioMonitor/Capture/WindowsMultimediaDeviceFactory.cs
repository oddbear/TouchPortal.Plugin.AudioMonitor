using System;
using Microsoft.Extensions.DependencyInjection;

namespace TouchPortal.Plugin.AudioMonitor.Capture
{
    public class WindowsMultimediaDeviceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public WindowsMultimediaDeviceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public WindowsMultimediaDevice Create(IPluginCallbacks callbacks)
        {
            return ActivatorUtilities.CreateInstance<WindowsMultimediaDevice>(_serviceProvider, callbacks);
        }
    }
}