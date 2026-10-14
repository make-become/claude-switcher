// swift tools/make-icon.swift → Resources/AppIcon.icns
// The Switcher's own icon: three profile dots on a dark rounded square (Claude's icon is not ours to ship).
import AppKit
func render(_ px: Int) -> Data {
    let rep = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: px, pixelsHigh: px, bitsPerSample: 8, samplesPerPixel: 4,
                               hasAlpha: true, isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0)!
    let n = CGFloat(px)
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
    let bg = NSRect(x: n * 0.1, y: n * 0.1, width: n * 0.8, height: n * 0.8)
    NSGradient(starting: NSColor(white: 0.22, alpha: 1), ending: NSColor(white: 0.08, alpha: 1))!
        .draw(in: NSBezierPath(roundedRect: bg, xRadius: n * 0.18, yRadius: n * 0.18), angle: -90)
    let colors = [NSColor(red: 0.20, green: 0.70, blue: 0.30, alpha: 1), NSColor(red: 0.10, green: 0.45, blue: 0.95, alpha: 1),
                  NSColor(red: 0.60, green: 0.25, blue: 0.85, alpha: 1)]
    let d = n * 0.17
    for (i, c) in colors.enumerated() {
        let x = n * 0.5 + CGFloat(i - 1) * n * 0.22 - d / 2
        c.setFill(); NSBezierPath(ovalIn: NSRect(x: x, y: n * 0.5 - d / 2, width: d, height: d)).fill()
    }
    NSGraphicsContext.current = nil
    return rep.representation(using: .png, properties: [:])!
}
let set = URL(fileURLWithPath: NSTemporaryDirectory() + "AppIcon.iconset")
try? FileManager.default.removeItem(at: set)
try! FileManager.default.createDirectory(at: set, withIntermediateDirectories: true)
for s in [16, 32, 128, 256, 512] {
    try! render(s).write(to: set.appendingPathComponent("icon_\(s)x\(s).png"))
    try! render(s * 2).write(to: set.appendingPathComponent("icon_\(s)x\(s)@2x.png"))
}
try! render(512).write(to: URL(fileURLWithPath: "docs/img/app-icon.png"))
let p = Process(); p.executableURL = URL(fileURLWithPath: "/usr/bin/iconutil")
p.arguments = ["-c", "icns", set.path, "-o", "Resources/AppIcon.icns"]; try! p.run(); p.waitUntilExit()
