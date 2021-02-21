using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Text;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;

namespace Peachpie.Library.Scripting
{
    [PhpExtension("standard")]
    public static class Standard
    {
        /// <summary>
        /// Return source with stripped comments and whitespace.
        /// </summary>
        /// <returns>The stripped source code will be returned on success, or an empty string on failure.</returns>
        public static string php_strip_whitespace(Context ctx, string filename)
        {
            var stream = PhpStream.Open(ctx, filename, "rt", StreamOpenOptions.Empty, StreamContext.Default)?.RawStream;
            if (stream == null)
            {
                return string.Empty;
            }

            Tokens t;
            var result = StringBuilderUtilities.Pool.Get();

            void Append(StringBuilder sb, CharSpan span)
            {
                sb.Append(span.Buffer, span.Start, span.Length);
            }

            using (var tokenizer = new Lexer(new StreamReader(stream, Encoding.UTF8), Encoding.UTF8))
            {
                while ((t = tokenizer.GetNextToken()) != Tokens.EOF)
                {
                    switch (t)
                    {
                        case Tokens.T_COMMENT:
                            // ignore
                            break;
                        case Tokens.T_WHITESPACE:
                            result.Append(' ');
                            break;
                        default:
                            //result.Append(tokenizer.TokenText);
                            // avoid copying and allocating string
                            Append(result, tokenizer.GetTokenSpan());
                            break;
                    }
                }
            }

            stream.Dispose();

            return StringBuilderUtilities.GetStringAndReturn(result);
        }
    }
}
