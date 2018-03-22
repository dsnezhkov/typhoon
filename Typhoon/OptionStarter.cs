using System;
using System.IO;

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
        /// Invoke CS extension script 
        /// </summary>
        internal static void ModeCSExec(String resource, String method, String klass)
        {
            //CompileRunSource
            Console.WriteLine(resource);

            String csxCode = "";

            switch (method.ToLower())
            {
                case "mem":
                    Console.WriteLine("TBD: Execution from Memory resource");
                    break;
                case "sfile":
                    Console.WriteLine("TBD: Execution from Source File resource");
                    break;
                case "lfile":
                    Console.WriteLine("TBD: Execution from Assembly File resource");
                    break;
                case "xfile":
                    Console.WriteLine("Execution from Extension File resource");

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
                            DynCSharpRunner.CompileRunSource(csxCode, klass, false);
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
