using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.CoreAudioApi;
using TouchPortal.Plugin.AudioMonitor.Models;
using TouchPortal.Plugin.AudioMonitor.Models.Enums;

namespace TouchPortal.Plugin.AudioMonitor.Capture
{
    public class WindowsMultimediaDevice
    {
        private readonly ILogger<WindowsMultimediaDevice> _logger;
        private readonly IOptionsMonitor<AppSettings.Capture> _appSettings;
        private readonly IPluginCallbacks _callbacks;

        private readonly List<MeterValues> _sessions = new List<MeterValues>();

        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly Thread _monitoringThread;

        //This is set as dirty, so it will updated the sources on startup:
        private bool _dirtySources = true;
        
        public WindowsMultimediaDevice(ILogger<WindowsMultimediaDevice> logger, IOptionsMonitor<AppSettings.Capture> appSettings,
                                       IPluginCallbacks callbacks)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));

            _deviceEnumerator = new MMDeviceEnumerator();

            _monitoringThread = new Thread(Monitoring) { IsBackground = true };
            _monitoringThread.Start();
            
            //Seems to be using a FileSystemWatcher, better to use a boolean because of concurrency and double events.
            _appSettings.OnChange(settings => _dirtySources = true);
        }
        
        private MMDevice GetDevice(AppSettings.Capture.Device device)
        {
            if (string.IsNullOrWhiteSpace(device.Name))
            {
                _logger.LogWarning("Device configuration missing a name. This will be ignored.");
                return null;
            }

            var dataFlow = "Output".Equals(device.Direction, StringComparison.OrdinalIgnoreCase)
                ? DataFlow.Render
                : DataFlow.Capture;
            
            if ("default".Equals(device.Name, StringComparison.OrdinalIgnoreCase))
                return _deviceEnumerator.GetDefaultAudioEndpoint(dataFlow, Role.Console);

            var mmDevice = _deviceEnumerator
                .EnumerateAudioEndPoints(dataFlow, DeviceState.Active)
                .FirstOrDefault(d => d.FriendlyName?.IndexOf(device.Name, StringComparison.OrdinalIgnoreCase) >= 0);
            
            if (mmDevice is null)
                _logger.LogWarning($"Device name '{device.Name}' did not match any devices. This will be ignored.");

            return mmDevice;
        }

        private void UpdateSources()
        {
            _logger.LogInformation("Updating sources.");

            //Do it simple for now, clear and dispose all:
            foreach (var sessionsValue in _sessions)
                sessionsValue.Dispose();

            _sessions.Clear();

            //Then re-add all:
            var sources = _appSettings.CurrentValue.Devices;
            foreach (var source in sources)
            {
                var mmDevice = GetDevice(source);
                var scale = source.Scale?.StartsWith("Lin", StringComparison.OrdinalIgnoreCase) == true
                    ? Scale.Linear
                    : Scale.Logarithmic;

                if (mmDevice is null)
                {
                    var meterValues = new MeterValues(null, scale, source.Label);
                    _sessions.Add(meterValues);
                }
                else
                {
                    var captureSession = mmDevice.DataFlow == DataFlow.Render
                        ? CaptureSession.FromAudioOutput(mmDevice)
                        : CaptureSession.FromAudioInput(mmDevice);

                    var meterValues = new MeterValues(captureSession, scale, source.Label);
                    _sessions.Add(meterValues);
                }
            }

            _dirtySources = false;
        }

        private void Monitoring()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        if (_dirtySources)
                            UpdateSources();

                        foreach (var meterValues in _sessions)
                            meterValues.MeasurePeakValue();
                        
                        _callbacks.MonitoringCallback(_sessions);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Issue from monitoring thread.");
                    }

                    Thread.Sleep(_appSettings.CurrentValue.UpdateInterval);
                }
            }
            catch (ThreadInterruptedException)
            {
                //If sleeping when the thread is Interrupted, this is ok.
            }
        }
    }
}
