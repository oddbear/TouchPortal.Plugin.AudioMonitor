using System;
using System.Linq;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class WindowsMultimediaDevice : IDisposable
    {
        private readonly int _samples;
        private readonly int _updateInterval;
        private readonly int _dbMin;
        private Action<double> _callback;

        private double _maxDecibel;
        private double _prevDecibel;
        private DateTime _prevUpdated;

        private readonly WaveInEvent _recorder;

        private MMDevice _mmDevice;
        private Thread _monitoringThread;

        public bool IsMonitoring { get; private set; }

        public WindowsMultimediaDevice(int samples, int updateInterval, int dbMin, Action<double> callback)
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
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            ClearMonitoring();

            //Recorder to be able to peak at the MasterVolume.
            _recorder = new WaveInEvent();
            _recorder.StartRecording();
        }

        public bool SetMultimediaDevice(string deviceName, int deviceOffset)
        {
            ClearMonitoring();
            _mmDevice = null;

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

                    //TODO: Add readonly settings for current selected device...
                    _mmDevice = devices[index];

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

            _maxDecibel = _dbMin;
            _prevDecibel = _dbMin;
            _prevUpdated = DateTime.MinValue;

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

                    if (decibel > _maxDecibel)
                    {
                        _maxDecibel = decibel;
                    }
                    else if (decibel > _prevDecibel || _prevUpdated < DateTime.Now.AddSeconds(-3))
                    {
                        _prevDecibel = decibel;
                        _prevUpdated = DateTime.Now;
                    }

                    _callback(decibel);
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
