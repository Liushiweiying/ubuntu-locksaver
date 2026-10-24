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
        private Point initialCursor;

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

        public void RequestClose()
        {
            if (closing)
            {
                return;
            }

            closing = true;
            closeAt = state.Elapsed;
            closeFrom = state.FadeInValue;
            timer.Interval = 16;
        }

        private void Start()
        {
            Diagnostics.Write("Session start");
            Cursor.Hide();
            initialCursor = Cursor.Position;

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

                if (state.Elapsed > 0.5)
                {
                    Point current = Cursor.Position;
                    if (Math.Abs(current.X - initialCursor.X) > 8 || Math.Abs(current.Y - initialCursor.Y) > 8)
                    {
                        RequestClose();
                        opacity = state.FadeInValue;
                    }
                }
            }

            foreach (ScreensaverForm form in forms)
            {
                form.Fade = opacity;
                form.Invalidate();
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
            Cursor.Show();
        }
    }
}
