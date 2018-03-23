#
#    This example combines dynamic CSharp compilation (CodeDom), netmodules and PyDLR
#
import clr

from System.Environment import CurrentDirectory
from System.IO import Path, Directory

from System.CodeDom import Compiler
from Microsoft.CSharp import CSharpCodeProvider


def ScreenCapture(x, y, width, height):

    from NetModules import User32, GDI32
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


def ModulesToAssembly(modules, name, code="", references=None, outputDirectory=None, inMemory=False):
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

    modulesListCompiler=';'.join(map(str, modules))        
    CompilerParams.CompilerOptions = "/addmodule:" +  modulesListCompiler
    print(CompilerParams.CompilerOptions)

    for reference in references or []:
        CompilerParams.ReferencedAssemblies.Add(reference)

    provider = CSharpCodeProvider()
    compile = provider.CompileAssemblyFromSource(CompilerParams, code)

    if compile.Errors.HasErrors:
        for err in compile.Errors:
            print(err)
        raise Exception("Compile error(s)")

    if inMemory:
        return compile.CompiledAssembly
    return compile.PathToAssembly



def GenerateNM(code, name, references=None, outputDirectory=None, inMemory=False):
    CompilerParams = Compiler.CompilerParameters()

    if outputDirectory is None:
        outputDirectory = Directory.GetCurrentDirectory()

    CompilerParams.OutputAssembly = Path.Combine(outputDirectory, name + ".netmodule")
    CompilerParams.GenerateInMemory = False

    CompilerParams.TreatWarningsAsErrors = False
    CompilerParams.GenerateExecutable = False
    CompilerParams.CompilerOptions = "/target:module"

    for reference in references or []:
        CompilerParams.ReferencedAssemblies.Add(reference)

    provider = CSharpCodeProvider()
    compile = provider.CompileAssemblyFromSource(CompilerParams, code)

    if compile.Errors.HasErrors:
        for err in compile.Errors:
            print(err)
        raise Exception("Compile error(s)")

    return "\"%s\"" % compile.PathToAssembly



user32_stub_dllimport = """
using System;
using System.Runtime.InteropServices;

namespace NetModules
{
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

gdi32_stub_dllimport = """
using System;
using System.Runtime.InteropServices;

namespace NetModules
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

"""

# Compile CS to netmodules
nuser32_module_path = GenerateNM(user32_stub_dllimport, 'NUser32', inMemory=False)
ngdi32_module_path = GenerateNM(gdi32_stub_dllimport, 'NGDI32', inMemory=False)

print(nuser32_module_path) 
print(ngdi32_module_path)

modulesList=[nuser32_module_path,ngdi32_module_path]

# Bind 
assemblyPath = ModulesToAssembly(modulesList, 'pinvoke', inMemory=False)
print(type(assemblyPath))
#print("Type : %s" % (type(assembly)))
#print("CodeBase: %s" % (assembly.CodeBase))
#print("FullName: %s" % (assembly.FullName))
#print("Dynamic? %s" % (assembly.IsDynamic))
#clr.AddReferenceToFileAndPath
clr.AddReferenceToFileAndPath(assemblyPath)
clr.AddReference('System.Drawing')

#from NetModules import User32, GDI32

image = ScreenCapture(0, 0, 80, 400)
image.Save("capture_module.png")

