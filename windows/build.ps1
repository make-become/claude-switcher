# Builds build\windows\ClaudeSwitcher.exe and build\Claude-Switcher-Windows.zip.
# Needs nothing beyond Windows: the C# compiler of .NET Framework 4.8, which Windows 10 and 11 ship.
#   powershell -ExecutionPolicy Bypass -File windows\build.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root                     # csc refuses long paths: everything below is relative
$out = 'build\windows'
New-Item -ItemType Directory -Force $out | Out-Null

# One version for the product, kept where the Mac build reads it.
$version = [regex]::Match((Get-Content 'Sources\main.swift' -Raw), 'switcherVersion = "([^"]+)"').Groups[1].Value
if (-not $version) { throw 'switcherVersion not found in Sources\main.swift' }
$numeric = (($version.Split('.') + @('0', '0', '0', '0'))[0..3]) -join '.'
@"
[assembly: System.Reflection.AssemblyTitle("Claude Switcher")]
[assembly: System.Reflection.AssemblyProduct("Claude Switcher")]
[assembly: System.Reflection.AssemblyCompany("make become")]
[assembly: System.Reflection.AssemblyCopyright("MIT licence. Unofficial: not affiliated with Anthropic.")]
[assembly: System.Reflection.AssemblyVersion("$numeric")]
[assembly: System.Reflection.AssemblyFileVersion("$numeric")]
[assembly: System.Reflection.AssemblyInformationalVersion("$version")]
"@ | Set-Content "$out\Version.cs" -Encoding UTF8

# The app icon, from the page's PNG: an .ico of PNG frames.
Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Image]::FromFile((Resolve-Path 'docs\img\app-icon.png'))
$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$frames = foreach ($s in $sizes) {
    $b = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($b)
    $g.InterpolationMode = 'HighQualityBicubic'; $g.DrawImage($src, 0, 0, $s, $s); $g.Dispose()
    $ms = New-Object System.IO.MemoryStream; $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
    , $ms.ToArray()
}
$src.Dispose()
$ico = New-Object System.IO.BinaryWriter ([System.IO.File]::Create((Join-Path $root "$out\AppIcon.ico")))
$ico.Write([uint16]0); $ico.Write([uint16]1); $ico.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $d = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $ico.Write([byte]$d); $ico.Write([byte]$d); $ico.Write([byte]0); $ico.Write([byte]0)
    $ico.Write([uint16]1); $ico.Write([uint16]32); $ico.Write([int]$frames[$i].Length); $ico.Write([int]$offset)
    $offset += $frames[$i].Length
}
foreach ($f in $frames) { $ico.Write($f) }
$ico.Dispose()

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw ".NET Framework 4.8's compiler is missing: $csc" }
$sources = @(Get-ChildItem 'windows\Sources\*.cs' | ForEach-Object { 'windows\Sources\' + $_.Name }) + "$out\Version.cs"
& $csc /nologo /target:winexe /optimize+ /warnaserror+ /out:"$out\ClaudeSwitcher.exe" `
    /win32icon:"$out\AppIcon.ico" /win32manifest:'windows\app.manifest' `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll `
    $sources
if ($LASTEXITCODE -ne 0) { throw "csc failed ($LASTEXITCODE)" }

$zip = 'build\Claude-Switcher-Windows.zip'
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path "$out\ClaudeSwitcher.exe" -DestinationPath $zip
Write-Output "built $out\ClaudeSwitcher.exe and $zip ($version)"
