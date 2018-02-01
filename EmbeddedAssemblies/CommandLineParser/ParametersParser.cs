using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cradle
{
    // SOURCE: http://www.siepman.nl/blog/post/2014/03/26/command-line-arguments-parsing-query-with-Linq.aspx
    public class ParametersParser : IEnumerable<Parameter>
    {
        private readonly bool _caseSensitive;
        private readonly List<Parameter> _parameters;
        public string ParametersText { get; private set; }

        public ParametersParser(
            string parametersText = null,
            bool caseSensitive = false,
            char keyValuesplitter = '=')
        {
            _caseSensitive = caseSensitive;
            ParametersText = parametersText != null ? parametersText : GetAllParametersText();
            _parameters = new BareParametersParser(ParametersText, keyValuesplitter)
                                 .Parameters.ToList();
        }

        public ParametersParser(bool caseSensitive)
            : this(null, caseSensitive)
        {
        }

        public IEnumerable<Parameter> GetParameters(string key)
        {
            return _parameters.Where(p => p.Key.Equals(key, ThisStringComparison));
        }

        public IEnumerable<string> GetValues(string key)
        {
            return GetParameters(key).Where(p => p.HasValue).Select(p => p.Value);
        }

        public string GetFirstValue(string key)
        {
            return GetFirstParameter(key).Value;
        }

        public Parameter GetFirstParameterOrDefault(string key)
        {
            return ParametersWithDistinctKeys.FirstOrDefault(KeyEqualsPredicate(key));
        }

        public Parameter GetFirstParameter(string key)
        {
            return ParametersWithDistinctKeys.First(KeyEqualsPredicate(key));
        }

        private Func<Parameter, bool> KeyEqualsPredicate(string key)
        {
            return p => p.Key.Equals(key, ThisStringComparison);
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return _parameters.Select(p => p.Key);
            }
        }

        public IEnumerable<string> DistinctKeys
        {
            get
            {
                return ParametersWithDistinctKeys.Select(p => p.Key);
            }
        }

        public IEnumerable<Parameter> ParametersWithDistinctKeys
        {
            get
            {
                return _parameters.GroupBy(k => k.Key, ThisEqualityComparer).Select(k => k.First());
            }
        }

        public bool HasKey(string key)
        {
            return GetParameters(key).Any();
        }

        public bool HasKeyAndValue(string key)
        {
            var parameter = GetFirstParameterOrDefault(key);
            return parameter != null && parameter.HasValue;
        }

        public bool HasKeyAndNoValue(string key)
        {
            var parameter = GetFirstParameterOrDefault(key);
            return parameter != null && !parameter.HasValue;
        }

        private IEqualityComparer<string> ThisEqualityComparer
        {
            get
            {
                return _caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            }
        }

        private StringComparison ThisStringComparison
        {
            get
            {
                return _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            }
        }

        public bool HasHelpKey
        {
            get
            {
                return HelpParameters.Any(h =>
                    _parameters.Any(p => p.Key.Equals(h, StringComparison.OrdinalIgnoreCase)));
            }
        }

        public static IEnumerable<string> HelpParameters
        {
            get
            {
                return new[] { "?", "help", "-?", "/?", "-help", "/help" };
            }
        }

        private static string GetAllParametersText()
        {
            var everything = Environment.CommandLine;
            var executablePath = Environment.GetCommandLineArgs()[0];

            var result = everything.StartsWith("\"") ?
                everything.Substring(executablePath.Length + 2) :
                everything.Substring(executablePath.Length);
            result = result.TrimStart(' ');
            return result;
        }

        public IEnumerator<Parameter> GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Parameter this[int index]
        {
            get
            {
                return _parameters[index];
            }
        }

        public int Count
        {
            get
            {
                return _parameters.Count;
            }
        }
    }
}
