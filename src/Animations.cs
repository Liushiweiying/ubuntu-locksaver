using System;
using System.Diagnostics;

namespace UbuntuLockSaver
{
    internal sealed class AnimationState
    {
        public const double FadeInSeconds = 0.6;
        public const double FadeOutSeconds = 0.35;
        public const double ClockFadeSeconds = 0.25;
        public const double ZoomPeriodSeconds = 120.0;
        public const double ZoomAmplitude = 0.06;

        private readonly Stopwatch clock = Stopwatch.StartNew();

        public double Elapsed
        {
            get { return clock.Elapsed.TotalSeconds; }
        }

        public double FadeInValue
        {
            get
            {
                double p = Elapsed / FadeInSeconds;
                if (p >= 1.0)
                {
                    return 1.0;
                }

                double q = 1.0 - p;
                return 1.0 - q * q * q;
            }
        }

        public double Zoom
        {
            get
            {
                double phase = (Elapsed % ZoomPeriodSeconds) / ZoomPeriodSeconds;
                return 1.0 + ZoomAmplitude * (0.5 - 0.5 * Math.Cos(2.0 * Math.PI * phase));
            }
        }
    }
}
