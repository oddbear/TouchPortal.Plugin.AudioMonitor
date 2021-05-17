using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;
using NAudio.CoreAudioApi;
using TouchPortal.Plugin.AudioMonitor.Models;

namespace TouchPortal.Plugin.AudioMonitor.Capture
{
    public class WindowsMultimediaDevice
    {
        private readonly IOptionsMonitor<AppSettings.Devices> _appSettings;
        private readonly IPluginCallbacks _callbacks;

        private Dictionary<string, CaptureSession> _sessions = new Dictionary<string, CaptureSession>();
        private readonly Thread _monitoringThread;

        public bool IsMonitoring { get; private set; }

        public WindowsMultimediaDevice(IOptionsMonitor<AppSettings.Devices> appSettings,
                                       IPluginCallbacks callbacks)
        {
            _appSettings = appSettings;
            _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));

            _monitoringThread = new Thread(Monitoring) { IsBackground = true };
            _monitoringThread.Start();
            IsMonitoring = true;
        }

        public bool AddMultimediaDevice(string deviceName, DataFlow dataFlow)
        {
            var enumerator = new MMDeviceEnumerator();

            if (deviceName == "default")
            {
                var mmDevice = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Console);
                switch (dataFlow)
                {
                    case DataFlow.All:
                    case DataFlow.Capture:
                        if (!_sessions.ContainsKey(mmDevice.ID))
                            _sessions.Add(mmDevice.ID, CaptureSession.FromAudioInput(mmDevice));
                        return true;
                    case DataFlow.Render:
                        if (!_sessions.ContainsKey(mmDevice.ID))
                            _sessions.Add(mmDevice.ID, CaptureSession.FromAudioOutput(mmDevice));
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                var mmDevice = enumerator
                    .EnumerateAudioEndPoints(dataFlow, DeviceState.Active)
                    .FirstOrDefault(device => device.FriendlyName.Contains(deviceName));

                if (mmDevice is null || _sessions.ContainsKey(mmDevice.ID))
                    return false;

                switch (mmDevice.DataFlow)
                {
                    case DataFlow.Capture:
                        _sessions.Add(mmDevice.ID, CaptureSession.FromAudioInput(mmDevice));
                        return true;
                    case DataFlow.Render:
                        _sessions.Add(mmDevice.ID, CaptureSession.FromAudioOutput(mmDevice));
                        return true;
                    default:
                        return false;
                }
            }
        }

        public void ToggleMonitoring()
        {
            if (IsMonitoring)
            {
                foreach (var captureSession in _sessions.Values)
                    captureSession.StartMonitor();

                IsMonitoring = false;
            }
            else
            {
                foreach (var captureSession in _sessions.Values)
                    captureSession.StopMonitoring();

                IsMonitoring = true;
            }
        }

        private void Monitoring()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        if (IsMonitoring)
                        {
                            //TODO: The values for Render seems quite high... Maybe these should be linear.
                            //TODO: This flow seems reversed (session should be under bar meter):
                            foreach (var sessionsValue in _sessions.Values)
                            {
                                sessionsValue.MeasurePeakValue();
                            }

                            var meters = _sessions
                                .Values
                                .Select(value => value.MeterValues)
                                .ToArray();

                            _callbacks.MonitoringCallback(meters);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Thread.Sleep(_appSettings.CurrentValue.UpdateInterval);
                }
            }
            catch (ThreadInterruptedException)
            {
                //Ignore, this situation is ok.
            }
        }
    }
}
