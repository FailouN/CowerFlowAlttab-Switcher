using System;
using System.Runtime.InteropServices;

namespace CoverflowAltTab
{
    public static class WindowActivator
    {
        private const int SW_RESTORE = 9;
        private const uint WM_CLOSE = 0x0010;

        // Активировать окно
        public static void ActivateWindow(IntPtr hWnd)
        {
            if (Native.IsIconic(hWnd))
                Native.ShowWindowAsync(hWnd, SW_RESTORE);
            Native.BringWindowToTop(hWnd);
            Native.SetForegroundWindow(hWnd);
        }

        // Закрыть окно
        public static void CloseWindow(IntPtr hWnd)
        {
            // Отправить сообщение WM_CLOSE, чтобы закрыть окно
            Native.PostMessage(hWnd, WM_CLOSE, 0, 0);
        }
    }
}
