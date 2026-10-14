// Claude Switcher: one Claude Desktop per profile (account), kept to ONE copy each.
//
// Why it exists, measured 2026-10-14:
// - Claude Desktop has no per-profile single-instance lock: launching the Work
//   profile twice gave two processes on one profile. So a running profile is
//   focused, never launched again.
// - Its login callback (claude://) reaches whichever instance macOS picks, so a
//   login can land in the wrong profile. "Safe login" quits the others first.
// - Only the default profile stores the account e-mail where it can be read, so
//   the e-mail shown is the one the user types; what IS readable everywhere is
//   lastKnownAccountUuid, used to warn when two profiles hold the same account.
//
// One binary, two modes. Without "ClaudeAccount" in Info.plist it is the
// Switcher (Dock + menu bar + ⌥⌘1…9). With it, it is one profile's own Dock icon,
// built by the Switcher, and lives exactly as long as that profile's Claude does.
import AppKit
import Carbon
import ServiceManagement

// MARK: - Profiles

struct Profile: Codable, Equatable {
    var id: String
    var name: String
    var dir: String?          // nil = Claude's default profile
    var color: String         // #RRGGBB
    var letter: String
    var email: String?
    var openAtLogin: Bool?
}

let home = NSHomeDirectory()
let supportDir = home + "/Library/Application Support/Claude Switcher"
/// CLAUDE_SWITCHER_PROFILES points at another file (used to take the screenshots on demo profiles).
let profilesFile = ProcessInfo.processInfo.environment["CLAUDE_SWITCHER_PROFILES"] ?? supportDir + "/profiles.json"
let claudeApp = "/Applications/Claude.app"
let claudeBin = claudeApp + "/Contents/MacOS/Claude"
let palette: [(String, String)] = [("Green", "#33B34D"), ("Blue", "#1A73F2"), ("Purple", "#9940D9"), ("Orange", "#F28C1A"),
                                   ("Red", "#E0362F"), ("Yellow", "#E6B800"), ("Pink", "#E64D99"), ("Teal", "#1AB3B3"), ("Grey", "#7F7F7F")]
let bundlePrefix = "com.makebecome.claude-switcher"

func loadProfiles() -> [Profile] {
    if let d = FileManager.default.contents(atPath: profilesFile), let p = try? JSONDecoder().decode([Profile].self, from: d) { return p }
    // First run: one profile, Claude's own default one, so nothing changes until you add another.
    let name = t("defaultName")
    let seed = [Profile(id: "main", name: name, dir: nil, color: "#33B34D", letter: String(name.prefix(1)))]
    saveProfiles(seed); return seed
}

func saveProfiles(_ p: [Profile]) {
    try? FileManager.default.createDirectory(atPath: supportDir, withIntermediateDirectories: true)
    let e = JSONEncoder(); e.outputFormatting = [.prettyPrinted, .sortedKeys]
    try? e.encode(p).write(to: URL(fileURLWithPath: profilesFile), options: .atomic)
}

func nsColor(_ hex: String) -> NSColor {
    let v = Int(hex.dropFirst(), radix: 16) ?? 0x7F7F7F
    return NSColor(red: CGFloat((v >> 16) & 255) / 255, green: CGFloat((v >> 8) & 255) / 255, blue: CGFloat(v & 255) / 255, alpha: 1)
}

func dataDir(_ p: Profile) -> String? { p.dir.map { home + "/Library/Application Support/" + $0 } }
func launcherURL(_ p: Profile) -> URL { URL(fileURLWithPath: home + "/Applications/Claude \(p.name).app") }

@discardableResult
func run(_ exe: String, _ args: [String]) -> String {
    let p = Process(); let out = Pipe()
    p.executableURL = URL(fileURLWithPath: exe); p.arguments = args
    p.standardOutput = out; p.standardError = FileHandle.nullDevice
    guard (try? p.run()) != nil else { return "" }
    let d = out.fileHandleForReading.readDataToEndOfFile(); p.waitUntilExit()
    return String(decoding: d, as: UTF8.self)
}

/// Main Claude process of every running profile, read once per call (helpers have another binary path).
func runningPids(_ profiles: [Profile]) -> [String: pid_t] {
    var res: [String: pid_t] = [:]
    for line in run("/bin/ps", ["-axo", "pid=,args="]).split(separator: "\n") {
        let t = line.trimmingCharacters(in: .whitespaces)
        guard let sp = t.firstIndex(of: " "), let pid = pid_t(t[..<sp]) else { continue }
        let args = t[t.index(after: sp)...]
        guard args == claudeBin || args.hasPrefix(claudeBin + " ") else { continue }
        for p in profiles where res[p.id] == nil {
            if let d = dataDir(p) { if args.contains("--user-data-dir=" + d) { res[p.id] = pid } }
            else if !args.contains("--user-data-dir=") { res[p.id] = pid }
        }
    }
    return res
}

func pid(of p: Profile) -> pid_t? { runningPids([p])[p.id] }

/// Account the profile last signed in with, as Claude Desktop records it.
func accountUuid(_ p: Profile) -> String? {
    let cfg = (dataDir(p) ?? home + "/Library/Application Support/Claude") + "/config.json"
    guard let d = FileManager.default.contents(atPath: cfg),
          let j = try? JSONSerialization.jsonObject(with: d) as? [String: Any] else { return nil }
    return j["lastKnownAccountUuid"] as? String
}

// MARK: - Open, focus, quit, login

func launch(_ p: Profile) {
    run("/usr/bin/open", ["-n", "-a", claudeApp] + (dataDir(p).map { ["--args", "--user-data-dir=" + $0] } ?? []))
}

/// Unhide, hand focus over, and send a Dock-style reopen so a closed window comes back.
func focus(_ pid: pid_t) {
    guard let app = NSRunningApplication(processIdentifier: pid) else { return }
    app.unhide()
    if #available(macOS 14, *) { NSApp.yieldActivation(to: app) }  // cooperative activation: hand focus over explicitly
    app.activate(options: [.activateAllWindows])
    let ev = NSAppleEventDescriptor(eventClass: AEEventClass(kCoreEventClass), eventID: AEEventID(kAEReopenApplication),
                                    targetDescriptor: NSAppleEventDescriptor(processIdentifier: pid),
                                    returnID: AEReturnID(kAutoGenerateReturnID), transactionID: AETransactionID(kAnyTransactionID))
    _ = try? ev.sendEvent(options: [.noReply], timeout: 5)
}

func openProfile(_ p: Profile) {
    if let pid = pid(of: p) { focus(pid) } else { launch(p) }
}

func quitProfile(_ p: Profile) {
    if let pid = pid(of: p) { NSRunningApplication(processIdentifier: pid)?.terminate() }
}

/// Login safely: only the target profile may be running, so claude:// lands in it.
func loginOnly(_ p: Profile, among all: [Profile]) {
    let pids = runningPids(all)
    let others = all.filter { $0.id != p.id && pids[$0.id] != nil }
    if !others.isEmpty {
        NSApp.activate(ignoringOtherApps: true)
        let alert = NSAlert()
        alert.messageText = t("loginTitle", p.name)
        alert.informativeText = t("loginBody", others.map(\.name).joined(separator: ", "))
        alert.addButton(withTitle: t("closeContinue")); alert.addButton(withTitle: t("cancel"))
        guard alert.runModal() == .alertFirstButtonReturn else { return }
        others.forEach { o in pids[o.id].flatMap { NSRunningApplication(processIdentifier: $0) }?.terminate() }
    }
    openProfile(p)
}

// MARK: - Launchers (one app per profile in ~/Applications, pinned in the Dock)

func renderIcon(_ p: Profile, px: Int) -> Data? {
    let base = NSWorkspace.shared.icon(forFile: claudeApp)
    guard let rep = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: px, pixelsHigh: px, bitsPerSample: 8, samplesPerPixel: 4,
                                     hasAlpha: true, isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0) else { return nil }
    let n = CGFloat(px)
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
    base.draw(in: NSRect(x: 0, y: 0, width: n, height: n))
    let r = NSRect(x: n * 0.50, y: n * 0.06, width: n * 0.46, height: n * 0.46)
    nsColor(p.color).setFill(); NSBezierPath(ovalIn: r).fill()
    NSColor.white.setStroke(); let ring = NSBezierPath(ovalIn: r.insetBy(dx: n * 0.01, dy: n * 0.01)); ring.lineWidth = n * 0.02; ring.stroke()
    let s = NSAttributedString(string: p.letter, attributes: [.font: NSFont.systemFont(ofSize: n * 0.30, weight: .heavy), .foregroundColor: NSColor.white])
    let sz = s.size(); s.draw(at: NSPoint(x: r.midX - sz.width / 2, y: r.midY - sz.height / 2))
    NSGraphicsContext.restoreGraphicsState()
    return rep.representation(using: .png, properties: [:])
}

/// Bump on every build: codesign rewrites each launcher's copy of the binary, so
/// bytes cannot be compared, and this is what tells a launcher it is out of date.
let switcherVersion = "3.1"
func iconStamp(_ p: Profile) -> String { "\(p.name)|\(p.color)|\(p.letter)" }

/// (Re)build a profile's launcher when its binary or look is out of date.
func ensureLauncher(_ p: Profile, force: Bool = false) {
    let fm = FileManager.default
    let app = launcherURL(p)
    let exe = app.appendingPathComponent("Contents/MacOS/ClaudeSwitcher")
    let plistURL = app.appendingPathComponent("Contents/Info.plist")
    let mine = Bundle.main.executableURL!
    let info0 = NSDictionary(contentsOf: plistURL)
    let current = (info0?["ClaudeIconStamp"] as? String) == iconStamp(p)
        && (info0?["ClaudeAccount"] as? String) == p.id
        && (info0?["ClaudeSwitcherVersion"] as? String) == switcherVersion
    if current && !force { return }

    try? fm.createDirectory(at: exe.deletingLastPathComponent(), withIntermediateDirectories: true)
    let res = app.appendingPathComponent("Contents/Resources")
    try? fm.createDirectory(at: res, withIntermediateDirectories: true)
    try? fm.removeItem(at: exe); try? fm.copyItem(at: mine, to: exe)

    let iconset = URL(fileURLWithPath: NSTemporaryDirectory() + "claude-\(p.id).iconset")
    try? fm.removeItem(at: iconset); try? fm.createDirectory(at: iconset, withIntermediateDirectories: true)
    for s in [16, 32, 128, 256, 512] {
        try? renderIcon(p, px: s)?.write(to: iconset.appendingPathComponent("icon_\(s)x\(s).png"))
        try? renderIcon(p, px: s * 2)?.write(to: iconset.appendingPathComponent("icon_\(s)x\(s)@2x.png"))
    }
    run("/usr/bin/iconutil", ["-c", "icns", iconset.path, "-o", res.appendingPathComponent("AppIcon.icns").path])
    try? fm.removeItem(at: iconset)

    let info: NSDictionary = [
        "CFBundleIdentifier": bundlePrefix + ".profile." + p.id, "CFBundleName": "Claude \(p.name)",
        "CFBundleExecutable": "ClaudeSwitcher", "CFBundleIconFile": "AppIcon", "CFBundlePackageType": "APPL",
        "CFBundleShortVersionString": switcherVersion, "LSMinimumSystemVersion": "13.0", "NSHighResolutionCapable": true,
        "NSAppleEventsUsageDescription": "Brings the Claude profile you pick to the front.",
        "ClaudeAccount": p.id, "ClaudeIconStamp": iconStamp(p), "ClaudeSwitcherVersion": switcherVersion,
    ]
    info.write(to: plistURL, atomically: true)
    run("/usr/bin/codesign", ["--force", "-s", "-", app.path])
    run("/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister", ["-f", app.path])
    run("/usr/bin/touch", [app.path])
}

// The Dock's pinned apps, edited directly; the Dock is restarted to read them.
func dockApps() -> [[String: Any]] {
    (CFPreferencesCopyAppValue("persistent-apps" as CFString, "com.apple.dock" as CFString) as? [[String: Any]]) ?? []
}
func dockURL(_ t: [String: Any]) -> String? {
    ((t["tile-data"] as? [String: Any])?["file-data"] as? [String: Any])?["_CFURLString"] as? String
}
func dockTile(_ u: URL) -> [String: Any] {
    ["tile-type": "file-tile", "tile-data": ["file-data": ["_CFURLString": u.absoluteString, "_CFURLStringType": 15]]]
}
/// Replace `old` with `new` in place, or add/remove when one of them is nil.
func dockEdit(old: URL?, new: URL?) {
    var apps = dockApps()
    let idx = old.flatMap { o in apps.firstIndex { dockURL($0) == o.absoluteString } }
    switch (idx, new) {
    case let (i?, n?): apps[i] = dockTile(n)
    case let (i?, nil): apps.remove(at: i)
    case let (nil, n?): if !apps.contains(where: { dockURL($0) == n.absoluteString }) { apps.append(dockTile(n)) }
    default: return
    }
    CFPreferencesSetAppValue("persistent-apps" as CFString, apps as CFArray, "com.apple.dock" as CFString)
    CFPreferencesAppSynchronize("com.apple.dock" as CFString)
    run("/usr/bin/killall", ["Dock"])
}

// Open-at-login for one profile: a LaunchAgent that opens its launcher.
func agentURL(_ p: Profile) -> URL { URL(fileURLWithPath: home + "/Library/LaunchAgents/\(bundlePrefix).profile.\(p.id).plist") }
func setOpenAtLogin(_ p: Profile, _ on: Bool) {
    if on {
        let d: NSDictionary = ["Label": "\(bundlePrefix).profile.\(p.id)", "RunAtLoad": true,
                               "ProgramArguments": ["/usr/bin/open", "-a", launcherURL(p).path]]
        d.write(to: agentURL(p), atomically: true)
    } else { try? FileManager.default.removeItem(at: agentURL(p)) }
}

// MARK: - Global hotkeys ⌥⌘1…9 (Carbon: no Accessibility permission needed)

var hotkeyAction: ((Int) -> Void)?
var hotkeyRefs: [EventHotKeyRef?] = []
var hotkeyHandlerInstalled = false

func registerHotkeys(_ count: Int) {
    hotkeyRefs.forEach { if let r = $0 { UnregisterEventHotKey(r) } }; hotkeyRefs = []
    if !hotkeyHandlerInstalled {
        var spec = EventTypeSpec(eventClass: OSType(kEventClassKeyboard), eventKind: UInt32(kEventHotKeyPressed))
        InstallEventHandler(GetApplicationEventTarget(), { _, ev, _ in
            var hk = EventHotKeyID()
            GetEventParameter(ev, EventParamName(kEventParamDirectObject), EventParamType(typeEventHotKeyID), nil,
                              MemoryLayout<EventHotKeyID>.size, nil, &hk)
            hotkeyAction?(Int(hk.id)); return noErr
        }, 1, &spec, nil, nil)
        hotkeyHandlerInstalled = true
    }
    let keys = [kVK_ANSI_1, kVK_ANSI_2, kVK_ANSI_3, kVK_ANSI_4, kVK_ANSI_5, kVK_ANSI_6, kVK_ANSI_7, kVK_ANSI_8, kVK_ANSI_9]
    for i in 0..<min(count, 9) {
        var ref: EventHotKeyRef?
        RegisterEventHotKey(UInt32(keys[i]), UInt32(optionKey | cmdKey), EventHotKeyID(signature: OSType(0x434C5357), id: UInt32(i)),
                            GetApplicationEventTarget(), 0, &ref)
        hotkeyRefs.append(ref)
    }
}

// MARK: - App

/// Set in a launcher's Info.plist: that bundle is one profile's own Dock icon.
let launcherID = Bundle.main.object(forInfoDictionaryKey: "ClaudeAccount") as? String

final class Delegate: NSObject, NSApplicationDelegate, NSMenuDelegate {
    var launchedAsLoginItem = false
    var seenRunning = false
    let started = Date()
    var statusItem: NSStatusItem?
    var showMenuBar: Bool {
        get { UserDefaults.standard.object(forKey: "showMenuBar") as? Bool ?? true }
        set { UserDefaults.standard.set(newValue, forKey: "showMenuBar"); syncStatusItem() }
    }

    var profiles: [Profile] { loadProfiles() }
    var myProfile: Profile? { launcherID.flatMap { id in profiles.first { $0.id == id } } }

    func applicationWillFinishLaunching(_ n: Notification) {
        let ev = NSAppleEventManager.shared().currentAppleEvent
        launchedAsLoginItem = ev?.eventID == AEEventID(kAEOpenApplication)
            && ev?.paramDescriptor(forKeyword: AEKeyword(keyAEPropData))?.enumCodeValue == OSType(keyAELaunchedAsLogInItem)
    }

    func applicationDidFinishLaunching(_ n: Notification) {
        if let i = CommandLine.arguments.firstIndex(of: "--snapshot"), i + 1 < CommandLine.arguments.count {
            snapshot(to: CommandLine.arguments[i + 1]); return
        }
        // Scriptable: `ClaudeSwitcher --dock <profile id> on|off` pins or unpins a profile's icon and exits.
        if let i = CommandLine.arguments.firstIndex(of: "--dock"), i + 2 < CommandLine.arguments.count {
            let a = CommandLine.arguments
            if let p = profiles.first(where: { $0.id == a[i + 1] }) {
                ensureLauncher(p)
                a[i + 2] == "on" ? dockEdit(old: nil, new: launcherURL(p)) : dockEdit(old: launcherURL(p), new: nil)
            }
            exit(0)
        }
        guard FileManager.default.fileExists(atPath: claudeBin) else {
            NSApp.activate(ignoringOtherApps: true)
            let a = NSAlert(); a.messageText = t("noClaude"); a.runModal(); NSApp.terminate(nil); return
        }
        if launcherID != nil { startLauncher(); return }
        let ps = profiles
        ps.forEach { ensureLauncher($0) }
        hotkeyAction = { [weak self] i in
            guard let self else { return }
            let ps = self.profiles
            if i < ps.count { NSApp.activate(ignoringOtherApps: true); openProfile(ps[i]) }
        }
        registerHotkeys(ps.count)
        syncStatusItem()
        Timer.scheduledTimer(withTimeInterval: 3, repeats: true) { [weak self] _ in self?.refreshStatusTitle() }
        if !launchedAsLoginItem && !CommandLine.arguments.contains("--quiet") { popMenu() }
    }

    /// Launcher: its Dock dot means "this profile is open". It opens the profile,
    /// then lives exactly as long as that Claude instance does.
    func startLauncher() {
        guard let p = myProfile else { NSApp.terminate(nil); return }
        openProfile(p)
        Timer.scheduledTimer(withTimeInterval: 3, repeats: true) { [weak self] _ in
            guard let self else { return }
            guard let p = self.myProfile else { NSApp.terminate(nil); return }
            if pid(of: p) != nil { self.seenRunning = true }
            else if self.seenRunning || Date().timeIntervalSince(self.started) > 90 { NSApp.terminate(nil) }
        }
    }

    func applicationShouldHandleReopen(_ s: NSApplication, hasVisibleWindows: Bool) -> Bool {
        if launcherID != nil { myProfile.map(openProfile) } else { popMenu() }
        return false
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ s: NSApplication) -> Bool { false }

    func applicationDockMenu(_ s: NSApplication) -> NSMenu? {
        if let p = myProfile { return profileMenu(p, running: pid(of: p) != nil) }
        return buildMenu()
    }

    // MARK: Menus

    func item(_ title: String, _ action: Selector, _ id: String? = nil) -> NSMenuItem {
        let it = NSMenuItem(title: title, action: action, keyEquivalent: ""); it.target = self; it.representedObject = id; return it
    }

    func profileMenu(_ p: Profile, running: Bool) -> NSMenu {
        let m = NSMenu()
        m.addItem(item(running ? t("front") : t("open"), #selector(openItem(_:)), p.id))
        if running { m.addItem(item(t("quitProfile", p.name), #selector(quitItem(_:)), p.id)) }
        m.addItem(item(t("safeLogin"), #selector(loginItem(_:)), p.id))
        let login = item(t("openAtLogin"), #selector(toggleLogin(_:)), p.id)
        login.state = p.openAtLogin == true ? .on : .off
        m.addItem(login)
        if launcherID == nil {
            let dock = item(t("showInDock"), #selector(toggleDock(_:)), p.id)
            dock.state = dockApps().contains { dockURL($0) == launcherURL(p).absoluteString } ? .on : .off
            m.addItem(dock)
            m.addItem(.separator())
            m.addItem(item(t("edit"), #selector(editItem(_:)), p.id))
            m.addItem(item(t("delete"), #selector(deleteItem(_:)), p.id))
        }
        return m
    }

    func buildMenu() -> NSMenu {
        let ps = profiles
        let pids = runningPids(ps)
        let uuids = Dictionary(uniqueKeysWithValues: ps.map { ($0.id, accountUuid($0)) })
        let m = NSMenu()
        for (i, p) in ps.enumerated() {
            let running = pids[p.id] != nil
            let it = item("", #selector(openItem(_:)), p.id)
            let title = NSMutableAttributedString(string: running ? "●  " : "○  ", attributes: [.foregroundColor: nsColor(p.color)])
            title.append(NSAttributedString(string: p.name, attributes: [.font: NSFont.menuFont(ofSize: 0)]))
            var sub = [String]()
            if let e = p.email, !e.isEmpty { sub.append(e) }
            if let u = uuids[p.id] ?? nil, let twin = ps.first(where: { $0.id != p.id && uuids[$0.id] == u }) {
                sub.append(t("sameAccount", twin.name))
            }
            if !sub.isEmpty {
                title.append(NSAttributedString(string: "   " + sub.joined(separator: " · "),
                                            attributes: [.foregroundColor: NSColor.secondaryLabelColor, .font: NSFont.menuFont(ofSize: 11)]))
            }
            it.attributedTitle = title
            if i < 9 { it.keyEquivalent = String(i + 1); it.keyEquivalentModifierMask = [.option, .command] }
            m.addItem(it)
        }
        m.addItem(.separator())
        let manage = NSMenuItem(title: t("manage"), action: nil, keyEquivalent: "")
        let mm = NSMenu()
        for p in ps {
            let pi = NSMenuItem(title: p.name, action: nil, keyEquivalent: "")
            pi.submenu = profileMenu(p, running: pids[p.id] != nil)
            mm.addItem(pi)
        }
        manage.submenu = mm
        m.addItem(manage)
        m.addItem(item(t("newProfile"), #selector(newItem(_:))))
        m.addItem(.separator())
        let mb = item(t("showMenuBar"), #selector(toggleMenuBar(_:)))
        mb.state = showMenuBar ? .on : .off
        m.addItem(mb)
        let al = item(t("switcherAtLogin"), #selector(toggleSwitcherAtLogin(_:)))
        al.state = SMAppService.mainApp.status == .enabled ? .on : .off
        m.addItem(al)
        m.addItem(item(t("quitSwitcher"), #selector(quitSwitcher(_:))))
        return m
    }

    /// The Switcher's menu, opened where the mouse is.
    func popMenu() {
        NSApp.activate(ignoringOtherApps: true)
        buildMenu().popUp(positioning: nil, at: NSEvent.mouseLocation, in: nil)
    }

    // MARK: Menu bar

    func syncStatusItem() {
        if showMenuBar && statusItem == nil {
            statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
            let m = NSMenu(); m.delegate = self; statusItem?.menu = m
            refreshStatusTitle()
        } else if !showMenuBar, let s = statusItem {
            NSStatusBar.system.removeStatusItem(s); statusItem = nil
        }
    }

    /// One dot per profile, in its color, filled when open.
    func refreshStatusTitle() {
        guard let b = statusItem?.button else { return }
        let ps = profiles; let pids = runningPids(ps)
        let dots = NSMutableAttributedString()
        for p in ps {
            dots.append(NSAttributedString(string: pids[p.id] != nil ? "●" : "○",
                                        attributes: [.foregroundColor: nsColor(p.color), .font: NSFont.systemFont(ofSize: 13)]))
        }
        b.attributedTitle = dots
    }

    func menuNeedsUpdate(_ menu: NSMenu) {
        menu.removeAllItems()
        let fresh = buildMenu()
        for it in fresh.items { fresh.removeItem(it); menu.addItem(it) }
    }

    // MARK: Actions

    func profile(_ s: NSMenuItem) -> Profile? { profiles.first { $0.id == s.representedObject as? String } }
    @objc func openItem(_ s: NSMenuItem) { profile(s).map(openProfile) }
    @objc func quitItem(_ s: NSMenuItem) { profile(s).map(quitProfile); refreshStatusTitle() }
    @objc func loginItem(_ s: NSMenuItem) { if let p = profile(s) { loginOnly(p, among: profiles) } }
    @objc func toggleMenuBar(_ s: NSMenuItem) { showMenuBar.toggle() }
    @objc func quitSwitcher(_ s: NSMenuItem) { NSApp.terminate(nil) }
    @objc func toggleSwitcherAtLogin(_ s: NSMenuItem) {
        if SMAppService.mainApp.status == .enabled { try? SMAppService.mainApp.unregister() } else { try? SMAppService.mainApp.register() }
    }

    /// Pin or unpin the profile's own icon. The launcher stays in ~/Applications either way,
    /// and is rebuilt at start if it went missing, so pinning it again always works.
    @objc func toggleDock(_ s: NSMenuItem) {
        guard let p = profile(s) else { return }
        let u = launcherURL(p)
        ensureLauncher(p)
        if dockApps().contains(where: { dockURL($0) == u.absoluteString }) { dockEdit(old: u, new: nil) } else { dockEdit(old: nil, new: u) }
    }

    @objc func toggleLogin(_ s: NSMenuItem) {
        var ps = profiles
        guard let i = ps.firstIndex(where: { $0.id == s.representedObject as? String }) else { return }
        let on = !(ps[i].openAtLogin ?? false)
        ps[i].openAtLogin = on; saveProfiles(ps); setOpenAtLogin(ps[i], on)
    }

    @objc func newItem(_ s: NSMenuItem) {
        let ps = profiles
        let used = Set(ps.map(\.color))
        let draft = Profile(id: "", name: "", dir: nil, color: palette.first { !used.contains($0.1) }?.1 ?? "#7F7F7F", letter: "")
        guard var p = editDialog(draft, title: t("newTitle"), existing: ps) else { return }
        let slug = p.name.lowercased().filter { $0.isLetter || $0.isNumber }
        p.id = ps.contains { $0.id == slug } ? slug + "-" + String(Int(Date().timeIntervalSince1970)) : slug
        p.dir = "Claude-" + p.name.filter { $0.isLetter || $0.isNumber || $0 == "-" }
        saveProfiles(ps + [p])
        ensureLauncher(p, force: true)
        dockEdit(old: nil, new: launcherURL(p))
        registerHotkeys(ps.count + 1); refreshStatusTitle()
        loginOnly(p, among: profiles)   // a new profile starts signed out
    }

    @objc func editItem(_ s: NSMenuItem) {
        var ps = profiles
        guard let i = ps.firstIndex(where: { $0.id == s.representedObject as? String }),
              let edited = editDialog(ps[i], title: t("editTitle", ps[i].name), existing: ps) else { return }
        let old = ps[i]
        ps[i] = edited; saveProfiles(ps)
        if old.name != edited.name {
            ensureLauncher(edited, force: true)
            dockEdit(old: launcherURL(old), new: launcherURL(edited))
            try? FileManager.default.trashItem(at: launcherURL(old), resultingItemURL: nil)
            if edited.openAtLogin == true { setOpenAtLogin(edited, true) }
        } else if iconStamp(old) != iconStamp(edited) {
            ensureLauncher(edited, force: true)
            run("/usr/bin/killall", ["Dock"])   // pick up the new icon
        }
        refreshStatusTitle()
    }

    @objc func deleteItem(_ s: NSMenuItem) {
        var ps = profiles
        guard let i = ps.firstIndex(where: { $0.id == s.representedObject as? String }) else { return }
        let p = ps[i]
        NSApp.activate(ignoringOtherApps: true)
        let a = NSAlert()
        a.messageText = t("deleteTitle", p.name)
        a.informativeText = t("deleteBody", p.dir.map { "~/Library/Application Support/\($0)" } ?? t("defaultData"))
        a.addButton(withTitle: t("deleteButton")); a.addButton(withTitle: t("cancel"))
        guard a.runModal() == .alertFirstButtonReturn else { return }
        quitProfile(p)
        setOpenAtLogin(p, false)
        dockEdit(old: launcherURL(p), new: nil)
        try? FileManager.default.trashItem(at: launcherURL(p), resultingItemURL: nil)
        ps.remove(at: i); saveProfiles(ps)
        registerHotkeys(ps.count); refreshStatusTitle()
    }

    /// Name, letter, color, e-mail. Returns nil on cancel.
    func editDialog(_ p: Profile, title: String, existing: [Profile]) -> Profile? {
        NSApp.activate(ignoringOtherApps: true)
        let name = NSTextField(string: p.name); name.placeholderString = t("namePh")
        let letter = NSTextField(string: p.letter); letter.placeholderString = t("letter")
        let email = NSTextField(string: p.email ?? ""); email.placeholderString = t("emailPh")
        let color = NSPopUpButton()
        for (n, hex) in palette {
            color.addItem(withTitle: t(n))
            let img = NSImage(size: NSSize(width: 12, height: 12), flipped: false) { r in nsColor(hex).setFill(); NSBezierPath(ovalIn: r).fill(); return true }
            color.lastItem?.image = img; color.lastItem?.representedObject = hex
        }
        color.selectItem(at: palette.firstIndex { $0.1 == p.color } ?? 0)
        let grid = NSGridView(views: [[NSTextField(labelWithString: t("name")), name], [NSTextField(labelWithString: t("letter")), letter],
                                      [NSTextField(labelWithString: t("color")), color], [NSTextField(labelWithString: t("email")), email]])
        grid.column(at: 1).width = 240
        grid.frame = NSRect(x: 0, y: 0, width: 320, height: grid.fittingSize.height)
        while true {
            let a = NSAlert(); a.messageText = title; a.accessoryView = grid
            a.addButton(withTitle: t("save")); a.addButton(withTitle: t("cancel"))
            a.window.initialFirstResponder = name
            guard a.runModal() == .alertFirstButtonReturn else { return nil }
            let n = name.stringValue.trimmingCharacters(in: .whitespaces)
            var problem: String?
            if n.isEmpty { problem = t("needName") }
            else if existing.contains(where: { $0.id != p.id && $0.name.lowercased() == n.lowercased() }) { problem = t("nameTaken", n) }
            if let problem {
                let e = NSAlert(); e.messageText = problem; e.runModal(); continue
            }
            var out = p
            out.name = n
            let l = letter.stringValue.trimmingCharacters(in: .whitespaces)
            out.letter = String((l.isEmpty ? n : l).prefix(1)).uppercased()
            out.color = color.selectedItem?.representedObject as? String ?? p.color
            let e = email.stringValue.trimmingCharacters(in: .whitespaces)
            out.email = e.isEmpty ? nil : e
            return out
        }
    }
}

let app = NSApplication.shared
let delegate = Delegate()
app.delegate = delegate
app.setActivationPolicy(.regular)
app.run()
