#!/bin/sh
# Claude Switcher installer.
#   curl -fsSL https://make-become.github.io/claude-switcher/install.sh | sh
# Downloads the latest release into ~/Applications, clears the download quarantine
# (the app is open source but not notarised by Apple), and opens it.
# Touches nothing else: Claude Desktop and your accounts are left as they are.
set -eu

REPO="make-become/claude-switcher"
DEST="${CLAUDE_SWITCHER_DEST:-$HOME/Applications}"
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
pkill -f "Claude Switcher.app/Contents/MacOS/ClaudeSwitcher" 2>/dev/null || true
sleep 1
rm -rf "$APP"
ditto "$TMP/Claude Switcher.app" "$APP"
xattr -dr com.apple.quarantine "$APP" 2>/dev/null || true

echo "→ Opening Claude Switcher"
open "$APP"
echo "✓ Installed: $APP"
echo "  Click its icon (or the dots in the menu bar) and choose \"New profile…\" to add an account."
