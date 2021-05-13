using System;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class ValueCache
    {
        private readonly int _dbMin;
        private DateTime _prevUpdated;
        public double PrevDecibel { get; private set; }
        public double MaxDecibel { get; private set; }

        public ValueCache(int dbMin)
        {
            _dbMin = dbMin;
            MaxDecibel = _dbMin;
            PrevDecibel = _dbMin;
        }

        public void SetValue(double decibel)
        {
            if (_prevUpdated < DateTime.Now.AddSeconds(-3))
                PrevDecibel = _dbMin;

            if (decibel >= MaxDecibel)
            {
                MaxDecibel = decibel;
                PrevDecibel = _dbMin;
            }
            else if (decibel > PrevDecibel)
            {
                PrevDecibel = decibel;
                _prevUpdated = DateTime.Now;
            }
        }

        public void ResetValues()
        {
            PrevDecibel = _dbMin;
            MaxDecibel = _dbMin;
        }
    }
}
