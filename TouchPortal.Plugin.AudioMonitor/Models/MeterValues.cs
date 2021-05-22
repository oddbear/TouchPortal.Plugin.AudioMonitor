using System;
using System.Text.RegularExpressions;
using TouchPortal.Plugin.AudioMonitor.Capture;
using TouchPortal.Plugin.AudioMonitor.Models.Enums;

namespace TouchPortal.Plugin.AudioMonitor.Models
{
    public class MeterValues : IDisposable
    {
        private readonly object _lock = new object();
        private readonly CaptureSession _captureSession;

        private DateTime _prevUpdated;

        public float Peak { get; private set; }
        public float PeakHold { get; private set; }
        public float PeakMax { get; private set; }

        public Scale RequestedScale { get; }
        public bool ShowLevels { get; }
        public string Alias { get; }

        public MeterValues(CaptureSession captureSession, AppSettings.Capture.Device source)
        {
            _captureSession = captureSession;

            RequestedScale = source.Scale?.StartsWith("Lin", StringComparison.OrdinalIgnoreCase) == true
                ? Scale.Linear
                : Scale.Logarithmic;
            
            Alias = Match(captureSession?.DeviceName, source.Label) ?? source.Label ?? string.Empty;

            ShowLevels = source.ShowLevels;
        }

        private string Match(string input, string pattern)
        {
            if (input is null || pattern is null)
                return null;

            try
            {
                var groups = Regex.Match(input, pattern).Groups;
                if (groups.Count > 1)
                    return groups[1].Value;

                if (groups[0].Success)
                    return groups[0].Value;

                return null;
            }
            //Pattern invalid:
            catch (ArgumentException)
            {
                return null;
            }
        }

        public void MeasurePeakValue()
        {
            if (_captureSession is null)
                return;

            var value = _captureSession.MeasurePeakValue();

            SetValue(value);
        }
        
        private void SetValue(float volume)
        {
            lock (_lock)
            {
                //Hold Duration:
                if (_prevUpdated < DateTime.Now.AddSeconds(-3))
                    PeakHold = 0;

                if (volume >= PeakMax)
                {
                    PeakMax = volume;
                    PeakHold = 0;
                }
                else if (volume > PeakHold)
                {
                    PeakHold = volume;
                    _prevUpdated = DateTime.Now;
                }

                Peak = volume;
            }
        }

        public void PauseMonitoring()
            => _captureSession.Recorder.StopRecording();

        public void ResumeMonitoring()
            => _captureSession.Recorder.StartRecording();

        public void ResetValues()
        {
            lock (_lock)
            {
                Peak = 0;
                PeakHold = 0;
                PeakMax = 0;
            }
        }

        public void Dispose()
        {
            _captureSession?.Dispose();
        }
    }
}
