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
    [AttributeUsage(AttributeTargets.Class)]
    public class CmdShellAttribute : Attribute
    {

    }

    /// <summary>
    /// CmdCommand attribute can be used to identify methods as 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CmdCommandAttribute : Attribute
    {
        /// <summary>
        /// The command text that is required to be typed by the user to execute
        /// the method the attribute marks. The command parser only looks for the
        /// first word in the input. Setting the command parameter to a multi-word
        /// string would ensure that it never gets executed.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// A text description of the command. This is text will be diplayed 
        /// to the user when using the help command.
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field )]
    public class CmdCommandHelpAttribute : Attribute
    {
        public CmdCommandHelpAttribute(string command)
        {
            this.Command = command;
        }

        public string Command { get; }
    }
}