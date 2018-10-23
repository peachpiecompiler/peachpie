using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Text;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    [PhpExtension("tokenizer")]
    public static class Tokenizer
    {
        // TODO: T_*** constants

        /// <summary>
        /// Recognises the ability to use reserved words in specific contexts.
        /// </summary>
        public const int TOKEN_PARSE = 1;

        /// <summary>
        /// Get the symbolic name of a given PHP token.
        /// </summary>
        [return: NotNull]
        public static string token_name(int token)
        {
            return ((Tokens)token).ToString();
        }

        /// <summary>
        /// Split given source into PHP tokens.
        /// </summary>
        /// <param name="source">The PHP source to parse.</param>
        /// <param name="flags"></param>
        /// <returns>
        /// An array of token identifiers.
        /// Each individual token identifier is either a single character (i.e.: ;, ., >, !, etc...),
        /// or a three element array containing the token index in element 0, the string content of the original token in element 1 and the line number in element 2.
        /// </returns>
        [return: NotNull]
        public static PhpArray/*!*/token_get_all(string source, int flags = 0)
        {
            var tokens = new PhpArray();

            Tokens t;
            var lines = LineBreaks.Create(source);
            var tokenizer = new Lexer(new StringReader(source), Encoding.UTF8);
            while ((t = tokenizer.GetNextToken()) != Tokens.EOF)
            {
                if (tokenizer.TokenSpan.Length == 1 && (int)t == tokenizer.TokenText[0])
                {
                    // single char token
                    tokens.Add(tokenizer.TokenText);
                }
                else
                {
                    // other
                    tokens.Add(new PhpArray(3)
                    {
                        (int)t,
                        tokenizer.TokenText,
                        lines.GetLineFromPosition(tokenizer.TokenSpan.Start) + 1,
                    });
                }

                //

                if (t == Tokens.T_ERROR)
                {
                    break;
                }
            }

            return tokens;
        }
    }
}
