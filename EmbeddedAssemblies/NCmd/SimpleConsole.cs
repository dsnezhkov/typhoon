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
    using System.Text;
    using C = System.Console;

    /// <summary>
    /// SimpleConsole provides utility functions for dealing with System.Console
    /// more efficiently. All methods are static.
    /// </summary>
    internal static class SimpleConsole
    {
        public static string GetString(bool cursorVisible)
        {
            var cv = C.CursorVisible;
            C.CursorVisible = cursorVisible;
            var returnValue = C.ReadLine();
            C.CursorVisible = cv;
            return returnValue;
        }

        public static char PromptCharChoice(string prompt, char[] choices)
        {
            var l = new List<char>(choices);
            if (choices.Length <= 0) return char.MinValue;
            C.Write(prompt);
            while (true)
            {
                var key = C.ReadKey(true);
                if (l.Contains(key.KeyChar))
                {
                    return key.KeyChar;
                }
            }
        }

        public static char PromptChar(string prompt)
        {
            C.Write(prompt);
            return C.ReadKey(true).KeyChar;
        }

        public static string PromptUser(string prompt, bool cursorVisible)
        {
            C.Write(prompt);
            return GetString(cursorVisible);
        }

        public static string PromptUser(string prompt)
        {
            return PromptUser(prompt, true);
        }

        /*
         * My Write and Write Line functions
         */
        public static void Wl(string s) { C.WriteLine(s); }
        public static void Wl() { C.WriteLine(); }
        public static void W(string s) { C.Write(s); }

        public static string Rl() { return C.ReadLine(); }

        public static void VersionStatement(ProgramMetaData p)
        {
            var sb = new StringBuilder();
            sb.Append(p.Title);
            sb.Append(" ");
            sb.Append(p.Version);
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append(p.LicenseStatement);
            sb.Append(Environment.NewLine);
            Wl(sb.ToString());
        }

        private static string GetAuthorStatement(string[] auths)
        {
            var s = "Written by ";
            var len = auths.Length;
            switch (len)
            {
                case 1:
                    s += auths[0];
                    break;
                case 2:
                    s += auths[0] + " and " + auths[1];
                    break;
                default:
                    s += auths[0];
                    for (var i = 0; i < auths.Length - 1; ++i)
                    {
                        s += "," + auths[0];
                    }
                    s += ", and " + auths[auths.Length - 1];
                    break;
            }
            s += ".";
            return s;
        }
    }
}
