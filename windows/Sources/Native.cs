// Win32 and shell interop: process command lines, windows, taskbar identity, shortcuts.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace ClaudeSwitcher {

static class Native {
    // --- Processes

    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("ntdll.dll")] static extern int NtQueryInformationProcess(IntPtr h, int cls, IntPtr buf, int len, out int ret);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr h, int flags, StringBuilder name, ref int size);
    const uint QueryLimited = 0x1000;

    /// The process's command line (ProcessCommandLineInformation, Windows 8.1+); null when unreadable.
    public static string CommandLine(int pid) {
        IntPtr h = OpenProcess(QueryLimited, false, pid);
        if (h == IntPtr.Zero) return null;
        try {
            int len;
            NtQueryInformationProcess(h, 60, IntPtr.Zero, 0, out len);
            if (len <= 0) return null;
            IntPtr buf = Marshal.AllocHGlobal(len);
            try {
                if (NtQueryInformationProcess(h, 60, buf, len, out len) != 0) return null;
                int n = (ushort)Marshal.ReadInt16(buf);                      // UNICODE_STRING.Length, in bytes
                return Marshal.PtrToStringUni(Marshal.ReadIntPtr(buf, IntPtr.Size), n / 2);
            } finally { Marshal.FreeHGlobal(buf); }
        } finally { CloseHandle(h); }
    }

    public static string ImagePath(int pid) {
        IntPtr h = OpenProcess(QueryLimited, false, pid);
        if (h == IntPtr.Zero) return null;
        try {
            var sb = new StringBuilder(1024); int size = sb.Capacity;
            return QueryFullProcessImageName(h, 0, sb, ref size) ? sb.ToString(0, size) : null;
        } finally { CloseHandle(h); }
    }

    // --- Windows

    public delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc f, IntPtr l);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr h, uint cmd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    /// Top-level windows of a process that are not owned by another window (the ones the taskbar shows).
    public static List<IntPtr> TopWindows(int pid) {
        var res = new List<IntPtr>();
        EnumWindows((h, l) => {
            uint p; GetWindowThreadProcessId(h, out p);
            if (p == pid && GetWindow(h, 4 /* GW_OWNER */) == IntPtr.Zero) res.Add(h);
            return true;
        }, IntPtr.Zero);
        return res;
    }

    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr h, uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
    public static bool IsTopLevel(IntPtr h) { return GetAncestor(h, 1 /* GA_PARENT */) == GetDesktopWindow(); }

    public static string TitleOf(IntPtr h) { var sb = new StringBuilder(256); GetWindowText(h, sb, 256); return sb.ToString(); }

    /// Restore and bring forward. Windows refuses focus changes from a background process, so when
    /// the first try fails a no-op Alt tap marks this process as the one the user just used.
    public static void Activate(IntPtr h) {
        ShowWindow(h, IsIconic(h) ? 9 /* SW_RESTORE */ : 5 /* SW_SHOW */);
        if (SetForegroundWindow(h) && GetForegroundWindow() == h) return;
        keybd_event(0x12, 0, 0, UIntPtr.Zero); keybd_event(0x12, 0, 2 /* KEYUP */, UIntPtr.Zero);
        SetForegroundWindow(h);
    }

    // Window events: a Claude window being shown gets its taskbar identity before it is drawn there.
    public delegate void WinEventProc(IntPtr hook, uint ev, IntPtr h, int obj, int child, uint thread, uint time);
    [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr mod, WinEventProc f, uint pid, uint thread, uint flags);
    public const uint EventObjectCreate = 0x8000, EventObjectShow = 0x8002;

    // Hotkeys
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
    public const uint ModAlt = 1, ModControl = 2, ModShift = 4, ModNoRepeat = 0x4000;
    public const int WmHotkey = 0x0312, WmCopyData = 0x004A;

    // One running Switcher: the others hand it their command line.
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, ref CopyData d);
    [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
    [StructLayout(LayoutKind.Sequential)] public struct CopyData { public IntPtr Data; public int Size; public IntPtr Ptr; }

    // --- Taskbar identity (AppUserModelID) on windows, on this process and on shortcuts

    [StructLayout(LayoutKind.Sequential, Pack = 4)] public struct PropertyKey { public Guid Fmtid; public uint Pid; }
    [StructLayout(LayoutKind.Explicit)] public struct PropVariant { [FieldOffset(0)] public ushort Vt; [FieldOffset(8)] public IntPtr Ptr; }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore {
        void GetCount(out uint c); void GetAt(uint i, out PropertyKey k);
        void GetValue(ref PropertyKey k, out PropVariant v); void SetValue(ref PropertyKey k, ref PropVariant v); void Commit();
    }
    [DllImport("shell32.dll")] static extern int SHGetPropertyStoreForWindow(IntPtr h, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore s);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern int SetCurrentProcessExplicitAppUserModelID(string id);
    [DllImport("ole32.dll")] static extern int PropVariantClear(ref PropVariant v);
    [DllImport("shell32.dll")] public static extern void SHChangeNotify(int ev, uint flags, IntPtr a, IntPtr b);

    static readonly Guid AppModel = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
    public const uint KeyId = 5, KeyRelaunchCommand = 2, KeyRelaunchIcon = 3, KeyRelaunchName = 4;

    static void Set(IPropertyStore s, uint key, string v) {
        var k = new PropertyKey { Fmtid = AppModel, Pid = key };
        var pv = new PropVariant { Vt = (ushort)(v == null ? 0 /* VT_EMPTY */ : 31 /* VT_LPWSTR */), Ptr = v == null ? IntPtr.Zero : Marshal.StringToCoTaskMemUni(v) };
        try { s.SetValue(ref k, ref pv); } finally { PropVariantClear(ref pv); }
    }

    /// Give a window (of any process) its own taskbar group, or with id null, give it back to its app.
    /// The relaunch values make a pinned button start the profile through the Switcher.
    public static bool TagWindow(IntPtr h, string id, string relaunchCommand, string name, string icon) {
        var iid = typeof(IPropertyStore).GUID;
        IPropertyStore s;
        if (SHGetPropertyStoreForWindow(h, ref iid, out s) != 0) return false;
        try {
            // The relaunch values first, the id last: the taskbar reads them when the id changes.
            Set(s, KeyRelaunchCommand, id == null ? null : relaunchCommand);
            Set(s, KeyRelaunchName, id == null ? null : name);
            Set(s, KeyRelaunchIcon, id == null ? null : icon);
            Set(s, KeyId, id);
            s.Commit();
            return true;
        } catch (Exception) { return false; }
        finally { Marshal.ReleaseComObject(s); }
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")] class ShellLink { }
    [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellLinkW {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder f, int n, IntPtr fd, uint flags);
        void GetIDList(out IntPtr p); void SetIDList(IntPtr p);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder s, int n);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string s);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder s, int n);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string s);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder s, int n);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string s);
        void GetHotkey(out short k); void SetHotkey(short k);
        void GetShowCmd(out int c); void SetShowCmd(int c);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder s, int n, out int i);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string s, int i);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string s, uint r);
        void Resolve(IntPtr h, uint flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string s);
    }

    /// A Start-menu shortcut carrying an AppUserModelID: the taskbar takes that group's name and icon from it.
    public static void WriteShortcut(string path, string target, string args, string icon, string description, string appId) {
        var link = (IShellLinkW)new ShellLink();
        try {
            link.SetPath(target);
            link.SetArguments(args);
            link.SetIconLocation(icon, 0);
            link.SetDescription(description);
            link.SetWorkingDirectory(System.IO.Path.GetDirectoryName(target));
            var store = (IPropertyStore)link;
            Set(store, KeyId, appId);
            store.Commit();
            ((IPersistFile)link).Save(path, true);
        } finally { Marshal.ReleaseComObject(link); }
        // The taskbar keeps what it read from a shortcut until the shell says the file changed.
        SHChangeNotify(0x00002000 /* SHCNE_UPDATEITEM */, 0x0005 /* SHCNF_PATHW */, path, IntPtr.Zero);
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern void SHChangeNotify(int ev, uint flags, string a, IntPtr b);

    /// Target, arguments and icon of an existing shortcut, to tell whether it needs rewriting.
    public static string[] ReadShortcut(string path) {
        var link = (IShellLinkW)new ShellLink();
        try {
            ((IPersistFile)link).Load(path, 0);
            var t = new StringBuilder(1024); var a = new StringBuilder(1024); var i = new StringBuilder(1024); int idx;
            link.GetPath(t, t.Capacity, IntPtr.Zero, 4 /* SLGP_RAWPATH */);
            link.GetArguments(a, a.Capacity);
            link.GetIconLocation(i, i.Capacity, out idx);
            return new[] { t.ToString(), a.ToString(), i.ToString() };
        } catch (Exception) { return null; }
        finally { Marshal.ReleaseComObject(link); }
    }
}

}
