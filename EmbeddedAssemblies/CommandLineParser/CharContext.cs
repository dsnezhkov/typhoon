using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cradle
{
    internal class CharContext
    {
        private readonly char _keyValuesplitter;

        public CharContext(char value, char keyValuesplitter = '=')
        {
            _keyValuesplitter = keyValuesplitter;
            Value = value;
            _isBetweenQuotes = new Lazy<bool>(GetIsBetweenQuotes);
        }

        public CharContext Previous { get; set; }
        public CharContext Next { get; set; }
        public char Value { get; private set; }

        private readonly Lazy<bool> _isBetweenQuotes;
        private bool IsBetweenQuotes
        {
            get
            {
                return _isBetweenQuotes.Value;
            }
        }

        private bool GetIsBetweenQuotes()
        {
            if (Previous == null) return false;
            if (Value != '"') return Previous.IsBetweenQuotes;
            if (IsToEscape || IsEscapedQuote) return Previous.IsBetweenQuotes;
            return !Previous.IsBetweenQuotes;
        }

        private bool UnEscapedQuote
        {
            get
            {
                if (Value != '"') return false;
                if (Previous == null) return true;
                return !Previous.IsToEscape;
            }
        }

        private bool IsToEscape
        {
            get
            {
                if (Previous == null ||
                    Next == null ||
                    Value != '"' ||
                    Next.Value != '"') return false;
                return !Previous.IsToEscape;
            }
        }

        private bool IsEscapedQuote
        {
            get
            {
                if (Previous == null ||
                    Value != '"') return false;
                return Previous.IsToEscape;
            }
        }

        public bool IsNetto
        {
            get
            {
                return !(IsToEscape || IsBetweenParameters || UnEscapedQuote);
            }
        }

        public bool IsBetweenParameters
        {
            get
            {
                return Value == ' ' && !IsBetweenQuotes;
            }
        }

        public bool IsFirstBetweenParameters
        {
            get
            {
                return IsBetweenParameters && !Previous.IsBetweenParameters;
            }
        }

        public bool IsKeyValueSplitter
        {
            get
            {
                return Value == _keyValuesplitter && !IsBetweenQuotes;
            }
        }

        public override string ToString() // Makes debugging easier
        {
            return Value.ToString();
        }
    }
}
