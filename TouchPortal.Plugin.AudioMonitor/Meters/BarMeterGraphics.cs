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
        
        private Orientation GetOrientation(ref int width, ref int height)
        {
            if (height >= width)
                return Orientation.Vertical;

            //Swap:
            var tmp = height;
            height = width;
            width = tmp;

            return Orientation.Horizontal;
        }

        private double ToDecibel(float value)
            => Math.Log10(value) * 20;

        private int ToPosition(Scale scale, float value, Rectangle rectangle)
        {
            if (scale == Scale.Logarithmic)
            {
                var decibel = ToDecibel(value);
                decibel = Math.Min(decibel, 0);
                decibel = Math.Max(decibel, _dbMin);

                var percentage = 1 - (decibel / _dbMin);

                var position = rectangle.Height * percentage;

                return (int)position;
            }
            else
            {
                var position = rectangle.Height * value;

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

            var width = _meterSettings.CurrentValue.Width;
            var height = _meterSettings.CurrentValue.Height;
            var orientation = GetOrientation(ref width, ref height);

            var rectangle = new Rectangle(0, 0, width, height);

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
                    
                    var peakPosition = ToPosition(scale, meter.Peak, rectangle);
                    FillBarMeter(graphics, bounds, peakPosition, scale);

                    var peakHoldPosition = ToPosition(scale, meter.PeakHold, rectangle);
                    DrawHorizontalLine(graphics, bounds, peakHoldPosition, _meterSettings.CurrentValue.PeakHold);

                    var peakMaxPosition = ToPosition(scale, meter.PeakMax, rectangle);
                    DrawHorizontalLine(graphics, bounds, peakMaxPosition, _meterSettings.CurrentValue.PeakMax);
                    
                }

                DrawGrids(graphics, rectangle);
                DrawBorder(graphics, rectangle);

                //Splitting lines between bars (if more than one):
                for (int i = 1; i < meters.Count; i++)
                {
                    var bounds = new Rectangle(meterWidth * i, 0, meterWidth, rectangle.Height);
                    DrawVerticalLine(graphics, bounds);
                }

                //Text on top off all graphics:
                for (int i = 0; i < meters.Count; i++)
                {
                    var meter = meters[i];
                    var scale = meter.RequestedScale;
                    var bounds = new Rectangle(meterWidth * i, 0, meterWidth, rectangle.Height);
                    //Move to per bar (and alias on each bar... vertically?)
                    var text = scale == Scale.Logarithmic
                        ? DecibelText(meter.Peak)
                        : LinearText(meter.Peak);

                    DrawAlias(graphics, bounds, meter.Alias, orientation);
                    DrawValueText(graphics, bounds, text, orientation);
                }

                //No more adding graphics after this:
                if (orientation == Orientation.Horizontal)
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
            var width = _meterSettings.CurrentValue.Width;
            var height = _meterSettings.CurrentValue.Height;
            var orientation = GetOrientation(ref width, ref height);

            var rectangle = new Rectangle(0, 0, width, height);
            using (var bitmap = new Bitmap(rectangle.Width, rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                FillBackground(graphics, rectangle);
                DrawGrids(graphics, rectangle);
                DrawValueText(graphics, rectangle, text, orientation);
                DrawBorder(graphics, rectangle);

                //No more adding graphics after this:
                if (orientation == Orientation.Horizontal)
                    bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);

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

        private void DrawAlias(Graphics graphics, Rectangle rectangle, string text, Orientation orientation)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            using (var font = new Font("Tahoma", 10, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.FromArgb(0xB0, 0x00, 0x00, 0x00)))
            {
                var color = Brushes.White;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var indent = orientation == Orientation.Horizontal ? 48 : 20;

                //Always along the bar:
                var measure = graphics.MeasureString(text, font);
                var xPosition = rectangle.Y + indent;
                var yPosition = (rectangle.Width - measure.Height) / 2;
                var textRectangle = new RectangleF(new PointF(xPosition, rectangle.X + yPosition), measure);

                //Basically moves bar:
                //                |
                //                |
                //                |
                //                |
                // to become: ----
                graphics.RotateTransform(-90);
                // move x,-h:      ----
                graphics.TranslateTransform(-rectangle.Height, 0);

                graphics.FillRectangle(brush, textRectangle);
                graphics.DrawString(text, font, color, textRectangle);

                graphics.ResetTransform();
            }
        }

        private void DrawValueText(Graphics graphics, Rectangle rectangle, string text, Orientation orientation)
        {
            using (var font = new Font("Tahoma", 10, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.FromArgb(0xB0, 0x00, 0x00, 0x00)))
            {
                var color = Brushes.White;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                
                if (orientation == Orientation.Horizontal)
                {
                    var measure = graphics.MeasureString(text, font);
                    var yPosition = (rectangle.Width - measure.Height) / 2 + rectangle.X;
                    var textRectangle = new RectangleF(new PointF(4, yPosition), measure);

                    graphics.RotateTransform(-90);
                    graphics.TranslateTransform(-rectangle.Height, 0);

                    graphics.FillRectangle(brush, textRectangle);
                    graphics.DrawString(text, font, color, textRectangle);

                    graphics.ResetTransform();
                }
                else
                {
                    var measure = graphics.MeasureString(text, font);
                    var xPosition = (rectangle.Width - measure.Width) / 2 + rectangle.X;
                    var yPosition = rectangle.Height - measure.Height;
                    var textRectangle = new RectangleF(new PointF(xPosition, yPosition), measure);

                    graphics.FillRectangle(brush, textRectangle);
                    graphics.DrawString(text, font, color, textRectangle);
                }
            }
        }
    }
}
