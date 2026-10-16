// Claude Desktop on Windows: where it is, which of its processes is which profile, and
// how to start, focus and quit one.
//
// Measured 2026-10-16 on Claude 2.42304 (Microsoft Store / MSIX package):
// - Electron keeps ONE instance per --user-data-dir: starting a running profile again hands
//   over to it and exits. So "start" also means "bring forward", and a URL passed on the
//   command line reaches that profile's running instance.
// - claude:// always goes to the package's own handler, `Claude.exe "%1"` with no
//   --user-data-dir: the default profile. (The HKCU\Software\Classes\claude key Claude writes
//   lives in its package's private registry, so nothing outside can redirect it.) That handler
//   process lives ~2.3 s before handing the URL over, which is what Safe login uses
//   (see App.WatchLogin).
// - Its main process is Claude.exe without --type=; helpers carry --type=. The Claude Code
//   CLI is also called claude.exe, but lives outside the package.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ClaudeSwitcher {

class ClaudeProcess {
    public int Pid;
    public string CommandLine;
    public string DataDir;       // --user-data-dir, null when absent
    public string Url;           // a claude:// argument: a callback being delivered
}

static class Claude {
    static readonly string Alias = Path.Combine(Profiles.LocalAppData, @"Microsoft\WindowsApps\claude-desktop.exe");
    static readonly string Squirrel = Path.Combine(Profiles.LocalAppData, @"AnthropicClaude\claude.exe");

    /// Claude.exe inside the newest installed package (Windows' package repository lists their folders),
    /// else the image of a running Claude.
    public static string PackagedExe() {
        try {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages")) {
                if (k != null) {
                    var newest = k.GetSubKeyNames()
                        .Where(n => n.StartsWith("Claude_", StringComparison.OrdinalIgnoreCase) && n.EndsWith("__pzs8sxrjxfjjc", StringComparison.OrdinalIgnoreCase))
                        .Select(n => { Version v; return new { n, v = Version.TryParse(n.Split('_')[1], out v) ? v : new Version(0, 0) }; })
                        .OrderByDescending(x => x.v).Select(x => x.n);
                    foreach (var n in newest)
                        using (var pk = k.OpenSubKey(n)) {
                            string root = pk == null ? null : pk.GetValue("PackageRootFolder") as string;
                            if (root != null && File.Exists(Path.Combine(root, @"app\Claude.exe"))) return Path.Combine(root, @"app\Claude.exe");
                        }
                }
            }
        } catch (Exception) { }
        foreach (var cp in Running()) {
            string image = Native.ImagePath(cp.Pid);
            if (image != null) return image;
        }
        return null;
    }

    /// What to run to start Claude Desktop; null when it is not installed.
    public static string Launcher() {
        if (File.Exists(Alias)) return Alias;
        if (File.Exists(Squirrel)) return Squirrel;
        return PackagedExe();
    }

    public static bool Installed { get { return Launcher() != null; } }

    static bool IsClaudeImage(string path) {
        if (path == null) return false;
        string p = path.ToLowerInvariant();
        return (p.Contains(@"\windowsapps\claude_") || p.Contains(@"\anthropicclaude\")) && Path.GetFileName(p) == "claude.exe";
    }

    // A pid's command line never changes, so it is read once.
    static readonly Dictionary<int, ClaudeProcess> cache = new Dictionary<int, ClaudeProcess>();
    static readonly HashSet<int> notMain = new HashSet<int>();

    /// Every running main Claude process (one per open profile, plus any callback in flight).
    public static List<ClaudeProcess> Running() {
        var res = new List<ClaudeProcess>();
        var alive = new HashSet<int>();
        foreach (var proc in Process.GetProcessesByName("claude")) {
            int pid = proc.Id; proc.Dispose();
            alive.Add(pid);
            if (notMain.Contains(pid)) continue;
            ClaudeProcess cp;
            if (!cache.TryGetValue(pid, out cp)) {
                string cl = Native.CommandLine(pid);
                if (cl == null) continue;  // not readable yet: try again next time
                if (cl.Contains("--type=") || !IsClaudeImage(Native.ImagePath(pid))) { notMain.Add(pid); continue; }
                cp = Parse(pid, cl);
                cache[pid] = cp;
            }
            res.Add(cp);
        }
        foreach (int dead in cache.Keys.Where(k => !alive.Contains(k)).ToList()) cache.Remove(dead);
        notMain.RemoveWhere(k => !alive.Contains(k));
        return res;
    }

    static ClaudeProcess Parse(int pid, string cl) {
        var cp = new ClaudeProcess { Pid = pid, CommandLine = cl };
        var m = Regex.Match(cl, "--user-data-dir=(?:\"([^\"]*)\"|(\\S+))", RegexOptions.IgnoreCase);
        if (m.Success) cp.DataDir = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
        var u = Regex.Match(cl, "(?:^|\\s)\"?(claude://[^\"\\s]*)", RegexOptions.IgnoreCase);
        if (u.Success) cp.Url = u.Groups[1].Value;
        return cp;
    }

    static string Norm(string dir) {
        try { return Path.GetFullPath(dir).TrimEnd('\\').ToLowerInvariant(); } catch (Exception) { return dir.ToLowerInvariant(); }
    }

    /// Whether a process runs this profile. Whole folder only: "Claude-Work" must not match "Claude-WorkVL".
    public static bool Runs(ClaudeProcess cp, Profile p) {
        string defaultDir = Norm(Path.Combine(Profiles.AppData, "Claude"));
        if (p.IsDefault) return cp.DataDir == null || Norm(cp.DataDir) == defaultDir;
        return cp.DataDir != null && Norm(cp.DataDir) == Norm(Profiles.DataDir(p));
    }

    /// Main pid of every open profile, read once per call.
    public static Dictionary<string, int> RunningPids(List<Profile> profiles) {
        var res = new Dictionary<string, int>();
        foreach (var cp in Running().Where(c => c.Url == null))
            foreach (var p in profiles)
                if (!res.ContainsKey(p.Id) && Runs(cp, p)) res[p.Id] = cp.Pid;
        return res;
    }

    public static int? Pid(Profile p) {
        int pid;
        return RunningPids(new List<Profile> { p }).TryGetValue(p.Id, out pid) ? pid : (int?)null;
    }

    public static void Launch(Profile p, string url = null) {
        string exe = Launcher();
        if (exe == null) return;
        var args = new List<string>();
        string dir = Profiles.DataDir(p);
        if (dir != null) args.Add("--user-data-dir=\"" + dir + "\"");
        if (url != null) args.Add("\"" + url + "\"");
        var si = new ProcessStartInfo(exe, string.Join(" ", args.ToArray())) { UseShellExecute = false };
        try { Process.Start(si).Dispose(); } catch (Exception) { }
    }

    /// Bring the profile forward: its window if it has one on screen, else start it again,
    /// which makes the running instance show its window.
    public static void Focus(Profile p, int pid) {
        IntPtr w = Native.TopWindows(pid).FirstOrDefault(h => Native.IsWindowVisible(h) && Native.TitleOf(h).Length > 0);
        if (w == IntPtr.Zero) { Launch(p); return; }
        Native.Activate(w);
    }

    public static void Open(Profile p) {
        int? pid = Pid(p);
        if (pid.HasValue) Focus(p, pid.Value); else Launch(p);
    }

    /// Ends the profile's main process; its helpers follow it.
    public static void Quit(Profile p) {
        int? pid = Pid(p);
        if (pid.HasValue) Kill(pid.Value);
    }

    public static void Kill(int pid) {
        try { using (var pr = Process.GetProcessById(pid)) pr.Kill(); } catch (Exception) { }
    }

    /// The square Claude logo shipped in the package, the base of every profile icon.
    public static string LogoPng() {
        string exe = PackagedExe();
        if (exe == null) return null;
        string assets = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(exe)), "Assets");
        foreach (var n in new[] { "Square44x44Logo.targetsize-256_altform-unplated.png", "Square44x44Logo.targetsize-256.png", "Square310x310Logo.png" }) {
            string f = Path.Combine(assets, n);
            if (File.Exists(f)) return f;
        }
        return null;
    }
}

}
