using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Typhoon
{
    internal class BareParametersParser
    {
        private readonly char _keyValuesplitter;
        private readonly string _text;

        public BareParametersParser(string text, char keyValuesplitter = '=')
        {
            _keyValuesplitter = keyValuesplitter;
            _text = text.Trim();
        }

        private IEnumerable<CharContext> CharContexts
        {
            get
            {
                var enumerator = _text.GetEnumerator();

                // go to the first char
                if (!enumerator.MoveNext())
                    yield break;

                CharContext previous = null;
                char value = enumerator.Current;

                //  Continue with the second char
                while (enumerator.MoveNext())
                {
                    var next = new CharContext(enumerator.Current, _keyValuesplitter);
                    var context = new CharContext(value, _keyValuesplitter)
                    {
                        Previous = previous,
                        Next = next
                    };
                    yield return context;

                    previous = context;
                    value = next.Value;
                }

                // Return the last char
                var last = new CharContext(value, _keyValuesplitter)
                {
                    Previous = previous,
                    Next = null
                };
                yield return last;
            }
        }

        public IEnumerable<Parameter> Parameters
        {
            get
            {
                var parameterChars = new List<CharContext>();
                var index = 0;
                foreach (var charContext in CharContexts)
                {
                    if (!charContext.IsBetweenParameters)
                    {
                        parameterChars.Add(charContext);
                    }
                    if (charContext.IsFirstBetweenParameters && parameterChars.Any())
                    {
                        yield return new Parameter(parameterChars, index);
                        parameterChars = new List<CharContext>();
                        index++;
                    }

                }
                if (parameterChars.Any())
                {
                    yield return new Parameter(parameterChars, index);
                }
            }
        }
    }
}
