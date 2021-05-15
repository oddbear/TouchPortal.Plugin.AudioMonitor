using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using TouchPortal.Plugin.AudioMonitor.Configuration;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class MonitorGraphics
    {
        private readonly AppConfiguration _appSettings;
        private readonly int _dbMin;

        private readonly Color _darkGrey = Color.FromArgb(0x30, 0x30, 0x30);

        public MonitorGraphics(AppConfiguration appSettings, int dbMin)
        {
            _appSettings = appSettings;
            _dbMin = dbMin;
        }
        
        private int DecibelToPosition(double decibel)
        {
            //Get percentage of Monitor bar, ex.
            //---   0db ---
            //     -6db
            //    -12db
            //     ...
            //--- -60db ---
            //Calculation:
            //-6db / -60 = 0.1
            //1 - 0.1 = 0.9
            //100px * 0.9 = 90px (fill)
            var percentage = decibel / _dbMin;
            var position = _appSettings.Height * percentage;
            return (int)position;
        }

        public byte[] DrawPng(string text)
            => DrawPng(text, _dbMin, _dbMin, _dbMin);

        public byte[] DrawPng(string text, double decibel, double prevDecibel, double maxDecibel)
        {
            var value = DecibelToPosition(decibel);
            var shortValue = DecibelToPosition(prevDecibel);
            var longValue = DecibelToPosition(maxDecibel);

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
                DrawLine(graphics, shortValue, Color.Blue);

                //Set all time value:
                DrawLine(graphics, longValue, Color.Red);

                //Text:
                DrawText(graphics, rectangle, text);

                //SafeZone indicator:
                DrawBorder(graphics, rectangle, value);

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
                        Colors = new[] { Color.DarkRed, Color.Yellow, Color.LightGreen, Color.LightGreen }
                    };
                }
                else
                {
                    gradient.InterpolationColors = new ColorBlend
                    {
                        Positions = new[] { 0.00f, 0.10f, 0.10f, 0.20f, 0.20f, 1.00f },
                        Colors = new[] { Color.DarkRed, Color.DarkRed, Color.Yellow, Color.Yellow, Color.LightGreen, Color.LightGreen }
                    };
                }

                graphics.FillRectangle(gradient, rectangle);
            }
        }

        private void ClearBackground(Graphics graphics, int width, int yPosition)
        {
            using (var background = new SolidBrush(Color.DarkGray))
            {
                var backgroundRect = new Rectangle(0, 0, width, yPosition);
                graphics.FillRectangle(background, backgroundRect);
            }
        }

        private void DrawGrids(Graphics graphics)
        {
            for (var y = 0; y < _appSettings.Height; y += _appSettings.Height / 10)
            {
                graphics.DrawLine(new Pen(_darkGrey), 0, y, _appSettings.Width, y);
            }
        }

        private void DrawLine(Graphics graphics, int yPosition, Color color)
        {
            if (yPosition < _appSettings.Height)
            {
                graphics.DrawLine(new Pen(color, 2), 0, yPosition, _appSettings.Width, yPosition);
            }
        }

        private void DrawBorder(Graphics graphics, Rectangle rectangle, int yPosition)
        {
            var color = yPosition < 10 ? Color.Red
                      : yPosition < 20 ? Color.Green
                      : _darkGrey;

            graphics.DrawRectangle(new Pen(color, 2) { Alignment = PenAlignment.Inset }, rectangle);
        }

        private void DrawText(Graphics graphics, Rectangle rectangle, string text)
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var font = new Font("Tahoma", 10, FontStyle.Bold);
            var color = Brushes.White;

            var measure = graphics.MeasureString(text, font);
            var xPosition = (rectangle.Width - (int)measure.Width) / 2;
            var yPosition = rectangle.Height - (int)measure.Height;
            var textRectangle = new RectangleF(new Point(xPosition, yPosition), measure);

            graphics.FillRectangle(new SolidBrush(Color.FromArgb(0xB0, 0x00, 0x00, 0x00)), textRectangle);
            graphics.DrawString(text, font, color, textRectangle);
        }
    }
}
