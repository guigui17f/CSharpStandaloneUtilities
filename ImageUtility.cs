using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GUIGUI17F
{
    public static class ImageUtility
    {
        public static Image GetThumbImage(Image originImage, int height)
        {
            if (height > originImage.Height)
            {
                height = originImage.Height;
            }
            int width = originImage.Width * height / originImage.Height;
            Image thumbImage = new Bitmap(width, height);
            using (Graphics graph = Graphics.FromImage(thumbImage))
            {
                graph.DrawImage(originImage, 0, 0, width, height);
            }
            return thumbImage;
        }
        
        public static Image AddWatermark(Image originImage, Image watermark, int x, int y)
        {
            if (originImage.Width < watermark.Width || originImage.Height < watermark.Height)
            {
                Console.WriteLine("watermark is larger than the origin image, will do nothing.");
                return originImage;
            }
            x = Clamp(x, 0, originImage.Width - watermark.Width);
            y = Clamp(y, 0, originImage.Height - watermark.Height);
            using (Graphics graph = Graphics.FromImage(originImage))
            {
                graph.DrawImage(watermark, x, y);
            }
            return originImage;
        }
        
        public static Image CreateVerificationCodeImage(string verificationCode)
        {
            Bitmap image = new Bitmap(Convert.ToInt32(Math.Ceiling(verificationCode.Length * 12.0)), 22);
            using (Graphics graph = Graphics.FromImage(image))
            {
                Random random = new Random();
                graph.Clear(Color.White);
                //draw noise lines
                using (Pen pen = new Pen(Color.Silver))
                {
                    for (int i = 0; i < 25; i++)
                    {
                        int x1 = random.Next(image.Width);
                        int y1 = random.Next(image.Height);
                        int x2 = random.Next(image.Width);
                        int y2 = random.Next(image.Height);
                        graph.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
                //draw verification code
                using (Font font = new Font("Arial", 12, FontStyle.Bold | FontStyle.Italic))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                               new Rectangle(0, 0, image.Width, image.Height),
                               Color.Blue, Color.DarkRed, 1.2f, true))
                    {
                        graph.DrawString(verificationCode, font, brush, 3, 2);
                    }
                }
                //draw noise points
                for (int i = 0; i < 100; i++)
                {
                    int x = random.Next(image.Width);
                    int y = random.Next(image.Height);
                    image.SetPixel(x, y, Color.FromArgb(random.Next()));
                }
                //draw border
                using (Pen pen = new Pen(Color.Silver))
                {
                    graph.DrawRectangle(pen, 0, 0, image.Width - 1, image.Height - 1);
                }
            }
            return image;
        }
        
        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }
            return value;
        }
    }
}