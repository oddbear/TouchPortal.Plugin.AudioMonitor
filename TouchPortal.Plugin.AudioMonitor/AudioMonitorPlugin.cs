using System;
using System.Collections.Generic;
using System.Linq;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class AudioMonitorPlugin : ITouchPortalEventHandler
    {
        string ITouchPortalEventHandler.PluginId => "oddbear.audio.monitor";

        private int _size = 100;
        private int _samples = 10;
        private int _updateInterval = 100;
        private int _dbMin = -60;
        
        private readonly ITouchPortalClient _client;
        private readonly WindowsMultimediaDevice _windowsMultimediaDevice;
        private readonly MonitorGraphics _monitorGraphics;

        public AudioMonitorPlugin()
        {
            _client = TouchPortalFactory.CreateClient(this);

            _windowsMultimediaDevice = new WindowsMultimediaDevice(_samples, _updateInterval, _dbMin);

            //TouchPortal requires square:
            _monitorGraphics = new MonitorGraphics(_size, _size);
        }

        public void Run()
            => _client.Connect();

        private void SetSettings(IEnumerable<Setting> settings)
        {
            var deviceName = settings
                .SingleOrDefault(setting => setting.Name == "Device Name")?.Value;

            var deviceUpdated = _windowsMultimediaDevice.SetMultimediaDevice(deviceName);
            if(deviceUpdated)
                _windowsMultimediaDevice.StartMonitoring(MonitoringCallback);
        }

        void ITouchPortalEventHandler.OnInfoEvent(InfoEvent message)
            => SetSettings(message.Settings);

        void ITouchPortalEventHandler.OnSettingsEvent(SettingsEvent message)
            => SetSettings(message.Values);

        public void MonitoringCallback(double decibel, double maxDecibelShort, double maxDecibelLong)
        {
            var value = DecibelToPosition(decibel);
            var shortValue = DecibelToPosition(maxDecibelShort);
            var longValue = DecibelToPosition(maxDecibelLong);
            var image = _monitorGraphics.DrawPng($"{decibel}db", value, shortValue, longValue);

            _client.StateUpdate("oddbear.audio.monitor.icon", Convert.ToBase64String(image));
        }

        private int DecibelToPosition(double decibel)
        {
            //Get percentage of Monitor bar, ex.
            //---   0db ---
            //     -6db
            //    -12db
            //     ...
            //--- -60db ---
            //Calculation:
            //-6db / -60 = 0.1
            //1 - 0.1 = 0.9
            //100px * 0.9 = 90px (fill)
            var percentage = decibel / _dbMin;
            var position = _size * percentage;
            return (int) position;
        }

        #region Ignored TouchPortal Events
        void ITouchPortalEventHandler.OnActionEvent(ActionEvent message)
        {
            //Not used.
        }

        void ITouchPortalEventHandler.OnBroadcastEvent(BroadcastEvent message)
        {
            //Not used.
        }

        void ITouchPortalEventHandler.OnClosedEvent(string message)
        {
            //Not used.
        }

        void ITouchPortalEventHandler.OnListChangedEvent(ListChangeEvent message)
        {
            //Not used.
        }

        void ITouchPortalEventHandler.OnUnhandledEvent(string jsonMessage)
        {
            //Not used.
        }
        #endregion
    }
}
