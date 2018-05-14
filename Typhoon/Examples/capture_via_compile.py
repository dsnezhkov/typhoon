#
# This example showcases dynamic compilation of managed ode with native interop, and then using the generated assembly 
# in PyDLR to invoke managed code
# PyDLR -> .Net -> (Interop) -> unmanaged
#

import clr

from System.Environment import CurrentDirectory
from System.IO import Path, Directory

from System.CodeDom import Compiler
from Microsoft.CSharp import CSharpCodeProvider


def Generate(code, name, references=None, outputDirectory=None, inMemory=False):
    CompilerParams = Compiler.CompilerParameters()

    if outputDirectory is None:
        outputDirectory = Directory.GetCurrentDirectory()
    if not inMemory:
        CompilerParams.OutputAssembly = Path.Combine(outputDirectory, name + ".dll")
        CompilerParams.GenerateInMemory = False
    else:
        CompilerParams.GenerateInMemory = True

    CompilerParams.TreatWarningsAsErrors = False
    CompilerParams.GenerateExecutable = False
    CompilerParams.CompilerOptions = "/optimize"

    for reference in references or []:
        CompilerParams.ReferencedAssemblies.Add(reference)

    provider = CSharpCodeProvider()
    compile = provider.CompileAssemblyFromSource(CompilerParams, code)

    if compile.Errors.HasErrors:
        raise Exception("Compile error: %r" % list(compile.Errors.List))

    if inMemory:
        return compile.CompiledAssembly
    return compile.PathToAssembly


def ScreenCapture(x, y, width, height):

    from UnmanagedCode import User32, GDI32
    from System.Drawing import Bitmap, Image

    hdcSrc = User32.GetWindowDC(User32.GetDesktopWindow())
    hdcDest = GDI32.CreateCompatibleDC(hdcSrc)
    hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height)
    GDI32.SelectObject(hdcDest, hBitmap)

    # 0x00CC0020 is the magic number for a copy raster operation
    GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, x, y, 0x00CC0020)
    result = Bitmap(Image.FromHbitmap(hBitmap))
    User32.ReleaseDC(User32.GetDesktopWindow(), hdcSrc)
    GDI32.DeleteDC(hdcDest)
    GDI32.DeleteObject(hBitmap)
    return result

def printImageMap(image):
    for y in range(image.Height):
        row = []
        for x in range(image.Width):
            color = image.GetPixel(x, y)
            value = color.R + color.G + color.B
            if value > 384:
                row.append(' ')
            else:
                row.append('X')
        print ''.join(row)

def doCapture():

    clr.AddReference('System.Drawing')

    image = ScreenCapture(0, 0, 50, 400)
    print(type(image))
    #printImageMap(image)
    image.Save("capture.png")
    

if __name__ == '<module>':

    unmanaged_code = """
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Runtime.InteropServices;

    namespace UnmanagedCode
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
    """

    assembly = Generate(unmanaged_code, 'UnmanagedCode', inMemory=True)
    clr.AddReference(assembly)


    doCapture()
