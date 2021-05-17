using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TouchPortal.Plugin.AudioMonitor.Capture;
using TouchPortal.Plugin.AudioMonitor.Meters;
using TouchPortal.Plugin.AudioMonitor.Models;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;

namespace TouchPortal.Plugin.AudioMonitor
{
    public interface IPluginCallbacks
    {
        void MultimediaDeviceUpdateCallback(string deviceName);
        void MonitoringCallback(Decibel decibel);
    }

    public class AudioMonitorPlugin : ITouchPortalEventHandler, IPluginCallbacks
    {
        private readonly ILogger<AudioMonitorPlugin> _logger;

        string ITouchPortalEventHandler.PluginId => "oddbear.audio.monitor";
        
        private string _device;
        private int _deviceOffset;

        private readonly ITouchPortalClient _client;
        private readonly WindowsMultimediaDevice _windowsMultimediaDevice;
        private readonly BarMeterGraphics _barMeterGraphics;
        private readonly MeterValues _meterValues;

        public AudioMonitorPlugin(ILogger<AudioMonitorPlugin> logger,
                                  ITouchPortalClientFactory clientFactory,
                                  WindowsMultimediaDeviceFactory windowsMultimediaDeviceFactory,
                                  BarMeterGraphics barMeterGraphics)
        {
            _logger = logger;
            _barMeterGraphics = barMeterGraphics;

            _client = clientFactory.Create(this);
            
            _windowsMultimediaDevice = windowsMultimediaDeviceFactory.Create(this);
            
            _meterValues = new MeterValues();
        }

        public void Run()
            => _client.Connect();

        private void SetSettings(IEnumerable<Setting> settings)
        {
            _device = settings
                .SingleOrDefault(setting => setting.Name == "Device Name")?.Value;

            StartMonitoring();
        }

        private void StartMonitoring()
        {
            var deviceUpdated = _windowsMultimediaDevice.SetMultimediaDevice(_device, _deviceOffset);
            if (deviceUpdated)
            {
                _meterValues.ResetValues();
                _windowsMultimediaDevice.StartMonitoring();
            }
            else
            {
                var image = _barMeterGraphics.DrawPng("no device");
                _client.StateUpdate("oddbear.audio.monitor.icon", Convert.ToBase64String(image));
                _client.StateUpdate("oddbear.audio.monitor.device", $"no device found: '{_device}'");
            }
        }

        void ITouchPortalEventHandler.OnInfoEvent(InfoEvent message)
            => SetSettings(message.Settings);

        void ITouchPortalEventHandler.OnSettingsEvent(SettingsEvent message)
            => SetSettings(message.Values);

        public void MultimediaDeviceUpdateCallback(string deviceName)
        {
            _client.StateUpdate("oddbear.audio.monitor.device", deviceName);
        }
        
        public void MonitoringCallback(Decibel decibel)
        {
            _meterValues.SetValue(decibel);
            
            var image = _barMeterGraphics.DrawPng(_meterValues);

            _client.StateUpdate("oddbear.audio.monitor.icon", Convert.ToBase64String(image));
        }
        
        void ITouchPortalEventHandler.OnActionEvent(ActionEvent message)
        {
            switch (message.ActionId)
            {
                case "oddbear.audio.monitor.clear":
                    _meterValues.ResetValues();
                    return;
                case "oddbear.audio.monitor.toggle":
                    _windowsMultimediaDevice.ToggleMonitoring();
                    return;
                case "oddbear.audio.monitor.next":
                    _deviceOffset++;
                    StartMonitoring();
                    return;
                case "oddbear.audio.monitor.prev":
                    _deviceOffset--;
                    StartMonitoring();
                    return;
                case "oddbear.audio.monitor.reset":
                    _deviceOffset = 0;
                    StartMonitoring();
                    return;
            }
        }

        #region Ignored TouchPortal Events
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
