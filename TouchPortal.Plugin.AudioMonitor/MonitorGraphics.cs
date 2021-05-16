using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using TouchPortal.Plugin.AudioMonitor.Settings;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class MonitorGraphics
    {
        private readonly AppSettings _appSettings;
        private readonly int _dbMin = -60;
        
        public MonitorGraphics(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        private double DecibelWindow(double decibel)
        {
            if (decibel > 0)
                return 0;

            if (decibel < _dbMin)
                return _dbMin;
            
            return decibel;
        }

        private int DecibelToPosition(double decibel)
        {
            var percentage = decibel / _dbMin;
            var position = _appSettings.Height * percentage;

            return (int)position;
        }

        public byte[] DrawPng(string text)
            => DrawPng(text, _dbMin, _dbMin, _dbMin);

        public byte[] DrawPng(double decibel, double prevDecibel, double maxDecibel)
        {
            var text = decibel < -60
                ? "low"
                : $"{decibel}db";

            decibel = DecibelWindow(decibel);
            prevDecibel = DecibelWindow(prevDecibel);
            maxDecibel = DecibelWindow(maxDecibel);

            var value = DecibelToPosition(decibel);
            var shortValue = DecibelToPosition(prevDecibel);
            var longValue = DecibelToPosition(maxDecibel);

            return DrawPng(text, value, shortValue, longValue);
        }

        private byte[] DrawPng(string text, int value, int shortValue, int longValue)
        {
            using (var bitmap = new Bitmap(_appSettings.Width, _appSettings.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var rectangle = new Rectangle(0, 0, _appSettings.Width, _appSettings.Height);

                //Background (fill as 100% volume):
                FillBackground(graphics, rectangle);

                //Clear background (ex. 90% volume, clear top 10%):
                ClearBackground(graphics, _appSettings.Width, value);

                //10x Grid:
                DrawGrids(graphics);

                //Set short time value:
                DrawLine(graphics, shortValue, _appSettings.ColorLinePrev);

                //Set all time value:
                DrawLine(graphics, longValue, _appSettings.ColorLineMax);

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
