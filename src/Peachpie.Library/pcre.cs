using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Library.Resources;
using PerlRegex = Peachpie.Library.RegularExpressions;
using Peachpie.Library.RegularExpressions;
using System.Buffers;

namespace Pchp.Library
{
    /// <summary>
    /// .NET implementation of Perl-Compatible Regular Expressions.
    /// </summary>
    /// <remarks>
    /// You should be aware of limitations of this implementation.
    /// The .NET implementation of PCRE does not provide the same behavior, the notes will be updated.
    /// </remarks>
    [PhpExtension("pcre")]
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
        /// Unmatched subpatterns are reported as <c>NULL</c>, otherwise they are reported as an empty string.
        /// </summary>
        public const int PREG_UNMATCHED_AS_NULL = 512;

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

        public const int PCRE_VERSION_MAJOR = 10;
        public const int PCRE_VERSION_MINOR = 33;

        /// <summary>PCRE version and release date</summary>
        public static string PCRE_VERSION => $"{PCRE_VERSION_MAJOR}.{PCRE_VERSION_MINOR} .NET";

        public const bool PCRE_JIT_SUPPORT = true;

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
                PhpException.ArgumentNull(nameof(input));
                return null;
            }

            var result = new PhpArray(input.Count);

            if (TryParseRegexp(pattern, out var regex))
            {
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

        public static PhpValue preg_replace(Context ctx, PhpValue pattern, PhpValue replacement, PhpValue subject, long limit = -1)
            => preg_replace(ctx, pattern, replacement, subject, limit, out _);

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
        public static PhpValue preg_replace(Context ctx, PhpValue pattern, PhpValue replacement, PhpValue subject, long limit, out long count)
        {
            return PregReplaceInternal(ctx, PreparePatternArray(ctx, pattern, replacement), subject, limit, out count, filter: false);
        }

        /// <summary>
        /// Perform a regular expression search and replace.
        /// </summary>
        public static PhpValue preg_filter(Context ctx, PhpValue pattern, PhpValue replacement, PhpValue subject, int limit = -1)
        {
            return preg_filter(ctx, pattern, replacement, subject, limit, out _);
        }

        /// <summary>
        /// Perform a regular expression search and replace.
        /// </summary>
        public static PhpValue preg_filter(Context ctx, PhpValue pattern, PhpValue replacement, PhpValue subject, int limit, out long count)
        {
            return PregReplaceInternal(ctx, PreparePatternArray(ctx, pattern, replacement), subject, limit, out count, filter: true);
        }

        /// <summary>
        /// Helper value representing a regular expression with its replacement expression.
        /// </summary>
        readonly struct PatternAndReplacement
        {
            public Regex Regex { get; }
            public string Replacement { get; }

            /// <summary>
            /// If set, <see cref="Replacement"/> is not used.
            /// </summary>
            public MatchEvaluator Evaluator { get; }

            public PhpString Replace(Context ctx, PhpString subject, int limit, ref long count)
            {
                var oldcount = count;
                var newvalue = Evaluator != null
                    ? Regex.Replace(subject.ToString(ctx), Evaluator, (int)limit, ref count)
                    : Regex.Replace(subject.ToString(ctx), Replacement, (int)limit, ref count);

                if (oldcount != count)
                {
                    subject = newvalue; // NOTE: possible corruption of 8bit string
                }
                else
                {
                    // - workaround for https://github.com/peachpiecompiler/peachpie/issues/178
                    // - use the original value if possible
                }

                return subject;
            }

            public PatternAndReplacement(Regex regex, string replacement)
            {
                this.Regex = regex;
                this.Replacement = replacement;
                this.Evaluator = null;
            }

            public PatternAndReplacement(Regex regex, MatchEvaluator evaluator)
            {
                this.Regex = regex;
                this.Replacement = null;
                this.Evaluator = evaluator;
            }
        }

        static PatternAndReplacement[] PreparePatternArray(Context ctx, PhpValue pattern, PhpValue replacement)
        {
            if (pattern.IsPhpArray(out var pattern_array))
            {
                var regexes = new PatternAndReplacement[pattern_array.Count];

                if (replacement.IsPhpArray(out var replacement_array))
                {
                    int i = 0;
                    var pattern_enumerator = pattern_array.GetFastEnumerator();
                    var replacement_enumerator = replacement_array.GetFastEnumerator();
                    bool replacement_valid = true;

                    while (pattern_enumerator.MoveNext())
                    {
                        string replacement_string;

                        // replacements are in array, move to next item and take it if possible, in other case take empty string:
                        if (replacement_valid && replacement_enumerator.MoveNext())
                        {
                            replacement_string = replacement_enumerator.CurrentValue.ToStringOrThrow(ctx);
                        }
                        else
                        {
                            replacement_string = string.Empty;
                            replacement_valid = false;  // end of replacement_enumerator, do not call MoveNext again
                        }

                        //
                        if (TryParseRegexp(pattern_enumerator.CurrentValue.ToStringOrThrow(ctx), out var regex))
                        {
                            regexes[i++] = new PatternAndReplacement(regex, replacement_string);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    // array pattern
                    // string replacement

                    var replacement_string = replacement.ToStringOrThrow(ctx);

                    int i = 0;
                    var pattern_enumerator = pattern_array.GetFastEnumerator();
                    while (pattern_enumerator.MoveNext())
                    {
                        if (TryParseRegexp(pattern_enumerator.CurrentValue.ToStringOrThrow(ctx), out var regex))
                        {
                            regexes[i++] = new PatternAndReplacement(regex, replacement_string);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                return regexes;
            }
            else
            {
                if (replacement.IsPhpArray(out _))
                {
                    // string pattern and array replacement not allowed:
                    // Parameter mismatch, pattern is a string while replacement is an array
                    PhpException.InvalidArgument(nameof(replacement), LibResources.replacement_array_pattern_not);
                    return null;
                }

                // string pattern
                // string replacement
                if (TryParseRegexp(pattern.ToStringOrThrow(ctx), out var regex))
                {
                    return new PatternAndReplacement[]
                    {
                        new PatternAndReplacement(regex, replacement.ToStringOrThrow(ctx))
                    };
                }
                else
                {
                    return null;
                }
            }
        }

        static PhpValue PregReplaceInternal(Context ctx, PatternAndReplacement[] regexes, PhpValue subject, long limit, out long count, bool filter)
        {
            count = 0;

            // PHP's behaviour for undocumented limit range
            if (limit < -1)
            {
                limit = 0;
            }

            if (regexes == null)
            {
                // NOTE: PHP returns FALSE when pattern is string and replacement is array which is wrong according to manual
                return PhpValue.Null;
            }

            // enumerate subjects first, then patterns and replacements
            if (subject.IsPhpArray(out var subject_array))
            {
                // returning PhpArray
                var arr = new PhpArray(subject_array.Count);
                var s = subject_array.GetFastEnumerator();
                while (s.MoveNext())
                {
                    var oldcount = count;
                    var newvalue = PregReplaceInternal(ctx, regexes, s.CurrentValue.ToPhpString(ctx), limit, ref count);

                    if (filter && oldcount == count)
                    {
                        continue;
                    }

                    arr[s.CurrentKey] = newvalue.DeepCopy();
                }

                return arr;
            }
            else
            {
                var newvalue = PregReplaceInternal(ctx, regexes, subject.ToPhpString(ctx), limit, ref count);

                if (filter && count == 0)
                {
                    return PhpValue.Null;
                }

                return newvalue;
            }
        }

        static PhpString PregReplaceInternal(Context ctx, PatternAndReplacement[] regexes, PhpString subject, long limit, ref long count)
        {
            for (int i = 0; i < regexes.Length; i++)
            {
                subject = regexes[i].Replace(ctx, subject, (int)limit, ref count);
            }

            //
            return subject;
        }

        static PatternAndReplacement[] PreparePatternArray(Context ctx, PhpValue pattern, IPhpCallable callback)
        {
            if (callback == null)
            {
                PhpException.ArgumentNull(nameof(callback));
                return null;
            }

            var evaluator = new MatchEvaluator(match =>
            {
                var matches_arr = new PhpArray();
                GroupsToPhpArray(match.PcreGroups, false, false, matches_arr);

                return callback
                    .Invoke(ctx, (PhpValue)matches_arr)
                    .ToStringOrThrow(ctx);
            });

            if (pattern.IsPhpArray(out var pattern_array))
            {
                var regexes = new PatternAndReplacement[pattern_array.Count];

                // array pattern
                int i = 0;
                var pattern_enumerator = pattern_array.GetFastEnumerator();
                while (pattern_enumerator.MoveNext())
                {
                    if (TryParseRegexp(pattern_enumerator.CurrentValue.ToStringOrThrow(ctx), out var regex))
                    {
                        regexes[i++] = new PatternAndReplacement(regex, evaluator);
                    }
                    else
                    {
                        return null;
                    }
                }

                return regexes;
            }
            else
            {
                // string pattern
                if (TryParseRegexp(pattern.ToStringOrThrow(ctx), out var regex))
                {
                    return new PatternAndReplacement[]
                    {
                        new PatternAndReplacement(regex, evaluator)
                    };
                }
                else
                {
                    return null;
                }
            }
        }

        public static PhpValue preg_replace_callback(Context ctx, PhpValue pattern, IPhpCallable callback, PhpValue subject, long limit = -1)
        {
            long count = 0;
            return preg_replace_callback(ctx, pattern, callback, subject, limit, ref count);
        }

        public static PhpValue preg_replace_callback(Context ctx, PhpValue pattern, IPhpCallable callback, PhpValue subject, long limit, ref long count)
        {
            return PregReplaceInternal(ctx, PreparePatternArray(ctx, pattern, callback), subject, limit, out count, filter: false);
        }

        static PatternAndReplacement[] PreparePatternArray(Context ctx, PhpArray patterns_and_callbacks)
        {
            if (patterns_and_callbacks == null)
            {
                PhpException.ArgumentNull(nameof(patterns_and_callbacks));
                return null;
            }

            var regexes = new PatternAndReplacement[patterns_and_callbacks.Count];

            int i = 0;
            var enumerator = patterns_and_callbacks.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                var pattern = enumerator.CurrentKey.String;
                var callback = enumerator.CurrentValue.AsCallable();

                if (TryParseRegexp(pattern, out var regex))
                {
                    var evaluator = new MatchEvaluator(match =>
                    {
                        var matches_arr = new PhpArray();
                        GroupsToPhpArray(match.PcreGroups, false, false, matches_arr);

                        return callback
                            .Invoke(ctx, (PhpValue)matches_arr)
                            .ToStringOrThrow(ctx);
                    });

                    regexes[i++] = new PatternAndReplacement(regex, evaluator);
                }
                else
                {
                    return null;
                }
            }

            //
            return regexes;
        }

        public static PhpValue preg_replace_callback_array(Context ctx, PhpArray patterns_and_callbacks, PhpValue subject, long limit = -1)
        {
            return preg_replace_callback_array(ctx, patterns_and_callbacks, subject, limit, out _);
        }

        /// <summary>
        /// Perform a regular expression search and replace using callbacks.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="patterns_and_callbacks">An associative array mapping patterns (keys) to callbacks (values).</param>
        /// <param name="subject">The string or an array with strings to search and replace.</param>
        /// <param name="limit">The maximum possible replacements for each pattern in each subject string. Defaults to -1 (no limit).</param>
        /// <param name="count">If specified, this variable will be filled with the number of replacements done.</param>
        /// <returns>
        /// preg_replace_callback_array() returns an array if the subject parameter is an array, or a string otherwise. On errors the return value is NULL.
        /// If matches are found, the new subject will be returned, otherwise subject will be returned unchanged.
        /// </returns>
        public static PhpValue preg_replace_callback_array(Context ctx, PhpArray patterns_and_callbacks, PhpValue subject, long limit, out long count)
        {
            return PregReplaceInternal(ctx, PreparePatternArray(ctx, patterns_and_callbacks), subject, limit, out count, filter: false);
        }

        [return: CastToFalse]
        public static int preg_match_all(string pattern, string subject)
        {
            return preg_match_all(pattern, subject, out _);
        }

        /// <summary>
        /// Perform a global regular expression match.
        /// </summary>
        [return: CastToFalse]
        public static int preg_match_all(string pattern, string subject, out PhpArray matches, int flags = PREG_PATTERN_ORDER, int offset = 0)
        {
            return Match(pattern, subject, out matches, flags, offset, true);
        }

        /// <summary>
        /// Perform a regular expression match.
        /// </summary>
        [return: CastToFalse]
        public static int preg_match(string pattern, string subject)
        {
            return TryParseRegexp(pattern, out var regex) && regex.Match(subject ?? string.Empty).Success ? 1 : 0;
        }

        /// <summary>
        /// Perform a regular expression match.
        /// </summary>
        [return: CastToFalse]
        public static int preg_match(string pattern, string subject, out PhpArray matches, int flags = 0, long offset = 0)
        {
            return Match(pattern, subject, out matches, flags, offset, false);
        }

        /// <summary>
        /// Perform a regular expression match.
        /// </summary>
        static int Match(string pattern, string subject, out PhpArray matches, int flags, long offset, bool matchAll)
        {
            if (!TryParseRegexp(pattern, out var regex))
            {
                matches = PhpArray.NewEmpty();
                return -1;
            }

            subject = subject ?? string.Empty;

            var m = regex.Match(subject, (offset < subject.Length) ? (int)offset : subject.Length);

            if ((regex.Options & PerlRegex.RegexOptions.PCRE_ANCHORED) != 0 && m.Success && m.Index != offset)
            {
                matches = PhpArray.NewEmpty();
                return -1;
            }

            if (m.Success)
            {
                if (!matchAll || (flags & PREG_PATTERN_ORDER) != 0)
                {
                    matches = new PhpArray(m.Groups.Count);
                }
                else
                {
                    matches = new PhpArray();
                }

                if (!matchAll)
                {
                    GroupsToPhpArray(m.PcreGroups, (flags & PREG_OFFSET_CAPTURE) != 0, (flags & PREG_UNMATCHED_AS_NULL) != 0, matches);
                    return 1;
                }

                // store all other matches in PhpArray matches
                if ((flags & PREG_SET_ORDER) != 0) // cannot test PatternOrder, it is 0, SetOrder must be tested
                    return FillMatchesArrayAllSetOrder(regex, m, ref matches, (flags & PREG_OFFSET_CAPTURE) != 0);
                else
                    return FillMatchesArrayAllPatternOrder(regex, m, ref matches, (flags & PREG_OFFSET_CAPTURE) != 0);
            }

            // no match has been found
            if (matchAll && (flags & PREG_SET_ORDER) == 0)
            {
                // in that case PHP returns an array filled with empty arrays according to parentheses count
                matches = new PhpArray(m.Groups.Count);
                for (int i = 0; i < regex.GetGroupNumbers().Length; i++)
                {
                    AddGroupNameToResult(regex, matches, i, (ms, groupName) =>
                    {
                        ms[groupName] = new PhpArray();
                    });

                    matches[i] = new PhpArray();
                }
            }
            else
            {
                matches = PhpArray.NewEmpty(); // empty array
            }

            return 0;
        }

        static bool IsDelimiterChar(char ch)
        {
            switch (ch)
            {
                case '\\':
                case '+':
                case '*':
                case '?':
                case '[':
                case '^':
                case ']':
                case '$':
                case '(':
                case ')':
                case '{':
                case '}':
                case '=':
                case '!':
                case '<':
                case '>':
                case '|':
                case ':':
                case '.':
                    return true;

                default:
                    return false;
            }
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
                bool escape = ch == delimiterChar || IsDelimiterChar(ch);

                if (escape)
                {
                    if (result == null)
                    {
                        result = StringBuilderUtilities.Pool.Get();
                    }

                    result.Append(str, lastEscape, i - lastEscape);
                    result.Append('\\');
                    lastEscape = i;
                }
            }

            if (result != null)
            {
                result.Append(str, lastEscape, str.Length - lastEscape);
                return StringBuilderUtilities.GetStringAndReturn(result);
            }
            else
            {
                return str;
            }
        }

        /// <summary>
        /// Splits <paramref name="subject"/> along boundaries matched by <paramref name="pattern"/> and returns an array containing substrings.
        /// 
        /// <paramref name="limit"/> specifies the maximum number of strings returned in the resulting array. If (limit-1) matches is found
        /// and there remain some characters to match whole remaining string is returned as the last element of the array.
        /// 
        /// Some flags may be specified. <see cref="PREG_SPLIT_NO_EMPTY"/> means no empty strings will be
        /// in the resulting array. <see cref="PREG_SPLIT_DELIM_CAPTURE"/> adds also substrings matching
        /// the delimiter and <see cref="PREG_SPLIT_OFFSET_CAPTURE"/> returns instead substrings the arrays
        /// containing appropriate substring at index 0 and the offset of this substring in original
        /// <paramref name="subject"/> at index 1.
        /// </summary>
        /// <param name="pattern">Regular expression to match to boundaries.</param>
        /// <param name="subject">String or string of bytes to split.</param>
        /// <param name="limit">Max number of elements in the resulting array.</param>
        /// <param name="flags">Flags affecting the returned array.</param>
        /// <returns>An array containing substrings.</returns>
        [return: CastToFalse]
        public static PhpArray preg_split(string pattern, string subject, int limit = -1, int flags = 0)
        {
            if (limit == 0) // 0 does not make sense, php's behavior is as it is -1
                limit = -1;
            if (limit < -1) // for all other negative values it seems that is as limit == 1
                limit = 1;

            if (!TryParseRegexp(pattern ?? string.Empty, out var regex))
            {
                return null; // FALSE
            }

            subject = subject ?? string.Empty;

            var m = regex.Match(subject);

            bool offset_capture = (flags & PREG_SPLIT_OFFSET_CAPTURE) != 0;
            var result = new PhpArray();
            int last_index = 0;

            while (m.Success && (limit == -1 || --limit > 0) && last_index < subject.Length)
            {
                // add part before match
                int length = m.Index - last_index;
                if (length > 0 || (flags & PREG_SPLIT_NO_EMPTY) == 0)
                    result.Add(NewArrayItem(subject.Substring(last_index, length), last_index, offset_capture));

                if (m.Value.Length > 0)
                {
                    if ((flags & PREG_SPLIT_DELIM_CAPTURE) != 0) // add all captures but not whole pattern match (start at 1)
                    {
                        List<object> lastUnsucessfulGroups = null;  // value of groups that was not successful since last succesful one
                        for (int i = 1; i < m.Groups.Count; i++)
                        {
                            var g = m.Groups[i];
                            if (g.Length > 0 || (flags & PREG_SPLIT_NO_EMPTY) == 0)
                            {
                                // the value to be added into the result:
                                object value = NewArrayItem(g.Value, g.Index, offset_capture);

                                if (g.Success)
                                {
                                    // group {i} was matched:
                                    // if there was some unsuccesfull matches before, add them now:
                                    if (lastUnsucessfulGroups != null && lastUnsucessfulGroups.Count > 0)
                                    {
                                        foreach (var x in lastUnsucessfulGroups)
                                            result.Add(x);
                                        lastUnsucessfulGroups.Clear();
                                    }
                                    // add the matched group:
                                    result.Add(value);
                                }
                                else
                                {
                                    // The match was unsuccesful, remember all the unsuccesful matches
                                    // and add them only if some succesful match will follow.
                                    // In PHP, unsuccessfully matched groups are trimmed by the end
                                    // (regexp processing stops when other groups cannot be matched):
                                    if (lastUnsucessfulGroups == null) lastUnsucessfulGroups = new List<object>();
                                    lastUnsucessfulGroups.Add(value);
                                }
                            }
                        }
                    }

                    last_index = m.Index + m.Length;
                }
                else // regular expression match an empty string => add one character
                {
                    // always not empty
                    result.Add(NewArrayItem(subject.Substring(last_index, 1), last_index, offset_capture));
                    last_index++;
                }

                m = m.NextMatch();
            }

            // add remaining string (might be empty)
            if (last_index < subject.Length || (flags & PREG_SPLIT_NO_EMPTY) == 0)
                result.Add(NewArrayItem(subject.Substring(last_index), last_index, offset_capture));

            return result;
        }

        #endregion

        static bool TryParseRegexp(string pattern, out PerlRegex.Regex regex)
        {
            try
            {
                regex = new PerlRegex.Regex(pattern);
            }
            catch (PerlRegex.RegexParseException error)
            {
                PhpException.Throw(
                    PhpError.Warning,
                    error.Offset > 0 // .HasValue
                        ? string.Format(LibResources.pcre_make_error, error.Message, error.Offset.ToString())
                        : error.Message
                );

                regex = null;
            }

            //
            return regex != null;
        }

        static void AddGroupNameToResult(PerlRegex.Regex regex, PhpArray matches, int i, Action<PhpArray, string> action)
        {
            var groupName = GetGroupName(regex, i);
            if (!string.IsNullOrEmpty(groupName))
            {
                action(matches, groupName);
            }
        }

        /// <summary>
        /// Goes through <paramref name="m"/> matches and fill <paramref name="matches"/> array with results
        /// according to Pattern Order.
        /// </summary>
        /// <param name="r"><see cref="Regex"/> that produced the match</param>
        /// <param name="m"><see cref="Match"/> to iterate through all matches by NextMatch() call.</param>
        /// <param name="matches">Array for storing results.</param>
        /// <param name="addOffsets">Whether or not add arrays with offsets instead of strings.</param>
        /// <returns>Number of full pattern matches.</returns>
        static int FillMatchesArrayAllPatternOrder(PerlRegex.Regex r, PerlRegex.Match m, ref PhpArray matches, bool addOffsets)
        {
            // second index, increases at each match in pattern order
            int j = 0;
            while (m.Success)
            {
                // add all groups
                for (int i = 0; i < m.Groups.Count; i++)
                {
                    var arr = NewArrayItem(m.Groups[i].Value, m.Groups[i].Index, addOffsets);

                    AddGroupNameToResult(r, matches, i, (ms, groupName) =>
                    {
                        if (!ms.TryGetValue(groupName, out var groupValue) || !groupValue.IsPhpArray(out var group))
                        {
                            ms[groupName] = group = new PhpArray();
                        }

                        group[j] = arr; // TODO: DeepCopy ?
                    });

                    if (!matches.TryGetValue(i, out var groupValue) || !groupValue.IsPhpArray(out var group))
                    {
                        matches[i] = group = new PhpArray();
                    }

                    group[j] = arr;
                }

                j++;
                m = m.NextMatch();
            }

            return j;
        }

        /// <summary>
        /// Goes through <paramref name="m"/> matches and fill <paramref name="matches"/> array with results
        /// according to Set Order.
        /// </summary>
        /// <param name="r"><see cref="Regex"/> that produced the match</param>
        /// <param name="m"><see cref="Match"/> to iterate through all matches by NextMatch() call.</param>
        /// <param name="matches">Array for storing results.</param>
        /// <param name="addOffsets">Whether or not add arrays with offsets instead of strings.</param>
        /// <returns>Number of full pattern matches.</returns>
        static int FillMatchesArrayAllSetOrder(PerlRegex.Regex r, PerlRegex.Match m, ref PhpArray matches, bool addOffsets)
        {
            // first index, increases at each match in set order
            int i = 0;

            while (m.Success)
            {
                var pa = new PhpArray(m.Groups.Count);

                // add all groups
                for (int j = 0; j < m.Groups.Count; j++)
                {
                    var arr = NewArrayItem(m.Groups[j].Value, m.Groups[j].Index, addOffsets);

                    AddGroupNameToResult(r, pa, j, (p, groupName) =>
                    {
                        p[groupName] = arr;
                    });

                    pa[j] = arr;
                }

                matches[i] = pa;
                i++;
                m = m.NextMatch();
            }

            return i;
        }

        static int GetLastSuccessfulGroup(PerlRegex.GroupCollection/*!*/ groups)
        {
            Debug.Assert(groups != null);

            for (int i = groups.Count - 1; i >= 0; i--)
            {
                if (groups[i].Success)
                    return i;
            }

            return -1;
        }

        static string GetGroupName(PerlRegex.Regex regex, int index)
        {
            var name = regex.GroupNameFromNumber(index);

            // anonymous groups and indexed groups:
            if (string.IsNullOrEmpty(name) || name.Equals(index.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat)))
            {
                name = null;
            }

            return name;
        }

        /// <summary>
        /// Used for handling Offset Capture flags. Returns just <paramref name="item"/> if
        /// <paramref name="offsetCapture"/> is <B>false</B> or an <see cref="PhpArray"/> containing
        /// <paramref name="item"/> at index 0 and <paramref name="index"/> at index 1.
        /// </summary>
        /// <param name="item">Item to add to return value.</param>
        /// <param name="index">Index to specify in return value if <paramref name="offsetCapture"/> is
        /// <B>true</B>.</param>
        /// <param name="offsetCapture">Whether or not to make <see cref="PhpArray"/> with item and index.</param>
        /// <returns></returns>
        static PhpValue NewArrayItem(string item, int index, bool offsetCapture)
        {
            if (offsetCapture)
            {
                return new PhpArray(2)
                {
                    item,
                    index
                };
            }
            else
            {
                return item;
            }
        }

        static void GroupsToPhpArray(PerlRegex.PcreGroupCollection groups, bool offsetCapture, bool unmatchedAsNull, PhpArray result)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                var value = (g.Success || !unmatchedAsNull) ? g.Value : null;
                var item = NewArrayItem(value, g.Index, offsetCapture);

                // All groups should be named.
                if (g.IsNamedGroup)
                {
                    result[g.Name] = item.DeepCopy();
                }

                result[i] = item;
            }
        }
    }
}
