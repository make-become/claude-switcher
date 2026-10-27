# Claude Switcher

A [make become](https://make-become.github.io/) project.

Several Claude Desktop accounts side by side on one Mac or Windows PC, one Dock or taskbar icon each.

**Unofficial.** Not made, endorsed or supported by Anthropic. It never modifies Claude Desktop; it only starts it.

**Page:** https://make-become.github.io/claude-switcher/ · **Details** (what a profile is on disk, everything it puts on your Mac): https://make-become.github.io/claude-switcher/details.html

## Install

**Mac:**

```bash
curl -fsSL https://make-become.github.io/claude-switcher/install.sh | sh
```

It downloads the latest release into `/Applications` (where Finder and Spotlight look; `~/Applications` if that is not writable), clears the download quarantine (the app is not notarised) and opens it. Read it first: [`docs/install.sh`](docs/install.sh).

**Windows 10 or 11** (PowerShell):

```powershell
irm https://make-become.github.io/claude-switcher/install.ps1 | iex
```

It downloads the latest release into `%LOCALAPPDATA%ProgramsClaude Switcher` (your user only, no administrator rights), clears the downloaded-from-the-internet mark (the program is not signed) and opens it. It is then listed in Settings → Apps, where it uninstalls. Read it first: [`docs/install.ps1`](docs/install.ps1).

## What it does

- **Profiles.** Each profile is one Claude Desktop with its own data folder, so its own login. All can be open at once.
- **A Dock icon per profile** with a coloured letter; the dot under it means that profile is open.
- **Never two copies of one profile**: an open profile is brought forward, not started again.
- **Menu** where the mouse is, in the Dock (right-click) and in the menu bar (one dot per profile).
- **⌥⌘1…9** bring a profile forward from any app.
- **Safe login** closes the other profiles while you sign in, so the browser's login callback reaches the right one.
- **Languages:** English, Italiano, Español, Français, Deutsch (from the Mac's settings).

### On Windows

The same, in Windows' terms:

- **A taskbar icon per profile**, with the same coloured letter: each profile's Claude windows get their own taskbar group, so they never stack under one Claude icon. Right-click it and *Pin to taskbar* to keep it there (Windows lets only you pin). "Own taskbar icon" turns it off per profile; the default profile starts without one, so your usual Claude icon is unchanged.
- **Start menu:** a *Claude Switcher* folder with the Switcher and one entry per profile. Opening the Switcher again pops its menu where the mouse is.
- **Notification area** instead of the menu bar: one dot per profile, filled when it is open.
- **Ctrl+Alt+Shift+1…9** bring a profile forward from any app (plain Ctrl+Alt is AltGr on many keyboards).
- **Safe login closes nothing.** On Windows a sign-in coming back from the browser (`claude://`) always starts the default profile's Claude. While one profile signs in, the Switcher catches that and hands the sign-in to the profile that asked, so the other profiles stay open.
- **Languages:** from Windows' display language.

## What a profile is on disk

| Where | What |
|---|---|
| `~/Library/Application Support/Claude` | Claude's own folder: your first profile. Never touched. |
| `~/Library/Application Support/Claude-<Name>` | Each extra profile's data, created by Claude Desktop on first launch. |
| `~/Applications/Claude <Name>.app` | The profile's launcher (its Dock icon). |
| `/Applications/Claude Switcher.app` | The app itself. |
| `~/Library/Application Support/Claude Switcher/profiles.json` | The profile list. |
| `~/Library/LaunchAgents/com.makebecome.claude-switcher.profile.<id>.plist` | Only with "Open when the Mac starts". |

On Windows:

| Where | What |
|---|---|
| `%APPDATA%Claude` | Claude's own folder: your first profile. Never touched. |
| `%APPDATA%Claude-<Name>` | Each extra profile's data, created by Claude Desktop on first launch. Claude from the Microsoft Store keeps it in its own copy of that folder, `%LOCALAPPDATA%PackagesClaude_pzs8sxrjxfjjcLocalCacheRoamingClaude-<Name>`. |
| `%LOCALAPPDATA%ProgramsClaude SwitcherClaudeSwitcher.exe` | The app itself. |
| `%APPDATA%Claude Switcherprofiles.json` | The profile list (same format as on the Mac). |
| `%APPDATA%Claude Switchericons` | Each profile's icon. |
| `Start MenuProgramsClaude Switcher` | The Switcher and one shortcut per profile. |
| `HKCU…CurrentVersionRun`, values `Claude Switcher…` | Only with "Open when Windows starts" / "Start Claude Switcher at login". |
| `HKCUSoftwareMakeBecomeClaude Switcher`, `HKCU…UninstallMakeBecome.ClaudeSwitcher` | One setting; the Settings → Apps entry. |

Deleting a profile removes its icon and launcher, **not** its data folder. Uninstalling (Settings → Apps, or `ClaudeSwitcher.exe --uninstall`) removes everything above except the profiles' data.

## Build

```bash
./build.sh            # → build/Claude Switcher.app (universal, ad-hoc signed)
```

Needs macOS 13+ and the Xcode command line tools. `swift tools/make-icon.swift` regenerates the app icon;
`ClaudeSwitcher --snapshot <dir>` renders the screenshots used on the page (with `CLAUDE_SWITCHER_PROFILES`
pointing at demo profiles and `CLAUDE_SWITCHER_LANG` choosing the language).
`ClaudeSwitcher --dock <profile id> on|off` pins or unpins a profile's Dock icon from a script.

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File windowsuild.ps1   # → buildwindowsClaudeSwitcher.exe, buildClaude-Switcher-Windows.zip
```

Needs nothing beyond Windows 10 or 11: it compiles with the C# compiler of .NET Framework 4.8, which Windows ships, and takes the version from `Sources/main.swift`.
`ClaudeSwitcher.exe --open <id>`, `--login <id>` (Safe login), `--icon <id> on|off`, `--quiet`, `--snapshot <dir>` (profile icons as PNG) and `--uninstall` script it.

## How it works

Extra profiles are started as `open -n -a /Applications/Claude.app --args --user-data-dir=<folder>`.
Claude Desktop has no per-profile single-instance lock, so the switcher checks the running processes and
focuses an open profile instead of launching it twice. Each launcher is a copy of the same binary with
`ClaudeAccount` set in its `Info.plist`.

On Windows, profiles are started as `claude-desktop.exe --user-data-dir=<folder>` (the Store app's own alias). There Claude Desktop
does keep one instance per folder, so starting an open profile again brings it forward. The taskbar groups windows by
AppUserModelID: the Switcher gives each profile's windows its own, and a Start-menu shortcut with that id gives the group its
name and icon. Claude Desktop is never modified. The code is in [`windows/`](windows/), what was measured about Claude on Windows
at the top of [`windows/Sources/Claude.cs`](windows/Sources/Claude.cs).

## Licence

MIT · [make become](https://make-become.github.io/), an artware studio. Claude is a trademark of Anthropic; this project is not affiliated with Anthropic.
