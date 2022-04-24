using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TouchPortal.Plugin.AudioMonitor.Capture;
using TouchPortal.Plugin.AudioMonitor.Meters;
using TouchPortal.Plugin.AudioMonitor.Models;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class AudioMonitorPlugin : ITouchPortalEventHandler, IPluginCallbacks
    {
        private readonly ILogger<AudioMonitorPlugin> _logger;

        string ITouchPortalEventHandler.PluginId => "oddbear.audio.monitor";
        
        private readonly ITouchPortalClient _client;
        private readonly WindowsMultimediaDevice _windowsMultimediaDevice;
        private readonly BarMeterGraphics _barMeterGraphics;

        public AudioMonitorPlugin(ILogger<AudioMonitorPlugin> logger,
                                  ITouchPortalClientFactory clientFactory,
                                  WindowsMultimediaDeviceFactory windowsMultimediaDeviceFactory,
                                  BarMeterGraphics barMeterGraphics)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _barMeterGraphics = barMeterGraphics ?? throw new ArgumentNullException(nameof(barMeterGraphics));

            _client = clientFactory.Create(this);
            _windowsMultimediaDevice = windowsMultimediaDeviceFactory.Create(this);
        }

        public void Run()
        {
            _client.Connect();
        }
        
        public void MonitoringCallback(IReadOnlyList<MeterValues> meters)
        {
            var image = _barMeterGraphics.DrawPng(meters);
            if (image?.Length > 0)
                _client.StateUpdate("oddbear.audio.monitor.icon", Convert.ToBase64String(image));
        }
        
        void ITouchPortalEventHandler.OnActionEvent(ActionEvent message)
        {
            _logger.LogDebug("Method invoked '{0}'", nameof(ITouchPortalEventHandler.OnActionEvent));
            
            switch (message.ActionId)
            {
                case "oddbear.audio.monitor.clear":
                    _windowsMultimediaDevice.ResetValues();
                    return;
                case "oddbear.audio.monitor.toggle":
                    _windowsMultimediaDevice.ToggleMonitoring();
                    return;
            }
        }

        void ITouchPortalEventHandler.OnClosedEvent(string message)
        {
            _logger.LogInformation("Close Event: Plugin Disconnected from TouchPortal.");
            Environment.Exit(0);
        }

        #region Ignored TouchPortal Events
        void ITouchPortalEventHandler.OnInfoEvent(InfoEvent message)
            => _logger.LogDebug("Method invoked '{0}'", nameof(ITouchPortalEventHandler.OnInfoEvent));

        void ITouchPortalEventHandler.OnSettingsEvent(SettingsEvent message)
            => _logger.LogDebug("Method invoked '{0}'", nameof(ITouchPortalEventHandler.OnSettingsEvent));

        void ITouchPortalEventHandler.OnBroadcastEvent(BroadcastEvent message)
            => _logger.LogDebug("Method invoked '{0}'", nameof(ITouchPortalEventHandler.OnBroadcastEvent));

        void ITouchPortalEventHandler.OnListChangedEvent(ListChangeEvent message)
            => _logger.LogDebug("Method invoked '{0}'", nameof(ITouchPortalEventHandler.OnListChangedEvent));

        void ITouchPortalEventHandler.OnUnhandledEvent(string jsonMessage)
            => _logger.LogDebug("Method invoked '{0}'", nameof(ITouchPortalEventHandler.OnUnhandledEvent));

        public void OnNotificationOptionClickedEvent(NotificationOptionClickedEvent message)
            => _logger.LogDebug("Method invoked '{0}'", nameof(ITouchPortalEventHandler.OnNotificationOptionClickedEvent));

        public void OnConnecterChangeEvent(ConnectorChangeEvent message)
            => _logger.LogDebug("Method invoked '{0}'", nameof(ITouchPortalEventHandler.OnConnecterChangeEvent));

        public void OnShortConnectorIdNotificationEvent(ConnectorInfo connectorInfo)
            => _logger.LogDebug("Method invoked '{0}'", nameof(ITouchPortalEventHandler.OnShortConnectorIdNotificationEvent));
        #endregion
    }
}
