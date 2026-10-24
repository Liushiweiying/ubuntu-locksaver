using System;
using System.IO;

namespace UbuntuLockSaver
{
    internal static class Diagnostics
    {
        private static readonly object Gate = new object();

        public static string LogPath
        {
            get { return Path.Combine(Path.GetTempPath(), "UbuntuLockSaver.log"); }
        }

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    File.AppendAllText(
                        LogPath,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine);
                }
            }
            catch (Exception)
            {
            }
        }

        public static void WriteException(string context, Exception exception)
        {
            Write(context + ": " + exception);
        }

        public static void Attach()
        {
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs args)
            {
                WriteException("UnhandledException", args.ExceptionObject as Exception);
            };

            System.Windows.Forms.Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs args)
            {
                WriteException("ThreadException", args.Exception);
            };
        }
    }
}
