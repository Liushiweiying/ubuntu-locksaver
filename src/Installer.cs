using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UbuntuLockSaver
{
    internal static class Installer
    {
        private const string ProductDirectory = "UbuntuLockSaver";
        private const string ScreensaverName = "UbuntuLockSaver.scr";
        private const string DesktopKey = @"Control Panel\Desktop";
        private const string BackupKey = @"Software\UbuntuLockSaver";
        private const int DefaultTimeoutSeconds = 600;

        internal static void Install()
        {
            string directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ProductDirectory);
            Directory.CreateDirectory(directory);
            string target = Path.Combine(directory, ScreensaverName);

            string source = Application.ExecutablePath;
            if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(source, target, true);
            }

            using (RegistryKey desktop = Registry.CurrentUser.OpenSubKey(DesktopKey, true))
            {
                if (desktop == null)
                {
                    return;
                }

                SaveBackup(desktop, target);

                string timeout = desktop.GetValue("ScreenSaveTimeOut") as string;
                int seconds;
                if (timeout == null || !int.TryParse(timeout, out seconds) || seconds < 60)
                {
                    seconds = DefaultTimeoutSeconds;
                }

                desktop.SetValue("SCRNSAVE.EXE", target, RegistryValueKind.String);
                desktop.SetValue("ScreenSaveActive", "1", RegistryValueKind.String);
                desktop.SetValue("ScreenSaveTimeOut", seconds.ToString(), RegistryValueKind.String);

                Native.SystemParametersInfo(Native.SPI_SETSCREENSAVEACTIVE, 1, IntPtr.Zero, Native.SPIF_UPDATEINIFILE | Native.SPIF_SENDCHANGE);
                Native.SystemParametersInfo(Native.SPI_SETSCREENSAVETIMEOUT, (uint)seconds, IntPtr.Zero, Native.SPIF_UPDATEINIFILE | Native.SPIF_SENDCHANGE);
            }

            Diagnostics.Write("Installed " + target);
        }

        internal static void Uninstall()
        {
            string target = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ProductDirectory,
                ScreensaverName);

            int active = 0;
            int timeout = DefaultTimeoutSeconds;

            using (RegistryKey desktop = Registry.CurrentUser.OpenSubKey(DesktopKey, true))
            {
                if (desktop != null)
                {
                    string current = desktop.GetValue("SCRNSAVE.EXE") as string;
                    if (current != null && string.Equals(Path.GetFullPath(current), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                    {
                        RestoreBackup(desktop);
                    }

                    string activeValue = desktop.GetValue("ScreenSaveActive") as string;
                    if (activeValue == "1")
                    {
                        active = 1;
                    }

                    string timeoutValue = desktop.GetValue("ScreenSaveTimeOut") as string;
                    int parsed;
                    if (timeoutValue != null && int.TryParse(timeoutValue, out parsed) && parsed > 0)
                    {
                        timeout = parsed;
                    }
                }
            }

            Native.SystemParametersInfo(Native.SPI_SETSCREENSAVEACTIVE, (uint)active, IntPtr.Zero, Native.SPIF_UPDATEINIFILE | Native.SPIF_SENDCHANGE);
            Native.SystemParametersInfo(Native.SPI_SETSCREENSAVETIMEOUT, (uint)timeout, IntPtr.Zero, Native.SPIF_UPDATEINIFILE | Native.SPIF_SENDCHANGE);

            try
            {
                if (File.Exists(target))
                {
                    File.Delete(target);
                }

                string directory = Path.GetDirectoryName(target);
                if (directory != null && Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length == 0)
                {
                    Directory.Delete(directory);
                }
            }
            catch (Exception exception)
            {
                Diagnostics.WriteException("Uninstall", exception);
            }

            Diagnostics.Write("Uninstalled " + target);
        }

        private static void SaveBackup(RegistryKey desktop, string target)
        {
            using (RegistryKey backup = Registry.CurrentUser.CreateSubKey(BackupKey))
            {
                if (backup == null)
                {
                    return;
                }

                string current = desktop.GetValue("SCRNSAVE.EXE") as string;
                bool alreadyOurs = current != null &&
                    string.Equals(Path.GetFullPath(current), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase);
                if (alreadyOurs)
                {
                    return;
                }

                backup.SetValue("HadBackup", 1, RegistryValueKind.DWord);
                CopyValue(desktop, backup, "SCRNSAVE.EXE", "OldScr");
                CopyValue(desktop, backup, "ScreenSaveActive", "OldActive");
                CopyValue(desktop, backup, "ScreenSaveTimeOut", "OldTimeout");
                CopyValue(desktop, backup, "ScreenSaverIsSecure", "OldSecure");
            }
        }

        private static void RestoreBackup(RegistryKey desktop)
        {
            using (RegistryKey backup = Registry.CurrentUser.OpenSubKey(BackupKey, false))
            {
                if (backup == null || backup.GetValue("HadBackup") == null)
                {
                    desktop.DeleteValue("SCRNSAVE.EXE", false);
                    return;
                }

                CopyValue(backup, desktop, "OldScr", "SCRNSAVE.EXE");
                CopyValue(backup, desktop, "OldActive", "ScreenSaveActive");
                CopyValue(backup, desktop, "OldTimeout", "ScreenSaveTimeOut");
                CopyValue(backup, desktop, "OldSecure", "ScreenSaverIsSecure");
            }

            Registry.CurrentUser.DeleteSubKeyTree(BackupKey, false);
        }

        private static void CopyValue(RegistryKey from, RegistryKey to, string sourceName, string targetName)
        {
            object value = from.GetValue(sourceName);
            if (value == null)
            {
                to.DeleteValue(targetName, false);
            }
            else if (value is int)
            {
                to.SetValue(targetName, value, RegistryValueKind.DWord);
            }
            else
            {
                to.SetValue(targetName, value.ToString(), RegistryValueKind.String);
            }
        }
    }
}
