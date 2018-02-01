/*
 * NCmd
 *
 * Copyright (c) Adam Adair 2016
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
 * to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

namespace NCmd
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security;
    using System.Text;
    using C = NCmd.SimpleConsole;


    /// <summary>
    /// Provides a simple framework for writing line-oriented command interpreters. Inspired 
    /// by the Python cmd library, this class is intended to make writing console based
    /// applications easier to write.
    /// </summary>
    public abstract class Cmd
    {
        private const string DocumentedCommandsText = "Documented commands (type help <topic>):";
        private const string UndocumentedCommandsText = "Undocumented Commands:";

        private bool _isInLoop;
        private bool _isInitialized;

        private Dictionary<string, ICommand> _commands;

        /// <summary>
        /// If true will turn on autocomplete when the command loop is started. True by default,
        /// so you must explicitly turn it off before starting to loop.
        /// </summary>
        public bool DoAutoComplete { get; set; }

        protected Cmd()
        {
            Intro = "";

            _isInLoop = false;
            DoAutoComplete = true;
        }

        /// <summary>
        /// Command prompt is the character sequence that is displayed to the user to prompt
        /// them for input. Common characters to use for this purpose are '>' and '$'. Newline
        /// characters should be avoid as this will cause problems for some of the other
        /// features of the input editor.
        /// </summary>
        public virtual string CommandPrompt { get; set; } = "> ";

        /// <summary>
        /// The string that gets printed first once CmdLoop is called. This string only gets
        /// printed once.
        /// </summary>
        public string Intro { get; set; }

        /// <summary>
        /// HistoryFileName is a file name for tracking command history. If this is left
        /// blank or set to null command history will not be saved to file for future
        /// session use.
        /// </summary>
        public string HistoryFileName { get; set; }

        /// <summary>
        /// Hook method executed just before the command line line is interpreted, 
        /// but after the input prompt is generated and issued. This method is a
        /// stub in Cmd; it exists to be overridden by subclasses. The return value 
        /// is used as the command which will be executed by the onecmd() method; 
        /// the precmd() implementation may re-write the command or simply return 
        /// line unchanged.
        /// </summary>
        /// <param name="line">The line as entered by the user</param>
        /// <returns>The returned line value</returns>
        public virtual string PreCmd(string line)
        {
            return line;
        }

        /// <summary>
        /// Hook method executed just after a command dispatch is finished. This method 
        /// is a stub in Cmd; it exists to be overridden by subclasses. line is the 
        /// command line which was executed.
        /// </summary>
        /// <param name="line"></param>
        public virtual void PostCmd(string line)
        {
        }

        /// <summary>
        /// Hook method executed once when CmdLoop() is called.
        /// </summary>
        public virtual void PreLoop()
        {
        }

        /// <summary>
        /// Hook method executed once when CmdLoop() is about to return.
        /// </summary>
        public virtual void PostLoop()
        {
        }

        /// <summary>
        /// Method called when an empty line is entered in response to the prompt.
        /// </summary>
        public virtual void EmptyLine()
        {
        }

        public virtual void HandleException(Exception ex)
        {
            DisplayExceptionDetails(ex);
        }

        public virtual void Default(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                C.Wl("Illegal command entered.");
            }
            else
            {
                var cmdName = line.Split(" ".ToCharArray());
                C.Wl($"Command '{cmdName[0]}' is not defined.");
            }
        }

        /// <summary>
        /// Call this method to signal that the command shell loop in CmdLoop should be exited.
        /// </summary>
        public void ExitLoop()
        {
            IsExiting = true;
        }

        /// <summary>
        /// True if the CmdLoop loop is or has exited.
        /// </summary>
        public bool IsExiting { get; private set; }

        /// <summary>
        /// Starts the shell loop. 
        /// </summary>
        /// <param name="intro">The intro string that gets printed once at the beginning.</param>
        public void CmdLoop(string intro)
        {
            Intro = intro;
            CmdLoop();
        }

        /// <summary>
        /// Starts the shell loop. Prior to entering the loop any Intro text
        /// will be printed to the console, and the PreLoop method will be
        /// executed.
        /// 
        /// </summary>
        public void CmdLoop()
        {
            if (_isInLoop) return; // just in case of multithreaded shennanigans 
            _isInLoop = true;

            if (!_isInitialized)
                InitCommandDictionary();

            if (!string.IsNullOrWhiteSpace(Intro) && Intro.Length > 0)
                C.Wl(Intro);

            PreLoop();
            var editor = new LineEditor(HistoryFileName);
            if (DoAutoComplete)
            {
                var list = new List<string>(_commands.Keys);
                editor.SetAutoCompleteCommandList(list);
            }
            IsExiting = false; // We should reset the exit condition priort to entering.
            while (!IsExiting)
            {
                try
                {
                    var userInput = editor.Edit(CommandPrompt, "");
                    userInput = PreCmd(userInput);

                    if (userInput == null)
                    {
                        // ctrl-D will return null, this should exit the loop
                        // without running the PostCmd method. 
                        ExitLoop();
                        continue;
                    }

                    HandleCommandString(userInput);
                    PostCmd(userInput);

                    // At this point it is safe to save the command history. 
                    // Any commands that cause an exception should not be saved to 
                    // history.
                    editor.SaveHistory();
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
            }

            PostLoop();
        }

        /// <summary>
        /// Add a comand. This normally does not need to be called if all the 
        /// commands are handled by methods internal to the object that implements 
        /// Cmd, but it is useful if the command implementation is in another 
        /// object.  Look at using a RelayCommand object for this purpose.
        /// </summary>
        /// <param name="command"></param>
        public void AddCommand(ICommand command)
        {
            if (_commands == null)
                _commands = new Dictionary<string, ICommand>();
            _commands.Add(command.CommandName.ToLower(), command);
        }

        private ICommand FindCommand(string commandName)
        {
            var c = commandName.ToLower();
            if (_commands == null || !_commands.ContainsKey(c)) return null;
            return _commands[c];
        }


        private void HandleCommandString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                EmptyLine();
                return;
            }
            if (_commands == null) return;
            var cmd = input.Trim().Split(" ".ToCharArray())[0];
            var args = input.Substring(cmd.Length).Trim();
            var command = FindCommand(cmd);
            if (command == null)
            {
                Default(input);
            }
            else
            {
                command.Execute(args);
            }
        }

        /// <summary>
        /// Initiates execution of a command.
        /// </summary>
        /// <param name="command">The command string to execute.</param>
        public virtual void OneCmd(string command)
        {
            try
            {
                if (!_isInitialized)
                    InitCommandDictionary();

                var userInput = PreCmd(command);
                HandleCommandString(userInput);
                PostCmd(userInput);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void DisplayExceptionDetails(Exception ex)
        {
            C.Wl("\n**********\nERROR!: " + ex.Message + "\n**********\n");
            if (C.PromptUser("stacktrace?:").ToUpper().StartsWith("Y", StringComparison.Ordinal))
            {
                C.Wl(ex.StackTrace);
            }
        }


        /// <summary>
        /// InitCommandDictionary uses reflection to find shell command methods
        /// and sets up the command dictionary for use by the shell loop.
        /// 
        /// This method also initializes the help system.
        /// </summary>
        private void InitCommandDictionary()
        {
            _isInitialized = true;
            var thisType = GetType();
            var methodList = thisType.GetMethods();
            // check each method to see if it is a command. 
            foreach (var method in methodList)
            {
                var isCommand = false;
                foreach (var attr in method.GetCustomAttributes())
                {
                    // First check for attributes that explicitly define 
                    // command methods.

                    if (!(attr is CmdCommandAttribute)) continue;
                    //This method is a command. 
                    isCommand = true;
                    var a = (CmdCommandAttribute)attr;
                    var cmd = a.Command;
                    if (string.IsNullOrWhiteSpace(cmd))
                    {
                        cmd = method.Name;
                    }
                    var command = new AutoCommand(this, method, cmd, a.Description);
                    AddCommand(command);
                }
                if (isCommand) continue;
                if (method.Name.StartsWith("Do_"))
                {
                    // Secondly, check if the method is a command through
                    // naming convention. Anthing that starts with Do_ should
                    // be considered a command.

                    var cmd = method.Name.Substring(3);
                    var command = new AutoCommand(this, method, cmd);
                    AddCommand(command);
                }
            }
            InitHelp(thisType);
        }

        #region Convenience Console Methods

        /// <summary>
        /// Gets input from user.
        /// </summary>
        /// <param name="prompt">text prompt displayed to the user</param>
        /// <returns>the string of text the user entered</returns>
        public string GetInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }

        /// <summary>
        /// Gets input from the user. Will continue to prompt user until
        /// input is entered
        /// </summary>
        /// <param name="prompt">text prompt displayed to the user</param>
        /// <returns>The string of text the user entered</returns>
        public string GetRequiredInput(string prompt)
        {
            var resp = "";
            while (string.IsNullOrWhiteSpace(resp))
            {
                Console.Write(prompt);
                resp = Console.ReadLine();
            }
            return resp;
        }

        /// <summary>
        /// Get a password from the user. The text will not be displayed to the 
        /// screen.
        /// </summary>
        /// <param name="prompt">Text prompt displayed to the user.</param>
        /// <returns>The password in a SecureString instance.</returns>
        public SecureString GetPassword(string prompt)
        {
            Console.Write(prompt);
            return GetPassword();
        }

        /// <summary>
        /// Get a password from the user. The text will not be displayed to the screen.
        /// </summary>
        /// <returns>The password in a SecureString instance.</returns>
        public SecureString GetPassword()
        {
            var pwd = new SecureString();
            while (true)
            {
                var i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length <= 0) continue;
                    pwd.RemoveAt(pwd.Length - 1);
                    Console.Write("\b \b");
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                    Console.Write("*");
                }
            }
            pwd.MakeReadOnly();
            return pwd;
        }

        public void WriteLine(string ln)
        {
            Console.WriteLine(ln);
        }

        public void WriteLine(string ln, params object[] p)
        {
            Console.WriteLine(ln, p);
        }

        public void WriteLine() { Console.WriteLine(); }

        public void Write(string ln)
        {
            Console.Write(ln);
        }

        #endregion

        #region Help System Methods

        private void InitHelp(Type thisType)
        {
            var props = thisType.GetProperties();
            var fields = thisType.GetFields();

            foreach (var p in fields)
            {
                var isFound = false;
                foreach (var attr in p.GetCustomAttributes())
                {
                    if (!(attr is CmdCommandHelpAttribute)) continue;
                    var a = (CmdCommandHelpAttribute)attr;
                    if (!_commands.ContainsKey(a.Command)) continue;
                    _commands[a.Command].HelpText = p.GetValue(this).ToString();
                    isFound = true;
                    break;
                }
                if (isFound) continue;
                if (!p.Name.StartsWith("Help_")) continue;
                var command = p.Name.Replace("Help_", "");
                if (_commands.ContainsKey(command))
                {
                    _commands[command].HelpText = p.GetValue(this).ToString();
                }
            }

            foreach (var p in props)
            {
                foreach (var attr in p.GetCustomAttributes())
                {
                    if (!(attr is CmdCommandHelpAttribute)) continue;
                    var a = (CmdCommandHelpAttribute)attr;
                    if (_commands.ContainsKey(a.Command))
                    {
                        _commands[a.Command].HelpText = p.GetValue(this, null).ToString();
                    }
                }
            }
        }

        [CmdCommand(Command = "help",
            Description = "List available commands with \"help\" or detailed help with \"help cmd\".")]
        public virtual void CmdGetHelp(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                ShowHelp();
                return;
            }
            if (_commands.ContainsKey(arg))
            {
                Console.WriteLine(_commands[arg].HelpText);
                return;
            }
            Console.WriteLine($"*** No help on {arg}");
        }

        private void ShowHelp()
        {
            var documented = new List<string>();
            var undocumented = new List<string>();
            foreach (var c in _commands)
            {
                if (string.IsNullOrWhiteSpace(c.Value.HelpText))
                {
                    undocumented.Add(c.Key);
                }
                else
                {
                    documented.Add(c.Key);
                }
            }
            var sb = new StringBuilder();

            if (documented.Count > 0)
            {
                sb.Append("\n" + DocumentedCommandsText + "\n");
                for (var i = 0; i < DocumentedCommandsText.Length; ++i)
                    sb.Append("=");
                sb.Append("\n");
                var cCount = 0;
                documented.Sort();
                foreach (var d in documented)
                {
                    if (cCount + d.Length > 80)
                    {
                        sb.Append("\n");
                        cCount = 0;
                    }
                    sb.Append(d);
                    sb.Append("  ");
                    cCount += d.Length + 2;
                }
                sb.Append("\n");
            }

            if (undocumented.Count > 0)
            {
                var cCount = 0;
                sb.Append("\n" + UndocumentedCommandsText + "\n");
                for (var i = 0; i < UndocumentedCommandsText.Length; ++i)
                    sb.Append("=");
                sb.Append("\n");
                foreach (var d in undocumented)
                {
                    if (cCount + d.Length > 80)
                    {
                        sb.Append("\n");
                        cCount = 0;
                    }
                    sb.Append(d);
                    sb.Append("  ");
                    cCount += d.Length + 2;
                }
                sb.Append("\n");
            }
            sb.Append("\n");
            Console.WriteLine(sb.ToString());
        }

        #endregion

        #region Usage and Version Statement Convenience methods


        /// <summary>
        /// This is a convenience methods for writing the usage statement of a command
        /// line program. It will write the Usage statment with list of optional parameters
        /// followed by a list of descriptions for the parameters. 
        /// </summary>
        /// <param name="programData">The program meta data</param>
        /// <param name="parser"></param>
        /// <param name="writer"></param>
        public static void WriteUsageStatement(IProgramMetaData programData, ArgumentParser parser, TextWriter writer)
        {
            writer.WriteLine($"Program: {programData.Title} v{programData.Version}");
            if (!string.IsNullOrEmpty(programData.Description))
            {
                writer.WriteLine($"\n{programData.Description}");
            }

            if (parser != null)
            {
                writer.WriteLine("\nOptional Arguments:");
                parser.WriteArgumentDescriptions(writer);
            }
        }

        public static void WriteVersionStatement(IProgramMetaData programData, TextWriter writer)
        {
            writer.WriteLine($"{programData.Title} v{programData.Version}");
            if (programData.BuildDateTime != null)
            {
                writer.WriteLine("Build Time: " + programData.BuildDateTime.Value.ToString(CultureInfo.CurrentCulture));
            }
            if (!string.IsNullOrEmpty(programData.LicenseStatement))
            {
                writer.WriteLine($"\n{programData.LicenseStatement}");
            }
        }

        #endregion
    }
}