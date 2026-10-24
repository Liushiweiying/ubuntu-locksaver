using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UbuntuLockSaver
{
    internal sealed class ScreensaverSession
    {
        private readonly List<ScreensaverForm> forms = new List<ScreensaverForm>();
        private readonly List<Bitmap> backdrops = new List<Bitmap>();
        private readonly Timer timer = new Timer();
        private readonly ApplicationContext context;
        private AnimationState state;
        private bool closing;
        private double closeAt;
        private double closeFrom;
        private double lastPaint;
        private bool cursorVisible = true;

        private ScreensaverSession()
        {
            context = new ApplicationContext();
        }

        public static void Run()
        {
            ScreensaverSession session = new ScreensaverSession();
            session.Start();
            Application.Run(session.context);
        }

        public void ShowCursor()
        {
            if (!cursorVisible)
            {
                Cursor.Show();
                cursorVisible = true;
            }
        }

        public void HideCursor()
        {
            if (cursorVisible)
            {
                Cursor.Hide();
                cursorVisible = false;
            }
        }

        public void RequestClose()
        {
            if (closing)
            {
                return;
            }

            Diagnostics.Write("RequestClose");
            closing = true;
            closeAt = state.Elapsed;
            closeFrom = state.FadeInValue;
            timer.Interval = 16;
        }

        private void Start()
        {
            Diagnostics.Write("Session start");
            cursorVisible = true;
            Cursor.Hide();
            cursorVisible = false;

            try
            {
                foreach (Screen screen in Screen.AllScreens)
                {
                    Bitmap backdrop = Wallpaper.CreateBlurredBackdrop(screen.Bounds);
                    backdrops.Add(backdrop);
                }

                state = new AnimationState();

                for (int i = 0; i < Screen.AllScreens.Length; i++)
                {
                    forms.Add(new ScreensaverForm(Screen.AllScreens[i], backdrops[i], state, this));
                }
            }
            catch (Exception)
            {
                Cleanup();
                throw;
            }

            foreach (ScreensaverForm form in forms)
            {
                form.Show();
            }

            if (forms.Count > 0)
            {
                forms[0].Activate();
            }

            timer.Interval = 16;
            timer.Tick += OnTick;
            timer.Start();
            Diagnostics.Write("Session shown, screens=" + forms.Count);
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (state == null)
            {
                return;
            }

            double opacity;

            if (closing)
            {
                double progress = (state.Elapsed - closeAt) / AnimationState.FadeOutSeconds;
                if (progress >= 1.0)
                {
                    Finish();
                    return;
                }

                opacity = closeFrom * (1.0 - progress);
            }
            else
            {
                opacity = state.FadeInValue;
            }

            double frameInterval = 0.033;
            for (int i = 0; i < forms.Count; i++)
            {
                if (forms[i].IsMenuVisible)
                {
                    frameInterval = 0.016;
                    break;
                }
            }

            bool animateContent = !closing && opacity >= 0.999 && state.Elapsed - lastPaint >= frameInterval;
            foreach (ScreensaverForm form in forms)
            {
                form.Fade = opacity;
                if (animateContent)
                {
                    form.Invalidate();
                }
            }

            if (animateContent)
            {
                lastPaint = state.Elapsed;
            }
        }

        private void Finish()
        {
            Diagnostics.Write("Session finish");
            timer.Stop();
            Cleanup();
            context.ExitThread();
        }

        private void Cleanup()
        {
            foreach (ScreensaverForm form in forms)
            {
                form.Hide();
                form.Close();
                form.Dispose();
            }

            forms.Clear();

            foreach (Bitmap backdrop in backdrops)
            {
                backdrop.Dispose();
            }

            backdrops.Clear();
            if (!cursorVisible)
            {
                Cursor.Show();
                cursorVisible = true;
            }
        }
    }
}
