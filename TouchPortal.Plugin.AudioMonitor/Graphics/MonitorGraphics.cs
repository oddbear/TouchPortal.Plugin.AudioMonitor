﻿using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace TouchPortal.Plugin.AudioMonitor.Graphics
{
    public class MonitorGraphics
    {
        private readonly Bitmap _bitmap;
        private readonly System.Drawing.Graphics _graphics;
        private readonly Rectangle _rectangle;

        private readonly Color _darkGrey = Color.FromArgb(0x30, 0x30, 0x30);

        public MonitorGraphics(int width, int height)
        {
            _bitmap = new Bitmap(width, height);
            _graphics = System.Drawing.Graphics.FromImage(_bitmap);
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

            //Set short time value:
            DrawLine(shortValue, Color.Blue);

            //Set all time value:
            DrawLine(longValue, Color.Red);

            //Text:
            DrawText(text);

            //SafeZone indicator:
            DrawBorder(value);
            
            using (var memoryStream = new MemoryStream())
            {
                _bitmap.Save(memoryStream, ImageFormat.Png);
                return memoryStream.ToArray();
            }
        }

        private void FillBackground(bool useSoftGradient = false)
        {
            using (var gradient = new LinearGradientBrush(_rectangle, Color.Black, Color.Black, 90, false))
            {
                if (useSoftGradient)
                {
                    gradient.InterpolationColors = new ColorBlend
                    {
                        Positions = new[] { 0.00f, 0.25f, 0.50f, 1.00f },
                        Colors = new[] { Color.DarkRed, Color.Yellow, Color.LightGreen, Color.LightGreen }
                    };
                }
                else
                {
                    gradient.InterpolationColors = new ColorBlend
                    {
                        Positions = new[] { 0.00f, 0.25f, 0.25f, 0.50f, 0.50f, 1.00f },
                        Colors = new[] { Color.DarkRed, Color.DarkRed, Color.Yellow, Color.Yellow, Color.LightGreen, Color.LightGreen }
                    };
                }

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
            var color = Brushes.White;

            var measure = _graphics.MeasureString(text, font);
            var xPosition = (_rectangle.Width - (int)measure.Width) / 2;
            var yPosition = _rectangle.Height - (int)measure.Height;
            var textRectangle = new RectangleF(new Point(xPosition, yPosition), measure);

            _graphics.FillRectangle(new SolidBrush(Color.FromArgb(0xB0, 0x00, 0x00, 0x00)), textRectangle);
            _graphics.DrawString(text, font, color, textRectangle);
        }
    }
}
