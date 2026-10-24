using System;
using System.Drawing;
using System.Windows.Forms;

namespace UbuntuLockSaver
{
    internal sealed class ScreensaverForm : Form
    {
        private readonly Renderer renderer;
        private readonly ScreensaverSession session;
        private readonly AnimationState animation;
        private readonly StatusMenu menu;

        public ScreensaverForm(Screen screen, Bitmap backdrop, AnimationState state, ScreensaverSession session)
        {
            this.session = session;
            this.animation = state;
            renderer = new Renderer(backdrop, screen.Bounds, state, false);
            menu = new StatusMenu(screen.Bounds, state, renderer);

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

            if (menu.IsOpen)
            {
                menu.Draw(e.Graphics, animation.Elapsed);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (menu.IsOpen)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    CloseMenu();
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            session.RequestClose();
            e.Handled = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (menu.IsOpen)
            {
                if (menu.PanelContains(e.Location))
                {
                    menu.MouseDown(e.Location);
                }
                else
                {
                    CloseMenu();
                }

                Invalidate();
                return;
            }

            if (renderer.StatusIconArea.Contains(e.Location))
            {
                OpenMenu();
                return;
            }

            session.RequestClose();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!menu.IsOpen)
            {
                return;
            }

            menu.MouseMove(e.Location);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            menu.MouseUp();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (menu.IsOpen)
            {
                menu.Close();
                session.HideCursor();
            }

            menu.Dispose();
            renderer.Dispose();
            base.OnFormClosed(e);
        }

        private void OpenMenu()
        {
            if (menu.IsOpen)
            {
                return;
            }

            menu.Open();
            session.ShowCursor();
            Invalidate();
        }

        private void CloseMenu()
        {
            if (!menu.IsOpen)
            {
                return;
            }

            menu.Close();
            session.HideCursor();
            Invalidate();
        }
    }
}
