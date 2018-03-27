using System;
using NCmd;
using Typhoon.Utils;

namespace Typhoon
{

    [CmdShell()]
    internal sealed class RunCommander : Cmd
    {

        [CmdCommandHelp("?")]
        public const string QHelp = @"
        ::: Help :::
            Usage:      ?
            Example:    ?
        ";
        [CmdCommand(Command = "?")]
        public void TaskQ(string args)
        {
            CmdGetHelp(null);
        }

        [CmdCommandHelp("printmap")]
        public const string PrintMapHelp = @"
        ::: Prints Loaded Memory Resources :::
            Usage:      printmap
            Example:    printmap 
        ";
        [CmdCommand(Command = "printmap")]
        public void TaskPrintMap(string args)
        {
            MMemoryLoader.PrintMemoryMap();
        }

        [CmdCommandHelp("dumprecord")]
        public const string DumpRecordHelp = @"
        ::: Dump contents of a loaded memory resource :::
            Usage:      dumprecord <resource name>
            Example:    dumprecord WmiQuery.cs
        ";
        [CmdCommand(Command = "dumprecord")]
        public void TaskDumpRecord(string args)
        {
            String[] taskArgs = args.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (taskArgs.Length != 1)
            {
                Console.WriteLine("Invalid number of arguments: ({0})", taskArgs.Length);
                return;
            }
            else
            {
                MMemoryLoader.DumpMMRecord(taskArgs[0]);
            }
        }


        [CmdCommandHelp("run")]
        public const string RunRecordHelp = @"
        ::: Execute code in a loaded memory resource :::
            Usage:      run <resource name>
            Example:    run  WmiQuery.cs
        ";
        [CmdCommand(Command = "run")]
        public void TaskRunRecord(string args)
        {
            String[] taskArgs = args.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (taskArgs.Length != 1)
            {
                Console.WriteLine("Invalid number of arguments: ({0})", taskArgs.Length);
                return;
            }
            else
            {
                Console.WriteLine("Pre RunMMRecord");
                MMResourceRunner.RunMMRecord(taskArgs[0]);
            }
        }

        [CmdCommandHelp("..")]
        public const string BacklHelp = @"Back up one level to top";
        [CmdCommand(Command = "..")]
        public void TaskBack(string arg)
        {
            // A Hack: ExitLoop() messes up resources on terminal. Just enter a new main loop
            var commander = new Commander();
            commander.CmdLoop();
        }

        public RunCommander()
        {
            LookAndFeel.ResetColorConsole(true);
            // Intro is the text that gets displayed when the (sub-)shell starts. 
            Intro = "Runner for resources\n========================\n\n";

            CommandPrompt = "met/run> ";
            HistoryFileName = "Typhoon";
        }

        public void Do_exit(string arg)
        {
            ExitLoop();
        }
        #region PrePost
        public override void PostCmd(string line)
        {
            base.PostCmd(line);
            LookAndFeel.ResetColorConsole(true);
        }
        public override string PreCmd(string line)
        {
            LookAndFeel.SetColorConsole(ConsoleColor.Gray, ConsoleColor.Black);
            if (ConfigUtil.DEBUG)
            {
                Console.WriteLine("Executing {0}", line);
            }
            return base.PreCmd(line);

        }
        public override void EmptyLine()
        {
            Console.WriteLine("?");
        }
        public override void PreLoop() { }
        public override void PostLoop() { }
        #endregion       
    }
}
