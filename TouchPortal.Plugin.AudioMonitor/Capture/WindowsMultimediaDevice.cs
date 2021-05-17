using System;
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
        
        private MMDevice _mmDevice;
        private WasapiCapture _recorder;
        private Thread _monitoringThread;

        public bool IsMonitoring { get; private set; }

        public WindowsMultimediaDevice(IOptionsMonitor<AppSettings.Devices> appSettings, IPluginCallbacks callbacks)
        {
            _appSettings = appSettings;
            _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
        }

        public bool SetMultimediaDevice(string deviceName, int deviceOffset)
        {
            ClearMonitoring();
            ClearMultimediaDevice();

            var enumerator = new MMDeviceEnumerator();
            //DataFlow.Capture -> Microphone/Input
            //DataFlow.Render -> Speakers/Output
            var devices = enumerator
                .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            
            if (string.IsNullOrWhiteSpace(deviceName))
                deviceName = enumerator
                    .GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia)
                    .FriendlyName;

            for (var i = 0; i < devices.Count; i++)
            {
                if (!devices[i].FriendlyName.Contains(deviceName))
                    continue;

                var index = GetIndex(i + deviceOffset, devices.Count);
                    
                _mmDevice = devices[index];

                _recorder = new WasapiCapture(_mmDevice);
                _recorder.StartRecording();

                _callbacks.MultimediaDeviceUpdateCallback(_mmDevice.FriendlyName);

                return true;
            }
            
            return false;
        }

        private void ClearMultimediaDevice()
        {
            _recorder?.Dispose();
            _recorder = null;
            _mmDevice?.Dispose();
            _mmDevice = null;
        }

        private int GetIndex(int offset, int len)
        {
            var index = offset % len;
            if (index < 0)
                index += len;

            return index;
        }

        public void StartMonitoring()
        {
            ClearMonitoring();
            
            _monitoringThread = new Thread(Monitoring)
            {
                IsBackground = true
            };
            _monitoringThread.Start();

            IsMonitoring = true;
        }
        
        public void ClearMonitoring()
        {
            _monitoringThread?.Interrupt();
            
            IsMonitoring = false;
        }

        public void ToggleMonitoring()
        {
            if (IsMonitoring)
                ClearMonitoring();
            else
                StartMonitoring();
        }

        private void Monitoring()
        {
            try
            {
                while (_mmDevice != null)
                {
                    var masterPeakValue = _mmDevice.AudioMeterInformation.MasterPeakValue;
                    
                    var decibel = Decibel.FromLinearPercentage(masterPeakValue);
                    
                    _callbacks.MonitoringCallback(decibel);

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
