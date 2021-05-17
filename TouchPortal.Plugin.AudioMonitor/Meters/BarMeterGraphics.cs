using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TouchPortal.Plugin.AudioMonitor.Models;

namespace TouchPortal.Plugin.AudioMonitor.Meters
{
    public class BarMeterGraphics
    {
        private readonly ILogger<BarMeterGraphics> _logger;
        private readonly IOptionsMonitor<AppSettings.BarMeterSettings> _meterSettings;
        private readonly Decibel _dbMin;
        private readonly Decibel _dbMax;

        public BarMeterGraphics(ILogger<BarMeterGraphics> logger,
                                IOptionsMonitor<AppSettings.BarMeterSettings> meterSettings)
        {
            _logger = logger;
            _meterSettings = meterSettings;

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
            var position = _meterSettings.CurrentValue.Height * percentage;

            return (int)position;
        }

        public byte[] DrawPng(string text)
        {
            var rectangle = new Rectangle(0, 0, _meterSettings.CurrentValue.Width, _meterSettings.CurrentValue.Height);
            using (var bitmap = new Bitmap(rectangle.Width, rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                FillBackground(graphics, rectangle);
                DrawGrids(graphics, rectangle);
                DrawText(graphics, rectangle, text);
                DrawBorder(graphics, rectangle);

                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);

                    return memoryStream.ToArray();
                }
            }
        }

        public byte[] DrawPng(IReadOnlyList<MeterValues> barMeters)
        {
            var defaultBarMeter = barMeters.FirstOrDefault();
            if (defaultBarMeter is null)
                return DrawPng("No Source");

            var text = defaultBarMeter.Peak >= _dbMin
                ? defaultBarMeter.Peak.ToString()
                : "low";

            var rectangle = new Rectangle(0, 0, _meterSettings.CurrentValue.Width, _meterSettings.CurrentValue.Height);

            using (var bitmap = new Bitmap(rectangle.Width, rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                FillBackground(graphics, rectangle);

                var barMeterWidth = rectangle.Width / barMeters.Count;
                for (int i = 0; i < barMeters.Count; i++)
                {
                    var barMeter = barMeters[i];
                    var bounds = new Rectangle(barMeterWidth * i, 0, barMeterWidth, rectangle.Height);

                    var peak = DecibelWindow(barMeter.Peak);
                    var peakPos = DecibelToPosition(peak);
                    FillBarMeter(graphics, bounds, peakPos);

                    var peakHold = DecibelWindow(barMeter.PeakHold);
                    var peakHoldPos = DecibelToPosition(peakHold);
                    DrawHorizontalLine(graphics, bounds, peakHoldPos, _meterSettings.CurrentValue.PeakHold);

                    var peakMax = DecibelWindow(barMeter.PeakMax);
                    var peakMaxPos = DecibelToPosition(peakMax);
                    DrawHorizontalLine(graphics, bounds, peakMaxPos, _meterSettings.CurrentValue.PeakMax);

                    if (i > 0)
                        DrawVerticalLine(graphics, bounds);
                }

                DrawGrids(graphics, rectangle);
                DrawText(graphics, rectangle, text);
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
            using (var background = new SolidBrush(_meterSettings.CurrentValue.Background))
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
                    Colors = new[] { _meterSettings.CurrentValue.High, _meterSettings.CurrentValue.High, _meterSettings.CurrentValue.Mid, _meterSettings.CurrentValue.Mid, _meterSettings.CurrentValue.Low, _meterSettings.CurrentValue.Low }
                };

                var y = rectangle.Height - peakValue;
                var bounds = new Rectangle(rectangle.X, y, rectangle.Width, peakValue);
                graphics.FillRectangle(gradient, bounds);
            }
        }
        
        private void DrawGrids(Graphics graphics, Rectangle rectangle)
        {
            using (var pen = new Pen(_meterSettings.CurrentValue.Overlay))
            {
                var height = rectangle.Height;
                for (var y = 0; y < height; y += height / 10)
                {
                    graphics.DrawLine(pen, 0, y, rectangle.Width, y);
                }
            }
        }

        private void DrawVerticalLine(Graphics graphics, Rectangle rectangle)
        {
            using (var pen = new Pen(_meterSettings.CurrentValue.Overlay))
            {
                var x = rectangle.X;
                graphics.DrawLine(pen, x, 0, x, rectangle.Height);
            }
        }

        private void DrawHorizontalLine(Graphics graphics, Rectangle rectangle, int peakValue, Color color)
        {
            using (var pen = new Pen(color, 2))
            {
                var y = rectangle.Height - peakValue;
                var x2 = rectangle.X + rectangle.Width;
                graphics.DrawLine(pen, rectangle.X, y, x2, y);
            }
        }

        private void DrawBorder(Graphics graphics, Rectangle rectangle)
        {
            using (var pen = new Pen(_meterSettings.CurrentValue.Overlay, 2))
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
