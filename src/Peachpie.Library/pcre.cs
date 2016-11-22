using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    /// <summary>
    /// .NET implementation of Perl-Compatible Regular Expressions.
    /// </summary>
    /// <remarks>
    /// You should be aware of limitations of this implementation.
    /// The .NET implementation of PCRE does not provide the same behavior, the notes will be updated.
    /// </remarks>
    public static class PCRE
    {
        #region Constants

        /// <summary>
        /// Orders results so that
        /// $matches[0] is an array of full pattern matches,
        /// $matches[1] is an array of strings matched by the first parenthesized subpattern,
        /// and so on.
        /// 
        /// This flag is only used with preg_match_all().	
        /// </summary>
        public const int PREG_PATTERN_ORDER = 1;

        /// <summary>
        /// Orders results so that
        /// $matches[0] is an array of first set of matches,
        /// $matches[1] is an array of second set of matches,
        /// and so on.
        /// 
        /// This flag is only used with preg_match_all().	
        /// </summary>
        public const int PREG_SET_ORDER = 2;

        /// <summary>
        /// <see cref="PREG_SPLIT_OFFSET_CAPTURE"/>.
        /// </summary>
        public const int PREG_OFFSET_CAPTURE = 256;

        /// <summary>
        /// This flag tells preg_split() to return only non-empty pieces.
        /// </summary>
        public const int PREG_SPLIT_NO_EMPTY = 1;

        /// <summary>
        /// This flag tells preg_split() to capture parenthesized expression in the delimiter pattern as well.
        /// </summary>
        public const int PREG_SPLIT_DELIM_CAPTURE = 2;

        /// <summary>
        /// If this flag is set, for every occurring match the appendant string offset will also be returned.
        /// Note that this changes the return values in an array where every element is an array consisting of the matched string at offset 0 and
        /// its string offset within subject at offset 1.
        /// This flag is only used for preg_split().	
        /// </summary>
        public const int PREG_SPLIT_OFFSET_CAPTURE = 4;

        public const int PREG_REPLACE_EVAL = 1;

        public const int PREG_GREP_INVERT = 1;

        public const int PREG_NO_ERROR = 0;
        public const int PREG_INTERNAL_ERROR = 1;
        public const int PREG_BACKTRACK_LIMIT_ERROR = 2;
        public const int PREG_RECURSION_LIMIT_ERROR = 3;
        public const int PREG_BAD_UTF8_ERROR = 4;
        public const int PREG_BAD_UTF8_OFFSET_ERROR = 5;
        public const int PREG_JIT_STACKLIMIT_ERROR = 6;

        /// <summary>PCRE version and release date</summary>
        public const string PCRE_VERSION = "7.0 .NET";

        #endregion

        #region Function stubs

        public static int preg_last_error()
        {
            return 0;
        }

        /// <summary>
        /// Return array entries that match the pattern.
        /// </summary>
        /// <param name="ctx">Current context. Cannot be <c>null</c>.</param>
        /// <param name="pattern">The pattern to search for.</param>
        /// <param name="input">The input array.</param>
        /// <param name="flags">If set to <see cref="PREG_GREP_INVERT"/>, this function returns the elements of the input array that do not match the given pattern.</param>
        /// <returns>Returns an array indexed using the keys from the input array.</returns>
        [return: CastToFalse]
        public static PhpArray preg_grep(Context ctx, string pattern, PhpArray input, int flags = 0)
        {
            if (input == null)
            {
                return null;
            }

            var result = new PhpArray(input.Count);

            if (input.Count != 0)
            {
                var regex = new PerlRegex.Regex(pattern);

                var enumerator = input.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    var str = enumerator.CurrentValue.ToStringOrThrow(ctx);
                    var m = regex.Match(str);

                    // move a copy to return array if success and not invert or
                    // not success and invert
                    if (m.Success ^ (flags & PREG_GREP_INVERT) != 0)
                    {
                        result.Add(enumerator.CurrentKey, enumerator.CurrentValue.DeepCopy());
                    }
                }
            }

            //
            return result;
        }

        public static PhpValue preg_replace(Context ctx, PhpValue pattern, PhpValue replacement, PhpValue subject, int limit = -1)
        {
            long count;
            return preg_replace(ctx, pattern, replacement, subject, limit, out count);
        }

        /// <summary>
        /// Perform a regular expression search and replace.
        /// </summary>
        /// <param name="ctx">A reference to current context. Cannot be <c>null</c>.</param>
        /// <param name="pattern">The pattern to search for. It can be either a string or an array with strings.</param>
        /// <param name="replacement">The string or an array with strings to replace.
        /// If this parameter is a string and the pattern parameter is an array, all patterns will be
        /// replaced by that string. If both pattern and replacement parameters are arrays, each pattern will be
        /// replaced by the replacement counterpart. If there are fewer elements in the replacement array than
        /// in the pattern array, any extra patterns will be replaced by an empty string.</param>
        /// <param name="subject">The string or an array with strings to search and replace.
        /// If subject is an array, then the search and replace is performed on every entry of subject, and the return value is an array as well.</param>
        /// <param name="limit">The maximum possible replacements for each pattern in each subject string. Defaults to <c>-1</c> (no limit).</param>
        /// <param name="count">This variable will be filled with the number of replacements done.</param>
        /// <returns></returns>
        public static PhpValue preg_replace(Context ctx, PhpValue pattern, PhpValue replacement, PhpValue subject, int limit, out long count)
        {
            count = 0;

            // PHP's behaviour for undocumented limit range
            if (limit < -1)
            {
                limit = 0;
            }

            //
            var replacement_array = replacement.AsArray();
            var pattern_array = pattern.AsArray();

            if (pattern_array == null)
            {
                if (replacement_array == null)
                {
                    // string pattern
                    // string replacement

                    return preg_replace(ctx, pattern.ToStringOrThrow(ctx), replacement.ToStringOrThrow(ctx), null, subject, limit, ref count);
                }
                else
                {
                    // string pattern and array replacement not allowed:
                    throw new ArgumentException("replacement_array_pattern_not", nameof(replacement));
                    // return PhpValue.Null;
                }
            }
            else if (replacement_array == null)
            {
                // array  pattern
                // string replacement
            }
            else
            {
                // array pattern
                // array replacement
            }

            throw new NotImplementedException();
        }

        static PhpValue preg_replace(Context ctx, string pattern, string replacement, PhpCallable callback, PhpValue subject, int limit, ref long count)
        {
            var regex = new PerlRegex.Regex(pattern);

            // TODO: count
            // TODO: callback

            var subject_array = subject.AsArray();
            if (subject_array == null)
            {
                return PhpValue.Create(regex.Replace(subject.ToStringOrThrow(ctx), replacement, limit));
            }
            else
            {
                var arr = new PhpArray(subject_array, false);
                var enumerator = arr.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    var newvalue = regex.Replace(enumerator.CurrentValue.ToStringOrThrow(ctx), replacement, limit);
                    enumerator.CurrentValue = PhpValue.Create(newvalue);
                }

                return PhpValue.Create(arr);
            }
        }

        [return: CastToFalse]
        public static int preg_match_all(Context ctx, string pattern, string subject)
        {
            PhpArray matches;
            return preg_match_all(ctx, pattern, subject, out matches);
        }

        /// <summary>
        /// Perform a global regular expression match.
        /// </summary>
        [return: CastToFalse]
        public static int preg_match_all(Context ctx, string pattern, string subject, out PhpArray matches, int flags = PREG_PATTERN_ORDER, int offset = 0)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Perform a regular expression match.
        /// </summary>
        [return: CastToFalse]
        public static int preg_match(Context ctx, string pattern, string subject)
        {
            var regex = new PerlRegex.Regex(pattern);
            return regex.Match(subject).Success ? 1 : 0;
        }

        /// <summary>
        /// Perform a regular expression match.
        /// </summary>
        [return: CastToFalse]
        public static int preg_match(Context ctx, string pattern, string subject, out PhpArray matches, int flags = 0, long offset = 0)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Quote regular expression characters.
        /// </summary>
        /// <remarks>
        /// The special regular expression characters are: . \ + * ? [ ^ ] $ ( ) { } = ! &lt; &gt; | : -
        /// Note that / is not a special regular expression character.
        /// </remarks>
        /// <param name="str">The string to be escaped.</param>
        /// <param name="delimiter">If the optional delimiter is specified, it will also be escaped.
        /// This is useful for escaping the delimiter that is required by the PCRE functions. The / is the most commonly used delimiter.</param>
        public static string preg_quote(string str, string delimiter = null)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            char delimiterChar = string.IsNullOrEmpty(delimiter)
                ? char.MaxValue // unused (?)
                : delimiter[0];

            StringBuilder result = null;
            int lastEscape = 0;

            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                bool escape = ch == delimiterChar || PerlRegex.RegexParser.IsDelimiterChar(ch);

                if (escape)
                {
                    if (result == null)
                    {
                        result = new StringBuilder(str.Length + 4);
                    }

                    result.Append(str, lastEscape, i - lastEscape);
                    result.Append('\\');
                    lastEscape = i;
                }
            }

            if (result != null)
            {
                result.Append(str, lastEscape, str.Length - lastEscape);
                return result.ToString();
            }
            else
            {
                return str;
            }
        }

        /// <summary>
        /// Split string by a regular expression.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray preg_split(string pattern, string subject, int limit = -1, int flags = 0)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return array entries that match the pattern.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray preg_grep(string pattern, PhpArray input, int flags = 0)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
