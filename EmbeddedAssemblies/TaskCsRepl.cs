using System;

namespace Cradle
{
    class TaskCsRepl
    {
        private String type;

        private dynamic pengine;
        private dynamic pscope;
        private dynamic pythonScript;

        public TaskCsRepl(String arg)
        {
            type = arg;
            pengine = IPythonUtil.GetPyEngine();
            pscope = IPythonUtil.GetNewScope();

            this.ProcessCommand();
        }

        private void ProcessCommand()
        {

            switch (type)
            {
                case "dyncs":
                    Console.WriteLine(Commander.TCsHelp);
                    replDCS();
                    break;
                default:
                    throw new ApplicationException($"Invalid mode. {Commander.TCsHelp}");
            }

        }

        private void replDCS()
        {
            Console.WriteLine(Commander.TCsHelp);
            while (true)
            {
                String dirLine = String.Empty;
                String codeLine = String.Empty;
                String directives = String.Empty;
                String code = String.Empty;

                LookAndFeel.SetColorConsole(ConsoleColor.Yellow, ConsoleColor.Black);
                Console.Write("directive> ");
                LookAndFeel.ResetColorConsole();
                dirLine = Console.ReadLine();
                while (!dirLine.StartsWith("END"))
                {
                    directives += dirLine;
                    LookAndFeel.SetColorConsole(ConsoleColor.Yellow, ConsoleColor.Black);
                    Console.Write("directive..> ");
                    LookAndFeel.ResetColorConsole();
                    dirLine = Console.ReadLine();
                }

                Console.WriteLine("--");
                LookAndFeel.SetColorConsole(ConsoleColor.Yellow, ConsoleColor.Black);
                Console.Write("code> ");
                LookAndFeel.ResetColorConsole();
                codeLine = Console.ReadLine();
                while (!codeLine.StartsWith("END"))
                {
                    code += codeLine;
                    LookAndFeel.SetColorConsole(ConsoleColor.Yellow, ConsoleColor.Black);
                    Console.Write("code..> ");
                    LookAndFeel.ResetColorConsole();
                    codeLine = Console.ReadLine();
                }
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
        }

    }

}
