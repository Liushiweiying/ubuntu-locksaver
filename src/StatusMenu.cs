using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace UbuntuLockSaver
{
    internal sealed class StatusMenu
    {
        private const double OpenSeconds = 0.18;
        private const float ShadowAlpha = 46f;

        private sealed class ToggleItem
        {
            public string Glyph;
            public string Label;
            public bool On;
            public Rectangle Bounds;
        }

        private sealed class SliderItem
        {
            public string Glyph;
            public double Value;
            public Rectangle Row;
            public Rectangle Track;
        }

        private static readonly StringFormat CenterFormat;
        private static readonly StringFormat LeftFormat;
        private static readonly StringFormat RightFormat;

        private readonly Rectangle screen;
        private readonly Renderer renderer;
        private readonly AnimationState state;
        private readonly Font labelFont;
        private readonly Font valueFont;
        private readonly Font iconFont;
        private readonly Font tileLabelFont;
        private readonly ToggleItem[] toggles;
        private readonly SliderItem[] sliders;
        private Rectangle panel;
        private Rectangle iconArea;
        private bool open;
        private double openedAt;
        private bool lightPanel;
        private Color textColor;
        private Color mutedColor;
        private Color separatorColor;
        private Color panelFill;
        private Color panelBorder;
        private Color offFill;
        private Color hoverFill;
        private Color trackColor;
        private static readonly Color AccentColor = Color.FromArgb(255, 53, 132, 228);
        private ToggleItem hoverToggle;
        private SliderItem dragSlider;

        static StatusMenu()
        {
            CenterFormat = new StringFormat();
            CenterFormat.Alignment = StringAlignment.Center;
            CenterFormat.LineAlignment = StringAlignment.Center;

            LeftFormat = new StringFormat();
            LeftFormat.Alignment = StringAlignment.Near;
            LeftFormat.LineAlignment = StringAlignment.Center;

            RightFormat = new StringFormat();
            RightFormat.Alignment = StringAlignment.Far;
            RightFormat.LineAlignment = StringAlignment.Center;
        }

        public StatusMenu(Rectangle bounds, AnimationState state, Renderer renderer)
        {
            this.screen = bounds;
            this.state = state;
            this.renderer = renderer;

            float h = bounds.Height;
            iconFont = new Font(Renderer.IconFontFamily, Math.Max(10f, h * 0.028f), FontStyle.Regular, GraphicsUnit.Pixel);
            labelFont = new Font(Renderer.UiFontFamily, Math.Max(10f, h * 0.021f), FontStyle.Regular, GraphicsUnit.Pixel);
            valueFont = new Font(Renderer.UiFontFamily, Math.Max(9f, h * 0.019f), FontStyle.Regular, GraphicsUnit.Pixel);
            tileLabelFont = new Font(Renderer.UiFontFamily, Math.Max(9f, h * 0.017f), FontStyle.Regular, GraphicsUnit.Pixel);

            toggles = new ToggleItem[]
            {
                new ToggleItem { Glyph = "\uE701", Label = "Wi-Fi", On = true },
                new ToggleItem { Glyph = "\uE702", Label = "蓝牙", On = false },
                new ToggleItem { Glyph = "\uE708", Label = "夜间模式", On = false }
            };

            sliders = new SliderItem[]
            {
                new SliderItem { Glyph = "\uE767", Value = 0.72 },
                new SliderItem { Glyph = "\uE706", Value = 0.85 }
            };

            iconArea = renderer.StatusIconArea;
            Layout();
        }

        public bool IsOpen
        {
            get { return open; }
        }

        public void Open()
        {
            if (open)
            {
                return;
            }

            open = true;
            openedAt = state.Elapsed;
            dragSlider = null;
            hoverToggle = null;
            ResolveTheme();
            Layout();
        }

        public void Close()
        {
            open = false;
            dragSlider = null;
            hoverToggle = null;
        }

        public bool ContainsStatusIcons(Point point)
        {
            return iconArea.Contains(point);
        }

        public bool PanelContains(Point point)
        {
            return panel.Contains(point);
        }

        public bool MouseDown(Point point)
        {
            if (!open || !panel.Contains(point))
            {
                return false;
            }

            for (int i = 0; i < toggles.Length; i++)
            {
                if (toggles[i].Bounds.Contains(point))
                {
                    toggles[i].On = !toggles[i].On;
                    return true;
                }
            }

            for (int i = 0; i < sliders.Length; i++)
            {
                if (sliders[i].Row.Contains(point))
                {
                    dragSlider = sliders[i];
                    SetSliderFromPoint(sliders[i], point.X);
                    return true;
                }
            }

            return true;
        }

        public void MouseMove(Point point)
        {
            if (!open)
            {
                return;
            }

            hoverToggle = null;
            for (int i = 0; i < toggles.Length; i++)
            {
                if (toggles[i].Bounds.Contains(point))
                {
                    hoverToggle = toggles[i];
                    break;
                }
            }

            if (dragSlider != null)
            {
                SetSliderFromPoint(dragSlider, point.X);
            }
        }

        public void MouseUp()
        {
            dragSlider = null;
        }

        public void Draw(Graphics g, double elapsed)
        {
            if (!open)
            {
                return;
            }

            double progress = (elapsed - openedAt) / OpenSeconds;
            if (progress < 0)
            {
                progress = 0;
            }
            else if (progress > 1)
            {
                progress = 1;
            }

            float eased = (float)(1.0 - Math.Pow(1.0 - progress, 3.0));
            float offset = (1f - eased) * (float)(screen.Height * 0.007);
            float alpha = eased;

            GraphicsState saved = g.Save();
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.TranslateTransform(0f, offset);

            RectangleF panelRect = new RectangleF(panel.X, panel.Y, panel.Width, panel.Height);
            float radius = (float)(screen.Height * 0.014);

            using (GraphicsPath shadowPath = RoundedRect(new RectangleF(panelRect.X, panelRect.Y + 4f, panelRect.Width, panelRect.Height), radius))
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb((int)(ShadowAlpha * alpha), 0, 0, 0)))
            {
                g.FillPath(shadow, shadowPath);
            }

            using (GraphicsPath path = RoundedRect(panelRect, radius))
            {
                using (SolidBrush fill = new SolidBrush(WithAlpha(panelFill, alpha)))
                {
                    g.FillPath(fill, path);
                }

                using (Pen border = new Pen(WithAlpha(panelBorder, alpha), 1f))
                {
                    g.DrawPath(border, path);
                }

                GraphicsState clip = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                DrawContent(g, alpha);
                g.Restore(clip);
            }

            g.Restore(saved);
        }

        public void Dispose()
        {
            labelFont.Dispose();
            valueFont.Dispose();
            iconFont.Dispose();
            tileLabelFont.Dispose();
        }

        private void DrawContent(Graphics g, float alpha)
        {
            for (int i = 0; i < toggles.Length; i++)
            {
                DrawToggle(g, toggles[i], alpha);
            }

            float separatorY = sliders[0].Row.Top - (slotGap() * 0.5f);
            DrawSeparator(g, separatorY, alpha);

            for (int i = 0; i < sliders.Length; i++)
            {
                DrawSlider(g, sliders[i], alpha);
            }
        }

        private float slotGap()
        {
            return (float)(screen.Height * 0.020);
        }

        private void DrawToggle(Graphics g, ToggleItem item, float alpha)
        {
            float radius = (float)(screen.Height * 0.008);
            using (GraphicsPath path = RoundedRect(new RectangleF(item.Bounds.X, item.Bounds.Y, item.Bounds.Width, item.Bounds.Height), radius))
            {
                Color fill;
                if (item.On)
                {
                    fill = AccentColor;
                }
                else if (item == hoverToggle)
                {
                    fill = hoverFill;
                }
                else
                {
                    fill = offFill;
                }

                using (SolidBrush brush = new SolidBrush(WithAlpha(fill, alpha)))
                {
                    g.FillPath(brush, path);
                }
            }

            Color contentColor = item.On ? Color.FromArgb(255, 255, 255, 255) : mutedColor;
            float iconHeight = item.Bounds.Height * 0.5f;
            RectangleF iconRect = new RectangleF(
                item.Bounds.X,
                item.Bounds.Y + item.Bounds.Height * 0.12f,
                item.Bounds.Width,
                iconHeight);
            DrawText(g, item.Glyph, iconFont, contentColor, iconRect, CenterFormat, alpha);

            RectangleF labelRect = new RectangleF(
                item.Bounds.X + 4f,
                item.Bounds.Y + item.Bounds.Height * 0.64f,
                item.Bounds.Width - 8f,
                item.Bounds.Height * 0.28f);
            DrawText(g, item.Label, tileLabelFont, contentColor, labelRect, CenterFormat, alpha);
        }

        private void DrawSlider(Graphics g, SliderItem item, float alpha)
        {
            float iconWidth = (float)(screen.Height * 0.032);
            float iconX = item.Row.X;
            RectangleF iconRect = new RectangleF(iconX, item.Row.Y, iconWidth, item.Row.Height);
            DrawText(g, item.Glyph, iconFont, textColor, iconRect, CenterFormat, alpha);

            float valueWidth = (float)(screen.Height * 0.048);
            RectangleF valueRect = new RectangleF(item.Row.Right - valueWidth, item.Row.Y, valueWidth, item.Row.Height);
            DrawText(g, ((int)Math.Round(item.Value * 100.0)) + "%", valueFont, mutedColor, valueRect, RightFormat, alpha);

            float trackHeight = (float)(screen.Height * 0.008);
            RectangleF track = item.Track.Width > 0
                ? new RectangleF(item.Track.X, item.Track.Y, item.Track.Width, item.Track.Height)
                : new RectangleF(
                    iconX + iconWidth + (float)(screen.Height * 0.010),
                    item.Row.Y + (item.Row.Height - trackHeight) * 0.5f,
                    Math.Max(1f, valueRect.X - (iconX + iconWidth + (float)(screen.Height * 0.010)) - (float)(screen.Height * 0.012)),
                    trackHeight);

            using (GraphicsPath trackPath = RoundedRect(track, trackHeight * 0.5f))
            using (SolidBrush trackBrush = new SolidBrush(WithAlpha(trackColor, alpha)))
            {
                g.FillPath(trackBrush, trackPath);
            }

            float knobX = track.X + (float)(track.Width * item.Value);
            RectangleF filled = new RectangleF(track.X, track.Y, Math.Max(0f, knobX - track.X), track.Height);
            using (GraphicsPath filledPath = RoundedRect(filled, trackHeight * 0.5f))
            using (SolidBrush filledBrush = new SolidBrush(WithAlpha(AccentColor, alpha)))
            {
                g.FillPath(filledBrush, filledPath);
            }

            float knobRadius = (float)(screen.Height * 0.011);
            RectangleF knob = new RectangleF(knobX - knobRadius, track.Y + track.Height * 0.5f - knobRadius, knobRadius * 2f, knobRadius * 2f);
            using (SolidBrush knobBrush = new SolidBrush(WithAlpha(Color.FromArgb(255, 255, 255, 255), alpha)))
            {
                g.FillEllipse(knobBrush, knob);
            }

            using (Pen knobBorder = new Pen(WithAlpha(Color.FromArgb(60, 0, 0, 0), alpha), 1f))
            {
                g.DrawEllipse(knobBorder, knob);
            }
        }

        private void DrawSeparator(Graphics g, float y, float alpha)
        {
            using (Pen pen = new Pen(WithAlpha(separatorColor, alpha), 1f))
            {
                g.DrawLine(pen, panel.X + slotGap(), y, panel.Right - slotGap(), y);
            }
        }

        private void DrawText(Graphics g, string text, Font font, Color color, RectangleF rect, StringFormat format, float alpha)
        {
            using (SolidBrush brush = new SolidBrush(WithAlpha(color, alpha)))
            {
                g.DrawString(text, font, brush, rect, format);
            }
        }

        private void Layout()
        {
            float h = screen.Height;
            float w = screen.Width;
            float pad = (float)(h * 0.020);
            float panelWidth = Math.Min((float)(h * 0.33), w * 0.5f);
            float right = w - (float)(h * 0.03);
            float top = iconArea.Bottom + (float)(h * 0.012);
            panel = new Rectangle((int)(right - panelWidth), (int)top, (int)panelWidth, 1);

            float contentLeft = panel.X + pad;
            float contentWidth = panel.Width - pad * 2f;
            float gap = (float)(h * 0.010);
            float tile = (contentWidth - gap * 2f) / 3f;
            float tileHeight = tile * 0.9f;
            float tileTop = panel.Y + pad;

            for (int i = 0; i < toggles.Length; i++)
            {
                toggles[i].Bounds = new Rectangle(
                    (int)(contentLeft + i * (tile + gap)),
                    (int)tileTop,
                    (int)tile,
                    (int)tileHeight);
            }

            float rowHeight = (float)(h * 0.052);
            float slidersTop = tileTop + tileHeight + (float)(h * 0.035);
            float iconWidth = (float)(h * 0.032);
            float valueWidth = (float)(h * 0.048);
            float trackHeight = (float)(h * 0.008);
            float trackLeft = contentLeft + iconWidth + (float)(h * 0.010);
            float trackRight = contentLeft + contentWidth - valueWidth - (float)(h * 0.012);
            for (int i = 0; i < sliders.Length; i++)
            {
                Rectangle row = new Rectangle(
                    (int)contentLeft,
                    (int)(slidersTop + i * rowHeight),
                    (int)contentWidth,
                    (int)rowHeight);
                sliders[i].Row = row;
                sliders[i].Track = new Rectangle(
                    (int)trackLeft,
                    (int)(row.Y + (row.Height - trackHeight) * 0.5f),
                    (int)Math.Max(1f, trackRight - trackLeft),
                    (int)trackHeight);
            }

            int bottom = (int)(sliders[sliders.Length - 1].Row.Bottom + pad * 0.75f);
            panel.Height = bottom - panel.Y;
        }

        private void ResolveTheme()
        {
            double luminance = SampleLuminance(panel);
            lightPanel = luminance > 0.55;

            if (lightPanel)
            {
                panelFill = Color.FromArgb(184, 250, 250, 252);
                panelBorder = Color.FromArgb(44, 0, 0, 0);
                textColor = Color.FromArgb(238, 18, 18, 20);
                mutedColor = Color.FromArgb(155, 70, 70, 74);
                separatorColor = Color.FromArgb(30, 0, 0, 0);
                offFill = Color.FromArgb(24, 0, 0, 0);
                hoverFill = Color.FromArgb(34, 0, 0, 0);
                trackColor = Color.FromArgb(36, 0, 0, 0);
            }
            else
            {
                panelFill = Color.FromArgb(172, 24, 24, 27);
                panelBorder = Color.FromArgb(54, 255, 255, 255);
                textColor = Color.FromArgb(240, 246, 246, 250);
                mutedColor = Color.FromArgb(160, 200, 200, 205);
                separatorColor = Color.FromArgb(36, 255, 255, 255);
                offFill = Color.FromArgb(30, 255, 255, 255);
                hoverFill = Color.FromArgb(42, 255, 255, 255);
                trackColor = Color.FromArgb(46, 255, 255, 255);
            }
        }

        private double SampleLuminance(Rectangle area)
        {
            Bitmap backdrop = renderer.Backdrop;
            Rectangle screenBounds = renderer.ScreenBounds;
            if (backdrop == null || screenBounds.Width <= 0 || screenBounds.Height <= 0 || area.Width <= 0 || area.Height <= 0)
            {
                return 0.5;
            }

            double scaleX = backdrop.Width / (double)screenBounds.Width;
            double scaleY = backdrop.Height / (double)screenBounds.Height;
            int step = Math.Max(8, area.Width / 24);
            double sum = 0.0;
            int count = 0;

            for (int y = area.Top + step / 2; y < area.Bottom; y += step)
            {
                if (y < 0 || y >= screenBounds.Height)
                {
                    continue;
                }

                int sourceY = (int)(y * scaleY);
                if (sourceY < 0 || sourceY >= backdrop.Height)
                {
                    continue;
                }

                for (int x = area.Left + step / 2; x < area.Right; x += step)
                {
                    if (x < 0 || x >= screenBounds.Width)
                    {
                        continue;
                    }

                    int sourceX = (int)(x * scaleX);
                    if (sourceX < 0 || sourceX >= backdrop.Width)
                    {
                        continue;
                    }

                    Color color = backdrop.GetPixel(sourceX, sourceY);
                    sum += (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;
                    count++;
                }
            }

            return count > 0 ? sum / count : 0.5;
        }

        private void SetSliderFromPoint(SliderItem slider, int x)
        {
            Rectangle track = slider.Track;
            if (track.Width <= 0)
            {
                track = slider.Row;
            }

            double value = (x - track.Left) / (double)Math.Max(1, track.Width);
            if (value < 0.0)
            {
                value = 0.0;
            }
            else if (value > 1.0)
            {
                value = 1.0;
            }

            slider.Value = value;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            int a = (int)Math.Round(color.A * Math.Max(0f, Math.Min(1f, alpha)));
            return Color.FromArgb(a, color.R, color.G, color.B);
        }

        private static GraphicsPath RoundedRect(RectangleF rect, float radius)
        {
            float diameter = Math.Max(1f, radius * 2f);
            if (diameter > rect.Width)
            {
                diameter = rect.Width;
            }

            if (diameter > rect.Height)
            {
                diameter = rect.Height;
            }

            GraphicsPath path = new GraphicsPath();
            float x = rect.X;
            float y = rect.Y;
            float w = rect.Width;
            float h = rect.Height;

            path.AddArc(x, y, diameter, diameter, 180f, 90f);
            path.AddArc(x + w - diameter, y, diameter, diameter, 270f, 90f);
            path.AddArc(x + w - diameter, y + h - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(x, y + h - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
        }
    }
}
