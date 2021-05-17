using System;
using TouchPortal.Plugin.AudioMonitor.Capture;
using TouchPortal.Plugin.AudioMonitor.Models.Enums;

namespace TouchPortal.Plugin.AudioMonitor.Models
{
    public class MeterValues : IDisposable
    {
        private readonly CaptureSession _captureSession;

        private DateTime _prevUpdated;
        private float _peak;
        private float _peakHold;
        private float _peakMax;

        public Decibel PeakMax => ToDecibel(_peakMax);
        public Decibel PeakHold => ToDecibel(_peakHold);
        public Decibel Peak => ToDecibel(_peak);

        public Scale RequestedScale { get; }

        public MeterValues(CaptureSession captureSession, Scale scale)
        {
            _captureSession = captureSession;
            RequestedScale = scale;
        }

        public void MeasurePeakValue()
        {
            if (_captureSession is null)
                return;

            var value = _captureSession.MeasurePeakValue();

            SetValue(value);
        }

        private Decibel ToDecibel(float volume)
            => Decibel.FromLinearPercentage(volume);

        private void SetValue(float volume)
        {
            //Hold Duration:
            if (_prevUpdated < DateTime.Now.AddSeconds(-3))
                _peak = 0;

            if (volume >= _peakMax)
            {
                _peakMax = volume;
                _peakHold = 0;
            }
            else if (volume > _peakHold)
            {
                _peakHold = volume;
                _prevUpdated = DateTime.Now;
            }

            _peak = volume;
        }

        public void Dispose()
        {
            _captureSession?.Dispose();
        }
    }
}
