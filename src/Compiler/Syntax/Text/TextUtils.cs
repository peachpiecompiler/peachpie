using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PHP.Core.Text
{
    #region TextUtils

    public static class TextUtils
    {
        /// <summary>
        /// Gets length of line break character sequence if any.
        /// </summary>
        /// <param name="text">Document text.</param>
        /// <param name="position">Index of character within <paramref name="text"/> to look at.</param>
        /// <returns>Length of line break character sequence at <paramref name="position"/>. In case of no line break, <c>0</c> is returned.</returns>
        public static int LengthOfLineBreak(string text, int position)
        {
            char c = text[position];
            if (c == '\r')
            {
                // \r
                if (++position >= text.Length || text[position] != '\n')
                    return 1;

                // \r\n
                return 2;
            }
            else
            {
                // \n
                // unicode line breaks
                if (c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
                    return 1;

                return 0;
            }
        }

        /// <summary>
        /// Gets length of line break character sequence if any.
        /// </summary>
        /// <remarks>See <see cref="LengthOfLineBreak(string, int)"/>.</remarks>
        public static int LengthOfLineBreak(char[] text, int position)
        {
            char c = text[position];
            if (c == '\r')
            {
                // \r
                if (++position >= text.Length || text[position] != '\n')
                    return 1;

                // \r\n
                return 2;
            }
            else
            {
                // \n
                // unicode line breaks
                if (c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
                    return 1;

                return 0;
            }
        }

        /// <summary>
        /// Gets <see cref="Span"/> of whole <paramref name="line"/>.
        /// </summary>
        /// <param name="lineBreaks">Information about line breaks in the document. Cannot be <c>null</c>.</param>
        /// <param name="line">Line number.</param>
        /// <returns><see cref="Span"/> of line specified by parameter <paramref name="line"/> within the document <paramref name="lineBreaks"/>.</returns>
        public static Span GetLineSpan(this ILineBreaks/*!*/lineBreaks, int line)
        {
            if (lineBreaks == null)
                throw new ArgumentNullException("lineBreaks");

            if (line < 0 || line > lineBreaks.Count)
                throw new ArgumentException("line");

            int start = (line != 0) ? lineBreaks.EndOfLineBreak(line - 1) : 0;
            int end = (line < lineBreaks.Count) ? lineBreaks.EndOfLineBreak(line) : lineBreaks.TextLength;

            return Span.FromBounds(start, end);
        }
    }

    #endregion
}
