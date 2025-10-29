using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Drawing;

namespace CoverflowAltTab
{
    public static class WindowEnumerator
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #region WinAPI
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true)] private static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int DWMWA_CLOAKED = 14;

        private static string GetClassNameStr(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetWindowTitleStr(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
        #endregion

        public static List<WindowInfo> GetOpenWindows()
        {
            List<WindowInfo> windows = new();

            IntPtr shellWindow = GetShellWindow();
            uint currentPid = (uint)Process.GetCurrentProcess().Id;

            EnumWindows((hWnd, lParam) =>
            {
                // базовые отсеивания
                if (hWnd == IntPtr.Zero) return true;
                if (hWnd == shellWindow) return true;            // окно обоев
                if (!IsWindowVisible(hWnd)) return true;          // только видимые

                // не показывать собственный процесс (SwitcherWindow и скрытые его окна)
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == currentPid) return true;

                // не показывать "спящие"/скрытые DWM окна (UWP и пр.)
                if (IsCloaked(hWnd)) return true;

                // не показывать toolwindows
                uint exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;

                // не показывать системные/служебные классы
                string className = GetClassNameStr(hWnd);
                if (className == "Shell_TrayWnd" ||
                    className == "Progman" ||
                    className == "WorkerW" ||
                    className.StartsWith("Xaml_WindowedPopupClass", StringComparison.OrdinalIgnoreCase) ||
                    className.StartsWith("Windows.UI.Input", StringComparison.OrdinalIgnoreCase) ||
                    className.StartsWith("IME", StringComparison.OrdinalIgnoreCase) ||
                    className.Contains("Candidate", StringComparison.OrdinalIgnoreCase))
                    return true;

                // не показывать окно "Интерфейс ввода Windows" (TextInputHost и подобные)
                if (IsTextInputProcess(pid)) return true;

                // заголовок обязателен (как в Alt+Tab для обычных окон)
                if (GetWindowTextLength(hWnd) == 0) return true;

                string title = GetWindowTitleStr(hWnd);

                // собрать иконку процесса (по возможности)
                string? exePath = null;
                ImageSource? icon = null;
                try
                {
                    using var p = Process.GetProcessById((int)pid);
                    exePath = p.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        using Icon? ic = Icon.ExtractAssociatedIcon(exePath);
                        if (ic != null)
                            icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                ic.Handle, System.Windows.Int32Rect.Empty,
                                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    }
                }
                catch { /* бывают AccessDenied / 32-bit vs 64-bit */ }

                // используем новый метод с альфа-маской вместо DWM thumbnail
                var preview = PreviewGenerator.CaptureWindowWithMask(hWnd);

                windows.Add(new WindowInfo
                {
                    Handle  = hWnd,
                    Title   = title,
                    Icon    = icon,
                    Preview = preview
                });

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        private static bool IsCloaked(IntPtr hWnd)
        {
            // 0 — нет, 1/2/… — cloaked (см. DWM_CLOAKED_* значения)
            if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0)
                return cloaked != 0;
            return false;
        }

        private static bool IsTextInputProcess(uint pid)
        {
            try
            {
                using var p = Process.GetProcessById((int)pid);
                string name = p.ProcessName ?? string.Empty;
                if (name.Equals("TextInputHost", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (name.Contains("InputApp", StringComparison.OrdinalIgnoreCase))
                    return true;

                string path = p.MainModule?.FileName ?? string.Empty;
                if (path.EndsWith("TextInputHost.exe", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (path.IndexOf("WindowsInternal.ComposableShell.Experiences.TextInput", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            catch { /* недоступен процесс — не считаем его текстовым вводом */ }
            return false;
        }
    }
}
