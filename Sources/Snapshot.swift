// `ClaudeSwitcher --snapshot <dir>` renders the app's own windows to PNG for the web page:
// the Dock icons, the menu, and the profile dialog. (Menu-bar items are drawn by the system
// on recent macOS, outside this process, so they cannot be captured this way.) It captures only
// this process's windows, so nothing else on the screen ends up in a picture.
import AppKit

private typealias CreateImage = @convention(c) (CGRect, UInt32, UInt32, UInt32) -> Unmanaged<CGImage>?
// Looked up at run time: the SDK marks it obsolete, but it still captures a process's own windows.
private let createImage: CreateImage? = {
    guard let h = dlopen(nil, RTLD_NOW), let f = dlsym(h, "CGWindowListCreateImage") else { return nil }
    return unsafeBitCast(f, to: CreateImage.self)
}()

/// Topmost window of this process at one of `layers`, written as PNG.
private func capture(layers: [Int], to path: String) {
    let all = (CGWindowListCopyWindowInfo([.optionOnScreenOnly], kCGNullWindowID) as? [[String: Any]]) ?? []
    guard let w = all.first(where: { ($0[kCGWindowOwnerPID as String] as? Int32) == getpid() && layers.contains(($0[kCGWindowLayer as String] as? Int) ?? -1) }),
          let id = w[kCGWindowNumber as String] as? UInt32,
          let img = createImage?(.null, 8 /* optionIncludingWindow */, id, 1 /* boundsIgnoreFraming */ | 8 /* bestResolution */)?.takeRetainedValue()
    else {
        let mine = all.filter { ($0[kCGWindowOwnerPID as String] as? Int32) == getpid() }
            .map { "layer \($0[kCGWindowLayer as String] ?? "?") bounds \($0[kCGWindowBounds as String] ?? "?")" }
        FileHandle.standardError.write("no window at layers \(layers); mine: \(mine)\n".data(using: .utf8)!); return
    }
    let rep = NSBitmapImageRep(cgImage: img)
    try? rep.representation(using: .png, properties: [:])?.write(to: URL(fileURLWithPath: path))
}

private func after(_ s: TimeInterval, modes: [RunLoop.Mode], _ f: @escaping () -> Void) {
    let tm = Timer(timeInterval: s, repeats: false) { _ in f() }
    modes.forEach { RunLoop.main.add(tm, forMode: $0) }
}

extension Delegate {
    func snapshot(to dir: String) {
        try? FileManager.default.createDirectory(atPath: dir, withIntermediateDirectories: true)
        for p in profiles { try? renderIcon(p, px: 256)?.write(to: URL(fileURLWithPath: "\(dir)/icon-\(p.id).png")) }
        NSApp.activate(ignoringOtherApps: true)
        after(1.0, modes: [.default]) {
            let menu = self.buildMenu()
            after(0.8, modes: [.eventTracking]) {
                capture(layers: [Int(CGWindowLevelForKey(.popUpMenuWindow))], to: "\(dir)/menu.png")
                menu.cancelTracking()
            }
            menu.popUp(positioning: nil, at: NSPoint(x: 300, y: 700), in: nil)
            if let p = self.profiles.first(where: { $0.dir != nil }) {
                after(1.5, modes: [.modalPanel]) {
                    // an app-modal alert moves from the normal level to the modal-panel level as it settles
                    capture(layers: [Int(CGWindowLevelForKey(.modalPanelWindow)), 0], to: "\(dir)/edit.png")
                    NSApp.abortModal()
                }
                _ = self.editDialog(p, title: t("editTitle", p.name), existing: self.profiles)
            }
            exit(0)
        }
    }
}
