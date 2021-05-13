using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class MonitorGraphics
    {
        private readonly Bitmap _bitmap;
        private readonly Graphics _graphics;
        private readonly Rectangle _rectangle;

        private readonly Color _darkGrey = Color.FromArgb(0x30, 0x30, 0x30);

        public MonitorGraphics(int width, int height)
        {
            _bitmap = new Bitmap(width, height);
            _graphics = Graphics.FromImage(_bitmap);
            _rectangle = new Rectangle(0, 0, width, height);
        }

        public byte[] DrawPng(string text, int value, int shortValue, int longValue)
        {
            //Background (fill as 100% volume):
            FillBackground();

            //Clear background (ex. 90% volume, clear top 10%):
            ClearBackground(value);

            //10x Grid:
            DrawGrids();

            //SafeZone indicator:
            DrawBorder(value);

            //Set short time value:
            DrawLine(shortValue, Color.Blue);

            //Set all time value:
            DrawLine(longValue, Color.Red);

            //Text:
            DrawText(text);

            using (var memoryStream = new MemoryStream())
            {
                _bitmap.Save(memoryStream, ImageFormat.Png);
                return memoryStream.ToArray();
            }
        }

        private void FillBackground()
        {
            using (var gradient = new LinearGradientBrush(_rectangle, Color.Black, Color.Black, 90, false))
            {
                gradient.InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0.00f, 0.10f, 0.20f, 1.00f },
                    Colors = new[] { Color.Red, Color.Yellow, Color.LightGreen, Color.LightGreen }
                };

                _graphics.FillRectangle(gradient, _rectangle);
            }
        }

        private void ClearBackground(int yPosition)
        {
            using (var background = new SolidBrush(Color.DarkGray))
            {
                var backgroundRect = new Rectangle(0, 0, _bitmap.Width, yPosition);
                _graphics.FillRectangle(background, backgroundRect);
            }
        }

        private void DrawGrids()
        {
            for (var y = 0; y < _bitmap.Height; y += _bitmap.Height / 10)
            {
                _graphics.DrawLine(new Pen(_darkGrey), 0, y, _bitmap.Width, y);
            }
        }

        private void DrawLine(int yPosition, Color color)
        {
            if (yPosition < _bitmap.Height)
            {
                _graphics.DrawLine(new Pen(color, 2), 0, yPosition, _bitmap.Width, yPosition);
            }
        }

        private void DrawBorder(int yPosition)
        {
            var color = yPosition < 10 ? Color.Red
                      : yPosition < 20 ? Color.Green
                      : _darkGrey;
            
            _graphics.DrawRectangle(new Pen(color, 2) { Alignment = PenAlignment.Inset }, _rectangle);
        }

        private void DrawText(string text)
        {
            _graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            _graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var font = new Font("Tahoma", 10, FontStyle.Bold);
            var color = Brushes.Blue;
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Far
            };

            _graphics.DrawString(text, font, color, _rectangle, format);
        }
    }
}
