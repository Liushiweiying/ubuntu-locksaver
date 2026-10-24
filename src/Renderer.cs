using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;

namespace UbuntuLockSaver
{
    internal sealed class Renderer : IDisposable
    {
        private const double BackdropScale = 1.10;

        private static readonly StringFormat Centered;
        private static readonly StringFormat RightAligned;
        private static readonly object FontLock = new object();
        private static bool fontFamiliesResolved;
        private static string clockFamily;
        private static string iconFamily;

        private readonly Bitmap backdrop;
        private readonly Rectangle bounds;
        private readonly AnimationState state;
        private readonly bool preview;

        private readonly Font clockFont;
        private readonly Font dateFont;
        private readonly Font promptFont;
        private readonly Font iconFont;

        private string lastClock;
        private string previousClock;
        private double clockChangeAt;

        static Renderer()
        {
            Centered = new StringFormat();
            Centered.Alignment = StringAlignment.Center;
            Centered.LineAlignment = StringAlignment.Center;

            RightAligned = new StringFormat();
            RightAligned.Alignment = StringAlignment.Far;
            RightAligned.LineAlignment = StringAlignment.Center;
        }

        public Renderer(Bitmap backdrop, Rectangle bounds, AnimationState state, bool preview)
        {
            this.backdrop = backdrop;
            this.bounds = bounds;
            this.state = state;
            this.preview = preview;

            EnsureFontFamilies();

            float clockSize = Math.Max(12f, (float)(bounds.Height * 0.12));
            clockFont = new Font(clockFamily, clockSize, FontStyle.Regular, GraphicsUnit.Pixel);
            dateFont = new Font(clockFamily, Math.Max(8f, clockSize * 0.22f), FontStyle.Regular, GraphicsUnit.Pixel);
            promptFont = new Font(clockFamily, Math.Max(9f, (float)(bounds.Height * 0.018)), FontStyle.Regular, GraphicsUnit.Pixel);
            iconFont = new Font(iconFamily, Math.Max(9f, (float)(bounds.Height * 0.024)), FontStyle.Regular, GraphicsUnit.Pixel);
        }

        public void Draw(Graphics g, DateTime now)
        {
            Wallpaper.ConfigureGraphics(g);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            DrawBackdrop(g);
            DrawClock(g, now);

            if (!preview || bounds.Height >= 240)
            {
                DrawDate(g, now);
                DrawPrompt(g);
                DrawStatusIcons(g);
            }
        }

        public void Dispose()
        {
            clockFont.Dispose();
            dateFont.Dispose();
            promptFont.Dispose();
            iconFont.Dispose();
        }

        private void DrawBackdrop(Graphics g)
        {
            double zoom = state.Zoom;
            float sourceWidth = (float)(backdrop.Width / (BackdropScale * zoom));
            float sourceHeight = (float)(backdrop.Height / (BackdropScale * zoom));
            float sourceX = (backdrop.Width - sourceWidth) / 2f;
            float sourceY = (backdrop.Height - sourceHeight) / 2f;

            InterpolationMode previous = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.Bilinear;
            g.DrawImage(
                backdrop,
                new Rectangle(0, 0, bounds.Width, bounds.Height),
                sourceX,
                sourceY,
                sourceWidth,
                sourceHeight,
                GraphicsUnit.Pixel);
            g.InterpolationMode = previous;
        }

        private void DrawClock(Graphics g, DateTime now)
        {
            string text = now.ToString("HH:mm", CultureInfo.InvariantCulture);
            double elapsed = state.Elapsed;

            if (lastClock == null)
            {
                lastClock = text;
            }
            else if (text != lastClock)
            {
                previousClock = lastClock;
                lastClock = text;
                clockChangeAt = elapsed;
            }

            if (previousClock != null)
            {
                float progress = (float)((elapsed - clockChangeAt) / AnimationState.ClockFadeSeconds);
                if (progress >= 1f)
                {
                    previousClock = null;
                }
                else
                {
                    DrawClockText(g, previousClock, 0.92f * (1f - progress));
                    DrawClockText(g, text, 0.92f * progress);
                    return;
                }
            }

            DrawClockText(g, text, 0.92f);
        }

        private void DrawClockText(Graphics g, string text, float alpha)
        {
            float size = clockFont.Size;
            RectangleF rect = new RectangleF(
                0f,
                (float)(bounds.Height * 0.42) - size,
                bounds.Width,
                size * 2f);
            DrawShadowedText(g, text, clockFont, rect, Centered, alpha, Math.Max(2f, size * 0.02f));
        }

        private void DrawDate(Graphics g, DateTime now)
        {
            float clockSize = clockFont.Size;
            float dateSize = dateFont.Size;
            RectangleF rect = new RectangleF(
                0f,
                (float)(bounds.Height * 0.42) + clockSize * 0.62f - dateSize,
                bounds.Width,
                dateSize * 2f);
            DrawShadowedText(g, FormatDate(now), dateFont, rect, Centered, 0.75f, Math.Max(1f, dateSize * 0.05f));
        }

        private void DrawPrompt(Graphics g)
        {
            float clockSize = clockFont.Size;
            float promptSize = promptFont.Size;
            RectangleF rect = new RectangleF(
                0f,
                (float)(bounds.Height * 0.42) + clockSize * 0.62f + clockSize * 0.22f * 2.2f,
                bounds.Width,
                promptSize * 2f);
            DrawShadowedText(g, PromptText(), promptFont, rect, Centered, 0.60f, 1f);
        }

        private void DrawStatusIcons(Graphics g)
        {
            float size = iconFont.Size;
            RectangleF rect = new RectangleF(
                (float)(bounds.Height * 0.03),
                (float)(bounds.Height * 0.03),
                bounds.Width - (float)(bounds.Height * 0.06),
                size * 1.8f);
            DrawShadowedText(g, "\uE701   \uE767   \uE7E8", iconFont, rect, RightAligned, 0.78f, 1f);
        }

        private static void DrawShadowedText(
            Graphics g,
            string text,
            Font font,
            RectangleF rect,
            StringFormat format,
            float alpha,
            float shadowOffset)
        {
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb((int)(alpha * 110f), 0, 0, 0)))
            {
                RectangleF shadowRect = new RectangleF(rect.X, rect.Y + shadowOffset, rect.Width, rect.Height);
                g.DrawString(text, font, shadow, shadowRect, format);
            }

            using (SolidBrush brush = new SolidBrush(Color.FromArgb((int)(alpha * 255f), 255, 255, 255)))
            {
                g.DrawString(text, font, brush, rect, format);
            }
        }

        private static string FormatDate(DateTime now)
        {
            CultureInfo culture = CultureInfo.CurrentUICulture;
            if (string.Equals(culture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase))
            {
                return now.ToString("M月d日 dddd", culture);
            }

            return now.ToString("dddd, MMMM d", culture);
        }

        private static string PromptText()
        {
            CultureInfo culture = CultureInfo.CurrentUICulture;
            if (string.Equals(culture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase))
            {
                return "单击或按键解锁";
            }

            return "Click or press a key to unlock";
        }

        private static void EnsureFontFamilies()
        {
            lock (FontLock)
            {
                if (fontFamiliesResolved)
                {
                    return;
                }

                using (InstalledFontCollection collection = new InstalledFontCollection())
                {
                    clockFamily = PickFamily(
                        collection,
                        new string[] { "Segoe UI Variable Display Light", "Segoe UI Variable Display", "Segoe UI Light", "Segoe UI" },
                        "Arial");
                    iconFamily = PickFamily(
                        collection,
                        new string[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" },
                        clockFamily);
                }

                fontFamiliesResolved = true;
            }
        }

        private static string PickFamily(InstalledFontCollection collection, string[] names, string fallback)
        {
            foreach (string name in names)
            {
                foreach (FontFamily family in collection.Families)
                {
                    if (string.Equals(family.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return name;
                    }
                }
            }

            return fallback;
        }
    }
}
