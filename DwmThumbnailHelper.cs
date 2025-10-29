using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace CoverflowAltTab
{
    public static class DwmThumbnailHelper
    {
        public static BitmapSource? CaptureWindowWithAlpha(IntPtr hWnd)
        {
            try
            {
                var hwndSource = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                if (hwndSource == IntPtr.Zero) return null;

                // Регистрируем превью
                if (Native.DwmRegisterThumbnail(hwndSource, hWnd, out IntPtr thumb) != 0 || thumb == IntPtr.Zero)
                    return null;

                // Узнаем размер исходного окна
                Native.PSIZE size;
                Native.DwmQueryThumbnailSourceSize(thumb, out size);

                // Настраиваем свойства превью
                var props = new Native.DWM_THUMBNAIL_PROPERTIES
                {
                    dwFlags = Native.DWM_TNP_VISIBLE | Native.DWM_TNP_RECTDESTINATION | Native.DWM_TNP_OPACITY,
                    fVisible = true,
                    opacity = 255,
                    fSourceClientAreaOnly = false,
                    rcDestination = new Native.RECT
                    {
                        Left = 0,
                        Top = 0,
                        Right = size.x,
                        Bottom = size.y
                    }
                };

                Native.DwmUpdateThumbnailProperties(thumb, ref props);

                // Создаём RenderTargetBitmap для рендеринга превью
                var rtb = new RenderTargetBitmap(size.x, size.y, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                var dv = new System.Windows.Media.DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(new System.Windows.Media.VisualBrush { Visual = HwndSource.FromHwnd(hWnd).RootVisual }, null, new Rect(0, 0, size.x, size.y));
                }
                rtb.Render(dv);
                rtb.Freeze();

                // Убираем превью (чтобы не оставлять висеть)
                Native.DwmUnregisterThumbnail(thumb);

                return rtb;
            }
            catch
            {
                return null;
            }
        }
    }
}
