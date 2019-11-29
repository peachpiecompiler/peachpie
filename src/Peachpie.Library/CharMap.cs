using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    /// <summary>
	/// Bitmap representig a set of Unicode characters.
	/// </summary>
	internal sealed class CharMap   // GENERICS
    {
        /// <summary>
        /// The bitmap.
        /// </summary>
        private uint[] flags;

        /// <summary>
        /// The index of the last integer in <see cref="flags"/> which has at least one bit ever set.
        /// </summary>
        private int lastDirty;

        /// <summary>
        /// Creates a new instance of <see cref="CharMap"/>.
        /// </summary>
        /// <param name="size">The number of characters to be mapped.</param>
        public CharMap(int size)
        {
            this.flags = new uint[size / 32];
            this.lastDirty = -1;
        }

        /// <summary>
        /// Creates a new instance of <see cref="CharMap"/> based on specified map.
        /// </summary>
        /// <param name="map">The bits used for character map.</param>
        public CharMap(uint[] map)
        {
            flags = map;
            int i = map.Length - 1;
            while (i >= 0 && flags[i] == 0) i--;
            lastDirty = i;
        }

        /// <summary>
        /// Retrieves whether a character belongs to the set.
        /// </summary>
        /// <param name="c">The character to be tested.</param>
        /// <returns>Whether <paramref name="c"/> is element of the set.</returns>
        /// <exception cref="IndexOutOfRangeException"><paramref name="c"/> is not mapped by this instance.</exception>
        public bool Contains(char c)
        {
            int div = c >> 5;
            return div <= lastDirty && (flags[div] & (0x80000000U >> (c & 0x1f))) != 0;
        }

        /// <summary>
        /// Adds character to the set.
        /// </summary>
        /// <param name="c">The character to be added.</param>
        /// <exception cref="IndexOutOfRangeException"><paramref name="c"/> is not mapped by this instance.</exception>
        public void Add(char c)
        {
            int div = c >> 5;
            flags[div] |= 0x80000000U >> (c & 0x1f);
            if (div > lastDirty)
                lastDirty = div;
        }

        /// <summary>
        /// Adds all characters contained in a specified string.
        /// </summary>
        /// <param name="str">The string which characters to add. Can be a <B>null</B> reference.</param>
        /// <exception cref="IndexOutOfRangeException">Some character within <paramref name="str"/> is not mapped by this instance.</exception>
        public void Add(string str)
        {
            if (str == null) return;

            for (int i = 0; i < str.Length; i++)
                Add(str[i]);
        }

        /// <summary>
        /// Removes character from the set.
        /// </summary>
        /// <param name="c">The character to be removed.</param>
        /// <exception cref="IndexOutOfRangeException"><paramref name="c"/> is not mapped by this instance.</exception>
        public void Remove(char c)
        {
            flags[c >> 5] &= ~(0x80000000U >> (c & 0x1f));
        }

        /// <summary>
        /// Adds a range of characters to the set.
        /// </summary>
        /// <param name="first">The lower bound of the range.</param>
        /// <param name="last">The upper bound of the range.</param>
        /// <exception cref="IndexOutOfRangeException"><paramref name="first"/> or <paramref name="last"/> are not mapped by this instance.</exception>
        /// <exception cref="ArgumentException">The <paramref name="first"/> is less than the <paramref name="last"/>.</exception>
        public void AddRange(char first, char last)
        {
            if (first >= last)
                //throw new ArgumentException(CoreResources.GetString("last_is_less_than_first"));
                throw new ArgumentException();

            int modf = first & 0x1f;
            int modl = last & 0x1f;
            int f = first >> 5;
            int l = last >> 5;

            if (l == f)
            {
                flags[f] |= (0xffffffffU >> (31 - modl + modf)) << (31 - modl);
            }
            else
            {
                // the first block:
                flags[f] |= 0xffffffffU >> modf;

                // the middle blocks (if any):
                for (int i = f + 1; i < l; i++)
                    flags[i] = 0xffffffffU;

                // the last block:
                if (modl < 31)
                    flags[l] |= ~(0xffffffffU >> (modl + 1));
                else
                    flags[l] = 0xffffffffU;
            }
            if (l > lastDirty) lastDirty = l;
        }


        /// <summary>
        /// Removes a range of characters from the set.
        /// </summary>
        /// <param name="first">The lower bound of the range.</param>
        /// <param name="last">The upper bound of the range.</param>
        /// <exception cref="IndexOutOfRangeException"><paramref name="first"/> or <paramref name="last"/> are not mapped by this instance.</exception>
        /// <exception cref="ArgumentException">The <paramref name="first"/> is less than the <paramref name="last"/>.</exception>
        public void RemoveRange(char first, char last)
        {
            if (first > last)
                //throw new ArgumentException(CoreResources.GetString("last_is_less_than_first"));
                throw new ArgumentException();

            int modf = first & 0x1f;
            int modl = last & 0x1f;
            int f = first >> 5;
            int l = last >> 5;

            if (l == f)
            {
                flags[f] &= ~((0xffffffffU >> (31 - modl + modf)) << (31 - modl));
            }
            else
            {
                // the first block:
                flags[f] &= ~(0xffffffffU >> modf);

                // the middle blocks (if any):
                Array.Clear(flags, f + 1, l - f - 1);

                // the last block:
                if (modl < 31)
                    flags[l] &= 0xffffffffU >> (modl + 1);
                else
                    flags[l] = 0U;
            }
        }


        /// <summary>
        /// Clears all bits in the map.
        /// </summary>
        /// <remarks>
        /// Doesn't necessarily lead to clearing the whole map. Clears the map up to the last bit ever set.
        /// </remarks>
        public void ClearAll()
        {
            Array.Clear(flags, 0, lastDirty + 1);
        }

        /// <summary>
        /// Adds characters matching given mask. 
        /// </summary>
        /// <param name="mask">The mask of characters to be added. Any collection with items convertible to the <see cref="char"/> type.</param>
        /// <remarks>
        /// <para>The <paramref name="mask"/> may contain single characters as well as intervals "a..b",
        /// where <I>a</I>, <I>b</I> are characters and <I>a</I> is less than or equal to <I>b</I>.</para>
        /// <para>There are no characters delimiting elements of the mask.</para>
        /// <para>If the mask is not valid as a whole its valid parts are processed.</para>
        /// </remarks>
        /// <example><c>"a..bA..Z0..9"</c> means alphanumeric characters, <c>"a.b..d"</c> means {'a', 'b', 'c', 'd', '.'} etc.</example>
        /// <exception cref="PhpException"><paramref name="mask"/> is not valid mask.</exception>
        /// <exception cref="InvalidCastException">An item of <paramref name="mask"/> is not convertible to character.</exception>
        /// <exception cref="IndexOutOfRangeException">Any character of <paramref name="mask"/> is not mapped by this instance.</exception>
        public void AddUsingMask(string mask)
        {
            if (mask == null) return;

            // implemented by automaton with the following states:
            const int char_empty = 0;     // no character read - initial state and after interval read state
            const int char_read = 1;      // one char read into buffer, no dots read
            const int dot_read = 2;       // one char read into buffer, one dot read
            const int dot_dot_read = 3;   // one char read into buffer, two dots read

            // the interval constructing character:
            const char dot = '.';

            int state = char_empty;       // initial state
            char first = '\0';            // the first character of an interval being processed 
            char last;                    // the last character of an interval being processed

            for (int i = 0; i < mask.Length; i++)
            {
                last = mask[i];
                switch (state)
                {
                    case char_empty:
                        first = last;
                        state = char_read;
                        break;

                    case char_read:
                        if (last != dot)
                        {
                            Add(first);
                            first = last;
                            state = char_read;
                        }
                        else
                            state = dot_read;
                        break;

                    case dot_read:
                        if (last != dot)
                        {
                            if (first == dot)  //eg: "..x" or "x.y"
                                //PhpException.Throw(PhpError.Warning, LibResources.GetString("char_range_no_char_on_left", ".."));
                                throw new NotImplementedException();
                            else
                                Add(first);

                            // The dot will be added and the last char read may be init char of an interval:
                            Add(dot);
                            first = last;
                            state = char_read;

                        }
                        else
                            state = dot_dot_read;
                        break;

                    case dot_dot_read:

                        if (first > last)  //eg: "a..b" or "b..a" 
                        {
                            // the first character will be added and the last char read may be init char of an interval:
                            Add(first);
                            Add(dot);
                            first = last;
                            state = char_read;

                            //PhpException.Throw(PhpError.Warning, LibResources.GetString("char_range_not_incrementing", ".."));
                            throw new NotImplementedException();
                        }
                        else
                        {
                            AddRange(first, last);
                        }

                        state = char_empty;
                        break;

                } //switch

            } //for

            // postprocessing:
            if (state != char_empty) Add(first);
            if (state == dot_read || state == dot_dot_read)
            {
                Add(dot);
                if (state == dot_dot_read)
                    //PhpException.Throw(PhpError.Warning, LibResources.GetString("char_range_no_char_on_right", ".."));
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Adds characters using a mask with specified interval bounds separator.
        /// </summary>
        /// <param name="mask">The mask.</param>
        /// <param name="separator">The separator.</param>
        public void AddUsingMask(string mask, string separator)
        {
            if (separator == null || separator == "")
                throw new ArgumentNullException("separator");

            int i, k, start;

            start = i = 0;

            for (;;)
            {
                while (i < mask.Length && mask[i] != separator[0])
                {
                    Add(mask[i]);
                    i++;
                }

                if (i == mask.Length) break;

                k = 1;
                while (k < separator.Length && i + k < mask.Length && mask[i + k] == separator[k]) k++;

                // entire separator read:
                if (k == separator.Length)
                {
                    // the end of the mask:
                    if (i + k == mask.Length)
                    {
                        for (int j = 0; j < separator.Length; j++)
                            Add(separator[j]);

                        if (i > start)
                            //PhpException.Throw(PhpError.Warning, LibResources.GetString("char_range_no_char_on_right"));
                            throw new NotImplementedException();

                        return;
                    }

                    // interval has its first point behind the starting point:
                    if (i > start)
                    {
                        if (mask[i - 1] > mask[i + k])
                        {
                            Add(mask[i - 1]);
                            //PhpException.Throw(PhpError.Warning, LibResources.GetString("char_range_not_incrementing"));
                            throw new NotImplementedException();
                        }
                        else
                        {
                            AddRange(mask[i - 1], mask[i + k]);
                        }

                        // entire interval has been read, starting from beginning:
                        start = i;
                        i += k + 1;
                    }
                    else
                    {
                        for (int j = 0; j < separator.Length; j++)
                            Add(separator[j]);

                        i += k;

                        //PhpException.Throw(PhpError.Warning, LibResources.GetString("char_range_no_char_on_left"));
                        throw new NotImplementedException();
                    }
                }
                else if (i + k == mask.Length)
                {
                    // part of the separator read:
                    for (int j = 0; j < k; j++)
                        Add(separator[j]);

                    return;
                }
                else
                {
                    Add(separator[0]);
                    i++;
                }
            }
        }

        /// <summary>
        /// Adds character range given a regular-expression like mask. E.g. [a-zA-Z].
        /// </summary>
        /// <param name="mask">The mask.</param>
        /// <param name="start">An index of '[' character in the mask where the range starts.</param>
        /// <param name="end">An index of the last character of the range. Usually the index of ']' character.</param>
        /// <param name="separator">The separator character. Usually '-'.</param>
        public void AddUsingRegularMask(string mask, int start, int end, char separator)
        {
            if (mask == null)
                throw new ArgumentNullException("mask");
            if (start < 0 || start >= mask.Length)
                throw new ArgumentOutOfRangeException("start");
            if (end < 0 || end >= mask.Length || end < start)
                throw new ArgumentOutOfRangeException("end");

            int i = start;
            while (i < end)
            {
                if (mask[i] == separator && i > start && i < end)
                {
                    // separator in between:
                    if (mask[i - 1] < mask[i + 1])
                        AddRange(mask[i - 1], mask[i + 1]);
                    else
                        AddRange(mask[i + 1], mask[i - 1]);
                }
                else
                {
                    // adds an ordinary character:
                    Add(mask[i]);
                }
                i++;
            }
        }

        /// <summary>
        /// Accumulates all characters contained or not contained in the set to the string in ascending order.
        /// </summary>
        /// <param name="first">The lower bound.</param>
        /// <param name="last">The upper bound.</param>
        /// <param name="complement">Whether to return characters not contained in the string.</param>
        /// <returns>
        /// Depending on the value of the <paramref name="complement"/> the method returns the string of characters in
        /// this instance and a complement of this instance, respectively, intersected with the 
        /// [<paramref name="first"/>; <paramref name="last"/>] interval.
        /// </returns>
        /// <exception cref="IndexOutOfRangeException"><paramref name="first"/> or <paramref name="last"/> are not mapped by this instance.</exception>
        public string ToString(char first, char last, bool complement)
        {
            if (first > last)
                //throw new ArgumentException(CoreResources.GetString("last_is_less_than_first"));
                throw new NotImplementedException();

            int modf = first & 0x1f;
            int modl = last & 0x1f;
            int f = first >> 5;
            int l = last >> 5;

            // an optimization:
            if (l > lastDirty && !complement)
            {
                // sets upper bound to the last bit in the lastDirty block:
                l = lastDirty;
                modl = 31;

                // the whole interval is beyond the last set bit:
                if (f > l) return String.Empty;
            }

            // if complementary set is required, we xor each item of the "flags" array by the "invert_equality" 
            // and so invert the result of comparison with zero in the following if statement:
            uint invert_inequality = (complement) ? 0xffffffffU : 0U;
            uint flg;
            char c = first;
            var result = StringBuilderUtilities.Pool.Get();

            if (f == l)
            {
                // the "first" and the "last" points to the same block:
                flg = flags[f] ^ invert_inequality;
                for (uint mask = (0x80000000U >> modf); mask > (0x80000000U >> modl); mask >>= 1)
                {
                    if ((flg & mask) != 0) result.Append(c);
                    c++;
                }
            }
            else
            {
                // the first block:
                flg = flags[f] ^ invert_inequality;
                for (uint mask = 0x80000000U >> modf; mask != 0; mask >>= 1)
                {
                    if ((flg & mask) != 0) result.Append(c);
                    c++;
                }

                // middle blocks (if any):
                for (int i = f + 1; i < l; i++)
                {
                    flg = flags[i] ^ invert_inequality;
                    for (uint mask = 0x80000000U; mask != 0; mask >>= 1)
                    {
                        if ((flg & mask) != 0) result.Append(c);
                        c++;
                    }
                }

                // the last block:
                flg = flags[l] ^ invert_inequality;
                for (uint mask = 0x80000000U; mask >= (0x80000000U >> modl); mask >>= 1)
                {
                    if ((flg & mask) != 0) result.Append(c);
                    c++;
                }
            }

            //
            return StringBuilderUtilities.GetStringAndReturn(result);
        }
    }
}
