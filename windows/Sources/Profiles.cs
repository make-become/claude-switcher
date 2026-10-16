// Profiles: the list in profiles.json, and where each profile lives on disk.
// Same file format as the Mac app, plus "ownIcon" (Windows only: the profile's windows get
// their own taskbar icon). Unknown keys are kept as they are.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace ClaudeSwitcher {

class Profile {
    public string Id = "", Name = "", Dir, Color = "#7F7F7F", Letter = "", Email;
    public bool OpenAtLogin, OwnIcon;

    public bool IsDefault { get { return Dir == null; } }
    public Profile Copy() { return (Profile)MemberwiseClone(); }
}

static class Profiles {
    public static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    public static readonly string SupportDir = Path.Combine(AppData, "Claude Switcher");
    /// CLAUDE_SWITCHER_PROFILES points at another file (demo profiles, tests).
    public static readonly string File_ = Environment.GetEnvironmentVariable("CLAUDE_SWITCHER_PROFILES") ?? Path.Combine(SupportDir, "profiles.json");
    /// Claude Desktop from the Microsoft Store writes Roaming AppData into its package's private copy.
    static readonly string PackageRoaming = Path.Combine(LocalAppData, @"Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming");

    public static readonly string[][] Palette = {
        new[] { "Green", "#33B34D" }, new[] { "Blue", "#1A73F2" }, new[] { "Purple", "#9940D9" }, new[] { "Orange", "#F28C1A" },
        new[] { "Red", "#E0362F" }, new[] { "Yellow", "#E6B800" }, new[] { "Pink", "#E64D99" }, new[] { "Teal", "#1AB3B3" }, new[] { "Grey", "#7F7F7F" } };

    public static List<Profile> Load() {
        try {
            if (File.Exists(File_)) {
                var raw = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(File_, Encoding.UTF8)) as object[];
                if (raw != null) return raw.OfType<Dictionary<string, object>>().Select(FromJson).ToList();
            }
        } catch (Exception) { }
        // First run: one profile, Claude's own default one, so nothing changes until you add another.
        string name = L10n.T("defaultName");
        var seed = new List<Profile> { new Profile { Id = "main", Name = name, Color = "#33B34D", Letter = name.Substring(0, 1) } };
        Save(seed);
        return seed;
    }

    static Profile FromJson(Dictionary<string, object> d) {
        Func<string, string> s = k => d.ContainsKey(k) && d[k] != null ? d[k].ToString() : null;
        Func<string, bool> b = k => d.ContainsKey(k) && d[k] is bool && (bool)d[k];
        return new Profile {
            Id = s("id") ?? "", Name = s("name") ?? "", Dir = s("dir"), Color = s("color") ?? "#7F7F7F",
            Letter = s("letter") ?? "", Email = s("email"), OpenAtLogin = b("openAtLogin"),
            // Profiles written by the Mac app have no ownIcon: extra profiles get one, the default one keeps Claude's.
            OwnIcon = d.ContainsKey("ownIcon") ? b("ownIcon") : s("dir") != null,
        };
    }

    public static void Save(List<Profile> ps) {
        Directory.CreateDirectory(Path.GetDirectoryName(File_));
        var js = new JavaScriptSerializer();
        var sb = new StringBuilder("[\n");
        for (int i = 0; i < ps.Count; i++) {
            var p = ps[i];
            // Sorted keys, one per line: the same shape the Mac app writes.
            var fields = new SortedDictionary<string, object>(StringComparer.Ordinal) {
                { "color", p.Color }, { "id", p.Id }, { "letter", p.Letter }, { "name", p.Name }, { "ownIcon", p.OwnIcon } };
            if (p.Dir != null) fields["dir"] = p.Dir;
            if (p.Email != null) fields["email"] = p.Email;
            if (p.OpenAtLogin) fields["openAtLogin"] = true;
            sb.Append("  {\n");
            sb.Append(string.Join(",\n", fields.Select(kv => "    " + js.Serialize(kv.Key) + " : " + js.Serialize(kv.Value)).ToArray()));
            sb.Append(i < ps.Count - 1 ? "\n  },\n" : "\n  }\n");
        }
        sb.Append("]\n");
        string tmp = File_ + ".tmp";
        File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
        if (File.Exists(File_)) File.Replace(tmp, File_, null); else File.Move(tmp, File_);
    }

    /// The folder passed as --user-data-dir; null for Claude's default profile.
    public static string DataDir(Profile p) { return p.Dir == null ? null : Path.Combine(AppData, p.Dir); }

    /// Where the profile's files really are: Claude from the Store keeps them in its package's copy.
    public static string ReadableDataDir(Profile p) {
        string name = p.Dir ?? "Claude";
        string[] candidates = { Path.Combine(PackageRoaming, name), Path.Combine(AppData, name) };
        return candidates.Where(d => File.Exists(Path.Combine(d, "config.json")))
                         .OrderByDescending(d => File.GetLastWriteTimeUtc(Path.Combine(d, "config.json")))
                         .FirstOrDefault() ?? candidates[1];
    }

    /// Account the profile last signed in with, as Claude Desktop records it.
    public static string AccountUuid(Profile p) {
        try {
            string cfg = Path.Combine(ReadableDataDir(p), "config.json");
            if (!File.Exists(cfg)) return null;
            var j = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(cfg)) as Dictionary<string, object>;
            object v;
            return j != null && j.TryGetValue("lastKnownAccountUuid", out v) ? v as string : null;
        } catch (Exception) { return null; }
    }

    /// "Work" → id "work", data folder "Claude-Work".
    public static void Assign(Profile p, List<Profile> existing) {
        string slug = new string(p.Name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (slug.Length == 0) slug = "profile";
        p.Id = existing.Any(e => e.Id == slug) ? slug + "-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() : slug;
        string safe = new string(p.Name.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        p.Dir = "Claude-" + (safe.Length > 0 ? safe : p.Id);
    }
}

}
