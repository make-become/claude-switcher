# Claude Switcher

A [make become](https://make-become.github.io/) project.

Several Claude Desktop accounts side by side on one Mac, one Dock icon each.

**Unofficial.** Not made, endorsed or supported by Anthropic. It never modifies Claude Desktop; it only starts it.

**Page:** https://make-become.github.io/claude-switcher/ · **Details** (what a profile is on disk, everything it puts on your Mac): https://make-become.github.io/claude-switcher/details.html

## Install

```bash
curl -fsSL https://make-become.github.io/claude-switcher/install.sh | sh
```

It downloads the latest release into `/Applications` (where Finder and Spotlight look; `~/Applications` if that is not writable), clears the download quarantine (the app is not notarised) and opens it. Read it first: [`docs/install.sh`](docs/install.sh).

## What it does

- **Profiles.** Each profile is one Claude Desktop with its own data folder, so its own login. All can be open at once.
- **A Dock icon per profile** with a coloured letter; the dot under it means that profile is open.
- **Never two copies of one profile**: an open profile is brought forward, not started again.
- **Menu** where the mouse is, in the Dock (right-click) and in the menu bar (one dot per profile).
- **⌥⌘1…9** bring a profile forward from any app.
- **Safe login** closes the other profiles while you sign in, so the browser's login callback reaches the right one.
- **Languages:** English, Italiano, Español, Français, Deutsch (from the Mac's settings).

## What a profile is on disk

| Where | What |
|---|---|
| `~/Library/Application Support/Claude` | Claude's own folder: your first profile. Never touched. |
| `~/Library/Application Support/Claude-<Name>` | Each extra profile's data, created by Claude Desktop on first launch. |
| `~/Applications/Claude <Name>.app` | The profile's launcher (its Dock icon). |
| `/Applications/Claude Switcher.app` | The app itself. |
| `~/Library/Application Support/Claude Switcher/profiles.json` | The profile list. |
| `~/Library/LaunchAgents/com.makebecome.claude-switcher.profile.<id>.plist` | Only with "Open when the Mac starts". |

Deleting a profile removes its icon and launcher, **not** its data folder.

## Build

```bash
./build.sh            # → build/Claude Switcher.app (universal, ad-hoc signed)
```

Needs macOS 13+ and the Xcode command line tools. `swift tools/make-icon.swift` regenerates the app icon;
`ClaudeSwitcher --snapshot <dir>` renders the screenshots used on the page (with `CLAUDE_SWITCHER_PROFILES`
pointing at demo profiles and `CLAUDE_SWITCHER_LANG` choosing the language).
`ClaudeSwitcher --dock <profile id> on|off` pins or unpins a profile's Dock icon from a script.

## How it works

Extra profiles are started as `open -n -a /Applications/Claude.app --args --user-data-dir=<folder>`.
Claude Desktop has no per-profile single-instance lock, so the switcher checks the running processes and
focuses an open profile instead of launching it twice. Each launcher is a copy of the same binary with
`ClaudeAccount` set in its `Info.plist`.

## Licence

MIT · [make become](https://make-become.github.io/), an artware studio. Claude is a trademark of Anthropic; this project is not affiliated with Anthropic.
