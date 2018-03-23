using System;

// Using python cmd inspired library from @adamadair to drive prompts (https://github.com/adamadair/NCmd)
using NCmd;
using System.Diagnostics;

namespace Typhoon
{
    [CmdShell()]
    internal sealed class Commander : Cmd
    {

        [CmdCommandHelp("?")]
        public const string THelpH = @"
        ::: Help :::
            Usage:      ?
            Example:    ?

        ";
        [CmdCommand(Command = "?")]
        public void THelp(string args)
        {
            CmdGetHelp(null);
        }

        [CmdCommandHelp("!")]
        public const string BangHelp = @"
        ::: Local OS shell execution :::
            Usage:      ! <shell command> [shell command args] 
            Example:    ! dir C:\

        ";
        [CmdCommand(Command = "!")]
        public void TBang(string args)
        {
            Console.WriteLine("OS shell exec");
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = " /C " + args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
        }

        [CmdCommandHelp("!!")]
        public const string DBangHelp = @"
        ::: Local PS shell execution :::
            Usage:      !! <powershell command> [shell command args] 
            Example:    !!  Get-ChildItem -Path C:\ | ft

        ";
        [CmdCommand(Command = "!!")]
        public void TDBang(string args)
        {
            Console.WriteLine("PS shell exec");
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = " -NonInteractive -NoLogo  -Command " + args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
        }

        [CmdCommandHelp("printmap")]
        public const string PrintMapHelp = @"
        ::: Prints Loaded Memory Resources :::
            Usage:      printmap
            Example:    printmap 

        ";
        [CmdCommand(Command = "printmap")]
        public void TPrintMap(string args)
        {
            MMemoryLoader.PrintMemoryMap();
        }

        // Enter Python REPL Command space
        [CmdCommandHelp("pyrepl")]
        public const string TPyHelp = @"
        ::: DLR Python REPL :::
            Usage:      pyrepl <mode := single|multi>
                            multi  - ipy multiline
                            single - ipy line-by-line
            Example:    pyrepl single

            Instructions:
                single - enter lines at prompt (>>>), followed by secondary lines (..>) until done
                    when done enter 'END' on a separate line to kick off evaluation of entered code
                multi - enter a valid Python code line by line 

        ";

        [CmdCommand(Command = "pyrepl")]
        public void TPyRepl(string mode)
        {
            try
            {
                new TaskPyRepl(mode);
            }catch(ApplicationException ae)
            {
                Console.WriteLine("ERROR: {0}", ae.Message);
            }
        }

        // Enter CS REPL Command space
        [CmdCommandHelp("csrepl")]
        public const string TCsHelp = @"
        ::: Dynamic CS REPL :::
            Usage:      csrepl <mode := multi|...>
            Example:    csrepl multi

            Instructions:

            1. Enter `using` import directives at prompt (directive>), until done. When done enter `END` on a separate line`.
            2. Enter code at prompt (code>), until done.When done enter `END` on a separate line.

        ";

        [CmdCommand(Command = "csrepl")]
        public void TCsRepl(string type)
        {
            try
            {
                new TaskCsRepl(type);
            }catch(ApplicationException ae)
            {
                Console.WriteLine("ERROR: {0}", ae.Message);
            }
        }



        [CmdCommandHelp("getvar")]
        public const string GetVarHelp = @"
        ::: Prints all settings VARs or a specified VAR ::: 
            Usage:      getvar [VAR]
            Example:    getvar SOMEVAR

        ";
        [CmdCommand(Command = "getvar")]
        public void TGetvar(string arg)
        {
            
            String[] settingArgs = arg.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            String setting;
            if ( settingArgs.Length == 0)
            {
                    ConfigUtil.PrintConfig();
            }
            else
            {
                setting = settingArgs[0];   
                if (ConfigUtil.GetConfigSetting(setting) == String.Empty)
                {
                    Console.WriteLine("var {0} value is undefined", setting); 
                }
            }
        }

        [CmdCommandHelp("setvar")]
        public const string SetVarHelp = @"
        ::: Sets a specified VAR to a VAL ::: 
            Usage:      setvar <VAR> <VAL>
            Example:    setvar SOMEVAR SOMEVAL

        ";
        [CmdCommand(Command = "setvar")]
        public void TSetVar(string arg)
        {
            String[] settingArgs = arg.Split();
            if (settingArgs.Length !=2)
            {
                Console.WriteLine("Need format `VAR VAL`");
                return;
            }

            String configKey = settingArgs[0];
            String configValue = settingArgs[1];
            if ( (configKey == String.Empty) || (configValue == String.Empty))
            {
                Console.WriteLine(@"Please specify the VAR and VAL to set. 
                                    If you want to reset the value use `unsetvar` command ");
            }
            else
            {
                ConfigUtil.SetConfigSetting(configKey, configValue);
            }
        }

        [CmdCommandHelp("unsetvar")]
        public const string UnSetVarHelp = @"
        ::: Unsets a specified VAR ::: 
            Usage:      unsetvar <VAR>
            Example:    unsetvar SOMEVAR

        ";
        [CmdCommand(Command = "unsetvar")]
        public void TUnsetVar(String arg)
        {
            String[] settingArgs = arg.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (settingArgs[0] == String.Empty)
            {
                Console.WriteLine("Please specify the VAR to unset");
            }
            else
            {
                ConfigUtil.SetConfigSetting(settingArgs[0], "");
            }
        }

        [CmdCommandHelp("run*")]
        public const string RunHelp = @"
        ::: Enter resource run menu ::: 
            Usage:      run*
            Example:    run*

        ";

        [CmdCommand(Command = "run*")]
        public void TRun(string arg)
        {
            var runCommander = new RunCommander();
            runCommander.CmdLoop();
        }



        [CmdCommandHelp("load*")]
        public const string LoadHelp = @"
        ::: Enter resource load menu ::: 
            Usage:      load*
            Example:    load*

        ";

        [CmdCommand(Command = "load*")]
        public void TLoad(string arg)
        {
            var loadCommander = new LoadCommander();
            loadCommander.CmdLoop();
        }


        public Commander()
        {

            CommandPrompt = "met> ";
            HistoryFileName = "Typhoon";
            LookAndFeel.SetColorConsole(ConsoleColor.Red, ConsoleColor.Black);
            Console.WriteLine("WARNING: Command History is on disk at : {0}{1}",
                 System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),HistoryFileName), 
                                "._history");
            LookAndFeel.ResetColorConsole();

        }

        public void Do_exit(string arg)
        {
            Console.WriteLine(@"Exiting...");
            ExitLoop();
        }

        public void Do_about(string arg)
        {
            Console.WriteLine(@"

====================================
Features: 
    DLR Python REPL             - Execute .NET DLR Python interpreter / integration
    Dynamic CSharp script REPL  - Execute CSharp code on the fly. With Win32 Interop
    HotPatch CSharp             - Load CSharp file in memory from local or network, dynamic compile and execute, Win32 Interop
    Hotpatch DLR Python         - Load DLR Python file in memory from local or network and execute, Win32 Interop
        TODO:
    - shellcode in CSharp
    - shellcode in DLR Python
    - CPython Integration
====================================

");
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
