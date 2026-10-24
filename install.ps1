param(
    [int] $TimeoutMinutes = 10,
    [switch] $Secure
)

$ErrorActionPreference = 'Stop'

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script as administrator.'
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = Join-Path $root 'dist\UbuntuLockSaver.scr'
if (-not (Test-Path $source)) {
    throw "Missing $source. Run build.ps1 first."
}

$target = Join-Path $env:WINDIR 'System32\UbuntuLockSaver.scr'
$desktop = 'HKCU:\Control Panel\Desktop'
$backup = 'HKCU:\Software\UbuntuLockSaver'

if (-not (Test-Path $backup)) {
    New-Item -Path $backup -Force | Out-Null
}

if (-not (Get-ItemProperty -Path $backup -Name HadBackup -ErrorAction SilentlyContinue)) {
    $current = Get-ItemProperty -Path $desktop
    New-ItemProperty -Path $backup -Name OldScr -Value ([string]$current.'SCRNSAVE.EXE') -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $backup -Name OldActive -Value ([string]$current.ScreenSaveActive) -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $backup -Name OldTimeout -Value ([string]$current.ScreenSaveTimeOut) -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $backup -Name OldSecure -Value ([string]$current.ScreenSaverIsSecure) -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $backup -Name HadBackup -Value 1 -PropertyType DWord -Force | Out-Null
}

Copy-Item $source $target -Force

$timeoutSeconds = [Math]::Max(60, $TimeoutMinutes * 60)
$secureValue = if ($Secure) { '1' } else { '0' }

Set-ItemProperty -Path $desktop -Name 'SCRNSAVE.EXE' -Value $target
Set-ItemProperty -Path $desktop -Name 'ScreenSaveActive' -Value '1'
Set-ItemProperty -Path $desktop -Name 'ScreenSaveTimeOut' -Value ([string]$timeoutSeconds)
Set-ItemProperty -Path $desktop -Name 'ScreenSaverIsSecure' -Value $secureValue

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class UbuntuLockSaverSpi {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SystemParametersInfo(uint action, uint param, IntPtr value, uint flags);
}
"@

[UbuntuLockSaverSpi]::SystemParametersInfo(0x0011, 1, [IntPtr]::Zero, 3) | Out-Null
[UbuntuLockSaverSpi]::SystemParametersInfo(0x000F, [uint32]$timeoutSeconds, [IntPtr]::Zero, 3) | Out-Null

Write-Host "Installed $target"
Write-Host "Timeout: $timeoutSeconds seconds; secure: $secureValue"
Start-Process 'control.exe' -ArgumentList 'desk.cpl,,@screensaver'
