using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace UbuntuLockSaver
{
    internal static class Wallpaper
    {
        private const double BackdropScale = 1.10;

        public static Bitmap CreateBlurredBackdrop(Rectangle bounds)
        {
            int width = Math.Max(1, (int)Math.Ceiling(bounds.Width * BackdropScale));
            int height = Math.Max(1, (int)Math.Ceiling(bounds.Height * BackdropScale));

            using (Bitmap source = LoadSource(bounds))
            using (Bitmap covered = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(covered))
                {
                    ConfigureGraphics(g);
                    DrawCover(g, source, new Rectangle(0, 0, width, height));
                }

                return Blur(covered);
            }
        }

        private static Bitmap LoadSource(Rectangle bounds)
        {
            Bitmap bitmap = TryLoad(GetTranscodedWallpaperPath());
            if (bitmap != null)
            {
                return bitmap;
            }

            bitmap = TryLoad(GetRegistryWallpaperPath());
            if (bitmap != null)
            {
                return bitmap;
            }

            bitmap = TryLoad(GetCachedWallpaperPath());
            if (bitmap != null)
            {
                return bitmap;
            }

            Bitmap capture = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(capture))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }

            return capture;
        }

        private static string GetTranscodedWallpaperPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Themes", "TranscodedWallpaper");
            return File.Exists(path) ? path : null;
        }

        private static string GetRegistryWallpaperPath()
        {
            try
            {
                object value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Desktop", "WallPaper", null);
                string path = value as string;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return path;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static string GetCachedWallpaperPath()
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "Themes", "CachedFiles");
                if (!Directory.Exists(dir))
                {
                    return null;
                }

                string newest = null;
                DateTime newestTime = DateTime.MinValue;
                foreach (string file in Directory.GetFiles(dir, "CachedImage_*"))
                {
                    DateTime time = File.GetLastWriteTimeUtc(file);
                    if (time > newestTime)
                    {
                        newestTime = time;
                        newest = file;
                    }
                }

                return newest;
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static Bitmap TryLoad(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image image = Image.FromStream(stream, true, true))
                {
                    return new Bitmap(image);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void DrawCover(Graphics g, Image source, Rectangle dest)
        {
            double scale = Math.Max(
                dest.Width / (double)source.Width,
                dest.Height / (double)source.Height);
            int width = (int)Math.Ceiling(source.Width * scale);
            int height = (int)Math.Ceiling(source.Height * scale);
            int x = dest.X + (dest.Width - width) / 2;
            int y = dest.Y + (dest.Height - height) / 2;
            g.DrawImage(source, new Rectangle(x, y, width, height));
        }

        private static Bitmap Blur(Bitmap source)
        {
            int smallWidth = Math.Max(1, source.Width / 4);
            int smallHeight = Math.Max(1, source.Height / 4);

            using (Bitmap small = new Bitmap(smallWidth, smallHeight, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(small))
                {
                    ConfigureGraphics(g);
                    g.DrawImage(source, new Rectangle(0, 0, smallWidth, smallHeight));
                }

                BitmapData data = small.LockBits(
                    new Rectangle(0, 0, smallWidth, smallHeight),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);
                byte[] pixels;
                try
                {
                    int bytes = Math.Abs(data.Stride) * smallHeight;
                    pixels = new byte[bytes];
                    Marshal.Copy(data.Scan0, pixels, 0, bytes);
                    byte[] temp = new byte[bytes];
                    for (int pass = 0; pass < 3; pass++)
                    {
                        BlurHorizontal(pixels, temp, smallWidth, smallHeight, 8);
                        BlurVertical(temp, pixels, smallWidth, smallHeight, 8);
                    }

                    Marshal.Copy(pixels, 0, data.Scan0, bytes);
                }
                finally
                {
                    small.UnlockBits(data);
                }

                Bitmap result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(result))
                {
                    ConfigureGraphics(g);
                    g.DrawImage(small, new Rectangle(0, 0, source.Width, source.Height));
                }

                return result;
            }
        }

        private static void BlurHorizontal(byte[] source, byte[] dest, int width, int height, int radius)
        {
            int stride = width * 4;
            int diameter = radius * 2 + 1;
            for (int y = 0; y < height; y++)
            {
                int row = y * stride;
                int r = 0;
                int g = 0;
                int b = 0;

                for (int i = -radius; i <= radius; i++)
                {
                    int x = Clamp(i, width - 1);
                    int index = row + x * 4;
                    b += source[index];
                    g += source[index + 1];
                    r += source[index + 2];
                }

                for (int x = 0; x < width; x++)
                {
                    int index = row + x * 4;
                    dest[index] = (byte)(b / diameter);
                    dest[index + 1] = (byte)(g / diameter);
                    dest[index + 2] = (byte)(r / diameter);
                    dest[index + 3] = source[index + 3];

                    int addX = Clamp(x + radius + 1, width - 1);
                    int subX = Clamp(x - radius, width - 1);
                    int add = row + addX * 4;
                    int sub = row + subX * 4;
                    b += source[add] - source[sub];
                    g += source[add + 1] - source[sub + 1];
                    r += source[add + 2] - source[sub + 2];
                }
            }
        }

        private static void BlurVertical(byte[] source, byte[] dest, int width, int height, int radius)
        {
            int stride = width * 4;
            int diameter = radius * 2 + 1;
            for (int x = 0; x < width; x++)
            {
                int column = x * 4;
                int r = 0;
                int g = 0;
                int b = 0;

                for (int i = -radius; i <= radius; i++)
                {
                    int y = Clamp(i, height - 1);
                    int index = y * stride + column;
                    b += source[index];
                    g += source[index + 1];
                    r += source[index + 2];
                }

                for (int y = 0; y < height; y++)
                {
                    int index = y * stride + column;
                    dest[index] = (byte)(b / diameter);
                    dest[index + 1] = (byte)(g / diameter);
                    dest[index + 2] = (byte)(r / diameter);
                    dest[index + 3] = source[index + 3];

                    int addY = Clamp(y + radius + 1, height - 1);
                    int subY = Clamp(y - radius, height - 1);
                    int add = addY * stride + column;
                    int sub = subY * stride + column;
                    b += source[add] - source[sub];
                    g += source[add + 1] - source[sub + 1];
                    r += source[add + 2] - source[sub + 2];
                }
            }
        }

        private static int Clamp(int value, int max)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        internal static void ConfigureGraphics(Graphics g)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
        }
    }
}
