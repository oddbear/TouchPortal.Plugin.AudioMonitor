using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using TouchPortal.Plugin.AudioMonitor.Models;
using TouchPortal.Plugin.AudioMonitor.Settings;

namespace TouchPortal.Plugin.AudioMonitor.Meters
{
    public class MonitorGraphics
    {
        private readonly AppSettings _appSettings;
        private readonly Decibel _dbMin;
        private readonly Decibel _dbMax;

        public MonitorGraphics(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _dbMin = Decibel.FromDecibelValue(-60);
            _dbMax = Decibel.FromDecibelValue(0);
        }

        private Decibel DecibelWindow(Decibel decibel)
        {
            if (decibel > _dbMax)
                return _dbMax;

            if (decibel < _dbMin)
                return _dbMin;
            
            return decibel;
        }

        private int DecibelToPosition(Decibel decibel)
        {
            var percentage = decibel.Value / _dbMin.Value;
            var position = _appSettings.Height * percentage;

            return (int)position;
        }

        public byte[] DrawPng(string text)
            => DrawPng(text, _appSettings.Height, _appSettings.Height, _appSettings.Height);

        public byte[] DrawPng(BarMeter barMeter)
        {
            var text = barMeter.Peak < _dbMin
                ? "low"
                : $"{barMeter.Peak}db";

            var peak = DecibelWindow(barMeter.Peak);
            var peakHold = DecibelWindow(barMeter.PeakHold);
            var peakMax = DecibelWindow(barMeter.PeakMax);

            var peakPos = DecibelToPosition(peak);
            var peakHoldPos = DecibelToPosition(peakHold);
            var peakMaxPos = DecibelToPosition(peakMax);

            return DrawPng(text, peakPos, peakHoldPos, peakMaxPos);
        }

        private byte[] DrawPng(string text, int peakPos, int peakHoldPos, int peakMaxPos)
        {
            using (var bitmap = new Bitmap(_appSettings.Width, _appSettings.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var rectangle = new Rectangle(0, 0, _appSettings.Width, _appSettings.Height);

                //Background (fill as 100% volume):
                FillBackground(graphics, rectangle);

                //Clear background (ex. 90% volume, clear top 10%):
                ClearBackground(graphics, _appSettings.Width, peakPos);

                //10x Grid:
                DrawGrids(graphics);

                //Set short time value:
                DrawLine(graphics, peakHoldPos, _appSettings.ColorPeakHold);

                //Set all time value:
                DrawLine(graphics, peakMaxPos, _appSettings.ColorPeakMax);

                //Text:
                DrawText(graphics, rectangle, text);

                //SafeZone indicator:
                DrawBorder(graphics, rectangle);

                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);

                    return memoryStream.ToArray();
                }
            }
        }

        private void FillBackground(Graphics graphics, Rectangle rectangle, bool useSoftGradient = false)
        {
            using (var gradient = new LinearGradientBrush(rectangle, Color.Black, Color.Black, 90, false))
            {
                if (useSoftGradient)
                {
                    gradient.InterpolationColors = new ColorBlend
                    {
                        Positions = new[] { 0.00f, 0.10f, 0.20f, 1.00f },
                        Colors = new[] { _appSettings.ColorBarMeterHigh, _appSettings.ColorBarMeterMid, _appSettings.ColorBarMeterLow, _appSettings.ColorBarMeterLow }
                    };
                }
                else
                {
                    gradient.InterpolationColors = new ColorBlend
                    {
                        Positions = new[] { 0.00f, 0.10f, 0.10f, 0.20f, 0.20f, 1.00f },
                        Colors = new[] { _appSettings.ColorBarMeterHigh, _appSettings.ColorBarMeterHigh, _appSettings.ColorBarMeterMid, _appSettings.ColorBarMeterMid, _appSettings.ColorBarMeterLow, _appSettings.ColorBarMeterLow }
                    };
                }

                graphics.FillRectangle(gradient, rectangle);
            }
        }

        private void ClearBackground(Graphics graphics, int width, int yPosition)
        {
            using (var background = new SolidBrush(_appSettings.ColorBackground))
            {
                var backgroundRect = new Rectangle(0, 0, width, yPosition);
                graphics.FillRectangle(background, backgroundRect);
            }
        }

        private void DrawGrids(Graphics graphics)
        {
            using (var pen = new Pen(_appSettings.ColorOverlay))
            {
                var height = _appSettings.Height;
                for (var y = 0; y < height; y += height / 10)
                {
                    graphics.DrawLine(pen, 0, y, _appSettings.Width, y);
                }
            }
        }

        private void DrawLine(Graphics graphics, int yPosition, Color color)
        {
            using (var pen = new Pen(color, 2))
            {
                if (yPosition < _appSettings.Height)
                {
                    graphics.DrawLine(pen, 0, yPosition, _appSettings.Width, yPosition);
                }
            }
        }

        private void DrawBorder(Graphics graphics, Rectangle rectangle)
        {
            using (var pen = new Pen(_appSettings.ColorOverlay, 2))
            {
                pen.Alignment = PenAlignment.Inset;

                graphics.DrawRectangle(pen, rectangle);
            }
        }

        private void DrawText(Graphics graphics, Rectangle rectangle, string text)
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var font = new Font("Tahoma", 10, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(0xB0, 0x00, 0x00, 0x00)))
            {
                var color = Brushes.White;

                var measure = graphics.MeasureString(text, font);
                var xPosition = (rectangle.Width - (int)measure.Width) / 2;
                var yPosition = rectangle.Height - (int)measure.Height;
                var textRectangle = new RectangleF(new Point(xPosition, yPosition), measure);

                graphics.FillRectangle(brush, textRectangle);
                graphics.DrawString(text, font, color, textRectangle);
            }
        }
    }
}
