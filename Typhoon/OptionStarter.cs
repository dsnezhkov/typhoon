using System;
using System.IO;
using Typhoon.Utils;

namespace Typhoon
{
    internal static class OptionStarter
    {

        /// <summary>
        /// Invoke Interactive MET Console
        /// </summary>
        internal static void ModeConsole()
        {

            Console.Clear();
            LookAndFeel.SetInitConsole("Typhoon", ConsoleColor.Red, ConsoleColor.Black);
            String intro = @"

            -= Typhoon Managed Execution Toolkit =- 

            ";
            LookAndFeel.SetColorConsole(ConsoleColor.Red, ConsoleColor.Black);
            var commander = new Commander();
            commander.CmdLoop(intro);
        }


        /// <summary>
        /// Python REPL
        /// </summary>
        /// <param name="mode"></param>
        internal static void ModePyRepl(String type)
        {
            new TaskPyRepl(type);
        }
        
        /// <summary>
        /// CS REPL
        /// </summary>
        internal static void ModeCSRepl(String type)
        {
            new TaskCsRepl(type);
        }

        /// <summary>
        /// Compiles CS Extension into Assembly given a resource file
        /// TODO: compiler options
        /// </summary>
        /// <param name="resource"></param>
        internal static string ModeCSCompile(String resource)
        {
            String csxCode = "";
            Console.WriteLine("Compiling Extension fron File resource {0}", resource);
            String assemblyPath = String.Empty; 

            // Local FS load
            if (File.Exists(resource))
            {
                try
                {
                    csxCode = File.ReadAllText(resource);
                    String compoptions = "/optimize";

                    assemblyPath = DynCSharpRunner.CompileSource(csxCode, false, false, compoptions);
                }
                catch (Exception fex)
                {
                    GeneralUtil.Usage(fex.Message);
                }
            }
            else
            {
                GeneralUtil.Usage("Check file location: " + resource);
            }
            return assemblyPath;
        }

        /// <summary>
        /// Invoke CS extension script 
        /// </summary>
        internal static void ModeCSExec(String resource, String method, String klass)
        {
            //CompileRunSource
            Console.WriteLine(resource);

            String csxCode = "";

            switch (method.ToLower())
            {
                case "afile":
                    Console.WriteLine("Execution of Extension from Assembly. Load and execute.");
                    // Local FS load
                    if (File.Exists(resource))
                    {
                        try
                        {
                            Console.WriteLine("Before LoadAndRun"); 
                            DynCSharpRunner.LoadAndRunType(resource, klass);
                        }
                        catch (Exception fex)
                        {
                            GeneralUtil.Usage(fex.Message);
                        }
                    }
                    else
                    {
                        GeneralUtil.Usage("Check file location: " + resource);
                    }

                    break;
                case "sfile":
                    Console.WriteLine("Execution of Extension Source File. Compile and execute on the fly");

                    if (klass == String.Empty) { 
                        GeneralUtil.Usage("Need a Namespace.Class to run ");
                        break;
                    }
                    // Local FS load
                    if (File.Exists(resource))
                    {
                        try
                        {
                            csxCode = File.ReadAllText(resource);
                            DynCSharpRunner.CompileRunXSource(csxCode, klass);
                        }
                        catch (Exception fex)
                        {
                            GeneralUtil.Usage(fex.Message);
                        }
                    }
                    else
                    {
                        GeneralUtil.Usage("Check file location: " + resource);
                    }

                    break;
                default:
                    GeneralUtil.Usage("Unknown -method: " + method);
                    break;
            }
        }

        /// <summary>
        /// Invoike DLR Py script
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="method"></param>
        internal static void ModePyExec(String resource, String method)
        {
            String pyCode = "";

            switch (method.ToLower())
            {
                case "mem":
                    Console.WriteLine("Memory resource");
                    break;
                case "sfile":
                    Console.WriteLine("File resource");
                    // Local FS load
                    if (File.Exists(resource))
                    {
                        try
                        {
                            pyCode = File.ReadAllText(resource);
                        }
                        catch (Exception fex)
                        {
                            GeneralUtil.Usage(fex.Message);
                        }
                    }
                    else
                    {
                        GeneralUtil.Usage("Check file location: " + resource);
                    }
                    break;
                default:
                    GeneralUtil.Usage("Unknown -method: " + method);
                    break;
            }

            // once we get resource code data by a valid method, execute it in DLR
            try
            {
                dynamic pengine = IPythonUtil.GetPyEngine();
                
                dynamic pscope = IPythonUtil.GetNewScope();
                if (ConfigUtil.DEBUG)
                {
                    Console.WriteLine("Python Code: \n\n__BEGIN__\n\n{0}\n\n__END__\n\n", pyCode);
                }

                dynamic pythonScript = IPythonUtil.GetPyEngine().CreateScriptSourceFromString(pyCode);
                pythonScript.Execute(pscope);
            }
            catch (Exception ae)
            {
                GeneralUtil.Usage("Iron Python Scope/Execution not created: " + ae.Message);
            }


        }
    }
}
