using System;
using System.Threading;
using NAudio.CoreAudioApi;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class WindowsMultimediaDevice : IDisposable
    {
        private readonly int _samples;
        private readonly int _updateInterval;
        private readonly int _dbMin;
        private readonly IPluginCallbacks _callbacks;
        
        private MMDevice _mmDevice;
        private WasapiCapture _recorder;
        private Thread _monitoringThread;

        public bool IsMonitoring { get; private set; }

        public WindowsMultimediaDevice(int samples, int updateInterval, int dbMin, IPluginCallbacks callbacks)
        {
            if (samples == 0)
                throw new ArgumentException("Samples must be more than 0. 10 could be a good number (updateInterval / samples = wait time).", nameof(samples));

            if (updateInterval == 0)
                throw new ArgumentException("Update interval must be more than samples. 100 or more could be a good number (milliseconds).", nameof(updateInterval));

            if (dbMin > 0)
                throw new ArgumentException("dbMin must be a negative number, -60 could be a good number (decibels).", nameof(dbMin));

            _samples = samples;
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
            if (string.IsNullOrWhiteSpace(deviceName))
                return false;

            //DataFlow.Capture -> Microphone/Input
            //DataFlow.Render -> Speakers/Output
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            for (var i = 0; i < devices.Count; i++)
            {
                if (devices[i].FriendlyName.Contains(deviceName))
                {
                    var index = GetIndex(i + deviceOffset, devices.Count);
                    
                    _mmDevice = devices[index];

                    _recorder = new WasapiCapture(_mmDevice);
                    _recorder.StartRecording();

                    _callbacks.MultimediaDeviceUpdateCallback(_mmDevice.FriendlyName);

                    return true;
                }
            }

            //_mmDevice.AudioEndpointVolume.VolumeRange -> IncrementDecibels, MaxDecibels, MinDecibels
            //_mmDevice.AudioEndpointVolume.Mute -> set or get mute status
            //_mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar -> set or get volume in percent 0.0 -> 1.0

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
                    var timeout = _updateInterval / _samples;

                    var maxSampleVolume = 0f;
                    for (var sample = 0; sample < _samples; sample++)
                    {
                        var current = _mmDevice.AudioMeterInformation.MasterPeakValue;
                        if (current > maxSampleVolume)
                            maxSampleVolume = current;

                        Thread.Sleep(timeout); //Interrupted from waiting ... on change...
                    }

                    //maxSampleVolume is now linear, ex...
                    //100% ~ 0db
                    //50% ~ -6db
                    //25% ~ -12db
                    //Convert to decibel:
                    var decibel = Math.Log10(maxSampleVolume) * 20;
                    decibel = Math.Round(decibel);
                    decibel = Math.Max(decibel, _dbMin);
                    decibel = Math.Min(decibel, 0);
                    
                    _callbacks.MonitoringCallback(decibel);
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
