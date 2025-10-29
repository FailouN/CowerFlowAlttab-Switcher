using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace CoverflowAltTab
{
    /// <summary>
    /// Получение превью окна:
    /// 1) PrintWindow(PW_RENDERFULLCONTENT) — максимально похоже на DWM;
    /// 2) если не удалось — BitBlt из DC окна;
    /// 3) затем рендерим маску (скруглённые углы + тень) поверх, с прозрачным фоном;
    /// 4) нормализация размеров под фиксированный максимум.
    /// </summary>
    public static class PreviewGenerator
    {
        private const int PW_RENDERFULLCONTENT = 0x00000002;
        private const int SRCCOPY = 0x00CC0020;

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, int nFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
                                          IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out RECT pvAttribute, int cbAttribute);

        private enum DWMWINDOWATTRIBUTE
        {
            DWMWA_EXTENDED_FRAME_BOUNDS = 9
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        public static ImageSource? CaptureWindowWithMask(
            IntPtr hwnd,
            int maxWidth = 500,
            int maxHeight = 350,
            int cornerRadius = 16,
            int shadowSize = 8,
            double shadowOpacity = 0.35)
        {
            var rawBmp = CaptureRawWindow(hwnd);
            if (rawBmp == null) return null;

            // нормализация размеров
            double scale = Math.Min((double)maxWidth / rawBmp.PixelWidth, (double)maxHeight / rawBmp.PixelHeight);
            if (scale > 1.0) scale = 1.0; // не увеличиваем, только уменьшаем

            int w = (int)(rawBmp.PixelWidth * scale);
            int h = (int)(rawBmp.PixelHeight * scale);

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // прозрачный фон
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w + shadowSize * 2, h + shadowSize * 2));

                // простая полупрозрачная тень
                var shadowBrush = new SolidColorBrush(Color.FromArgb((byte)(shadowOpacity * 255), 0, 0, 0));
                dc.DrawRoundedRectangle(
                    shadowBrush,
                    null,
                    new Rect(shadowSize, shadowSize, w, h),
                    cornerRadius + shadowSize,
                    cornerRadius + shadowSize);

                // скруглённые углы и отрисовка превью
                var clip = new RectangleGeometry(new Rect(shadowSize, shadowSize, w, h), cornerRadius, cornerRadius);
                dc.PushClip(clip);
                dc.DrawImage(rawBmp, new Rect(shadowSize, shadowSize, w, h));
                dc.Pop();
            }

            var rtb = new RenderTargetBitmap(w + shadowSize * 2, h + shadowSize * 2, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        private static BitmapSource? CaptureRawWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return null;

            RECT bounds;
            if (DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out bounds, Marshal.SizeOf<RECT>()) != 0
                || bounds.Width <= 0 || bounds.Height <= 0)
            {
                if (!GetWindowRect(hwnd, out bounds) || bounds.Width <= 0 || bounds.Height <= 0)
                    return null;
            }

            IntPtr hdcSrc = GetWindowDC(hwnd);
            if (hdcSrc == IntPtr.Zero) return null;

            IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, bounds.Width, bounds.Height);
            IntPtr hOld = SelectObject(hdcDest, hBitmap);

            bool ok = false;
            try
            {
                ok = PrintWindow(hwnd, hdcDest, PW_RENDERFULLCONTENT);
                if (!ok)
                {
                    ok = BitBlt(hdcDest, 0, 0, bounds.Width, bounds.Height, hdcSrc, 0, 0, SRCCOPY);
                }
            }
            finally
            {
                SelectObject(hdcDest, hOld);
                DeleteDC(hdcDest);
                ReleaseDC(hwnd, hdcSrc);
            }

            if (!ok)
            {
                DeleteObject(hBitmap);
                return null;
            }

            try
            {
                var bmp = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bmp.Freeze();
                return bmp;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
    }
}
