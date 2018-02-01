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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.RegularExpressions;


    internal static class StringUtilityFunctions
    {
        public static IEnumerable<string> WrappedLines(string self, params int[] widths)
        {
            IEnumerable<int> w = widths;
            return WrappedLines(self, w);
        }

        public static IEnumerable<string> WrappedLines(string self, IEnumerable<int> widths)
        {
            if (widths == null)
                throw new ArgumentNullException(nameof(widths));
            return CreateWrappedLinesIterator(self, widths);
        }

        private static IEnumerable<string> CreateWrappedLinesIterator(string self, IEnumerable<int> widths)
        {
            if (string.IsNullOrEmpty(self))
            {
                yield return string.Empty;
                yield break;
            }
            using (var ewidths = widths.GetEnumerator())
            {
                bool? hw = null;
                var width = GetNextWidth(ewidths, int.MaxValue, ref hw);
                var start = 0;
                do
                {
                    var end = GetLineEnd(start, width, self);
                    var c = self[end - 1];
                    if (char.IsWhiteSpace(c))
                        --end;
                    var needContinuation = end != self.Length && !IsEolChar(c);
                    var continuation = "";
                    if (needContinuation)
                    {
                        --end;
                        continuation = "-";
                    }
                    var line = self.Substring(start, end - start) + continuation;
                    yield return line;
                    start = end;
                    if (char.IsWhiteSpace(c))
                        ++start;
                    width = GetNextWidth(ewidths, width, ref hw);
                } while (start < self.Length);
            }
        }

        private static int GetNextWidth(IEnumerator<int> ewidths, int curWidth, ref bool? eValid)
        {
            if (eValid.HasValue && (!eValid.Value)) return curWidth;
            curWidth = (eValid = ewidths.MoveNext()).Value ? ewidths.Current : curWidth;
            // '.' is any character, - is for a continuation
            const string minWidth = ".-";
            if (curWidth < minWidth.Length)
                throw new ArgumentOutOfRangeException(nameof(curWidth),
                    $"Element must be >= {minWidth.Length}, was {curWidth}.");
            return curWidth;
        }

        private static bool IsEolChar(char c)
        {
            return !char.IsLetterOrDigit(c);
        }

        private static int GetLineEnd(int start, int length, string description)
        {
            var end = Math.Min(start + length, description.Length);
            var sep = -1;
            for (var i = start; i < end; ++i)
            {
                if (description[i] == '\n')
                    return i + 1;
                if (IsEolChar(description[i]))
                    sep = i + 1;
            }
            if (sep == -1 || end == description.Length)
                return end;
            return sep;
        }
    }

    public class ArgumentValueCollection : IList, IList<string>
    {
        private readonly ArgumentContext _c;

        private readonly List<string> _values = new List<string>();

        internal ArgumentValueCollection(ArgumentContext c)
        {
            _c = c;
        }

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        #endregion

        #region IEnumerable<T>

        public IEnumerator<string> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        #endregion

        public List<string> ToList()
        {
            return new List<string>(_values);
        }

        public string[] ToArray()
        {
            return _values.ToArray();
        }

        public override string ToString()
        {
            return string.Join(", ", _values.ToArray());
        }

        #region ICollection

        void ICollection.CopyTo(Array array, int index)
        {
            (_values as ICollection).CopyTo(array, index);
        }

        bool ICollection.IsSynchronized => (_values as ICollection).IsSynchronized;

        object ICollection.SyncRoot => (_values as ICollection).SyncRoot;

        #endregion

        #region ICollection<T>

        public void Add(string item)
        {
            _values.Add(item);
        }

        public void Clear()
        {
            _values.Clear();
        }

        public bool Contains(string item)
        {
            return _values.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _values.CopyTo(array, arrayIndex);
        }

        public bool Remove(string item)
        {
            return _values.Remove(item);
        }

        public int Count => _values.Count;

        public bool IsReadOnly => false;

        #endregion

        #region IList

        int IList.Add(object value)
        {
            return (_values as IList).Add(value);
        }

        bool IList.Contains(object value)
        {
            return (_values as IList).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return (_values as IList).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            (_values as IList).Insert(index, value);
        }

        void IList.Remove(object value)
        {
            (_values as IList).Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            (_values as IList).RemoveAt(index);
        }

        bool IList.IsFixedSize => false;

        object IList.this[int index]
        {
            get { return this[index]; }
            set { (_values as IList)[index] = value; }
        }

        #endregion

        #region IList<T>

        public int IndexOf(string item)
        {
            return _values.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _values.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _values.RemoveAt(index);
        }

        private void AssertValid(int index)
        {
            if (_c.Argument == null)
                throw new InvalidOperationException("ArgumentContext.Argument is null.");
            if (index >= _c.Argument.MaxValueCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (_c.Argument.ArgumentValueType == ArgumentValueType.Required &&
                index >= _values.Count)
                throw new ArgParseException(string.Format(
                    _c.ArgumentParser.MessageLocalizer("Missing required value for Argument '{0}'."), _c.ArgumentName),
                    _c.ArgumentName);
        }

        public string this[int index]
        {
            get
            {
                AssertValid(index);
                return index >= _values.Count ? null : _values[index];
            }
            set { _values[index] = value; }
        }

        #endregion
    }

    public class ArgumentContext
    {
        public ArgumentContext(ArgumentParser parser)
        {
            ArgumentParser = parser;
            ArgumentValues = new ArgumentValueCollection(this);
        }

        public Argument Argument { get; set; }

        public string ArgumentName { get; set; }

        public int ArgumentIndex { get; set; }

        public ArgumentParser ArgumentParser { get; }

        public ArgumentValueCollection ArgumentValues { get; }
    }

    public enum ArgumentValueType
    {
        None,
        Optional,
        Required
    }

    public abstract class Argument
    {
       
        private static readonly char[] NameTerminator = {'=', ':'};

        protected Argument(string prototype, string description)
            : this(prototype, description, 1, false)
        {
        }

        protected Argument(string prototype, string description, int maxValueCount)
            : this(prototype, description, maxValueCount, false)
        {
        }

        protected Argument(string prototype, string description, int maxValueCount, bool hidden)
        {
            if (prototype == null)
                throw new ArgumentNullException(nameof(prototype));
            if (prototype.Length == 0)
                throw new ArgumentException("Cannot be the empty string.", nameof(prototype));
            if (maxValueCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValueCount));

            Prototype = prototype;
            Description = description;
            MaxValueCount = maxValueCount;
            Names = this is ArgumentParser.Category
                ? new[] {prototype + GetHashCode()}
                : prototype.Split('|');

            if (this is ArgumentParser.Category)
                return;

            ArgumentValueType = ParsePrototype();
            Hidden = hidden;

            if (MaxValueCount == 0 && ArgumentValueType != ArgumentValueType.None)
                throw new ArgumentException(
                    "Cannot provide maxValueCount of 0 for ArgumentValueType.Required or " +
                    "ArgumentValueType.Optional.",
                    nameof(maxValueCount));
            if (ArgumentValueType == ArgumentValueType.None && maxValueCount > 1)
                throw new ArgumentException(
                    $"Cannot provide maxValueCount of {maxValueCount} for OptionValueType.None.",
                    nameof(maxValueCount));
            if (Array.IndexOf(Names, "<>") >= 0 &&
                ((Names.Length == 1 && ArgumentValueType != ArgumentValueType.None) ||
                 (Names.Length > 1 && MaxValueCount > 1)))
                throw new ArgumentException(
                    "The default Argument handler '<>' cannot require values.",
                    nameof(prototype));
        }

        public string Prototype { get; }
        public string Description { get; }
        public ArgumentValueType ArgumentValueType { get; }
        public int MaxValueCount { get; }
        public bool Hidden { get; }

        internal string[] Names { get; }
        internal string[] ValueSeparators { get; private set; }

        public string[] GetNames()
        {
            return (string[]) Names.Clone();
        }

        public string[] GetValueSeparators()
        {
            if (ValueSeparators == null)
                return new string[0];
            return (string[]) ValueSeparators.Clone();
        }

        protected static T Parse<T>(string value, ArgumentContext c)
        {
            var tt = typeof(T);
            var nullable = tt.IsValueType && tt.IsGenericType &&
                           !tt.IsGenericTypeDefinition &&
                           tt.GetGenericTypeDefinition() == typeof(Nullable<>);
            var targetType = nullable ? tt.GetGenericArguments()[0] : typeof(T);
            var conv = TypeDescriptor.GetConverter(targetType);
            var t = default(T);
            try
            {
                if (value != null)
                    t = (T) conv.ConvertFromString(value);
            }
            catch (Exception e)
            {
                throw new ArgParseException(
                    string.Format(
                        c.ArgumentParser.MessageLocalizer("Could not convert string `{0}' to type {1} for Argument `{2}'."),
                        value, targetType.Name, c.ArgumentName),
                    c.ArgumentName, e);
            }
            return t;
        }

        private ArgumentValueType ParsePrototype()
        {
            var type = '\0';
            var seps = new List<string>();
            for (var i = 0; i < Names.Length; ++i)
            {
                var name = Names[i];
                if (name.Length == 0)
                    throw new Exception("Empty Argument names are not supported.");

                var end = name.IndexOfAny(NameTerminator);
                if (end == -1)
                    continue;
                Names[i] = name.Substring(0, end);
                if (type == '\0' || type == name[end])
                    type = name[end];
                else
                    throw new Exception(
                        $"Conflicting argument types: '{type}' vs. '{name[end]}'.");
                AddSeparators(name, end, seps);
            }

            if (type == '\0')
                return ArgumentValueType.None;

            if (MaxValueCount <= 1 && seps.Count != 0)
                throw new Exception(
                    $"Cannot provide key/value separators for arguments taking {MaxValueCount} value(s).");
            if (MaxValueCount <= 1) return type == '=' ? ArgumentValueType.Required : ArgumentValueType.Optional;
            if (seps.Count == 0)
                ValueSeparators = new[] {":", "="};
            else if (seps.Count == 1 && seps[0].Length == 0)
                ValueSeparators = null;
            else
                ValueSeparators = seps.ToArray();

            return type == '=' ? ArgumentValueType.Required : ArgumentValueType.Optional;
        }

        private static void AddSeparators(string name, int end, ICollection<string> seps)
        {
            var start = -1;
            for (var i = end + 1; i < name.Length; ++i)
            {
                switch (name[i])
                {
                    case '{':
                        if (start != -1)
                            throw new ArgumentException(
                                $"Ill-formed name/value separator found in \"{name}\".",
                                nameof(name));
                        start = i + 1;
                        break;
                    case '}':
                        if (start == -1)
                            throw new ArgumentException(
                                $"Ill-formed name/value separator found in \"{name}\".",
                                nameof(name));
                        seps.Add(name.Substring(start, i - start));
                        start = -1;
                        break;
                    default:
                        if (start == -1)
                            seps.Add(name[i].ToString());
                        break;
                }
            }
            if (start != -1)
                throw new ArgumentException(
                    $"Ill-formed name/value separator found in \"{name}\".",
                    nameof(name));
        }

        public void Invoke(ArgumentContext c)
        {
            OnParseComplete(c);
            c.ArgumentName = null;
            c.Argument = null;
            c.ArgumentValues.Clear();
        }

        protected abstract void OnParseComplete(ArgumentContext c);

        public override string ToString()
        {
            return Prototype;
        }
    }

    public abstract class ArgumentSource
    {
        public abstract string Description { get; }

        public abstract string[] GetNames();
        public abstract bool GetArguments(string value, out IEnumerable<string> replacement);

        public static IEnumerable<string> GetArgumentsFromFile(string file)
        {
            return GetArguments(File.OpenText(file), true);
        }

        public static IEnumerable<string> GetArguments(TextReader reader)
        {
            return GetArguments(reader, false);
        }

        private static IEnumerable<string> GetArguments(TextReader reader, bool close)
        {
            try
            {
                var arg = new StringBuilder();

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var t = line.Length;

                    for (var i = 0; i < t; i++)
                    {
                        var c = line[i];

                        switch (c)
                        {
                            case '"':
                            case '\'':
                                var end = c;

                                for (i++; i < t; i++)
                                {
                                    c = line[i];

                                    if (c == end)
                                        break;
                                    arg.Append(c);
                                }
                                break;
                            case ' ':
                                if (arg.Length <= 0) continue;
                                yield return arg.ToString();
                                arg.Length = 0;
                                break;
                            default:
                                arg.Append(c);
                                break;
                        }
                    }
                    if (arg.Length <= 0) continue;
                    yield return arg.ToString();
                    arg.Length = 0;
                }
            }
            finally
            {
                if (close)
                    reader.Close();
            }
        }
    }

    public class ResponseFileSource : ArgumentSource
    {
        public override string Description => "Read response file for more arguments.";

        public override string[] GetNames()
        {
            return new[] {"@file"};
        }

        public override bool GetArguments(string value, out IEnumerable<string> replacement)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("@"))
            {
                replacement = null;
                return false;
            }
            replacement = GetArgumentsFromFile(value.Substring(1));
            return true;
        }
    }

    [Serializable]
    public class ArgParseException : Exception
    {
        public ArgParseException()
        {
        }

        public ArgParseException(string message, string argumentName)
            : base(message)
        {
            ArgumentName = argumentName;
        }

        public ArgParseException(string message, string argumentName, Exception innerException)
            : base(message, innerException)
        {
            ArgumentName = argumentName;
        }

        protected ArgParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ArgumentName = info.GetString("ArgumentName");
        }

        public string ArgumentName { get; }

    }

    public delegate void ArgumentAction<TKey, TValue>(TKey key, TValue value);

    public class ArgumentParser : KeyedCollection<string, Argument>
    {
        private const int ArgumentWidth = 29;
        private const int DescriptionFirstWidth = 80 - ArgumentWidth;
        private const int DescriptionRemWidth = 80 - ArgumentWidth - 2;

        private readonly Regex _valueArgument = new Regex(
            @"^(?<flag>--|-|/)(?<name>[^:=]+)((?<sep>[:=])(?<value>.*))?$");

        private readonly List<ArgumentSource> _sources = new List<ArgumentSource>();

        public ArgumentParser()
            : this(f => f)
        {
        }

        public ArgumentParser(Converter<string, string> localizer)
        {
            MessageLocalizer = localizer;
            ArgumentSources = new ReadOnlyCollection<ArgumentSource>(_sources);
        }

        public Converter<string, string> MessageLocalizer { get; }

        public ReadOnlyCollection<ArgumentSource> ArgumentSources { get; }


        protected override string GetKeyForItem(Argument item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (item.Names != null && item.Names.Length > 0)
                return item.Names[0];
            throw new InvalidOperationException("Argument has no names!");
        }

        [Obsolete("Use KeyedCollection.this[string]")]
        protected Argument GetArgumentForName(string argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));
            try
            {
                return base[argument];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        protected override void InsertItem(int index, Argument item)
        {
            base.InsertItem(index, item);
            AddImpl(item);
        }

        protected override void RemoveItem(int index)
        {
            var p = Items[index];
            base.RemoveItem(index);            
            for (var i = 1; i < p.Names.Length; ++i)
            {
                Dictionary.Remove(p.Names[i]);
            }
        }

        protected override void SetItem(int index, Argument item)
        {
            base.SetItem(index, item);
            AddImpl(item);
        }

        private void AddImpl(Argument argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));
            var added = new List<string>(argument.Names.Length);
            try
            {                
                for (var i = 1; i < argument.Names.Length; ++i)
                {
                    Dictionary.Add(argument.Names[i], argument);
                    added.Add(argument.Names[i]);
                }
            }
            catch (Exception)
            {
                foreach (var name in added)
                    Dictionary.Remove(name);
                throw;
            }
        }

        public ArgumentParser Add(string header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));
            Add(new Category(header));
            return this;
        }


        public new ArgumentParser Add(Argument argument)
        {
            base.Add(argument);
            return this;
        }

        public ArgumentParser Add(string prototype, Action<string> action)
        {
            return Add(prototype, null, action);
        }

        public ArgumentParser Add(string prototype, string description, Action<string> action)
        {
            return Add(prototype, description, action, false);
        }

        public ArgumentParser Add(string prototype, string description, Action<string> action, bool hidden)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            Argument p = new ActionArgument(prototype, description, 1,
                delegate(ArgumentValueCollection v) { action(v[0]); }, hidden);
            base.Add(p);
            return this;
        }

        public ArgumentParser Add(string prototype, ArgumentAction<string, string> action)
        {
            return Add(prototype, null, action);
        }

        public ArgumentParser Add(string prototype, string description, ArgumentAction<string, string> action)
        {
            return Add(prototype, description, action, false);
        }

        public ArgumentParser Add(string prototype, string description, ArgumentAction<string, string> action, bool hidden)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            Argument p = new ActionArgument(prototype, description, 2,
                delegate(ArgumentValueCollection v) { action(v[0], v[1]); }, hidden);
            base.Add(p);
            return this;
        }

        public ArgumentParser Add<T>(string prototype, Action<T> action)
        {
            return Add(prototype, null, action);
        }

        public ArgumentParser Add<T>(string prototype, string description, Action<T> action)
        {
            return Add(new ActionArgument<T>(prototype, description, action));
        }

        public ArgumentParser Add<TKey, TValue>(string prototype, ArgumentAction<TKey, TValue> action)
        {
            return Add(prototype, null, action);
        }

        public ArgumentParser Add<TKey, TValue>(string prototype, string description, ArgumentAction<TKey, TValue> action)
        {
            return Add(new ActionArgument<TKey, TValue>(prototype, description, action));
        }

        public ArgumentParser Add(ArgumentSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            _sources.Add(source);
            return this;
        }

        protected virtual ArgumentContext CreateArgumentContext()
        {
            return new ArgumentContext(this);
        }

        public List<string> Parse(IEnumerable<string> arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));
            var c = CreateArgumentContext();
            c.ArgumentIndex = -1;
            var process = true;
            var unprocessed = new List<string>();
            var def = Contains("<>") ? this["<>"] : null;
            var ae = new ArgumentEnumerator(arguments);
            foreach (var argument in ae)
            {
                ++c.ArgumentIndex;
                if (argument == "--")
                {
                    process = false;
                    continue;
                }
                if (!process)
                {
                    Unprocessed(unprocessed, def, c, argument);
                    continue;
                }
                if (AddSource(ae, argument))
                    continue;
                if (!Parse(argument, c))
                    Unprocessed(unprocessed, def, c, argument);
            }
            c.Argument?.Invoke(c);
            return unprocessed;
        }

        private bool AddSource(ArgumentEnumerator ae, string argument)
        {
            foreach (var source in _sources)
            {
                IEnumerable<string> replacement;
                if (!source.GetArguments(argument, out replacement))
                    continue;
                ae.Add(replacement);
                return true;
            }
            return false;
        }

        private static void Unprocessed(ICollection<string> extra, Argument def, ArgumentContext c, string argument)
        {
            if (def == null)
            {
                extra.Add(argument);
                return;
            }
            c.ArgumentValues.Add(argument);
            c.Argument = def;
            c.Argument.Invoke(c);
        }

        protected bool GetArgumentParts(string argument, out string flag, out string name, out string sep,
            out string value)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            flag = name = sep = value = null;
            var m = _valueArgument.Match(argument);
            if (!m.Success)
            {
                return false;
            }
            flag = m.Groups["flag"].Value;
            name = m.Groups["name"].Value;
            if (!m.Groups["sep"].Success || !m.Groups["value"].Success) return true;
            sep = m.Groups["sep"].Value;
            value = m.Groups["value"].Value;
            return true;
        }

        protected virtual bool Parse(string argument, ArgumentContext c)
        {
            if (c.Argument != null)
            {
                ParseValue(argument, c);
                return true;
            }

            string f, n, s, v;
            if (!GetArgumentParts(argument, out f, out n, out s, out v))
                return false;

            if (!Contains(n)) return ParseBool(argument, n, c) || ParseBundledValue(f, $"{n}{s}{v}", c);
            var p = this[n];
            c.ArgumentName = f + n;
            c.Argument = p;
            switch (p.ArgumentValueType)
            {
                case ArgumentValueType.None:
                    c.ArgumentValues.Add(n);
                    c.Argument.Invoke(c);
                    break;
                case ArgumentValueType.Optional:
                case ArgumentValueType.Required:
                    ParseValue(v, c);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return true;
        }

        private void ParseValue(string argument, ArgumentContext c)
        {
            if (argument != null)
                foreach (var o in c.Argument.ValueSeparators != null ? argument.Split(c.Argument.ValueSeparators, c.Argument.MaxValueCount - c.ArgumentValues.Count, StringSplitOptions.None) : new[] {argument})
                {
                    c.ArgumentValues.Add(o);
                }
            if (c.ArgumentValues.Count == c.Argument.MaxValueCount || c.Argument.ArgumentValueType == ArgumentValueType.Optional)
                c.Argument.Invoke(c);
            else if (c.ArgumentValues.Count > c.Argument.MaxValueCount)
            {
                throw new ArgParseException(MessageLocalizer(
                    $"Error: Found {c.ArgumentValues.Count} argument values when expecting {c.Argument.MaxValueCount}."), c.ArgumentName);
            }
        }

        private bool ParseBool(string argument, string n, ArgumentContext c)
        {
            string rn;
            if (n.Length < 1 || (n[n.Length - 1] != '+' && n[n.Length - 1] != '-') ||
                !Contains(rn = n.Substring(0, n.Length - 1))) return false;
            var p = this[rn];
            var v = n[n.Length - 1] == '+' ? argument : null;
            c.ArgumentName = argument;
            c.Argument = p;
            c.ArgumentValues.Add(v);
            p.Invoke(c);
            return true;
        }

        private bool ParseBundledValue(string f, string n, ArgumentContext c)
        {
            if (f != "-")
                return false;
            for (var i = 0; i < n.Length; ++i)
            {
                var opt = f + n[i];
                var rn = n[i].ToString();
                if (!Contains(rn))
                {
                    if (i == 0)
                        return false;
                    throw new ArgParseException(string.Format(MessageLocalizer("Cannot use unregistered Argument '{0}' in bundle '{1}'."), rn, f + n), null);
                }
                var p = this[rn];
                switch (p.ArgumentValueType)
                {
                    case ArgumentValueType.None:
                        Invoke(c, opt, n, p);
                        break;
                    case ArgumentValueType.Optional:
                    case ArgumentValueType.Required:
                    {
                        var v = n.Substring(i + 1);
                        c.Argument = p;
                        c.ArgumentName = opt;
                        ParseValue(v.Length != 0 ? v : null, c);
                        return true;
                    }
                    default:
                        throw new InvalidOperationException("Unknown ArgumentValueType: " + p.ArgumentValueType);
                }
            }
            return true;
        }

        private static void Invoke(ArgumentContext c, string name, string value, Argument argument)
        {
            c.ArgumentName = name;
            c.Argument = argument;
            c.ArgumentValues.Add(value);
            argument.Invoke(c);
        }

        public void WriteArgumentDescriptions(TextWriter o)
        {
            foreach (var p in this)
            {
                var written = 0;

                if (p.Hidden)
                    continue;

                var c = p as Category;
                if (c != null)
                {
                    WriteDescription(o, p.Description, "", 80, 80);
                    continue;
                }

                if (!WriteArgumentPrototype(o, p, ref written))
                    continue;

                if (written < ArgumentWidth)
                    o.Write(new string(' ', ArgumentWidth - written));
                else
                {
                    o.WriteLine();
                    o.Write(new string(' ', ArgumentWidth));
                }

                WriteDescription(o, p.Description, new string(' ', ArgumentWidth + 2), DescriptionFirstWidth, DescriptionRemWidth);
            }

            foreach (var s in _sources)
            {
                var names = s.GetNames();
                if (names == null || names.Length == 0)
                    continue;

                var written = 0;

                Write(o, ref written, "  ");
                Write(o, ref written, names[0]);
                for (var i = 1; i < names.Length; ++i)
                {
                    Write(o, ref written, ", ");
                    Write(o, ref written, names[i]);
                }

                if (written < ArgumentWidth)
                    o.Write(new string(' ', ArgumentWidth - written));
                else
                {
                    o.WriteLine();
                    o.Write(new string(' ', ArgumentWidth));
                }

                WriteDescription(o, s.Description, new string(' ', ArgumentWidth + 2), DescriptionFirstWidth, DescriptionRemWidth);
            }
        }

        private void WriteDescription(TextWriter o, string value, string prefix, int firstWidth, int remWidth)
        {
            var indent = false;
            foreach (var line in GetLines(MessageLocalizer(GetDescription(value)), firstWidth, remWidth))
            {
                if (indent)
                    o.Write(prefix);
                o.WriteLine(line);
                indent = true;
            }
        }

        private bool WriteArgumentPrototype(TextWriter o, Argument p, ref int written)
        {
            var names = p.Names;

            var i = GetNextArgumentIndex(names, 0);
            if (i == names.Length)
                return false;

            if (names[i].Length == 1)
            {
                Write(o, ref written, "  -");
                Write(o, ref written, names[0]);
            }
            else
            {
                Write(o, ref written, "      --");
                Write(o, ref written, names[0]);
            }

            for (i = GetNextArgumentIndex(names, i + 1); i < names.Length; i = GetNextArgumentIndex(names, i + 1))
            {
                Write(o, ref written, ", ");
                Write(o, ref written, names[i].Length == 1 ? "-" : "--");
                Write(o, ref written, names[i]);
            }

            if (p.ArgumentValueType == ArgumentValueType.Optional || p.ArgumentValueType == ArgumentValueType.Required)
            {
                if (p.ArgumentValueType == ArgumentValueType.Optional)
                {
                    Write(o, ref written, MessageLocalizer("["));
                }
                Write(o, ref written, MessageLocalizer("=" + GetArgumentName(0, p.MaxValueCount, p.Description)));
                var sep = p.ValueSeparators != null && p.ValueSeparators.Length > 0 ? p.ValueSeparators[0] : " ";
                for (var c = 1; c < p.MaxValueCount; ++c)
                {
                    Write(o, ref written, MessageLocalizer(sep + GetArgumentName(c, p.MaxValueCount, p.Description)));
                }
                if (p.ArgumentValueType == ArgumentValueType.Optional)
                {
                    Write(o, ref written, MessageLocalizer("]"));
                }
            }
            return true;
        }

        private static int GetNextArgumentIndex(string[] names, int i)
        {
            while (i < names.Length && names[i] == "<>")
            {
                ++i;
            }
            return i;
        }

        private static void Write(TextWriter o, ref int n, string s)
        {
            n += s.Length;
            o.Write(s);
        }

        private static string GetArgumentName(int index, int maxIndex, string description)
        {
            if (description == null)
                return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
            var nameStart = maxIndex == 1 ? new[] {"{0:", "{"} : new[] {"{" + index + ":"};
            foreach (var t in nameStart)
            {
                int start, j = 0;
                do
                {
                    start = description.IndexOf(t, j, StringComparison.Ordinal);
                } while (start >= 0 && j != 0 && description[j++ - 1] == '{');
                if (start == -1)
                    continue;
                var end = description.IndexOf("}", start, StringComparison.Ordinal);
                if (end == -1)
                    continue;
                return description.Substring(start + t.Length, end - start - t.Length);
            }
            return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
        }

        private static string GetDescription(string description)
        {
            if (description == null)
                return string.Empty;
            var sb = new StringBuilder(description.Length);
            var start = -1;
            for (var i = 0; i < description.Length; ++i)
            {
                switch (description[i])
                {
                    case '{':
                        if (i == start)
                        {
                            sb.Append('{');
                            start = -1;
                        }
                        else if (start < 0)
                            start = i + 1;
                        break;
                    case '}':
                        if (start < 0)
                        {
                            if (i + 1 == description.Length || description[i + 1] != '}')
                                throw new InvalidOperationException("Invalid Argument description: " + description);
                            ++i;
                            sb.Append("}");
                        }
                        else
                        {
                            sb.Append(description.Substring(start, i - start));
                            start = -1;
                        }
                        break;
                    case ':':
                        if (start < 0)
                            goto default;
                        start = i + 1;
                        break;
                    default:
                        if (start < 0)
                            sb.Append(description[i]);
                        break;
                }
            }
            return sb.ToString();
        }

        private static IEnumerable<string> GetLines(string description, int firstWidth, int remWidth)
        {
            return StringUtilityFunctions.WrappedLines(description, firstWidth, remWidth);
        }

        internal sealed class Category : Argument
        {
            public Category(string description) : base("=:Category:= " + description, description)
            {
            }

            protected override void OnParseComplete(ArgumentContext c)
            {
                throw new NotSupportedException("Category.OnParseComplete should not be invoked.");
            }
        }

        private sealed class ActionArgument : Argument
        {
            private readonly Action<ArgumentValueCollection> _action;

            public ActionArgument(string prototype, string description, int count, Action<ArgumentValueCollection> action) : this(prototype, description, count, action, false)
            {
            }

            public ActionArgument(string prototype, string description, int count, Action<ArgumentValueCollection> action, bool hidden) : base(prototype, description, count, hidden)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));
                _action = action;
            }

            protected override void OnParseComplete(ArgumentContext c)
            {
                _action(c.ArgumentValues);
            }
        }

        private sealed class ActionArgument<T> : Argument
        {
            private readonly Action<T> _action;

            public ActionArgument(string prototype, string description, Action<T> action) : base(prototype, description, 1)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));
                _action = action;
            }

            protected override void OnParseComplete(ArgumentContext c)
            {
                _action(Parse<T>(c.ArgumentValues[0], c));
            }
        }

        private sealed class ActionArgument<TKey, TValue> : Argument
        {
            private readonly ArgumentAction<TKey, TValue> _action;

            public ActionArgument(string prototype, string description, ArgumentAction<TKey, TValue> action) : base(prototype, description, 2)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));
                _action = action;
            }

            protected override void OnParseComplete(ArgumentContext c)
            {
                _action(Parse<TKey>(c.ArgumentValues[0], c), Parse<TValue>(c.ArgumentValues[1], c));
            }
        }

        private class ArgumentEnumerator : IEnumerable<string>
        {
            private readonly List<IEnumerator<string>> _sources = new List<IEnumerator<string>>();

            public ArgumentEnumerator(IEnumerable<string> arguments)
            {
                _sources.Add(arguments.GetEnumerator());
            }

            public IEnumerator<string> GetEnumerator()
            {
                do
                {
                    var c = _sources[_sources.Count - 1];
                    if (c.MoveNext())
                        yield return c.Current;
                    else
                    {
                        c.Dispose();
                        _sources.RemoveAt(_sources.Count - 1);
                    }
                } while (_sources.Count > 0);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(IEnumerable<string> arguments)
            {
                _sources.Add(arguments.GetEnumerator());
            }
        }
    }
}