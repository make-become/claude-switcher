#!/bin/zsh
# Builds "Claude Switcher.app" into ./build. Needs the Xcode command line tools (xcode-select --install).
set -e
cd "${0:A:h}"
APP="build/Claude Switcher.app"
VERSION=$(grep -o 'switcherVersion = "[^"]*"' Sources/main.swift | cut -d'"' -f2)
rm -rf "$APP"; mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
swiftc -O -target arm64-apple-macos13 -o build/ClaudeSwitcher-arm64 Sources/*.swift
swiftc -O -target x86_64-apple-macos13 -o build/ClaudeSwitcher-x86_64 Sources/*.swift
lipo -create build/ClaudeSwitcher-arm64 build/ClaudeSwitcher-x86_64 -output "$APP/Contents/MacOS/ClaudeSwitcher"
rm build/ClaudeSwitcher-arm64 build/ClaudeSwitcher-x86_64
cp Resources/AppIcon.icns "$APP/Contents/Resources/AppIcon.icns"
sed "s/__VERSION__/$VERSION/" Resources/Info.plist > "$APP/Contents/Info.plist"
codesign --force -s - "$APP"
echo "built $APP ($VERSION)"
