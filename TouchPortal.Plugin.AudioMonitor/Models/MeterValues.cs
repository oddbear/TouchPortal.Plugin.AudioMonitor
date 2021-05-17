using System;
using TouchPortal.Plugin.AudioMonitor.Capture;
using TouchPortal.Plugin.AudioMonitor.Models.Enums;

namespace TouchPortal.Plugin.AudioMonitor.Models
{
    public class MeterValues : IDisposable
    {
        private readonly CaptureSession _captureSession;

        private DateTime _prevUpdated;

        public float Peak { get; private set; }
        public float PeakHold { get; private set; }
        public float PeakMax { get; private set; }

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
        
        private void SetValue(float volume)
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

        public void Dispose()
        {
            _captureSession?.Dispose();
        }
    }
}
