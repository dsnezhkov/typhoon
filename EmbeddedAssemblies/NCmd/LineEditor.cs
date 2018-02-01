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
    using System.IO;
    using System.Text;
    using System.Threading;
    using C = System.Console;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// LineEditor provides the user interface to the Cmd command shell. It is not
    /// intended to be used alone, so it has not been made public. 
    /// </summary>
    internal class LineEditor
    {
                
        // The text being edited.
        private StringBuilder _text;

        // The text as it is rendered (replaces (char)1 with ^A on display for example).
        private readonly StringBuilder _renderedText;

        // The prompt specified, and the prompt shown to the user.
        private string _shownPrompt;

        // The current cursor position, indexes into "text", for an index
        // into _renderedText, use TextToRenderPos
        private int _cursor;

        // The row where we started displaying data.
        private int _homeRow;

        // The maximum length that has been displayed on the screen
        private int _maxRendered;

        // If we are done editing, this breaks the interactive loop
        private bool _done;

        // The thread where the Editing started taking place
        private Thread _editThread;

        // Our object that tracks history
        private readonly History _history;

        // The contents of the kill buffer (cut/paste in Emacs parlance)
        private string _killBuffer = "";

        // The string being searched for
        private string _search;
        private string _lastSearch;

        // whether we are searching (-1= reverse; 0 = no; 1 = forward)
        private int _searching;

        // The position where we found the match.
        private int _matchAt;

        // Used to implement the Kill semantics (multiple Alt-Ds accumulate)
        private KeyHandler _lastHandler;

        private delegate void KeyHandler();

        private struct Handler
        {
            public readonly ConsoleKeyInfo KeyInfo;
            public readonly KeyHandler KeyHandler;

            public Handler(ConsoleKey key, KeyHandler h)
            {
                KeyInfo = new ConsoleKeyInfo((char)0, key, false, false, false);
                KeyHandler = h;
            }

            private Handler(char c, KeyHandler h)
            {
                KeyHandler = h;
                // Use the "Zoom" as a flag that we only have a character.
                KeyInfo = new ConsoleKeyInfo(c, ConsoleKey.Zoom, false, false, false);
            }

            private Handler(ConsoleKeyInfo keyInfo, KeyHandler h)
            {
                KeyInfo = keyInfo;
                KeyHandler = h;
            }

            public static Handler Control(char c, KeyHandler h)
            {
                return new Handler((char)(c - 'A' + 1), h);
            }

            public static Handler Alt(char c, ConsoleKey k, KeyHandler h)
            {
                var cki = new ConsoleKeyInfo(c, k, false, true, false);
                return new Handler(cki, h);
            }
        }

        /// <summary>
        ///   Invoked when the user requests auto-completion using the tab character
        /// </summary>
        /// <remarks>
        ///    The result is null for no values found, an array with a single
        ///    string, in that case the string should be the text to be inserted
        ///    for example if the word at pos is "T", the result for a completion
        ///    of "ToString" should be "oString", not "ToString".
        ///
        ///    When there are multiple results, the result should be the full
        ///    text
        /// </remarks>
        public AutoCompleteHandler AutoCompleteEvent;

        private List<string> _commands;

        public void SetAutoCompleteCommandList(List<string> commands)
        {
            _commands = commands;
            AutoCompleteEvent = AutoCompleteMe;
        }

        private Completion AutoCompleteMe(string text, int pos)
        {
            var prefix = text;
            var completion = new Completion(prefix, _commands.Where(s => s.StartsWith(prefix)).Select(c => c.Replace(prefix, "")).ToArray());
            return completion;
        }

        private static Handler[] _handlers;

        public LineEditor(string name) : this(name, 10) { }

        public LineEditor(string name, int histsize)
        {
            _handlers = new[] {
                new Handler (ConsoleKey.Home,       CmdHome),
                new Handler (ConsoleKey.End,        CmdEnd),
                new Handler (ConsoleKey.LeftArrow,  CmdLeft),
                new Handler (ConsoleKey.RightArrow, CmdRight),
                new Handler (ConsoleKey.UpArrow,    CmdHistoryPrev),
                new Handler (ConsoleKey.DownArrow,  CmdHistoryNext),
                new Handler (ConsoleKey.Enter,      CmdDone),
                new Handler (ConsoleKey.Backspace,  CmdBackspace),
                new Handler (ConsoleKey.Delete,     CmdDeleteChar),
                new Handler (ConsoleKey.Tab,        CmdTabOrComplete),
				
				// Emacs keys
				Handler.Control ('A', CmdHome),
                Handler.Control ('E', CmdEnd),
                Handler.Control ('B', CmdLeft),
                Handler.Control ('F', CmdRight),
                Handler.Control ('P', CmdHistoryPrev),
                Handler.Control ('N', CmdHistoryNext),
                Handler.Control ('K', CmdKillToEof),
                Handler.Control ('Y', CmdYank),
                Handler.Control ('D', CmdDeleteChar),
                Handler.Control ('L', CmdRefresh),
                Handler.Control ('R', CmdReverseSearch),
                Handler.Control ('G', delegate {} ),
                Handler.Alt ('B', ConsoleKey.B, CmdBackwardWord),
                Handler.Alt ('F', ConsoleKey.F, CmdForwardWord),

                Handler.Alt ('D', ConsoleKey.D, CmdDeleteWord),
                Handler.Alt ((char) 8, ConsoleKey.Backspace, CmdDeleteBackword),
				
				Handler.Control ('Q', delegate { HandleChar (C.ReadKey (true).KeyChar); })
            };

            _renderedText = new StringBuilder();
            _text = new StringBuilder();

            _history = new History(name, histsize);
        }

        private int WindowWidth {
            get {
                // Since Mono doesn't support the Console.WindowWidth attribute
                // default to 80, it seems to work ok for most purposes.
                if (Console.WindowWidth == 0)
                    return 80;
                return Console.WindowWidth;
            }
        }

        private void Render()
        {
            SimpleConsole.W(_shownPrompt);
            SimpleConsole.W(_renderedText.ToString());

            var max = Math.Max(_renderedText.Length + _shownPrompt.Length, _maxRendered);

            for (var i = _renderedText.Length + _shownPrompt.Length; i < _maxRendered; i++)
                C.Write(' ');
            _maxRendered = _shownPrompt.Length + _renderedText.Length;

            // Write one more to ensure that we always wrap around properly if we are at the
            // end of a line.
            C.Write(' ');

            UpdateHomeRow(max);
        }

        private void UpdateHomeRow(int screenpos)
        {
            var lines = 1 + (screenpos / WindowWidth);

            _homeRow = C.CursorTop - (lines - 1);
            if (_homeRow < 0)
                _homeRow = 0;
        }


        private void RenderFrom(int pos)
        {
            var rpos = TextToRenderPos(pos);
            int i;

            for (i = rpos; i < _renderedText.Length; i++)
                C.Write(_renderedText[i]);

            if ((_shownPrompt.Length + _renderedText.Length) > _maxRendered)
                _maxRendered = _shownPrompt.Length + _renderedText.Length;
            else
            {
                var maxExtra = _maxRendered - _shownPrompt.Length;
                for (; i < maxExtra; i++)
                    C.Write(' ');
            }
        }

        private void ComputeRendered()
        {
            _renderedText.Length = 0;

            for (var i = 0; i < _text.Length; i++)
            {
                var c = (int)_text[i];
                if (c < 26)
                {
                    if (c == '\t')
                        _renderedText.Append("    ");
                    else
                    {
                        _renderedText.Append('^');
                        _renderedText.Append((char)(c + 'A' - 1));
                    }
                }
                else
                    _renderedText.Append((char)c);
            }
        }

        private int TextToRenderPos(int pos)
        {
            var p = 0;

            for (var i = 0; i < pos; i++)
            {
                var c = (int)_text[i];

                if (c < 26)
                {
                    if (c == 9)
                        p += 4;
                    else
                        p += 2;
                }
                else
                    p++;
            }

            return p;
        }

        private int TextToScreenPos(int pos)
        {
            return _shownPrompt.Length + TextToRenderPos(pos);
        }

        private string Prompt { get; set; }

        private int LineCount => (_shownPrompt.Length + _renderedText.Length) / WindowWidth;

        private void ForceCursor(int newpos)
        {
            _cursor = newpos;

            var actualPos = _shownPrompt.Length + TextToRenderPos(_cursor);
            var row = _homeRow + (actualPos / WindowWidth);
            var col = actualPos % WindowWidth;

            if (row >= C.BufferHeight)
                row = C.BufferHeight - 1;
            C.SetCursorPosition(col, row);
        }

        private void UpdateCursor(int newpos)
        {
            if (_cursor == newpos)
                return;

            ForceCursor(newpos);
        }

        private void InsertChar(char c)
        {
            var prevLines = LineCount;
            _text = _text.Insert(_cursor, c);
            ComputeRendered();
            if (prevLines != LineCount)
            {
                C.SetCursorPosition(0, _homeRow);
                Render();
                ForceCursor(++_cursor);
            }
            else
            {
                RenderFrom(_cursor);
                ForceCursor(++_cursor);
                UpdateHomeRow(TextToScreenPos(_cursor));
            }
        }

        //
        // Commands
        //
        private void CmdDone()
        {
            _done = true;
        }

        private void CmdTabOrComplete()
        {
            var complete = false;

            if (AutoCompleteEvent != null)
            {
                if (TabAtStartCompletes)
                    complete = true;
                else
                {
                    for (var i = 0; i < _cursor; i++)
                    {
                        if (char.IsWhiteSpace(_text[i])) continue;
                        complete = true;
                        break;
                    }
                }

                if (complete)
                {
                    var completion = AutoCompleteEvent(_text.ToString(), _cursor);
                    var completions = completion.Result;
                    if (completions == null)
                        return;

                    var ncompletions = completions.Length;
                    if (ncompletions == 0)
                        return;

                    if (completions.Length == 1)
                    {
                        InsertTextAtCursor(completions[0]);
                    }
                    else
                    {
                        var last = -1;

                        for (var p = 0; p < completions[0].Length; p++)
                        {
                            var c = completions[0][p];


                            for (var i = 1; i < ncompletions; i++)
                            {
                                if ((completions[i].Length - 1) < p)
                                    goto mismatch;

                                if (completions[i][p] != c)
                                {
                                    goto mismatch;
                                }
                            }
                            last = p;
                        }
                    mismatch:
                        if (last != -1)
                        {
                            InsertTextAtCursor(completions[0].Substring(0, last + 1));
                        }
                        SimpleConsole.Wl();
                        foreach (string s in completions)
                        {
                            SimpleConsole.W(completion.Prefix);
                            SimpleConsole.W(s);
                            C.Write(' ');
                        }
                        SimpleConsole.Wl();
                        Render();
                        ForceCursor(_cursor);
                    }
                }
                else
                    HandleChar('\t');
            }
            else
                HandleChar('\t');
        }

        private void CmdHome()
        {
            UpdateCursor(0);
        }

        private void CmdEnd()
        {
            UpdateCursor(_text.Length);
        }

        private void CmdLeft()
        {
            if (_cursor == 0)
                return;

            UpdateCursor(_cursor - 1);
        }

        private void CmdBackwardWord()
        {
            var p = WordBackward(_cursor);
            if (p == -1)
                return;
            UpdateCursor(p);
        }

        private void CmdForwardWord()
        {
            var p = WordForward(_cursor);
            if (p == -1)
                return;
            UpdateCursor(p);
        }

        private void CmdRight()
        {
            if (_cursor == _text.Length)
                return;

            UpdateCursor(_cursor + 1);
        }

        private void RenderAfter(int p)
        {
            ForceCursor(p);
            RenderFrom(p);
            ForceCursor(_cursor);
        }

        private void CmdBackspace()
        {
            if (_cursor == 0)
                return;

            _text.Remove(--_cursor, 1);
            ComputeRendered();
            RenderAfter(_cursor);
        }

        private void CmdDeleteChar()
        {
            // If there is no input, this behaves like EOF
            if (_text.Length == 0)
            {
                _done = true;
                _text = null;
                SimpleConsole.Wl();
                return;
            }

            if (_cursor == _text.Length)
                return;
            _text.Remove(_cursor, 1);
            ComputeRendered();
            RenderAfter(_cursor);
        }

        private int WordForward(int p)
        {
            if (p >= _text.Length)
                return -1;

            int i = p;
            if (char.IsPunctuation(_text[p]) || char.IsSymbol(_text[p]) || char.IsWhiteSpace(_text[p]))
            {
                for (; i < _text.Length; i++)
                {
                    if (char.IsLetterOrDigit(_text[i]))
                        break;
                }
                for (; i < _text.Length; i++)
                {
                    if (!char.IsLetterOrDigit(_text[i]))
                        break;
                }
            }
            else
            {
                for (; i < _text.Length; i++)
                {
                    if (!char.IsLetterOrDigit(_text[i]))
                        break;
                }
            }
            if (i != p)
                return i;
            return -1;
        }

        int WordBackward(int p)
        {
            if (p == 0)
                return -1;

            int i = p - 1;
            if (i == 0)
                return 0;

            if (char.IsPunctuation(_text[i]) || char.IsSymbol(_text[i]) || char.IsWhiteSpace(_text[i]))
            {
                for (; i >= 0; i--)
                {
                    if (char.IsLetterOrDigit(_text[i]))
                        break;
                }
                for (; i >= 0; i--)
                {
                    if (!char.IsLetterOrDigit(_text[i]))
                        break;
                }
            }
            else
            {
                for (; i >= 0; i--)
                {
                    if (!char.IsLetterOrDigit(_text[i]))
                        break;
                }
            }
            i++;

            if (i != p)
                return i;

            return -1;
        }

        private void CmdDeleteWord()
        {
            var pos = WordForward(_cursor);

            if (pos == -1)
                return;

            var k = _text.ToString(_cursor, pos - _cursor);

            if (_lastHandler == CmdDeleteWord)
                _killBuffer = _killBuffer + k;
            else
                _killBuffer = k;

            _text.Remove(_cursor, pos - _cursor);
            ComputeRendered();
            RenderAfter(_cursor);
        }

        private void CmdDeleteBackword()
        {
            var pos = WordBackward(_cursor);
            if (pos == -1)
                return;

            string k = _text.ToString(pos, _cursor - pos);

            if (_lastHandler == CmdDeleteBackword)
                _killBuffer = k + _killBuffer;
            else
                _killBuffer = k;

            _text.Remove(pos, _cursor - pos);
            ComputeRendered();
            RenderAfter(pos);
        }

        //
        // Adds the current line to the history if needed
        //
        private void HistoryUpdateLine()
        {
            _history.Update(_text.ToString());
        }

        private void CmdHistoryPrev()
        {
            if (!_history.PreviousAvailable())
                return;

            HistoryUpdateLine();

            SetText(_history.Previous());
        }

        private void CmdHistoryNext()
        {
            if (!_history.NextAvailable())
                return;

            _history.Update(_text.ToString());
            SetText(_history.Next());

        }

        private void CmdKillToEof()
        {
            _killBuffer = _text.ToString(_cursor, _text.Length - _cursor);
            _text.Length = _cursor;
            ComputeRendered();
            RenderAfter(_cursor);
        }

        private void CmdYank()
        {
            InsertTextAtCursor(_killBuffer);
        }

        private void InsertTextAtCursor(string str)
        {
            var prevLines = LineCount;
            _text.Insert(_cursor, str);
            ComputeRendered();
            if (prevLines != LineCount)
            {
                C.SetCursorPosition(0, _homeRow);
                Render();
                _cursor += str.Length;
                ForceCursor(_cursor);
            }
            else
            {
                RenderFrom(_cursor);
                _cursor += str.Length;
                ForceCursor(_cursor);
                UpdateHomeRow(TextToScreenPos(_cursor));
            }
        }

        private void SetSearchPrompt(string s)
        {
            SetPrompt("(reverse-i-search)`" + s + "': ");
        }

        private void ReverseSearch()
        {
            int p;

            if (_cursor == _text.Length)
            {
                // The cursor is at the end of the string
                p = _text.ToString().LastIndexOf(_search, StringComparison.Ordinal);
                if (p != -1)
                {
                    _matchAt = p;
                    _cursor = p;
                    ForceCursor(_cursor);
                    return;
                }
            }
            else
            {
                // The cursor is somewhere in the middle of the string
                var start = (_cursor == _matchAt) ? _cursor - 1 : _cursor;
                if (start != -1)
                {
                    p = _text.ToString().LastIndexOf(_search, start, StringComparison.Ordinal);
                    if (p != -1)
                    {
                        _matchAt = p;
                        _cursor = p;
                        ForceCursor(_cursor);
                        return;
                    }
                }
            }

            // Need to search backwards in history
            HistoryUpdateLine();
            var s = _history.SearchBackward(_search);
            if (s == null) return;
            _matchAt = -1;
            SetText(s);
            ReverseSearch();
        }

        private void CmdReverseSearch()
        {
            if (_searching == 0)
            {
                _matchAt = -1;
                _lastSearch = _search;
                _searching = -1;
                _search = "";
                SetSearchPrompt("");
            }
            else
            {
                if (_search == "")
                {
                    if (string.IsNullOrEmpty(_lastSearch)) return;
                    _search = _lastSearch;
                    SetSearchPrompt(_search);

                    ReverseSearch();
                    return;
                }
                ReverseSearch();
            }
        }

        private void SearchAppend(char c)
        {
            _search = _search + c;
            SetSearchPrompt(_search);

            //
            // If the new typed data still matches the current text, stay here
            //
            if (_cursor < _text.Length)
            {
                var r = _text.ToString(_cursor, _text.Length - _cursor);
                if (r.StartsWith(_search))
                    return;
            }

            ReverseSearch();
        }

        private void CmdRefresh()
        {
            C.Clear();
            _maxRendered = 0;
            Render();
            ForceCursor(_cursor);
        }

        void InterruptEdit(object sender, ConsoleCancelEventArgs a)
        {
            // Do not abort our program:
            a.Cancel = true;

            // Interrupt the editor
            _editThread.Abort();
        }

        void HandleChar(char c)
        {
            if (_searching != 0)
                SearchAppend(c);
            else
                InsertChar(c);
        }

        private void EditLoop()
        {
            while (!_done)
            {
                ConsoleModifiers mod;

                var cki = C.ReadKey(true);
                if (cki.Key == ConsoleKey.Escape)
                {
                    cki = C.ReadKey(true);

                    mod = ConsoleModifiers.Alt;
                }
                else
                    mod = cki.Modifiers;

                var handled = false;

                foreach (var handler in _handlers)
                {
                    var t = handler.KeyInfo;

                    if (t.Key == cki.Key && t.Modifiers == mod)
                    {
                        handled = true;
                        handler.KeyHandler();
                        _lastHandler = handler.KeyHandler;
                        break;
                    }
                    if (t.KeyChar == cki.KeyChar && t.Key == ConsoleKey.Zoom)
                    {
                        handled = true;
                        handler.KeyHandler();
                        _lastHandler = handler.KeyHandler;
                        break;
                    }
                }
                if (handled)
                {
                    if (_searching != 0)
                    {
                        if (_lastHandler != CmdReverseSearch)
                        {
                            _searching = 0;
                            SetPrompt(Prompt);
                        }
                    }
                    continue;
                }

                if (cki.KeyChar != (char)0)
                    HandleChar(cki.KeyChar);
            }
        }

        private void InitText(string initial)
        {
            _text = new StringBuilder(initial);
            ComputeRendered();
            _cursor = _text.Length;
            Render();
            ForceCursor(_cursor);
        }

        private void SetText(string newtext)
        {
            C.SetCursorPosition(0, _homeRow);
            InitText(newtext);
        }

        private void SetPrompt(string newprompt)
        {
            _shownPrompt = newprompt;
            C.SetCursorPosition(0, _homeRow);
            Render();
            ForceCursor(_cursor);
        }

        public string Edit(string prompt, string initial)
        {
            _editThread = Thread.CurrentThread;
            _searching = 0;
            C.CancelKeyPress += InterruptEdit;

            _done = false;
            _history.CursorToEnd();
            _maxRendered = 0;

            Prompt = prompt;
            _shownPrompt = prompt;
            InitText(initial);
            _history.Append(initial);

            do
            {
                try
                {
                    EditLoop();
                }
                catch (ThreadAbortException)
                {
                    _searching = 0;
                    Thread.ResetAbort();
                    SimpleConsole.Wl();
                    SetPrompt(prompt);
                    SetText("");
                }
            } while (!_done);
            SimpleConsole.Wl();

            C.CancelKeyPress -= InterruptEdit;

            if (_text == null)
            {
                _history.Close();
                return null;
            }

            var result = _text.ToString();
            if (result != "")
                _history.Accept(result);
            else
                _history.RemoveLast();

            return result;
        }

        public void SaveHistory()
        {
            _history?.Close();
        }

        public bool TabAtStartCompletes { get; set; }

        //
        // Emulates the bash-like behavior, where edits done to the
        // history are recorded
        //
        private class History
        {
            private readonly string[] _history;
            private int _head;
            private int _tail;
            private int _cursor;
            private int _count;
            private readonly string _histfile;

            public History(string app, int size)
            {
                if (size < 1)
                    throw new ArgumentException("size");

                if (app != null)
                {
                    var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    //SimpleConsole.WL (dir);
                    if (!Directory.Exists(dir))
                    {
                        try
                        {
                            Directory.CreateDirectory(dir);
                        }
                        catch
                        {
                            app = null;
                        }
                    }
                    if (app != null)
                        _histfile = Path.Combine(dir, app) + "._history";
                }

                _history = new string[size];
                _head = _tail = _cursor = 0;

                if (!File.Exists(_histfile)) return;
                using (var sr = File.OpenText(_histfile))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line != "")
                            Append(line);
                    }
                }
            }

            public void Close()
            {
                if (_histfile == null)
                    return;

                try
                {
                    using (var sw = File.CreateText(_histfile))
                    {
                        var start = (_count == _history.Length) ? _head : _tail;
                        for (var i = start; i < start + _count; i++)
                        {
                            var p = i % _history.Length;
                            sw.WriteLine(_history[p]);
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }

            //
            // Appends a value to the _history
            //
            public void Append(string s)
            {
                //SimpleConsole.WL ("APPENDING {0} head={1} tail={2}", s, head, tail);
                _history[_head] = s;
                _head = (_head + 1) % _history.Length;
                if (_head == _tail)
                    _tail = (_tail + 1 % _history.Length);
                if (_count != _history.Length)
                    _count++;
                //SimpleConsole.WL ("DONE: head={1} tail={2}", s, head, tail);
            }

            //
            // Updates the current _cursor location with the string,
            // to support editing of _history items.   For the current
            // line to participate, an Append must be done before.
            //
            public void Update(string s)
            {
                _history[_cursor] = s;
            }

            public void RemoveLast()
            {
                _head = _head - 1;
                if (_head < 0)
                    _head = _history.Length - 1;
            }

            public void Accept(string s)
            {
                var t = _head - 1;
                if (t < 0)
                    t = _history.Length - 1;

                _history[t] = s;
            }

            public bool PreviousAvailable()
            {
                //SimpleConsole.WL ("h={0} t={1} _cursor={2}", head, tail, _cursor);
                if (_count == 0)
                    return false;
                var next = _cursor - 1;
                if (next < 0)
                    next = _count - 1;

                return next != _head;
            }

            public bool NextAvailable()
            {
                if (_count == 0)
                    return false;
                var next = (_cursor + 1) % _history.Length;
                return next != _head;
            }

            //
            // Returns: a string with the previous line contents, or
            // nul if there is no data in the _history to move to.
            //
            public string Previous()
            {
                if (!PreviousAvailable())
                    return null;

                _cursor--;
                if (_cursor < 0)
                    _cursor = _history.Length - 1;

                return _history[_cursor];
            }

            public string Next()
            {
                if (!NextAvailable())
                    return null;

                _cursor = (_cursor + 1) % _history.Length;
                return _history[_cursor];
            }

            public void CursorToEnd()
            {
                if (_head == _tail)
                    return;

                _cursor = _head;
            }

            public string SearchBackward(string term)
            {
                for (var i = 0; i < _count; i++)
                {
                    var slot = _cursor - i - 1;
                    if (slot < 0)
                        slot = _history.Length + slot;
                    if (slot >= _history.Length)
                        slot = 0;
                    if (_history[slot] == null || _history[slot].IndexOf(term, StringComparison.Ordinal) == -1) continue;
                    _cursor = slot;
                    return _history[slot];
                }
                return null;
            }
        }
    }

    internal class Completion
    {
        public string[] Result;
        public string Prefix;

        public Completion(string prefix, string[] result)
        {
            Prefix = prefix;
            Result = result;
        }
    }

    internal delegate Completion AutoCompleteHandler(string text, int pos);
    
}
