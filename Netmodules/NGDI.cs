using System;
using System.Runtime.InteropServices;

namespace Netmodules
{
    public class GDI32
    {
        [DllImport("GDI32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("GDI32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth,
            int nHeight);

        [DllImport("GDI32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("GDI32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
                                        int nWidth, int nHeight, IntPtr hdcSrc,
                                        int nXSrc, int nYSrc, int dwRop);

        [DllImport("GDI32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("GDI32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
