$ErrorActionPreference = 'Stop'

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script as administrator.'
}

$target = Join-Path $env:WINDIR 'System32\UbuntuLockSaver.scr'
$desktop = 'HKCU:\Control Panel\Desktop'
$backup = 'HKCU:\Software\UbuntuLockSaver'

if (Test-Path $backup) {
    $saved = Get-ItemProperty -Path $backup
    if ($saved.HadBackup -eq 1) {
        if ($saved.OldScr) {
            Set-ItemProperty -Path $desktop -Name 'SCRNSAVE.EXE' -Value $saved.OldScr
        }
        if ($saved.OldActive) {
            Set-ItemProperty -Path $desktop -Name 'ScreenSaveActive' -Value $saved.OldActive
        }
        if ($saved.OldTimeout) {
            Set-ItemProperty -Path $desktop -Name 'ScreenSaveTimeOut' -Value $saved.OldTimeout
        }
        if ($saved.OldSecure) {
            Set-ItemProperty -Path $desktop -Name 'ScreenSaverIsSecure' -Value $saved.OldSecure
        }
    }

    Remove-Item -Path $backup -Recurse -Force
}

if (Test-Path $target) {
    Remove-Item $target -Force
}

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class UbuntuLockSaverSpi {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SystemParametersInfo(uint action, uint param, IntPtr value, uint flags);
}
"@

[UbuntuLockSaverSpi]::SystemParametersInfo(0x0011, 1, [IntPtr]::Zero, 3) | Out-Null

Write-Host 'Ubuntu Lock Saver removed.'
