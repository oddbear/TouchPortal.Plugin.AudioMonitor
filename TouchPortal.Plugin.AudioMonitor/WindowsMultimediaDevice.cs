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

        private double _maxDecibel;
        private double _prevDecibel;
        private DateTime _prevUpdated;

        private readonly WaveInEvent _recorder;

        private MMDevice _mmDevice;
        private Thread _monitoringThread;

        public WindowsMultimediaDevice(int samples, int updateInterval, int dbMin)
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

            ClearMonitoring();

            //Recorder to be able to peak at the MasterVolume.
            _recorder = new WaveInEvent();
            _recorder.StartRecording();
        }

        public bool SetMultimediaDevice(string deviceName)
        {
            ClearMonitoring();
            var enumerator = new MMDeviceEnumerator();
            if (string.IsNullOrWhiteSpace(deviceName))
                return false;

            //DataFlow.Capture -> Microphone/Input
            //DataFlow.Render -> Speakers/Output
            _mmDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                .FirstOrDefault(mmDevice => mmDevice.FriendlyName.Contains(deviceName));

            //_mmDevice.AudioEndpointVolume.VolumeRange -> IncrementDecibels, MaxDecibels, MinDecibels
            //_mmDevice.AudioEndpointVolume.Mute -> set or get mute status
            //_mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar -> set or get volume in percent 0.0 -> 1.0

            ClearMonitoring();

            return _mmDevice != null;
        }
        public void StartMonitoring(Action<double> callback)
        {
            ClearMonitoring();
            if (callback is null)
                return;

            _monitoringThread = new Thread(() => Monitoring(callback))
            {
                IsBackground = true
            };
            _monitoringThread.Start();
        }

        public void ClearMonitoring()
        {
            _monitoringThread?.Interrupt();

            _maxDecibel = _dbMin;
            _prevDecibel = _dbMin;
            _prevUpdated = DateTime.MinValue;
        }

        private void Monitoring(Action<double> callback)
        {
            try
            {
                while (true)
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

                    callback(decibel);
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
