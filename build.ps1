$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $compiler)) {
    throw 'C# compiler not found.'
}

$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$sources = Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object { $_.FullName }
$output = Join-Path $dist 'UbuntuLockSaver.exe'
$manifest = Join-Path $root 'app.manifest'

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 `
    ('/win32manifest:' + $manifest) `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    ('/out:' + $output) $sources

if ($LASTEXITCODE -ne 0) {
    throw "csc failed with exit code $LASTEXITCODE"
}

Copy-Item $output (Join-Path $dist 'UbuntuLockSaver.scr') -Force
Write-Host "Built $(Join-Path $dist 'UbuntuLockSaver.scr')"
