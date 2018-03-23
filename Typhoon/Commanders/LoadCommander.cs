using System;
using NCmd;
using System.Collections.Generic;

namespace Typhoon
{

    [CmdShell()]
    internal sealed class LoadCommander : Cmd
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

        [CmdCommandHelp("unload")]
        public const string UnloadHelp = @"
        ::: Unload a loaded memory resource :::
            Usage:      unload <resource name>
            Example:    unload WmiQuery.cs
        ";
        [CmdCommand(Command = "unload")]
        public void TaskUnload(string args)
        {
            String[] taskArgs = args.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (taskArgs.Length != 1)
            {
                Console.WriteLine("Invalid number of arguments: ({0})", taskArgs.Length);
                return;
            }else
            {
                MMemoryLoader.RemoveMMRecord(taskArgs[0]);
            }
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

        [CmdCommandHelp("local")]
        public const string LoadHelp = @"
        ::: Load local resource to memory store :::
            Usage:      local <type> <location> <access>
                            type    : py|cs|dll|zip
                            location: local \path\on\disk (no spaces) 
                            access  : file - backed by file (TBD) | anon - anonymous memory
            Example:    local py .\Script.py anon

        ";

        [CmdCommand(Command = "local")]
        public void TaskLocal(string args)
        {

            List<String> lTypes     = new List<String>() { "py", "cs", "dll", "zip" };
            List<String> lAccess    = new List<String>() { "file", "anon" };

            String[] taskArgs = args.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (taskArgs.Length != 3)
            {
                Console.WriteLine("Invalid number of arguments: ({0})", taskArgs.Length);
                return;

            }

            String type     = taskArgs[0];
            String location = taskArgs[1]; // Does not handle \s or \t in path name, as args is a string.
            String access   = taskArgs[2];

            if ( lTypes.Exists(t => t == type))
            {
                if ( lAccess.Exists(a => a == access))
                {
                    if (System.IO.File.Exists(location))
                    {
                       Console.WriteLine("Passing type:{0} location:{1} access:{2}", type,location,access);
                       MMemoryLoader.LoadMMRecordFromFile(location, type);

                    }else
                    {
                       Console.WriteLine("Invalid argument <{0}>", location);

                    }
                }else
                {
                       Console.WriteLine("Invalid argument <{0}>", access);
                }

            }else
            {
                   Console.WriteLine("Invalid argument <{0}>", type);
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

        public LoadCommander()
        {
            LookAndFeel.ResetColorConsole();
            // Intro is the text that gets displayed when the (sub-)shell starts. 
            Intro = "Loader for resources\n========================\n\n";

            CommandPrompt = "met/load> ";
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
            LookAndFeel.ResetColorConsole();
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
