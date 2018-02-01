using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cradle
{
    public class Parameter
    {
        public int Index { get; private set; }
        private readonly IEnumerable<CharContext> _charContexts;

        internal Parameter(IEnumerable<CharContext> charContexts, int index)
        {
            Index = index;
            _charContexts = charContexts;
        }

        public override string ToString()
        {
            return Bruto;
        }

        // Including quotes
        public string Bruto
        {
            get
            {
                var charInfos = _charContexts.Select(c => c.Value);
                return new string(charInfos.ToArray());
            }
        }

        // Excluding quotes
        public string Netto
        {
            get
            {
                var charInfos = _charContexts.Where(c => c.IsNetto).Select(c => c.Value);
                return new string(charInfos.ToArray());
            }
        }

        public string Key
        {
            get
            {
                if (!HasValue)
                {
                    return Netto;
                }
                var valueChars = _charContexts.Take(IndexOfKeyValueSplitter)
                    .Where(c => c.IsNetto)
                    .Select(v => v.Value);
                var result = new string(valueChars.ToArray());
                return result;
            }
        }

        public bool HasValue
        {
            get
            {
                return IndexOfKeyValueSplitter > -1;
            }
        }

        public string Value
        {
            get
            {
                if (!HasValue)
                {
                    return null;
                }
                var valueChars = _charContexts.Skip(IndexOfKeyValueSplitter + 1)
                    .Where(c => c.IsNetto)
                    .Select(v => v.Value);
                var result = new string(valueChars.ToArray());
                return result;
            }
        }

        private int IndexOfKeyValueSplitter
        {
            get
            {
                for (var index = 0; index < _charContexts.Count(); index++)
                {
                    var charContext = _charContexts.ElementAt(index);
                    if (charContext.IsKeyValueSplitter)
                    {
                        return index;
                    }
                }
                return -1;
            }
        }

    }
}
