using System.Drawing;

namespace TouchPortal.Plugin.AudioMonitor.Models
{
    public class AppSettings
    {
        public class AppOptions
        {
            public Devices Devices { get; set; }
            public BarMeterSettings BarMeter { get; set; }
        }

        public class Devices
        {
            public int UpdateInterval { get; set; } = 100;
        }

        public class BarMeterSettings
        {
            public int Width { get; set; } = 100;
            public int Height { get; set; } = 100;

            public Color Background { get; set; } = Color.Transparent;
            public Color Overlay { get; set; } = Color.FromArgb(0xFF, 0x30, 0x30, 0x30);

            public Color PeakHold { get; set; } = Color.Blue;
            public Color PeakMax { get; set; } = Color.Red;

            public Color Low { get; set; } = Color.LightGreen;
            public Color Mid { get; set; } = Color.Yellow;
            public Color High { get; set; } = Color.DarkRed;
        }
    }
}
