using System;
using System.Runtime.InteropServices;

namespace UbuntuLockSaver
{
    internal static class Native
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        internal static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        internal static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SystemParametersInfo(uint action, uint param, IntPtr value, uint flags);

        internal const uint SPI_SETSCREENSAVETIMEOUT = 0x000F;
        internal const uint SPI_SETSCREENSAVEACTIVE = 0x0011;
        internal const uint SPIF_UPDATEINIFILE = 0x01;
        internal const uint SPIF_SENDCHANGE = 0x02;

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        internal static void SetDpiAwareness()
        {
            try
            {
                IntPtr perMonitorV2 = new IntPtr(-4);
                if (SetProcessDpiAwarenessContext(perMonitorV2))
                {
                    return;
                }
            }
            catch (EntryPointNotFoundException)
            {
            }

            try
            {
                SetProcessDPIAware();
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }
}
