using System;
using System.Windows.Media;

namespace CoverflowAltTab
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = string.Empty;
        public ImageSource? Icon { get; set; }

        // DWM-like превью окна
        public ImageSource? Preview { get; set; }
    }
}
