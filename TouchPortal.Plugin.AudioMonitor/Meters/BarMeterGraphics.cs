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
            var percentage = 1 - (decibel.Value / _dbMin.Value);
            var position = _appSettings.Height * percentage;

            return (int)position;
        }

        public byte[] DrawPng(string text)
            => DrawPng(text, _appSettings.Height, _appSettings.Height, _appSettings.Height);

        public byte[] DrawPng(BarMeter barMeter)
        {
            var text = barMeter.Peak >= _dbMin
                ? barMeter.Peak.ToString()
                : "low";

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
            var rectangle = new Rectangle(0, 0, _appSettings.Width, _appSettings.Height);

            using (var bitmap = new Bitmap(rectangle.Width, rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                //Background (fill as 100% volume):
                FillBackground(graphics, rectangle);

                //Clear background (ex. 90% volume, clear top 10%):
                FillBarMeter(graphics, rectangle, peakPos);

                //10x Grid:
                DrawGrids(graphics, rectangle);

                //Set short time value:
                DrawLine(graphics, rectangle, peakHoldPos, _appSettings.ColorPeakHold);

                //Set all time value:
                DrawLine(graphics, rectangle, peakMaxPos, _appSettings.ColorPeakMax);

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

        private void FillBackground(Graphics graphics, Rectangle rectangle)
        {
            using (var background = new SolidBrush(_appSettings.ColorBackground))
            {
                graphics.FillRectangle(background, rectangle);
            }
        }

        private void FillBarMeter(Graphics graphics, Rectangle rectangle, int peakValue)
        {
            using (var gradient = new LinearGradientBrush(rectangle, Color.Black, Color.Black, 90, false))
            {
                gradient.InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0.00f, 0.10f, 0.10f, 0.20f, 0.20f, 1.00f },
                    Colors = new[] { _appSettings.ColorBarMeterHigh, _appSettings.ColorBarMeterHigh, _appSettings.ColorBarMeterMid, _appSettings.ColorBarMeterMid, _appSettings.ColorBarMeterLow, _appSettings.ColorBarMeterLow }
                };

                var y = rectangle.Height - peakValue;
                var bounds = new Rectangle(0, y, rectangle.Width, peakValue);
                graphics.FillRectangle(gradient, bounds);
            }
        }
        
        private void DrawGrids(Graphics graphics, Rectangle rectangle)
        {
            using (var pen = new Pen(_appSettings.ColorOverlay))
            {
                var height = rectangle.Height;
                for (var y = 0; y < height; y += height / 10)
                {
                    graphics.DrawLine(pen, 0, y, rectangle.Width, y);
                }
            }
        }

        private void DrawLine(Graphics graphics, Rectangle rectangle, int peakValue, Color color)
        {
            using (var pen = new Pen(color, 2))
            {
                var y = rectangle.Height - peakValue;
                graphics.DrawLine(pen, 0, y, rectangle.Width, y);
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
