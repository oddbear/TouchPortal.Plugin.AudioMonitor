using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;
using TouchPortal.Plugin.AudioMonitor.Graphics;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;

namespace TouchPortal.Plugin.AudioMonitor
{
    public interface IPluginCallbacks
    {
        void MultimediaDeviceUpdateCallback(string deviceName);
        void MonitoringCallback(double decibel);
    }

    public class AudioMonitorPlugin : ITouchPortalEventHandler, IPluginCallbacks
    {
        string ITouchPortalEventHandler.PluginId => "oddbear.audio.monitor";

        private readonly int _size;
        private readonly int _dbMin;

        private string _dataFlow;
        private string _deviceName;

        private readonly ITouchPortalClient _client;
        private readonly WindowsMultimediaDevice _windowsMultimediaDevice;
        private readonly MonitorGraphics _monitorGraphics;
        private readonly ValueCache _valueCache;

        public AudioMonitorPlugin()
        {
            _size = 100;
            _dbMin = -60;
            var updateInterval = TimeSpan.FromMilliseconds(100);

            _client = TouchPortalFactory.CreateClient(this);

            _windowsMultimediaDevice = new WindowsMultimediaDevice(updateInterval, _dbMin, this);

            //TouchPortal requires square:
            _monitorGraphics = new MonitorGraphics(_size, _size);
            _valueCache = new ValueCache(_dbMin);
        }

        public void Run()
        {
            _client.Connect();

            var inputs = _windowsMultimediaDevice.GetSources(DataFlow.Capture);
            _client.ChoiceUpdate("oddbear.audio.monitor.source.device", inputs);

            _client.CreateState("oddbear.audio.monitor.source.icon2", "Audio Monitor Test Image Stream");
            _client.CreateState("oddbear.audio.monitor.source.device2", "Audio Monitor Test Device Name");
        }

        private void SetSettings(IEnumerable<Setting> settings)
        {
            _deviceName = settings
                .SingleOrDefault(setting => setting.Name == "Device Name")?.Value;

            StartMonitoring();
        }

        private void StartMonitoring()
        {
            if(!Enum.TryParse<DataFlow>(_dataFlow, out var dataFlow))
                dataFlow = DataFlow.Capture;

            var deviceUpdated = _windowsMultimediaDevice.SetMultimediaDevice(_deviceName, dataFlow);
            if (deviceUpdated)
            {
                _valueCache.ResetValues();
                _windowsMultimediaDevice.StartMonitoring();
            }
            else
            {
                var image = _monitorGraphics.DrawPng("no device", _size, _size, _size);
                _client.StateUpdate("oddbear.audio.monitor.icon", Convert.ToBase64String(image));
                _client.StateUpdate("oddbear.audio.monitor.device", $"no device found: '{_deviceName}'");
            }
        }

        void ITouchPortalEventHandler.OnInfoEvent(InfoEvent message)
            => SetSettings(message.Settings);

        void ITouchPortalEventHandler.OnSettingsEvent(SettingsEvent message)
            => SetSettings(message.Values);

        public void MultimediaDeviceUpdateCallback(string deviceName)
            => _client.StateUpdate("oddbear.audio.monitor.device", deviceName);

        public void MonitoringCallback(double decibel)
        {
            _valueCache.SetValue(decibel);

            var value = DecibelToPosition(decibel);
            var shortValue = DecibelToPosition(_valueCache.PrevDecibel);
            var longValue = DecibelToPosition(_valueCache.MaxDecibel);

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
        
        void ITouchPortalEventHandler.OnActionEvent(ActionEvent message)
        {
            switch (message.ActionId)
            {
                case "oddbear.audio.monitor.clear":
                    _valueCache.ResetValues();
                    return;
                case "oddbear.audio.monitor.toggle":
                    _windowsMultimediaDevice.ToggleMonitoring();
                    return;
                case "oddbear.audio.monitor.source":
                    _dataFlow = message.GetValue("oddbear.audio.monitor.source.type");
                    _deviceName = message.GetValue("oddbear.audio.monitor.source.device");
                    StartMonitoring();
                    return;
            }
        }

        private void AudioTypeSelected(string value, string instanceId)
        {
            switch (value)
            {
                case "Audio Output":
                    var outputs = _windowsMultimediaDevice.GetSources(DataFlow.Render);
                    _client.ChoiceUpdate("oddbear.audio.monitor.source.device", outputs, instanceId);
                    return;
                case "Audio Input":
                    var inputs = _windowsMultimediaDevice.GetSources(DataFlow.Capture);
                    _client.ChoiceUpdate("oddbear.audio.monitor.source.device", inputs, instanceId);
                    return;
            }
        }

        private void AudioSourceSelected(string value)
        {
            //Do stuff...
        }

        void ITouchPortalEventHandler.OnListChangedEvent(ListChangeEvent message)
        {
            switch (message.ListId)
            {
                case "oddbear.audio.monitor.source.type":
                    AudioTypeSelected(message.Value, message.InstanceId);
                    return;
                case "oddbear.audio.monitor.source.device":
                    AudioSourceSelected(message.Value);
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

        void ITouchPortalEventHandler.OnUnhandledEvent(string jsonMessage)
        {
            //Not used.
        }
        #endregion
    }
}
