// Claude Switcher for Windows: one Claude Desktop per profile (account), each with its own
// taskbar icon, kept to one copy each, reachable from a menu, the notification area and
// Ctrl+Alt+Shift+1…9. See Claude.cs for what was measured about Claude Desktop on Windows.
//
// One process: the first copy stays running (quietly, in the notification area); every later
// launch (the Start-menu entry, a pinned profile, "Open when Windows starts") hands its
// command line to it and exits.
//
//   ClaudeSwitcher.exe                     pop the menu where the mouse is
//   ClaudeSwitcher.exe --open <id>         open or bring forward one profile
//   ClaudeSwitcher.exe --login <id>        Safe login: route the browser's sign-in to that profile
//   ClaudeSwitcher.exe --quiet             start without popping the menu (used at login)
//   ClaudeSwitcher.exe --icon <id> on|off  give a profile its own taskbar icon, or not
//   ClaudeSwitcher.exe --snapshot <dir>    write each profile's icon as PNG (for the page)
//   ClaudeSwitcher.exe --uninstall         remove everything it put on this PC except the profiles' data
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ClaudeSwitcher {

static class Program {
    public static string Version { get { return Application.ProductVersion; } }
    const string IpcTitle = "MakeBecome.ClaudeSwitcher.IPC";

    [STAThread]
    static int Main(string[] args) {
        Native.SetCurrentProcessExplicitAppUserModelID(Taskbar.SwitcherId);
        int snap = Array.IndexOf(args, "--snapshot");
        if (snap >= 0 && snap + 1 < args.Length) { Snapshot(args[snap + 1]); return 0; }

        bool first;
        using (var mutex = new Mutex(true, @"Local\MakeBecome.ClaudeSwitcher", out first)) {
            if (!first) return Forward(args) ? 0 : 1;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!Claude.Installed) { MessageBox.Show(L10n.T("noClaude"), "Claude Switcher"); return 1; }
            Application.Run(new App(IpcTitle, args));
            GC.KeepAlive(mutex);
        }
        return 0;
    }

    /// Hand the command line to the running copy. It may still be starting (two entries run at
    /// login): its window appears within a few seconds.
    static bool Forward(string[] args) {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        IntPtr h;
        while ((h = Native.FindWindow(null, IpcTitle)) == IntPtr.Zero) {
            if (DateTime.UtcNow > deadline) return false;
            Thread.Sleep(100);
        }
        uint pid; Native.GetWindowThreadProcessId(h, out pid);
        Native.AllowSetForegroundWindow((int)pid);   // let it bring Claude, or its menu, forward
        byte[] data = Encoding.UTF8.GetBytes(string.Join("\n", args));
        IntPtr buf = Marshal.AllocHGlobal(Math.Max(1, data.Length));
        try {
            Marshal.Copy(data, 0, buf, data.Length);
            var cd = new Native.CopyData { Data = new IntPtr(1), Size = data.Length, Ptr = buf };
            Native.SendMessage(h, Native.WmCopyData, IntPtr.Zero, ref cd);
        } finally { Marshal.FreeHGlobal(buf); }
        return true;
    }

    static void Snapshot(string dir) {
        Directory.CreateDirectory(dir);
        foreach (var p in Profiles.Load())
            using (var b = Taskbar.RenderIcon(p, 256)) b.Save(Path.Combine(dir, "icon-" + p.Id + ".png"), System.Drawing.Imaging.ImageFormat.Png);
    }
}

/// The hidden window that owns the menus, receives hotkeys and the other copies' command lines.
class App : Form {
    readonly NotifyIcon tray = new NotifyIcon();
    readonly System.Windows.Forms.Timer refresh = new System.Windows.Forms.Timer { Interval = 2000 };
    readonly System.Windows.Forms.Timer loginWatch = new System.Windows.Forms.Timer { Interval = 50 };
    Native.WinEventProc onShow;      // kept referenced: the hook calls it for as long as the app runs
    int hotkeys;

    const string SettingsKey = @"Software\MakeBecome\Claude Switcher";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunSwitcher = "Claude Switcher";
    static string RunProfile(Profile p) { return "Claude Switcher: " + p.Id; }

    List<Profile> profiles { get { return Profiles.Load(); } }

    readonly string[] startArgs;

    public App(string ipcTitle, string[] args) {
        startArgs = args;
        Text = ipcTitle;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-32000, -32000);
        Size = new Size(1, 1);
        Opacity = 0;

        var ps = profiles;
        foreach (var p in ps) try { Taskbar.Ensure(p); } catch (Exception) { }
        try { Taskbar.EnsureSelf(); Taskbar.Prune(ps); RegisterUninstall(); } catch (Exception) { }
        tray.MouseUp += (s, e) => PopMenu();
        refresh.Tick += (s, e) => Refresh_();
        loginWatch.Tick += (s, e) => WatchLogin();
    }

    protected override void SetVisibleCore(bool value) {
        // Never shown: create the handle (for hotkeys and messages), stay invisible.
        if (!IsHandleCreated) { CreateHandle(); OnCreated(); }
        base.SetVisibleCore(false);
    }

    void OnCreated() {
        RegisterHotkeys(profiles.Count);
        onShow = (hook, ev, h, obj, child, thread, time) => { if (obj == 0 && ev != 0x8001 /* destroy */) OnWindowShown(h); };
        Native.SetWinEventHook(Native.EventObjectCreate, Native.EventObjectShow, IntPtr.Zero, onShow, 0, 0, 0 /* out of context */);
        SyncTray();
        refresh.Start();
        BeginInvoke(new Action(() => Command(startArgs)));
    }

    // --- Command lines (ours at start, the other copies' later)

    void Command(string[] args) {
        int i = Array.IndexOf(args, "--open");
        if (i >= 0 && i + 1 < args.Length) {
            var p = profiles.FirstOrDefault(x => x.Id == args[i + 1]);
            if (p != null) Claude.Open(p);
            return;
        }
        i = Array.IndexOf(args, "--login");
        if (i >= 0 && i + 1 < args.Length) {
            var p = profiles.FirstOrDefault(x => x.Id == args[i + 1]);
            if (p != null) SafeLogin(p);
            return;
        }
        i = Array.IndexOf(args, "--icon");
        if (i >= 0 && i + 2 < args.Length) { SetOwnIcon(args[i + 1], args[i + 2] == "on"); return; }
        if (args.Contains("--uninstall")) { Uninstall(); return; }
        if (args.Contains("--quiet")) return;
        PopMenu();
    }

    protected override void WndProc(ref Message m) {
        if (m.Msg == Native.WmCopyData) {
            var cd = (Native.CopyData)Marshal.PtrToStructure(m.LParam, typeof(Native.CopyData));
            var bytes = new byte[cd.Size];
            if (cd.Size > 0) Marshal.Copy(cd.Ptr, bytes, 0, cd.Size);
            string[] args = cd.Size == 0 ? new string[0] : Encoding.UTF8.GetString(bytes).Split('\n');
            BeginInvoke(new Action(() => Command(args)));   // reply first: the sender is waiting
            m.Result = new IntPtr(1);
            return;
        }
        if (m.Msg == Native.WmHotkey) {
            int n = m.WParam.ToInt32();
            var ps = profiles;
            if (n < ps.Count) Claude.Open(ps[n]);
            return;
        }
        base.WndProc(ref m);
    }

    // --- Hotkeys Ctrl+Alt+Shift+1…9 (plain Ctrl+Alt is AltGr on many keyboards: it types { [ ] })

    void RegisterHotkeys(int count) {
        for (int i = 0; i < hotkeys; i++) Native.UnregisterHotKey(Handle, i);
        hotkeys = Math.Min(count, 9);
        for (int i = 0; i < hotkeys; i++)
            Native.RegisterHotKey(Handle, i, Native.ModControl | Native.ModAlt | Native.ModShift | Native.ModNoRepeat, (uint)('1' + i));
    }

    static string HotkeyText(int i) { return "Ctrl+Alt+Shift+" + (i + 1); }

    // --- Taskbar identity of Claude's windows

    // Every window created anywhere comes through here, so other processes are dismissed by pid,
    // remembered until the next refresh.
    readonly HashSet<uint> notClaude = new HashSet<uint>();
    readonly Dictionary<uint, string> claudePids = new Dictionary<uint, string>();   // main pid → profile id

    void OnWindowShown(IntPtr h) {
        if (!Native.IsTopLevel(h)) return;
        uint pid; Native.GetWindowThreadProcessId(h, out pid);
        if (notClaude.Contains(pid)) return;
        string id;
        if (!claudePids.TryGetValue(pid, out id)) {
            // A process seen for the first time: Claude's processes are few, the others many.
            string image = Native.ImagePath((int)pid);
            if (image == null || !Path.GetFileName(image).Equals("claude.exe", StringComparison.OrdinalIgnoreCase)) { notClaude.Add(pid); return; }
            Refresh_();
            if (!claudePids.TryGetValue(pid, out id)) { notClaude.Add(pid); return; }
        }
        var p = profiles.FirstOrDefault(x => x.Id == id);
        if (p != null) Taskbar.Tag(h, p, false);
    }

    void Refresh_() {
        var ps = profiles; var pids = Claude.RunningPids(ps);
        notClaude.Clear(); claudePids.Clear();
        foreach (var kv in pids) claudePids[(uint)kv.Value] = kv.Key;
        Taskbar.TagAll(ps, pids);
        RefreshTray(ps, pids);
    }

    // --- Notification area: one dot per profile, in its colour, filled when open

    bool ShowTray {
        get { using (var k = Registry.CurrentUser.OpenSubKey(SettingsKey)) return k == null || !(k.GetValue("ShowTray") is int) || (int)k.GetValue("ShowTray") != 0; }
        set { using (var k = Registry.CurrentUser.CreateSubKey(SettingsKey)) k.SetValue("ShowTray", value ? 1 : 0, RegistryValueKind.DWord); SyncTray(); }
    }

    void SyncTray() { Refresh_(); tray.Visible = ShowTray; }

    IntPtr trayIcon;
    void RefreshTray(List<Profile> ps, Dictionary<string, int> pids) {
        int px = SystemInformation.SmallIconSize.Width;
        using (var b = new Bitmap(px, px)) {
            using (var g = Graphics.FromImage(b)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int cols = ps.Count <= 1 ? 1 : ps.Count <= 4 ? 2 : 3;
                float cell = px / (float)cols, d = cell * 0.78f;
                for (int i = 0; i < Math.Min(ps.Count, 9); i++) {
                    var r = new RectangleF((i % cols) * cell + (cell - d) / 2, (i / cols) * cell + (cell - d) / 2, d, d);
                    var c = Taskbar.ColorOf(ps[i].Color);
                    if (pids.ContainsKey(ps[i].Id)) using (var f = new SolidBrush(c)) g.FillEllipse(f, r);
                    else using (var pen = new Pen(c, Math.Max(1f, d * 0.18f))) g.DrawEllipse(pen, r.X + d * 0.09f, r.Y + d * 0.09f, d * 0.82f, d * 0.82f);
                }
            }
            IntPtr h = b.GetHicon();
            tray.Icon = Icon.FromHandle(h);
            if (trayIcon != IntPtr.Zero) DestroyIcon(trayIcon);
            trayIcon = h;
        }
        string tip = "Claude Switcher\n" + string.Join("  ", ps.Select(p => (pids.ContainsKey(p.Id) ? "● " : "○ ") + p.Name).ToArray());
        tray.Text = tip.Length > 63 ? tip.Substring(0, 62) + "…" : tip;   // the limit Windows Forms enforces
    }
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

    // --- Menus

    ContextMenuStrip menu;

    /// The Switcher's menu, opened where the mouse is.
    void PopMenu() {
        if (menu != null) { menu.Close(); menu.Dispose(); }
        menu = BuildMenu();
        Native.SetForegroundWindow(Handle);   // so a click elsewhere closes the menu
        menu.Show(Cursor.Position);
    }

    static Bitmap Dot(string hex, bool filled) {
        int px = (int)(12 * (SystemInformation.SmallIconSize.Width / 16f));
        var b = new Bitmap(px, px);
        using (var g = Graphics.FromImage(b)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var c = Taskbar.ColorOf(hex);
            if (filled) using (var f = new SolidBrush(c)) g.FillEllipse(f, 1, 1, px - 2, px - 2);
            else using (var pen = new Pen(c, Math.Max(1.5f, px / 8f))) g.DrawEllipse(pen, 1.5f, 1.5f, px - 3, px - 3);
        }
        return b;
    }

    ToolStripMenuItem Item(string text, Action act, bool check = false) {
        var it = new ToolStripMenuItem(text) { Checked = check };
        if (act != null) it.Click += (s, e) => act();
        return it;
    }

    ContextMenuStrip BuildMenu() {
        var ps = profiles;
        var pids = Claude.RunningPids(ps);
        var uuids = ps.ToDictionary(x => x.Id, x => Profiles.AccountUuid(x));
        var m = new ContextMenuStrip { ShowImageMargin = true };
        for (int i = 0; i < ps.Count; i++) {
            var p = ps[i];
            var sub = new List<string>();
            if (!string.IsNullOrEmpty(p.Email)) sub.Add(p.Email);
            var twin = uuids[p.Id] == null ? null : ps.FirstOrDefault(o => o.Id != p.Id && uuids[o.Id] == uuids[p.Id]);
            if (twin != null) sub.Add(L10n.T("sameAccount", twin.Name));
            var it = Item(p.Name + (sub.Count > 0 ? "     " + string.Join(" · ", sub.ToArray()) : ""), () => Claude.Open(p));
            it.Image = Dot(p.Color, pids.ContainsKey(p.Id));
            if (i < 9) it.ShortcutKeyDisplayString = HotkeyText(i);
            m.Items.Add(it);
        }
        m.Items.Add(new ToolStripSeparator());
        var manage = Item(L10n.T("manage"), null);
        foreach (var p in ps) {
            var pi = Item(p.Name, null);
            pi.Image = Dot(p.Color, pids.ContainsKey(p.Id));
            pi.DropDownItems.AddRange(ProfileItems(p, pids.ContainsKey(p.Id)));
            manage.DropDownItems.Add(pi);
        }
        m.Items.Add(manage);
        m.Items.Add(Item(L10n.T("newProfile"), NewProfile));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(Item(L10n.T("showTray"), () => ShowTray = !ShowTray, ShowTray));
        m.Items.Add(Item(L10n.T("switcherAtLogin"), ToggleSwitcherAtLogin, RunValue(RunSwitcher) != null));
        m.Items.Add(Item(L10n.T("quitSwitcher"), Quit));
        return m;
    }

    ToolStripItem[] ProfileItems(Profile p, bool running) {
        var items = new List<ToolStripItem> { Item(running ? L10n.T("front") : L10n.T("open"), () => Claude.Open(p)) };
        if (running) items.Add(Item(L10n.T("quitProfile", p.Name), () => { Claude.Quit(p); Refresh_(); }));
        if (!p.IsDefault) items.Add(Item(L10n.T("safeLogin"), () => SafeLogin(p)));
        items.Add(Item(L10n.T("openAtLogin"), () => ToggleOpenAtLogin(p.Id), p.OpenAtLogin));
        items.Add(Item(L10n.T("ownIcon"), () => SetOwnIcon(p.Id, !p.OwnIcon), p.OwnIcon));
        items.Add(new ToolStripSeparator());
        items.Add(Item(L10n.T("edit"), () => EditProfile(p.Id)));
        items.Add(Item(L10n.T("delete"), () => DeleteProfile(p.Id)));
        return items.ToArray();
    }

    // --- Safe login
    //
    // Claude's claude:// sign-in callback always starts the DEFAULT profile's Claude.exe, which hands the
    // URL to the running default instance after ~2 s (or starts the default profile). While one profile
    // is signing in, the Switcher catches that process, ends it, and delivers its URL to the profile
    // that asked: no other profile has to close (on the Mac they must).

    Profile loggingIn;
    DateTime loginUntil;
    string loginUuidBefore;
    int loginTicks;

    void SafeLogin(Profile p) {
        if (p.IsDefault) { Claude.Open(p); return; }   // callbacks reach it already
        loggingIn = p; loginUntil = DateTime.UtcNow.AddMinutes(10); loginUuidBefore = Profiles.AccountUuid(p);
        loginWatch.Start();
        Balloon(L10n.T("loginTitle", p.Name), L10n.T("loginBody", p.Name));
        Claude.Open(p);
    }

    void WatchLogin() {
        if (loggingIn == null || DateTime.UtcNow > loginUntil) { StopLogin(); return; }
        foreach (var cp in Claude.Running().Where(c => c.Url != null && c.DataDir == null).ToList()) {
            Claude.Kill(cp.Pid);
            Claude.Launch(loggingIn, cp.Url);
            loginUntil = DateTime.UtcNow.AddMinutes(2);   // a sign-in can take more than one round trip
        }
        // Signed in: Claude records the account, and the routing ends. (Its file is read once a second.)
        if (++loginTicks % 20 != 0) return;
        string now = Profiles.AccountUuid(loggingIn);
        if (now != null && now != loginUuidBefore) { Balloon(L10n.T("loginDone", loggingIn.Name), ""); StopLogin(); }
    }

    void StopLogin() { loginWatch.Stop(); loggingIn = null; }

    void Balloon(string title, string text) {
        if (!tray.Visible) return;
        tray.BalloonTipTitle = title;
        tray.BalloonTipText = text.Length > 0 ? text : title;
        tray.ShowBalloonTip(8000);
    }

    // --- Actions

    void Quit() { tray.Visible = false; Application.Exit(); }

    // --- Install and uninstall (Settings > Apps lists it when it runs from where the installer puts it)

    public static readonly string InstallDir = Path.Combine(Profiles.LocalAppData, @"Programs\Claude Switcher");
    const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MakeBecome.ClaudeSwitcher";
    static bool Installed { get { return string.Equals(Path.GetDirectoryName(Taskbar.Exe), InstallDir, StringComparison.OrdinalIgnoreCase); } }

    static void RegisterUninstall() {
        if (!Installed) return;
        using (var k = Registry.CurrentUser.CreateSubKey(UninstallKey)) {
            k.SetValue("DisplayName", "Claude Switcher");
            k.SetValue("DisplayVersion", Program.Version);
            k.SetValue("Publisher", "make become");
            k.SetValue("DisplayIcon", Taskbar.Exe);
            k.SetValue("InstallLocation", InstallDir);
            k.SetValue("URLInfoAbout", "https://make-become.github.io/claude-switcher/");
            k.SetValue("UninstallString", "\"" + Taskbar.Exe + "\" --uninstall");
            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }
    }

    /// Everything the Switcher put on this PC goes, except the profiles' data (Claude's own folders) and,
    /// when it was not installed by the installer, the program file itself.
    void Uninstall() {
        Taskbar.UntagAll();
        foreach (var p in profiles) SetRun(RunProfile(p), null);
        SetRun(RunSwitcher, null);
        try { if (Directory.Exists(Taskbar.StartMenuDir)) Directory.Delete(Taskbar.StartMenuDir, true); } catch (Exception) { }
        try { if (Directory.Exists(Profiles.SupportDir)) Directory.Delete(Profiles.SupportDir, true); } catch (Exception) { }
        try { Registry.CurrentUser.DeleteSubKeyTree(SettingsKey, false); } catch (Exception) { }
        try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch (Exception) { }
        if (Installed) {
            // A running program cannot delete itself: a shell does it once this process has ended.
            var si = new ProcessStartInfo("cmd.exe", "/c ping -n 3 127.0.0.1 >nul & rmdir /s /q \"" + InstallDir + "\"") {
                CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden };
            try { Process.Start(si).Dispose(); } catch (Exception) { }
        }
        Quit();
    }

    static string RunValue(string name) { using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k == null ? null : k.GetValue(name) as string; }
    static void SetRun(string name, string cmd) {
        using (var k = Registry.CurrentUser.CreateSubKey(RunKey)) { if (cmd == null) k.DeleteValue(name, false); else k.SetValue(name, cmd); }
    }

    void ToggleSwitcherAtLogin() { SetRun(RunSwitcher, RunValue(RunSwitcher) != null ? null : "\"" + Taskbar.Exe + "\" --quiet"); }

    void ToggleOpenAtLogin(string id) {
        var ps = profiles; var p = ps.FirstOrDefault(x => x.Id == id);
        if (p == null) return;
        p.OpenAtLogin = !p.OpenAtLogin; Profiles.Save(ps);
        SetRun(RunProfile(p), p.OpenAtLogin ? "\"" + Taskbar.Exe + "\" " + Taskbar.OpenArgs(p) : null);
    }

    void SetOwnIcon(string id, bool on) {
        var ps = profiles; var p = ps.FirstOrDefault(x => x.Id == id);
        if (p == null || p.OwnIcon == on) return;
        p.OwnIcon = on; Profiles.Save(ps);
        Taskbar.Ensure(p);
        Refresh_();
        if (on) Balloon("Claude " + p.Name, L10n.T("pinHint", p.Name));
    }

    void NewProfile() {
        var ps = profiles;
        var used = new HashSet<string>(ps.Select(x => x.Color));
        var free = Profiles.Palette.FirstOrDefault(c => !used.Contains(c[1]));
        var draft = new Profile { Color = free != null ? free[1] : "#7F7F7F", OwnIcon = true };
        var p = EditDialog.Show(draft, L10n.T("newTitle"), ps);
        if (p == null) return;
        Profiles.Assign(p, ps);
        ps.Add(p); Profiles.Save(ps);
        Taskbar.Ensure(p, true);
        RegisterHotkeys(ps.Count); Refresh_();
        SafeLogin(p);   // a new profile starts signed out
    }

    void EditProfile(string id) {
        var ps = profiles;
        int i = ps.FindIndex(x => x.Id == id);
        if (i < 0) return;
        var old = ps[i];
        var edited = EditDialog.Show(old, L10n.T("editTitle", old.Name), ps);
        if (edited == null) return;
        ps[i] = edited; Profiles.Save(ps);
        if (old.Name != edited.Name) Taskbar.Remove(old);
        Taskbar.Ensure(edited, true);
        if (edited.OpenAtLogin) SetRun(RunProfile(edited), "\"" + Taskbar.Exe + "\" " + Taskbar.OpenArgs(edited));
        Refresh_();   // open windows pick up the new name and icon
    }

    void DeleteProfile(string id) {
        var ps = profiles; var p = ps.FirstOrDefault(x => x.Id == id);
        if (p == null) return;
        string data = p.Dir == null ? L10n.T("defaultData") : @"%APPDATA%\" + p.Dir;
        if (!Confirm(L10n.T("deleteBody", data), L10n.T("deleteTitle", p.Name))) return;
        Claude.Quit(p);
        SetRun(RunProfile(p), null);
        Taskbar.Remove(p);
        ps.RemoveAll(x => x.Id == id); Profiles.Save(ps);
        RegisterHotkeys(ps.Count); Refresh_();
    }

    /// A question in front of everything: this app has no window of its own to put it over.
    static bool Confirm(string text, string title) {
        using (var owner = new Form { TopMost = true, ShowInTaskbar = false, Opacity = 0, StartPosition = FormStartPosition.Manual,
                                      Location = new Point(-32000, -32000), Size = new Size(1, 1) }) {
            owner.Show(); owner.Activate();
            return MessageBox.Show(owner, text, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.OK;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e) { tray.Visible = false; tray.Dispose(); base.OnFormClosed(e); }
}

/// Name, letter, colour, e-mail. Returns null on cancel.
static class EditDialog {
    public static Profile Show(Profile p, string title, List<Profile> existing) {
        using (var f = new Form { Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
                                  StartPosition = FormStartPosition.CenterScreen, AutoScaleMode = AutoScaleMode.Dpi, AutoSize = true,
                                  AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12), TopMost = true, ShowInTaskbar = true }) {
            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
            Func<string, Label> label = s => new Label { Text = s, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 12, 6) };
            var name = new TextBox { Text = p.Name, Width = 260 };
            var letter = new TextBox { Text = p.Letter, Width = 40, MaxLength = 2 };
            var email = new TextBox { Text = p.Email ?? "", Width = 260 };
            SetCue(name, L10n.T("namePh")); SetCue(email, L10n.T("emailPh"));
            var color = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DrawMode = DrawMode.OwnerDrawFixed, Width = 160 };
            foreach (var c in Profiles.Palette) color.Items.Add(c);
            color.DrawItem += (s, e) => {
                e.DrawBackground();
                if (e.Index < 0) return;
                var c = (string[])color.Items[e.Index];
                int d = e.Bounds.Height - 6;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(Taskbar.ColorOf(c[1]))) e.Graphics.FillEllipse(b, e.Bounds.X + 3, e.Bounds.Y + 3, d, d);
                TextRenderer.DrawText(e.Graphics, L10n.T(c[0]), e.Font, new Point(e.Bounds.X + d + 10, e.Bounds.Y + 1), e.ForeColor);
            };
            int sel = Array.FindIndex(Profiles.Palette, c => c[1] == p.Color);
            color.SelectedIndex = sel < 0 ? 0 : sel;
            grid.Controls.Add(label(L10n.T("name")), 0, 0); grid.Controls.Add(name, 1, 0);
            grid.Controls.Add(label(L10n.T("letter")), 0, 1); grid.Controls.Add(letter, 1, 1);
            grid.Controls.Add(label(L10n.T("color")), 0, 2); grid.Controls.Add(color, 1, 2);
            grid.Controls.Add(label(L10n.T("email")), 0, 3); grid.Controls.Add(email, 1, 3);
            var ok = new Button { Text = L10n.T("save"), DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = L10n.T("cancel"), DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
            buttons.Controls.Add(cancel); buttons.Controls.Add(ok);
            grid.Controls.Add(buttons, 0, 4); grid.SetColumnSpan(buttons, 2);
            f.Controls.Add(grid);
            f.AcceptButton = ok; f.CancelButton = cancel;
            f.Shown += (s, e) => { f.Activate(); name.Focus(); };

            while (true) {
                if (f.ShowDialog() != DialogResult.OK) return null;
                string n = name.Text.Trim();
                string problem = null;
                if (n.Length == 0) problem = L10n.T("needName");
                else if (existing.Any(x => x.Id != p.Id && string.Equals(x.Name, n, StringComparison.CurrentCultureIgnoreCase))) problem = L10n.T("nameTaken", n);
                if (problem != null) { MessageBox.Show(f, problem, title); continue; }
                var res = p.Copy();
                res.Name = n;
                string l = letter.Text.Trim();
                res.Letter = (l.Length > 0 ? l : n).Substring(0, 1).ToUpper();
                res.Color = ((string[])color.SelectedItem)[1];
                string e2 = email.Text.Trim();
                res.Email = e2.Length > 0 ? e2 : null;
                return res;
            }
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, string l);
    static void SetCue(TextBox t, string cue) { t.HandleCreated += (s, e) => SendMessage(t.Handle, 0x1501 /* EM_SETCUEBANNER */, IntPtr.Zero, cue); }
}

}
