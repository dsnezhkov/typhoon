using System;
using System.IO;

namespace Cradle
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

        internal static void ModeCSExec(String resource, String method)
        {
            Console.WriteLine(resource);
        }
        internal static void ModePyExec(String resource, String method)
        {
            String pyCode = "";

            switch (method.ToLower())
            {
                case "mem":
                    Console.WriteLine("Memory resource");
                    break;
                case "file":
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
