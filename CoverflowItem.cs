// CoverflowItem.cs
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Interop;

namespace CoverflowAltTab
{
    public class CoverflowItem
    {
        public IntPtr Hwnd;
        public string Title { get; set; } = string.Empty;
        public ImageSource? Preview { get; set; }
        public ImageSource? Icon { get; set; }
        public GeometryModel3D? Model { get; set; }
        public Point3D Position { get; set; }
        public double Scale { get; set; }
        public AxisAngleRotation3D Rotation { get; set; }
        public double Opacity { get; set; }
        public Material Material { get; set; }
    }
}
