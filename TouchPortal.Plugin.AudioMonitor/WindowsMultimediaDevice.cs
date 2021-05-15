using System;
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

        public bool SetMultimediaDevice(string deviceName, int deviceOffset)
        {
            ClearMonitoring();
            _mmDevice = null;
            _recorder?.Dispose();

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

        private  int GetIndex(int offset, int len)
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
