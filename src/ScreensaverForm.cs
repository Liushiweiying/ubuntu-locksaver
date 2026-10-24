using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace UbuntuLockSaver
{
    internal sealed class ScreensaverForm : Form
    {
        private readonly Renderer renderer;
        private readonly ScreensaverSession session;
        private readonly Stopwatch shownFor = Stopwatch.StartNew();
        private readonly Point initialCursor;

        public ScreensaverForm(Screen screen, Bitmap backdrop, AnimationState state, ScreensaverSession session)
        {
            this.session = session;
            renderer = new Renderer(backdrop, screen.Bounds, state, false);

            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            Bounds = screen.Bounds;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            Opacity = 0.0;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Ubuntu Lock Saver";
            TopMost = true;

            initialCursor = Cursor.Position;
        }

        public double Fade
        {
            set
            {
                double fade = Math.Max(0.0, Math.Min(1.0, value));
                if (Math.Abs(Opacity - fade) > 0.001)
                {
                    Opacity = fade;
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            renderer.Draw(e.Graphics, DateTime.Now);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            session.RequestClose();
            e.Handled = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            session.RequestClose();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (shownFor.ElapsedMilliseconds < 500)
            {
                return;
            }

            Point current = Cursor.Position;
            if (Math.Abs(current.X - initialCursor.X) > 8 || Math.Abs(current.Y - initialCursor.Y) > 8)
            {
                session.RequestClose();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            renderer.Dispose();
            base.OnFormClosed(e);
        }
    }
}
