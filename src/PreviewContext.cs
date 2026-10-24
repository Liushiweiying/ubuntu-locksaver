using System;
using System.Drawing;
using System.Windows.Forms;

namespace UbuntuLockSaver
{
    internal sealed class PreviewContext : ApplicationContext
    {
        private readonly IntPtr parent;
        private readonly Timer timer;
        private readonly Bitmap backdrop;
        private readonly AnimationState state;
        private readonly Renderer renderer;

        public PreviewContext(IntPtr parent)
        {
            this.parent = parent;

            Native.RECT rect;
            Native.GetClientRect(parent, out rect);
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, rect.Right - rect.Left), Math.Max(1, rect.Bottom - rect.Top));

            backdrop = Wallpaper.CreateBlurredBackdrop(bounds);
            state = new AnimationState();
            renderer = new Renderer(backdrop, bounds, state, true);

            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += OnTick;
            timer.Start();

            OnTick(null, EventArgs.Empty);
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!Native.IsWindow(parent))
            {
                Shutdown();
                return;
            }

            Native.RECT rect;
            Native.GetClientRect(parent, out rect);
            if (rect.Right <= rect.Left || rect.Bottom <= rect.Top)
            {
                return;
            }

            using (Graphics g = Graphics.FromHwnd(parent))
            {
                renderer.Draw(g, DateTime.Now);
            }
        }

        private void Shutdown()
        {
            timer.Stop();
            renderer.Dispose();
            backdrop.Dispose();
            ExitThread();
        }
    }
}
