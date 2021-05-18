using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TouchPortal.Plugin.AudioMonitor.Models;
using TouchPortal.Plugin.AudioMonitor.Models.Enums;

namespace TouchPortal.Plugin.AudioMonitor.Meters
{
    public class BarMeterGraphics
    {
        private readonly ILogger<BarMeterGraphics> _logger;
        private readonly IOptionsMonitor<AppSettings.BarMeterSettings> _meterSettings;
        private readonly double _dbMin;

        public BarMeterGraphics(ILogger<BarMeterGraphics> logger,
                                IOptionsMonitor<AppSettings.BarMeterSettings> meterSettings)
        {
            _logger = logger;
            _meterSettings = meterSettings;

            _dbMin = -60;
        }
        
        private double ToDecibel(float value)
            => Math.Log10(value) * 20;

        private int ToPosition(Scale scale, float value)
        {
            if (scale == Scale.Logarithmic)
            {
                var decibel = ToDecibel(value);
                decibel = Math.Min(decibel, 0);
                decibel = Math.Max(decibel, _dbMin);

                var percentage = 1 - (decibel / _dbMin);

                var position = _meterSettings.CurrentValue.Height * percentage;

                return (int)position;
            }
            else
            {
                var position = _meterSettings.CurrentValue.Height * value;

                return (int)position;
            }
        }
        
        private string DecibelText(float value)
        {
            var decibel = ToDecibel(value);
            decibel = Math.Round(decibel);
            return decibel >= _dbMin
                ? $"{decibel}db"
                : "low";
        }

        private string LinearText(float value)
        {
            var volume = Math.Round(value * 100);
            return $"{volume}%";
        }

        public byte[] DrawPng(IReadOnlyList<MeterValues> meters)
        {
            var defaultBarMeter = meters.FirstOrDefault();
            if (defaultBarMeter is null)
                return DrawPng("No Source");

            var rectangle = new Rectangle(0, 0, _meterSettings.CurrentValue.Width, _meterSettings.CurrentValue.Height);

            using (var bitmap = new Bitmap(rectangle.Width, rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                FillBackground(graphics, rectangle);

                var meterWidth = rectangle.Width / meters.Count;
                for (int i = 0; i < meters.Count; i++)
                {
                    var meter = meters[i];
                    var scale = meter.RequestedScale;
                    var bounds = new Rectangle(meterWidth * i, 0, meterWidth, rectangle.Height);
                    
                    var peakPosition = ToPosition(scale, meter.Peak);
                    FillBarMeter(graphics, bounds, peakPosition, scale);

                    var peakHoldPosition = ToPosition(scale, meter.PeakHold);
                    DrawHorizontalLine(graphics, bounds, peakHoldPosition, _meterSettings.CurrentValue.PeakHold);

                    var peakMaxPosition = ToPosition(scale, meter.PeakMax);
                    DrawHorizontalLine(graphics, bounds, peakMaxPosition, _meterSettings.CurrentValue.PeakMax);
                    
                    DrawAlias(graphics, bounds, meter.Alias);

                    if (i > 0)
                        DrawVerticalLine(graphics, bounds);

                    //Move to per bar (and alias on each bar... vertically?)
                    var text = scale == Scale.Logarithmic
                        ? DecibelText(meter.Peak)
                        : LinearText(meter.Peak);

                    DrawValueText(graphics, bounds, text);
                }

                DrawGrids(graphics, rectangle);
                DrawBorder(graphics, rectangle);
                
                //No more adding graphics after this:
                if (_meterSettings.CurrentValue.Orientation.StartsWith("Horizontal", StringComparison.CurrentCulture))
                    bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);

                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);

                    return memoryStream.ToArray();
                }
            }
        }

        public byte[] DrawPng(string text)
        {
            var rectangle = new Rectangle(0, 0, _meterSettings.CurrentValue.Width, _meterSettings.CurrentValue.Height);
            using (var bitmap = new Bitmap(rectangle.Width, rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                FillBackground(graphics, rectangle);
                DrawGrids(graphics, rectangle);
                DrawValueText(graphics, rectangle, text);
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

        private void FillBarMeter(Graphics graphics, Rectangle rectangle, int peakValue, Scale scale)
        {
            using (var gradient = new LinearGradientBrush(rectangle, Color.Black, Color.Black, 90, false))
            {
                gradient.InterpolationColors = new ColorBlend
                {
                    Positions = scale == Scale.Logarithmic
                        ? new [] { 0.00f, 0.10f, 0.10f, 0.20f, 0.20f, 1.00f }
                        : new [] { 0.00f, 0.25f, 0.25f, 0.50f, 0.50f, 1.00f },
                    Colors = new[] { _meterSettings.CurrentValue.High, _meterSettings.CurrentValue.High, _meterSettings.CurrentValue.Mid, _meterSettings.CurrentValue.Mid, _meterSettings.CurrentValue.Low, _meterSettings.CurrentValue.Low }
                };

                var border = 4;
                var y = rectangle.Height - peakValue;
                var bounds = new Rectangle(rectangle.X + border, y, rectangle.Width - (border * 2), peakValue);
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

        private void DrawAlias(Graphics graphics, Rectangle rectangle, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            using (var font = new Font("Tahoma", 10, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.FromArgb(0xB0, 0x00, 0x00, 0x00)))
            {
                //TODO: Fine tune. (and compensate for DrawValueText text).
                var color = Brushes.White;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var measure = graphics.MeasureString(text, font);
                var yPosition = (rectangle.Width - (int)measure.Height) / 2;
                var textRectangle = new RectangleF(new Point(rectangle.Y + 20, rectangle.X + yPosition), measure);

                //Basically moves ---- bar to become:
                // |
                // |
                // |
                // |
                //Bar... therefore, X = Y, and width = height etc.
                graphics.RotateTransform(-90);
                graphics.TranslateTransform(-rectangle.Height, 0);

                graphics.FillRectangle(brush, textRectangle);
                graphics.DrawString(text, font, color, textRectangle);

                graphics.ResetTransform();
            }
        }

        private void DrawValueText(Graphics graphics, Rectangle rectangle, string text)
        {
            //TODO: FontSize based on rectangle size:
            using (var font = new Font("Tahoma", 8, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.FromArgb(0xB0, 0x00, 0x00, 0x00)))
            {
                var color = Brushes.White;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                //TODO: Option to rotate:
                var measure = graphics.MeasureString(text, font);
                var xPosition = (rectangle.Width - (int)measure.Width) / 2 + rectangle.X;
                var yPosition = rectangle.Height - (int)measure.Height;
                var textRectangle = new RectangleF(new Point(xPosition, yPosition), measure);

                graphics.FillRectangle(brush, textRectangle);
                graphics.DrawString(text, font, color, textRectangle);
            }
        }
    }
}
