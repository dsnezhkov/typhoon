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
    using System.Reflection;

    /// <summary>
    /// A command is operation that is executed at the request of the user. 
    /// The request is normally the user typing the name of the command at the 
    /// shell prompt.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// The name of the command. This is what the user types at the prompt
        /// to initiate command execution.
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Some useful text that helps the user execute the command properly.
        /// </summary>
        string HelpText { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        void Execute(string arg);
        
    }

    public class AutoCommand : ICommand
    {

        private readonly MethodInfo _method;
        private readonly object _parent;

        public string CommandName { get; set; }
        public string HelpText { get; set; }

        public AutoCommand(object obj, MethodInfo info, string command, string help="")
        {            
            _parent = obj;
            _method = info;
            HelpText = help;
            CommandName = command;
        }

        public void Execute(string arg)
        {
            _method.Invoke(_parent, new object[]{arg});
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<string> _execute;

        public string CommandName { get; set; }
        public string HelpText { get; set; }

        public RelayCommand(Action<string> execute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }
            this._execute = execute;
        }
        public void Execute(string arg)
        {
            _execute(arg);
        }
    }
}
