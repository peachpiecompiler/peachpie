using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.Library.PDO.Utilities
{
    internal class StatementStringParser
    {
        public enum Tokens
        {
            Text,
            QuotedString,
            UnnamedParameter,
        }

        protected virtual void Next(Tokens token, string text, int start, int length) // TODO: (Tokens token, ReadOnlySpan<char> tokentext)
        {
            // to be overriden
        }

        public void ParseString(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // current token
            int tstart = 0;
            
            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '?':
                        Next(Tokens.Text, text, tstart, i - tstart);
                        
                        // unnamed parameter
                        Next(Tokens.UnnamedParameter, text, i, 1);

                        tstart = i + 1;
                        break;

                    case '"':
                    case '\'':
                        Next(Tokens.Text, text, tstart, i - tstart);
                        
                        // quoted string
                        tstart = i;
                        var quoted = text[i];   // the opening quote
                        for (i = i + 1; i < text.Length && text[i] != quoted; i++)
                        {
                            if (text[i] == '\\') i++;   // skip the escaped character
                        }

                        i++; // consume the closing quote

                        Next(Tokens.QuotedString, text, tstart, i - tstart);

                        tstart = i;
                        break;
                }
            }

            // finalize the Text token
            Next(Tokens.Text, text, tstart, text.Length - tstart);
        }
    }
}
