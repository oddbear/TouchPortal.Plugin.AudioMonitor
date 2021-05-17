using System;

namespace TouchPortal.Plugin.AudioMonitor.Models
{
    public class MeterValues
    {
        private DateTime _prevUpdated;

        public Decibel PeakMax { get; private set; }
        public Decibel PeakHold { get; private set; }
        public Decibel Peak { get; private set; }

        public void SetValue(Decibel decibel)
        {
            //Hold Duration:
            if (_prevUpdated < DateTime.Now.AddSeconds(-3))
                PeakHold = Decibel.Empty;

            if (decibel.Value >= PeakMax.Value)
            {
                PeakMax = decibel;
                PeakHold = Decibel.Empty;
            }
            else if (decibel.Value > PeakHold.Value)
            {
                PeakHold = decibel;
                _prevUpdated = DateTime.Now;
            }

            Peak = decibel;
        }

        public void ResetValues()
        {
            PeakHold = Decibel.Empty;
            PeakMax = Decibel.Empty;
        }
    }
}
