using System.Collections.Generic;
using TouchPortal.Plugin.AudioMonitor.Models;

namespace TouchPortal.Plugin.AudioMonitor
{
    public interface IPluginCallbacks
    {
        void MultimediaDeviceUpdateCallback(string deviceName);
        void MonitoringCallback(IReadOnlyList<MeterValues> meters);
    }
}