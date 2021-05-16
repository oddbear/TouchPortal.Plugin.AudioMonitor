using System;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class ValueCache
    {
        private DateTime _prevUpdated;
        public double PrevDecibel { get; private set; }
        public double MaxDecibel { get; private set; }
        
        public void SetValue(double decibel)
        {
            if (_prevUpdated < DateTime.Now.AddSeconds(-3))
                PrevDecibel = double.MinValue;

            if (decibel >= MaxDecibel)
            {
                MaxDecibel = decibel;
                PrevDecibel = double.MinValue;
            }
            else if (decibel > PrevDecibel)
            {
                PrevDecibel = decibel;
                _prevUpdated = DateTime.Now;
            }
        }

        public void ResetValues()
        {
            PrevDecibel = double.MinValue;
            MaxDecibel = double.MinValue;
        }
    }
}
