# Claude Switcher installer for Windows.
#   irm https://make-become.github.io/claude-switcher/install.ps1 | iex
# Downloads the latest release into %LOCALAPPDATA%\Programs\Claude Switcher (for your user only, no
# administrator rights), clears the downloaded-from-the-internet mark (the program is open source
# but not signed), and opens it. It then lists itself in Settings > Apps, where it can be uninstalled.
# Touches nothing else: Claude Desktop and your accounts are left as they are.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$repo = 'make-become/claude-switcher'
$dest = if ($env:CLAUDE_SWITCHER_DEST) { $env:CLAUDE_SWITCHER_DEST } else { Join-Path $env:LOCALAPPDATA 'Programs\Claude Switcher' }
$exe = Join-Path $dest 'ClaudeSwitcher.exe'
$url = "https://github.com/$repo/releases/latest/download/Claude-Switcher-Windows.zip"

if ($env:OS -ne 'Windows_NT') { Write-Output 'This installer is for Windows; on a Mac: curl -fsSL https://make-become.github.io/claude-switcher/install.sh | sh'; return }
if (-not (Test-Path (Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps\claude-desktop.exe')) -and
    -not (Test-Path (Join-Path $env:LOCALAPPDATA 'AnthropicClaude\claude.exe'))) {
    Write-Output 'Note: Claude Desktop is not installed yet. Get it from https://claude.ai/download'
}

$tmp = Join-Path ([IO.Path]::GetTempPath()) ('claude-switcher-' + [guid]::NewGuid())
New-Item -ItemType Directory $tmp | Out-Null
try {
    if ($env:CLAUDE_SWITCHER_ZIP) {
        # A local build instead of the release (to test this installer).
        Copy-Item $env:CLAUDE_SWITCHER_ZIP (Join-Path $tmp 'claude-switcher.zip')
    } else {
        Write-Output "-> Downloading $url"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest $url -OutFile (Join-Path $tmp 'claude-switcher.zip') -UseBasicParsing
    }
    Expand-Archive (Join-Path $tmp 'claude-switcher.zip') $tmp
    if (-not (Test-Path (Join-Path $tmp 'ClaudeSwitcher.exe'))) { throw 'The download did not contain ClaudeSwitcher.exe' }

    Write-Output "-> Installing into $dest"
    New-Item -ItemType Directory -Force $dest | Out-Null
    # Quit the running copy (by its path, so nothing else is touched) so it can be replaced;
    # profile shortcuts point at the same path and keep working.
    Get-Process ClaudeSwitcher -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe } | ForEach-Object { $_.Kill(); $_.WaitForExit(5000) | Out-Null }
    Copy-Item (Join-Path $tmp 'ClaudeSwitcher.exe') $exe -Force
    Unblock-File $exe
} finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Output '-> Opening Claude Switcher'
Start-Process $exe
Write-Output "OK Installed: $exe"
Write-Output '   Click Claude Switcher in the Start menu (or the dots in the notification area) and choose "New profile..." to add an account.'
