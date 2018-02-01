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



unmanaged_code = """
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace UnmanagedCode
{

    public class User32
    {

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, int options);

    }
}
"""

# Bytecode, no disk forl to LoadLibrary from
assembly = Generate(unmanaged_code, 'UnmanagedCode', inMemory=True)
print("Type : %s" % (type(assembly)))
print("CodeBase: %s" % (assembly.CodeBase))
print("FullName: %s" % (assembly.FullName))
print("Dynamic? %s" % (assembly.IsDynamic))
print("Location: %s" % (assembly.Location))
clr.AddReference(assembly)

from UnmanagedCode import User32
from System import IntPtr
User32.MessageBox(IntPtr.Zero, "Hello World", "Platform Invoke Sample", 0);