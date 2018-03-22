using System;
using System.Runtime.InteropServices;

namespace Netmodules
{
    //  c:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe -target:module NUser32.cs
    public class User32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint wCmd);

        [DllImport("User32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("User32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

    }

}
