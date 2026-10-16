// One taskbar icon per profile. The Mac app builds a launcher app per profile for its Dock icon;
// on Windows the taskbar groups windows by AppUserModelID, so each profile's Claude windows are
// given the profile's own id, and a Start-menu shortcut with that id gives the group its name and
// icon (and is what "Pin to taskbar" keeps). Claude Desktop itself is never modified.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;

namespace ClaudeSwitcher {

static class Taskbar {
    public const string SwitcherId = "MakeBecome.ClaudeSwitcher";
    public static string AppId(Profile p) { return SwitcherId + ".Profile." + p.Id; }

    public static readonly string StartMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Claude Switcher");
    static readonly string IconDir = Path.Combine(Profiles.SupportDir, "icons");
    public static string Exe { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }

    public static string ShortcutPath(Profile p) { return Path.Combine(StartMenuDir, "Claude " + SafeName(p.Name) + ".lnk"); }
    /// One file per look: Windows caches icons by path, so a changed icon gets a new name.
    public static string IconPath(Profile p) { return Path.Combine(IconDir, p.Id + "-" + Hash(Stamp(p)) + ".ico"); }
    static string Hash(string s) {
        using (var sha = System.Security.Cryptography.SHA1.Create())
            return string.Concat(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s)).Take(4).Select(b => b.ToString("x2")).ToArray());
    }
    static readonly string PinnedDir = Path.Combine(Profiles.AppData, @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
    public static string OpenArgs(Profile p) { return "--open " + p.Id; }
    static string SafeName(string n) { return new string(n.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray()); }

    public static Color ColorOf(string hex) {
        int v;
        if (hex == null || hex.Length != 7 || !int.TryParse(hex.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out v)) v = 0x7F7F7F;
        return Color.FromArgb((v >> 16) & 255, (v >> 8) & 255, v & 255);
    }

    static string logoPath;
    static string LogoPath { get { if (logoPath == null) logoPath = Claude.LogoPng() ?? ""; return logoPath; } }
    static Image logo;
    static Image Logo() {
        if (logo != null) return logo;
        string png = LogoPath.Length > 0 ? LogoPath : null;
        try { if (png != null) using (var f = Image.FromFile(png)) logo = new Bitmap(f); } catch (Exception) { }
        if (logo == null) {
            try { string exe = Claude.PackagedExe() ?? Exe; logo = Icon.ExtractAssociatedIcon(exe).ToBitmap(); } catch (Exception) { }
        }
        return logo;
    }

    /// Claude's logo with the profile's letter on a coloured disc, bottom right (as on the Mac).
    public static Bitmap RenderIcon(Profile p, int px) {
        var bmp = new Bitmap(px, px, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float n = px;
            var base_ = Logo();
            if (base_ != null) g.DrawImage(base_, 0, 0, n, n);
            // Small sizes: a bigger disc, or the letter cannot be read.
            float d = px <= 24 ? n * 0.62f : n * 0.50f;
            var r = new RectangleF(n - d - n * 0.02f, n - d - n * 0.02f, d, d);
            using (var fill = new SolidBrush(ColorOf(p.Color))) g.FillEllipse(fill, r);
            using (var ring = new Pen(Color.White, Math.Max(1f, n * 0.03f))) g.DrawEllipse(ring, r);
            using (var font = new Font("Segoe UI", d * 0.62f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(p.Letter, font, Brushes.White, new RectangleF(r.X, r.Y + d * 0.04f, r.Width, r.Height), sf);
        }
        return bmp;
    }

    /// An .ico of PNG frames (Windows Vista and later read those at every size).
    public static void WriteIco(string path, Func<int, Bitmap> render) {
        int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };
        var frames = sizes.Select(s => { using (var b = render(s)) using (var ms = new MemoryStream()) { b.Save(ms, ImageFormat.Png); return ms.ToArray(); } }).ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string tmp = path + ".tmp";
        using (var w = new BinaryWriter(File.Create(tmp))) {
            w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++) {
                w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i])); w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                w.Write((byte)0); w.Write((byte)0); w.Write((ushort)1); w.Write((ushort)32);
                w.Write(frames[i].Length); w.Write(offset);
                offset += frames[i].Length;
            }
            foreach (var f in frames) w.Write(f);
        }
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }

    /// What an icon was drawn from: drawn again when any of it changes (Claude's logo found later included).
    static string Stamp(Profile p) { return p.Name + "|" + p.Color + "|" + p.Letter + "|" + Program.Version + "|" + LogoPath; }

    /// (Re)build the profile's icon and Start-menu shortcut when they are missing or out of date.
    public static void Ensure(Profile p, bool force = false) {
        string ico = IconPath(p);
        if (!File.Exists(ico) || force) {
            WriteIco(ico, s => RenderIcon(p, s));
            // The profile's older icons go, after any pinned copy of its shortcut is moved to the new one.
            UpdatePins(p, ico);
            foreach (var old in IconsOf(p.Id).Where(f => !f.Equals(ico, StringComparison.OrdinalIgnoreCase)))
                try { File.Delete(old); } catch (Exception) { }
            stale.Add(p.Id);
        }
        string lnk = ShortcutPath(p);
        var cur = File.Exists(lnk) ? Native.ReadShortcut(lnk) : null;
        if (force || cur == null || !string.Equals(cur[0], Exe, StringComparison.OrdinalIgnoreCase) || cur[1] != OpenArgs(p) || cur[2] != ico) {
            Directory.CreateDirectory(StartMenuDir);
            Native.WriteShortcut(lnk, Exe, OpenArgs(p), ico, L10n.T("shortcutDesc", p.Name), AppId(p));
        }
    }

    /// The Switcher's own Start-menu entry; launching it again pops the menu, like a click on the Dock icon.
    public static void EnsureSelf() {
        string lnk = Path.Combine(StartMenuDir, "Claude Switcher.lnk");
        var cur = File.Exists(lnk) ? Native.ReadShortcut(lnk) : null;
        if (cur != null && string.Equals(cur[0], Exe, StringComparison.OrdinalIgnoreCase)) return;
        Directory.CreateDirectory(StartMenuDir);
        Native.WriteShortcut(lnk, Exe, "", Exe, "Claude Switcher", SwitcherId);
    }

    /// The icon files of one profile: "<id>-<8 hex>.ico", and nothing of a profile whose id merely starts the same.
    static IEnumerable<string> IconsOf(string id) {
        if (!Directory.Exists(IconDir)) return new string[0];
        var re = new System.Text.RegularExpressions.Regex("^" + System.Text.RegularExpressions.Regex.Escape(id) + "-[0-9a-f]{8}\\.ico$");
        return Directory.GetFiles(IconDir).Where(f => re.IsMatch(Path.GetFileName(f)));
    }

    /// A pinned copy of the profile's shortcut keeps its own icon path: point it at the new icon.
    static void UpdatePins(Profile p, string ico) {
        if (!Directory.Exists(PinnedDir)) return;
        foreach (var lnk in Directory.GetFiles(PinnedDir, "*.lnk")) {
            var cur = Native.ReadShortcut(lnk);
            if (cur != null && string.Equals(cur[0], Exe, StringComparison.OrdinalIgnoreCase) && cur[1] == OpenArgs(p) && cur[2] != ico)
                try { Native.WriteShortcut(lnk, Exe, OpenArgs(p), ico, L10n.T("shortcutDesc", p.Name), AppId(p)); } catch (Exception) { }
        }
        Native.SHChangeNotify(0x08000000 /* SHCNE_ASSOCCHANGED */, 0, IntPtr.Zero, IntPtr.Zero);
    }

    /// Shortcuts and icons of profiles that no longer exist (deleted, or renamed) go away.
    public static void Prune(List<Profile> ps) {
        if (Directory.Exists(StartMenuDir)) {
            var keep = new HashSet<string>(ps.Select(p => ShortcutPath(p).ToLowerInvariant())) { Path.Combine(StartMenuDir, "Claude Switcher.lnk").ToLowerInvariant() };
            foreach (var f in Directory.GetFiles(StartMenuDir, "*.lnk").Where(f => !keep.Contains(f.ToLowerInvariant())))
                try { File.Delete(f); } catch (Exception) { }
        }
        if (Directory.Exists(IconDir)) {
            var current = new HashSet<string>(ps.Select(p => IconPath(p).ToLowerInvariant()));
            foreach (var f in Directory.GetFiles(IconDir).Where(f => !current.Contains(f.ToLowerInvariant())))
                try { File.Delete(f); } catch (Exception) { }
        }
    }

    public static void Remove(Profile p) {
        foreach (var f in new[] { ShortcutPath(p) }.Concat(IconsOf(p.Id)))
            try { File.Delete(f); } catch (Exception) { }
    }

    // --- Windows of running profiles

    // hwnd → the id it was given ("" = given back to Claude), so each window is tagged once.
    static readonly Dictionary<IntPtr, string> tagged = new Dictionary<IntPtr, string>();
    // Profiles whose icon was redrawn: the taskbar keeps the icon it read when the group formed,
    // so their open windows leave the group and come back, and the group forms again.
    static readonly HashSet<string> stale = new HashSet<string>();

    /// Give every window of every open profile the taskbar identity its profile asks for.
    public static void TagAll(List<Profile> ps, Dictionary<string, int> pids) {
        foreach (var h in tagged.Keys.Where(h => !Native.IsWindow(h)).ToList()) tagged.Remove(h);
        foreach (var p in ps) {
            int pid;
            if (!pids.TryGetValue(p.Id, out pid)) continue;
            bool regroup = stale.Remove(p.Id);
            foreach (var h in Native.TopWindows(pid)) Tag(h, p, regroup);
        }
    }

    /// Every window this run gave an id goes back to Claude's own taskbar icon.
    public static void UntagAll() {
        foreach (var kv in tagged.Where(kv => kv.Value != "" && Native.IsWindow(kv.Key)).ToList())
            Native.TagWindow(kv.Key, null, null, null, null);
        tagged.Clear();
    }

    public static void Tag(IntPtr h, Profile p, bool regroup) {
        string want = p.OwnIcon ? AppId(p) : "";
        string have;
        bool known = tagged.TryGetValue(h, out have);
        if (known && have == want && !regroup) return;
        if (!known && want == "") { tagged[h] = ""; return; }   // never tagged: it already is Claude's
        string relaunch = "\"" + Exe + "\" " + OpenArgs(p), name = "Claude " + p.Name, icon = IconPath(p) + ",0";
        if (regroup && want != "") Native.TagWindow(h, null, relaunch, name, icon);
        if (Native.TagWindow(h, want == "" ? null : want, relaunch, name, icon)) tagged[h] = want;
    }
}

}
