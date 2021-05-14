using System;
using System.Linq;
using System.Threading;
using NAudio.CoreAudioApi;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class WindowsMultimediaDevice : IDisposable
    {
        private readonly TimeSpan _updateInterval;
        private readonly int _dbMin;
        private readonly IPluginCallbacks _callbacks;
        
        private MMDevice _mmDevice;
        private WasapiCapture _recorder;
        private Thread _monitoringThread;

        public bool IsMonitoring { get; private set; }

        public WindowsMultimediaDevice(TimeSpan updateInterval, int dbMin, IPluginCallbacks callbacks)
        {
            if (updateInterval == TimeSpan.Zero)
                throw new ArgumentException("Update interval must be more than samples. 100 or more could be a good number (milliseconds).", nameof(updateInterval));

            if (dbMin > 0)
                throw new ArgumentException("dbMin must be a negative number, -60 could be a good number (decibels).", nameof(dbMin));
            
            _updateInterval = updateInterval;
            _dbMin = dbMin;
            _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));

            ClearMonitoring();
        }

        public string[] GetSources(DataFlow dataFlow)
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator
                .EnumerateAudioEndPoints(dataFlow, DeviceState.Active);

            return devices.Select(device => device.FriendlyName).ToArray();
        }

        public bool SetMultimediaDevice(string deviceName, DataFlow dataFlow)
        {
            ClearMonitoring();
            _mmDevice = null;
            _recorder?.Dispose();

            var enumerator = new MMDeviceEnumerator();
            
            if (string.IsNullOrWhiteSpace(deviceName))
                deviceName = enumerator
                    .GetDefaultAudioEndpoint(dataFlow, Role.Multimedia)
                    .FriendlyName;

            //DataFlow.Capture -> Microphone/Input
            //DataFlow.Render -> Speakers/Output
            _mmDevice = enumerator
                .EnumerateAudioEndPoints(dataFlow, DeviceState.Active)
                .FirstOrDefault(mmDevice => mmDevice.FriendlyName == deviceName);

            if (_mmDevice is null)
                return false;

            _recorder = new WasapiCapture(_mmDevice);
            _recorder.StartRecording();

            _callbacks.MultimediaDeviceUpdateCallback(_mmDevice.FriendlyName);
            
            //_mmDevice.AudioEndpointVolume.VolumeRange -> IncrementDecibels, MaxDecibels, MinDecibels
            //_mmDevice.AudioEndpointVolume.Mute -> set or get mute status
            //_mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar -> set or get volume in percent 0.0 -> 1.0

            return true;
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

                    //maxSampleVolume is now linear, ex...
                    //100% ~ 0db
                    //50% ~ -6db
                    //25% ~ -12db
                    //Convert to decibel:
                    var decibel = Math.Log10(masterPeakValue) * 20;
                    decibel = Math.Round(decibel);
                    decibel = Math.Max(decibel, _dbMin);
                    decibel = Math.Min(decibel, 0);
                    
                    _callbacks.MonitoringCallback(decibel);

                    Thread.Sleep(_updateInterval); //Interrupted from waiting ... on change...
                }
            }
            catch (ThreadInterruptedException)
            {
                //Ignore, this situation is ok.
            }
        }

        public void Dispose()
        {
            _recorder.Dispose();
        }
    }
}
