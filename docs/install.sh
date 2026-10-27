#!/bin/sh
# Claude Switcher installer.
#   curl -fsSL https://make-become.github.io/claude-switcher/install.sh | sh
# Downloads the latest release into /Applications (or ~/Applications when that is not
# writable), clears the download quarantine
# (the app is open source but not notarised by Apple), and opens it.
# Touches nothing else: Claude Desktop and your accounts are left as they are.
set -eu

REPO="make-become/claude-switcher"
# /Applications is where Finder's Applications and Spotlight look; admin users can write it without sudo.
if [ -n "${CLAUDE_SWITCHER_DEST:-}" ]; then DEST="$CLAUDE_SWITCHER_DEST"
elif [ -w /Applications ]; then DEST=/Applications
else DEST="$HOME/Applications"; fi
APP="$DEST/Claude Switcher.app"
URL="https://github.com/$REPO/releases/latest/download/Claude-Switcher-macOS.zip"

[ "$(uname -s)" = Darwin ] || { echo "Claude Switcher runs on macOS only."; exit 1; }
if [ ! -d /Applications/Claude.app ]; then
  echo "Note: Claude Desktop is not in /Applications yet. Get it from https://claude.ai/download"
fi

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

echo "→ Downloading $URL"
curl -fsSL "$URL" -o "$TMP/claude-switcher.zip"
ditto -x -k "$TMP/claude-switcher.zip" "$TMP"
[ -d "$TMP/Claude Switcher.app" ] || { echo "The download did not contain Claude Switcher.app"; exit 1; }

echo "→ Installing into $DEST"
mkdir -p "$DEST"
# Quit a running copy so it can be replaced; the per-profile launchers update themselves on next start.
# naima: allow-wait-loop a single quit of the old copy before replacing it, not a wait
pkill -f "Claude Switcher.app/Contents/MacOS/ClaudeSwitcher" 2>/dev/null || true
sleep 1
# Remember whether any copy was in the Dock: the Dock forgets an icon whose app is deleted.
WAS_PINNED=$(defaults read com.apple.dock persistent-apps 2>/dev/null | grep -c 'Claude%20Switcher.app/' || true)
rm -rf "$APP"
# A copy left by an older install in the other Applications folder would be a second, stale app.
for OLD in "/Applications/Claude Switcher.app" "$HOME/Applications/Claude Switcher.app"; do
  [ "$OLD" != "$APP" ] && [ -d "$OLD" ] && rm -rf "$OLD" && echo "→ Removed the older copy in ${OLD%/*}"
done
ditto "$TMP/Claude Switcher.app" "$APP"
xattr -dr com.apple.quarantine "$APP" 2>/dev/null || true

if [ "${WAS_PINNED:-0}" -gt 0 ]; then
  "$APP/Contents/MacOS/ClaudeSwitcher" --pin-self 2>/dev/null || true
  sleep 2
fi

echo "→ Opening Claude Switcher"
open "$APP"
echo "✓ Installed: $APP"
echo "  Click its icon (or the dots in the menu bar) and choose \"New profile…\" to add an account."
