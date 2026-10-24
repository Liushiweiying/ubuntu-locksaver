using System;
using System.Windows.Forms;

namespace UbuntuLockSaver
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Diagnostics.Attach();
            Native.SetDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string argument = args.Length > 0 ? args[0].Trim() : "/c";
            string lower = argument.ToLowerInvariant();

            if (lower.StartsWith("/p"))
            {
                IntPtr previewHandle = ParseHandle(argument, args);
                if (previewHandle != IntPtr.Zero)
                {
                    Application.Run(new PreviewContext(previewHandle));
                    return;
                }

                Application.Run(new ConfigForm());
                return;
            }

            if (lower.StartsWith("/s"))
            {
                try
                {
                    ScreensaverSession.Run();
                }
                catch (Exception exception)
                {
                    Diagnostics.WriteException("ScreensaverSession", exception);
                    throw;
                }

                return;
            }

            Application.Run(new ConfigForm());
        }

        private static IntPtr ParseHandle(string argument, string[] args)
        {
            string value = null;

            int colon = argument.IndexOf(':');
            if (colon >= 0 && colon + 1 < argument.Length)
            {
                value = argument.Substring(colon + 1).Trim();
            }
            else if (argument.Length > 2)
            {
                value = argument.Substring(2).Trim();
            }
            else if (args.Length > 1)
            {
                value = args[1].Trim();
            }

            long handle;
            if (value != null && long.TryParse(value, out handle) && handle != 0)
            {
                return new IntPtr(handle);
            }

            return IntPtr.Zero;
        }
    }
}
