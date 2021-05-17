using System.Drawing;

namespace TouchPortal.Plugin.AudioMonitor.Models
{
    public class AppSettings
    {
        public class AppOptions
        {
            public Capture Capture { get; set; }
            public BarMeterSettings BarMeter { get; set; }
        }

        public class Capture
        {
            public int UpdateInterval { get; set; } = 100;

            public Device[] Devices { get; set; }

            public class Device
            {
                /// <summary>
                /// Name of the device, this can be partial, ex. 'Chat Mic (TC-Helicon GoXLR)' can be written as 'Chat Mic'.
                /// As long as there is not two devices with the same name, this will be fine.
                /// </summary>
                public string Name { get; set; }

                /// <summary>
                /// Input or Output:
                /// </summary>
                public string Direction { get; set; } = "Input";

                /// <summary>
                /// Log or lin, Logarithmic (db) or Linear (%) scale.
                /// </summary>
                public string Scale { get; set; } = "Logarithmic";
            }
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
