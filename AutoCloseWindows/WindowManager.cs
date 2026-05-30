using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AutoCloseWindows
{
    public static class WindowManager
    {
        // ──────────────────────────────────────────────
        //  Win32 API
        // ──────────────────────────────────────────────
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private const uint WM_CLOSE         = 0x0010;
        private const int  GWL_EXSTYLE      = -20;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // ──────────────────────────────────────────────
        //  System process blacklist
        // ──────────────────────────────────────────────
        private static readonly HashSet<string> SystemProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "dwm", "winlogon", "csrss", "lsass", "services", "svchost", "smss", "wininit",
            "taskmgr", "SearchUI", "SearchHost", "ShellExperienceHost",
            "StartMenuExperienceHost", "ctfmon", "TextInputHost", "SystemSettings",
            "ApplicationFrameHost", "RuntimeBroker", "sihost", "fontdrvhost",
        };

        // Only File Explorer folder windows (CabinetWClass) — NOT the shell (taskbar/desktop)
        private static readonly HashSet<string> ExplorerFolderClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CabinetWClass",
            "ExploreWClass",
        };

        // ──────────────────────────────────────────────
        //  Public API
        // ──────────────────────────────────────────────
        public static List<WindowInfo> GetCloseableWindows(int selfPid)
        {
            var list        = new List<WindowInfo>();
            var seenHandles = new HashSet<IntPtr>();

            // ── Pass 1: enum top-level windows ────────
            // Catches browsers and any app with a standard or custom-frame window.
            // NOTE: WS_CAPTION / WS_SYSMENU are intentionally NOT checked here.
            //       VS Code, Zalo, Claude and many Electron apps use frameless windows
            //       (WS_POPUP | WS_THICKFRAME) that lack those style bits but are still
            //       fully visible, titled user windows.
            EnumWindows((hWnd, _) =>
            {
                if (IsCloseableWindow(hWnd, selfPid, out string title, out string procName))
                {
                    list.Add(new WindowInfo(hWnd, title, procName));
                    seenHandles.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);

            // ── Pass 2: process main windows (safety net) ─────────────────────────
            // Some apps register their main window handle with the OS differently.
            // Process.MainWindowHandle gives us a second chance to catch them.
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.Id == selfPid) continue;
                    if (proc.MainWindowHandle == IntPtr.Zero) continue;
                    if (seenHandles.Contains(proc.MainWindowHandle)) continue;
                    if (!IsWindowVisible(proc.MainWindowHandle)) continue;
                    if (SystemProcessNames.Contains(proc.ProcessName)) continue;

                    if (proc.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                    {
                        var cn = new StringBuilder(256);
                        GetClassName(proc.MainWindowHandle, cn, cn.Capacity);
                        if (!ExplorerFolderClasses.Contains(cn.ToString())) continue;
                    }

                    int tlen = GetWindowTextLength(proc.MainWindowHandle);
                    if (tlen == 0) continue;
                    var tsb = new StringBuilder(tlen + 1);
                    GetWindowText(proc.MainWindowHandle, tsb, tsb.Capacity);
                    string t = tsb.ToString().Trim();
                    if (string.IsNullOrEmpty(t)) continue;

                    list.Add(new WindowInfo(proc.MainWindowHandle, t, proc.ProcessName));
                    seenHandles.Add(proc.MainWindowHandle);
                }
                catch { }
            }

            return list;
        }

        public static int CloseAllWindows(int selfPid)
        {
            var windows = GetCloseableWindows(selfPid);
            foreach (var w in windows)
                PostMessage(w.Handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            return windows.Count;
        }

        public static void CloseWindowList(IEnumerable<WindowInfo> windows)
        {
            foreach (var w in windows)
                PostMessage(w.Handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        // ──────────────────────────────────────────────
        //  Window filter (Pass 1)
        // ──────────────────────────────────────────────
        private static bool IsCloseableWindow(IntPtr hWnd, int selfPid,
                                              out string title, out string processName)
        {
            title = string.Empty;
            processName = string.Empty;

            // Must be visible
            if (!IsWindowVisible(hWnd)) return false;

            // Skip floating tool bars / system trays (no taskbar button)
            long exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0) return false;

            // Must have a non-empty title
            int len = GetWindowTextLength(hWnd);
            if (len == 0) return false;
            var sb = new StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            title = sb.ToString().Trim();
            if (string.IsNullOrEmpty(title)) return false;

            // Get process — skip ourselves
            GetWindowThreadProcessId(hWnd, out uint pid);
            if ((int)pid == selfPid) return false;

            try
            {
                var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;
            }
            catch { return false; }

            // Skip system / shell processes
            if (SystemProcessNames.Contains(processName)) return false;

            // explorer.exe: only File Explorer folder windows, not shell components
            if (processName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
            {
                var cn = new StringBuilder(256);
                GetClassName(hWnd, cn, cn.Capacity);
                return ExplorerFolderClasses.Contains(cn.ToString());
            }

            return true;
        }
    }

    // ──────────────────────────────────────────────
    //  Data container
    // ──────────────────────────────────────────────
    public class WindowInfo
    {
        public IntPtr  Handle      { get; }
        public string  Title       { get; }
        public string  ProcessName { get; }

        public WindowInfo(IntPtr handle, string title, string processName)
        {
            Handle      = handle;
            Title       = title;
            ProcessName = processName;
        }

        public override string ToString() => $"[{ProcessName}] {Title}";
    }
}
