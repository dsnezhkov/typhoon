using System;
using Typhoon.Utils;

namespace Typhoon
{
    class TaskCsRepl
    {
        private String type;

        public TaskCsRepl(String arg)
        {
            type = arg;
            this.ProcessCommand();
        }

        private void ProcessCommand()
        {

            Console.CancelKeyPress += ReplLineCancel;

            switch (type)
            {
                case "multi":
                    Console.WriteLine(Commander.TCsHelp);
                    replDCS();
                    break;
                default:
                    throw new ApplicationException($"Invalid mode specified. See help for details: {Commander.TCsHelp}");
            }

        }

        private void replDCS()
        {

            bool replRunning = true;

            while (replRunning)
            {

                String dirLine = String.Empty;
                String codeLine = String.Empty;
                String directives = String.Empty;
                String code = String.Empty;

                LookAndFeel.ResetColorConsole();
                LookAndFeel.SetColorConsole(ConsoleColor.Yellow, ConsoleColor.Black);

                while (replRunning)
                {
                    Console.Write("directive> ");
                    dirLine = Console.ReadLine();

                    // Imeddiately exit REPL
                    if (dirLine.StartsWith("QUIT"))
                    {
                        replRunning = false;
                        break;
                    }

                    if (dirLine.StartsWith("END"))
                    {
                        break;
                    }

                    directives += dirLine;
                }


                while (replRunning)
                {
                    Console.Write("code> ");
                    codeLine = Console.ReadLine();

                    // Imeddiately exit REPL
                    if (codeLine.StartsWith("QUIT"))
                    {
                        replRunning = false;
                        break;
                    }

                    if (codeLine.StartsWith("END"))
                    {
                        break;
                    }

                    code += codeLine;
                }

                if (replRunning)
                {
                    if (ConfigUtil.DEBUG)
                    {
                        Console.WriteLine("Directives: \n{0}, \nCode: \n{1}", directives, code);
                    }
                    LookAndFeel.SetColorConsole(ConsoleColor.Red, ConsoleColor.Black);
                    Console.WriteLine("Perforing dynamic compile and execution.");
                    if (!DynCSharpRunner.CompileRunSnippet(directives, code))
                        Console.WriteLine("Errors in compilation...");
                    LookAndFeel.ResetColorConsole();
                }
                else
                {
                    return;
                }
            }


        }

        protected static void ReplLineCancel(object sender, ConsoleCancelEventArgs args)
        {

            string specialKey = args.SpecialKey.ToString();
            switch (specialKey)
            {
                case "ControlC":
                    Console.WriteLine("To quit REPL type `QUIT`");
                    // Set the Cancel property to true to prevent the process from terminating.
                    args.Cancel = true;
                    break;
                default:
                    break;
            }

        }

    }
}
