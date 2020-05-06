using Pchp.Core;
using Pchp.Library.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    #region Enumerations

    /// <summary>
    /// Type of sorting.
    /// </summary>
    public enum ComparisonMethod
    {
        /// <summary>Regular comparison.</summary>
        Regular = 0,

        /// <summary>Numeric comparison.</summary>
        Numeric = 1,

        /// <summary>String comparison.</summary>
        String = 2,

        /// <summary>String comparison respecting to locale.</summary>
        LocaleString = 5,

        /// <summary>Compare items as strings using "natural ordering".</summary>
        Natural = 6,

        /// <summary>In combination with <see cref="String"/> or <see cref="Natural"/> denotates case-insensitive comparison.</summary>
        FlagCase = 8,

        /// <summary>Undefined comparison.</summary>
        Undefined = -1
    };

    /// <summary>
    /// Sort order.
    /// </summary>
    public enum SortingOrder
    {
        /// <summary>Descending</summary>
        Descending = 3,

        /// <summary>Ascending</summary>
        Ascending = 4,

        /// <summary>Undefined</summary>
        Undefined = -1
    }

    /// <summary>
    /// Whether or not the sort is case-sensitive.
    /// </summary>
    public enum LetterCase
    {
        /// <summary>Lower case.</summary>
        Lower = 0,

        /// <summary>Upper case.</summary>
        Upper = 1
    }

    #endregion

    /// <summary>
    /// Implements PHP array functions.
    /// </summary>
    [PhpExtension("standard")]
    public static class Arrays
    {
        #region Constants

        public const int SORT_REGULAR = (int)ComparisonMethod.Regular;
        public const int SORT_NUMERIC = (int)ComparisonMethod.Numeric;
        public const int SORT_STRING = (int)ComparisonMethod.String;
        public const int SORT_LOCALE_STRING = (int)ComparisonMethod.LocaleString;
        public const int SORT_NATURAL = (int)ComparisonMethod.Natural;

        /// <summary>
        /// In combination with <see cref="SORT_STRING"/> or <see cref="SORT_NATURAL"/> denotates case-insensitive comparison.
        /// </summary>
        public const int SORT_FLAG_CASE = (int)ComparisonMethod.FlagCase;

        public const int SORT_DESC = (int)SortingOrder.Descending;
        public const int SORT_ASC = (int)SortingOrder.Ascending;

        public const int CASE_LOWER = (int)LetterCase.Lower;
        public const int CASE_UPPER = (int)LetterCase.Upper;

        #endregion

        #region reset, pos, prev, next, key, end, each

        /// <summary>
        /// Retrieves a value being pointed by an array intrinsic enumerator.
        /// </summary>
        /// <param name="array">The array which current value to return.</param>
        /// <returns><b>False</b>, if the intrinsic enumerator is behind the last item of <paramref name="array"/>, 
        /// otherwise the value being pointed by the enumerator (beware of values which are <b>false</b>!).</returns>
        /// <remarks>The value returned is dereferenced.</remarks>
        public static PhpValue current(PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return PhpValue.Null;
            }

            return array.IntrinsicEnumerator.CurrentValue.GetValue(); // NOTE: gets FALSE if at end
        }

        /// <summary>
        /// Retrieves a value being pointed by an array intrinsic enumerator.
        /// </summary>
        /// <param name="array">The array which current value to return.</param>
        /// <returns>
        /// <b>False</b> if the intrinsic enumerator is behind the last item of <paramref name="array"/>, 
        /// otherwise the value being pointed by the enumerator (beware of values which are <b>false</b>!).
        /// </returns>
        /// <remarks>
        /// Alias of <see cref="current"/>. The value returned is dereferenced.
        /// </remarks>
        public static object pos(PhpArray array) => current(array);

        /// <summary>
        /// Retrieves a key being pointed by an array intrinsic enumerator.
        /// </summary>
        /// <param name="array">The array which current key to return.</param>
        /// <returns>
        /// <b>Null</b>, if the intrinsic enumerator is behind the last item of <paramref name="array"/>, 
        /// otherwise the key being pointed by the enumerator.
        /// </returns>
        public static PhpValue key(PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return PhpValue.Null;
            }

            // note, key can't be of type PhpAlias, hence no dereferencing follows:
            return array.IntrinsicEnumerator.CurrentKey; // NOTE: gets NULL if not valid
        }

        /// <summary>
        /// Advances array intrinsic enumerator one item forward.
        /// </summary>
        /// <param name="array">The array which intrinsic enumerator to advance.</param>
        /// <returns>
        /// The value being pointed by the enumerator after it has been advanced
        /// or <b>false</b> if the enumerator has moved behind the last item of <paramref name="array"/>.
        /// </returns>
        /// <remarks>The value returned is dereferenced.</remarks>
        public static PhpValue next(PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return PhpValue.Null;
            }

            // moves to the next item and returns false if there is no such item:
            return array.IntrinsicEnumerator.MoveNext()
                ? array.IntrinsicEnumerator.CurrentValue.GetValue()
                : PhpValue.False;
        }

        /// <summary>
        /// Moves array intrinsic enumerator one item backward.
        /// </summary>
        /// <param name="array">The array which intrinsic enumerator to move.</param>
        /// <returns>
        /// The value being pointed by the enumerator after it has been moved
        /// or <b>false</b> if the enumerator has moved before the first item of <paramref name="array"/>.
        /// </returns>
        /// <remarks>The value returned is dereferenced.</remarks>
        public static PhpValue prev(PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return PhpValue.Null;
            }

            // moves to the previous item and returns false if there is no such item:
            return (array.IntrinsicEnumerator.MovePrevious())
                ? array.IntrinsicEnumerator.CurrentValue.GetValue()
                : PhpValue.False;
        }

        /// <summary>
        /// Moves array intrinsic enumerator so it will point to the last item of the array.
        /// </summary>
        /// <param name="array">The array which intrinsic enumerator to move.</param>
        /// <returns>The last value in the <paramref name="array"/> or <b>false</b> if <paramref name="array"/> 
        /// is empty.</returns>
        /// <remarks>The value returned is dereferenced.</remarks>
        public static PhpValue end(PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return PhpValue.Null;
            }

            // moves to the last item and returns false if there is no such item:
            return (array.IntrinsicEnumerator.MoveLast())
                ? array.IntrinsicEnumerator.CurrentValue.GetValue()
                : PhpValue.False;
        }

        /// <summary>
        /// Moves array intrinsic enumerator so it will point to the first item of the array.
        /// </summary>
        /// <param name="array">The array which intrinsic enumerator to move.</param>
        /// <returns>The first value in the <paramref name="array"/> or <b>false</b> if <paramref name="array"/> 
        /// is empty.</returns>
        /// <remarks>The value returned is dereferenced.</remarks>
        public static PhpValue reset(PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return PhpValue.Null;
            }

            // moves to the last item and returns false if there is no such item:
            return (array.IntrinsicEnumerator.MoveFirst())
                ? array.IntrinsicEnumerator.CurrentValue.GetValue()
                : PhpValue.False;
        }

        /// <summary>
        /// Retrieves the current entry and advances array intrinsic enumerator one item forward.
        /// </summary>
        /// <param name="array">The array which entry get and which intrinsic enumerator to advance.</param>
        /// <returns>
        /// The instance of <see cref="PhpArray"/>(0 =&gt; key, 1 =&gt; value, "key" =&gt; key, "value" =&gt; value)
        /// where key and value are pointed by the enumerator before it is advanced
        /// or <b>false</b> if the enumerator has been behind the last item of <paramref name="array"/>
        /// before the call.
        /// </returns>
        [Obsolete]
        [return: CastToFalse]
        public static PhpArray each(PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                return null;
            }

            if (array.IntrinsicEnumerator.AtEnd)
            {
                return null;
            }

            var entry = array.IntrinsicEnumerator.Current;
            array.IntrinsicEnumerator.MoveNext();

            // dereferences result since enumerator doesn't do so:
            var key = entry.Key;
            var value = entry.Value.GetValue().DeepCopy();

            // creates the resulting array:
            var result = new PhpArray(4);
            result.Add(1, value);
            result.Add("value", value);
            result.Add(0, key);
            result.Add("key", key);

            // keys and values should be inplace deeply copied:
            return result;
        }

        #endregion

        #region array_pop, array_push, array_shift, array_unshift, array_reverse


        /// <summary>
        /// Removes the last item from an array and returns it.
        /// </summary>
        /// <param name="array">The array whcih item to pop.</param>
        /// <returns>The last item of <paramref name="array"/> or a <b>null</b> reference if it is empty.</returns>
        /// <remarks>Resets intrinsic enumerator.</remarks>
        public static PhpValue array_pop([PhpRw] PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            if (array.Count == 0) return PhpValue.Null;

            // dereferences result since the array doesn't do so:
            var result = array.RemoveLast().Value.GetValue();

            array.RestartIntrinsicEnumerator();

            return result;
        }

        /// <summary>
        /// Adds multiple items into an array.
        /// </summary>
        /// <param name="array">The array where to add values.</param>
        /// <param name="vars">The array of values to add.</param>
        /// <returns>The number of items in array after all items was added.</returns>
        public static int array_push([PhpRw] PhpArray array, params PhpValue[] vars)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return 0;
                throw new ArgumentNullException();
            }

            // adds copies variables (if called by PHP):
            for (int i = 0; i < vars.Length; i++)
            {
                array.Add(vars[i].GetValue());
            }

            return array.Count;
        }

        /// <summary>
        /// Removes the first item of an array and reindex integer keys starting from zero.
        /// </summary>
        /// <param name="array">The array to be shifted.</param>
        /// <returns>The removed object.</returns>
        /// <remarks>Resets intrinsic enumerator.</remarks>
        public static PhpValue array_shift([PhpRw] PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            if (array.Count == 0) return PhpValue.Null;

            // dereferences result since the array doesn't do so:
            var result = array.RemoveFirst().Value.GetValue();

            // reindexes integer keys starting from zero:
            array.ReindexIntegers(0);
            array.RestartIntrinsicEnumerator();

            return result;
        }

        /// <summary>
        /// Inserts specified items before the first item of an array and reindex integer keys starting from zero.
        /// </summary>
        /// <param name="array">The array to be unshifted.</param>
        /// <param name="vars">Variables to be inserted.</param>
        /// <returns>The number of items in resulting array.</returns>
        public static int array_unshift([PhpRw] PhpArray array, params PhpValue[] vars)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return 0;
                throw new ArgumentNullException();
            }

            // reindexes integer keys starting from the number of items to be prepended:
            array.ReindexIntegers(vars.Length);

            // prepends items indexing keys from 0 to the number of items - 1:
            for (int i = vars.Length - 1; i >= 0; i--)
            {
                array.Prepend(i, vars[i].GetValue());
            }

            return array.Count;
        }

        /// <summary>
        /// Returns array which elements are taken from a specified one in reversed order.
        /// </summary>
        /// <param name="array">The array to be reversed.</param>
        /// <param name="preserveKeys">Whether keys should be left untouched. 
        /// If set to <b>false</b> then integer keys are reindexed starting from zero.</param>
        /// <returns>The array <paramref name="array"/> with items in reversed order.</returns>
        public static PhpArray array_reverse([PhpRw] PhpArray array, bool preserveKeys = false)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            PhpArray result;

            // clone the source array and revers entries order
            result = array.DeepCopy();
            result.Reverse();

            if (!preserveKeys)
            {
                // change the key of integer key from 0
                result.ReindexIntegers(0);
            }

            // if called by PHP languge then all items in the result should be inplace deeply copied:
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        #endregion

        #region array_slice, array_splice

        /// <summary>
        /// Retrieves a slice of specified array.
        /// </summary>
        /// <param name="array">The array which slice to get.</param>
        /// <param name="offset">The relativized offset of the first item of the slice.</param>
        /// <param name="length">The relativized length of the slice. Default <c>NULL</c> results in the maximum length.</param>
        /// <param name="preserveKeys">Whether to preserve integer keys. If <B>false</B>, the integer keys are reset.</param>
        /// <returns>The slice of <paramref name="array"/>.</returns>
        /// <remarks>
        /// See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> and <paramref name="length"/>.
        /// </remarks>
        public static PhpArray array_slice(PhpArray array, int offset, PhpValue length = default(PhpValue), bool preserveKeys = false)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            int ilength = Operators.IsSet(length) ? (int)length.ToLong() : int.MaxValue;

            // absolutizes range:
            PhpMath.AbsolutizeRange(ref offset, ref ilength, array.Count);

            var iterator = array.GetFastEnumerator();

            // moves iterator to the first item of the slice;
            // PERF: find offset in O(1) in arrays without holes
            for (int i = -1; i < offset; i++)
                if (iterator.MoveNext() == false)
                    break;

            // copies the slice:
            PhpArray result = new PhpArray(ilength);
            int ikey = 0;
            for (int i = 0; i < ilength; i++)
            {
                var entry = iterator.Current;

                // integer keys are reindexed if preserveKeys is false, string keys are not touched:
                if (preserveKeys || entry.Key.IsString)
                {
                    result.Add(entry.Key, entry.Value);
                }
                else
                {
                    result.Add(ikey++, entry.Value);
                }

                iterator.MoveNext();
            }

            //result.InplaceCopyOnReturn = true;
            return result;
        }

        /// <summary>
        /// Replaces a slice of an array with specified item(s).
        /// </summary>
        public static PhpArray array_splice(PhpArray array, int offset, PhpValue length = default(PhpValue), PhpValue replacement = default(PhpValue))
        {
            if (array == null)
            {
                //PhpException.Throw(
                //    PhpError.Warning,
                //    string.Format(Strings.unexpected_arg_given, "array", PhpArray.PhpTypeName, PhpVariable.TypeNameNull));
                //return null;
                throw new ArgumentNullException();
            }

            int ilength = Operators.IsSet(length) ? (int)length.ToLong() : int.MaxValue;

            return SpliceInternal(array, offset, ilength, replacement);
        }

        /// <summary>
        /// Implementation of <see cref="array_splice"/>.
        /// </summary>
        /// <remarks>Whether to make a deep-copy of items in the replacement.</remarks>
        internal static PhpArray SpliceInternal(PhpArray array, int offset, int length, PhpValue replacement)
        {
            Debug.Assert(array != null);
            int count = array.Count;

            // converts offset and length to interval [first,last]:
            PhpMath.AbsolutizeRange(ref offset, ref length, count);

            OrderedDictionary result;
            PhpArray arrtmp;

            if (Operators.IsEmpty(replacement)) // => not set or empty()
            {
                // replacement is null or empty:

                result = array.ReindexAndReplace(offset, length, null);
            }
            else if ((arrtmp = replacement.AsArray()) != null)
            {
                // replacement is an array:

                // provides deep copies:
                ICollection<PhpValue> e = arrtmp.Values;

                // does replacement:
                result = array.ReindexAndReplace(offset, length, e);
            }
            else
            {
                // replacement is another type //

                // does replacement:
                result = array.ReindexAndReplace(offset, length, new[] { replacement });
            }

            return new PhpArray(result);
        }

        #endregion

        #region shuffle, array_rand

        /// <summary>
        /// Randomizes the order of elements in the array using PhpMath random numbers generator.
        /// </summary>
        /// <exception cref="PhpException">Thrown if the <paramref name="array"/> argument is null.</exception>
        /// <remarks>Reindexes all keys in the resulting array.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool shuffle([PhpRw] PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull("array");
                return false;
            }

            array.Shuffle(PhpMath.Generator);
            array.ReindexAll();

            return true;
        }

        /// <summary>
        /// Chooses specified number of keys from an array at random.
        /// </summary>
        /// <param name="array">The <see cref="PhpArray"/> from which to choose.</param>
        /// <param name="count">The number of items to choose.</param>
        /// <returns>Either <see cref="PhpArray"/> of chosen keys (<paramref name="count"/> &gt; 1) or a single key.</returns>
        /// <remarks>
        /// Items are chosen uniformly in time <I>O(n)</I>, where <I>n</I> is the number of items in the 
        /// <paramref name="array"/> using conveyor belt sampling. 
        /// </remarks>
        /// <exception cref="NullReferenceException"><paramref name="array"/>  is a <B>null</B> reference.</exception>
        /// <exception cref="PhpException"><paramref name="count"/> is not positive and less 
        /// than the number of items in <paramref name="array"/>. (Warning)</exception>
        public static PhpValue array_rand(PhpArray array, int count = 1)
        {
            if (count == 1)
            {
                var result = new List<PhpValue>(1);
                return RandomSubset(array.Keys, result, count, PhpMath.Generator) ? result[0] : PhpValue.Null;
            }
            else
            {
                var result = new PhpArray(count > 0 ? count : 0);
                if (RandomSubset(array.Keys, result, count, PhpMath.Generator))
                {
                    //result.InplaceCopyOnReturn = true;
                    return PhpValue.Create(result);
                }
                else
                {
                    return PhpValue.Null;
                }
            }
        }

        /// <summary>
        /// Chooses specified number of items from a collection at random.
        /// </summary>
        /// <param name="source">The <see cref="ICollection"/> from which to choose.</param>
        /// <param name="result">The <see cref="IList"/> where to add chosen items.</param>
        /// <param name="count">The number of items to choose.</param>
        /// <param name="generator">The initialized random numbers generator.</param>
        /// <remarks>
        /// Items are chosen uniformly in time <I>O(n)</I>, where <I>n</I> is the number of items in the collection
        /// using conveyor belt sampling. 
        /// </remarks>
        /// <returns><B>false</B> on failure.</returns>
        /// <exception cref="PhpException">Either <paramref name="source"/> or <paramref name="result"/> or 
        /// <paramref name="generator"/> is a <B>null</B> reference (Warning)</exception>
        /// <exception cref="PhpException"><paramref name="count"/> is not positive and less 
        /// than the number of items in <paramref name="source"/>. (Warning)</exception>
        private static bool RandomSubset(ICollection<IntStringKey> source, IList<PhpValue> result, int count, Random generator)
        {
            if (source == null)
            {
                PhpException.ArgumentNull(nameof(source));
                return false;
            }
            if (result == null)
            {
                PhpException.ArgumentNull(nameof(result));
                return false;
            }
            if (generator == null)
            {
                PhpException.ArgumentNull(nameof(generator));
                return false;
            }
            if (count < 1 || count > source.Count)
            {
                PhpException.InvalidArgument(nameof(count), string.Format(LibResources.number_of_items_not_between_one_and_item_count, count, source.Count));
                return false;
            }

            int n = source.Count;
            using (var iterator = source.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    // adds item to result with probability count/n:
                    if ((double)count > generator.NextDouble() * n)
                    {
                        result.Add(PhpValue.Create(iterator.Current));
                        if (--count == 0) break;
                    }
                    n--;
                }
            }

            return true;
        }

        #endregion

        #region array_key_exists, in_array, array_search

        /// <summary>
        /// Checks if a key exists in the array.
        /// </summary>
        /// <param name="key">The key to be searched for.</param>
        /// <param name="array">The array where to search for the key.</param>
        /// <returns>Whether the <paramref name="key"/> exists in the <paramref name="array"/>.</returns>
        /// <remarks><paramref name="key"/> is converted before the search.</remarks>
        /// <exception cref="PhpException"><paramref name="array"/> argument is a <B>null</B> reference (Warning).</exception>
        /// <exception cref="PhpException"><paramref name="key"/> has type which is illegal for array key.</exception>
        public static bool array_key_exists(IntStringKey key, PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return false;
            }

            return array.ContainsKey(key);

            //if (Core.Convert.ObjectToArrayKey(key, out array_key))
            //    return array.ContainsKey(array_key);

            //PhpException.Throw(PhpError.Warning, CoreResources.GetString("illegal_offset_type"));
            //return false;
        }

        /// <summary>
        /// Alias of <see cref="array_key_exists"/>.
        /// </summary>
        public static bool key_exists(IntStringKey key, PhpArray array) => array_key_exists(key, array);

        /// <summary>
        /// Checks if a value exists in an array.
        /// </summary>
        /// <param name="needle">The value to search for.</param>
        /// <param name="haystack">The <see cref="PhpArray"/> where to search.</param>
        /// <param name="strict">Whether strict comparison method (operator ===) is used for comparing values.</param>
        /// <returns>Whether there is the <paramref name="needle"/> value in the <see cref="PhpArray"/>.</returns>
        /// <exception cref="PhpException"><paramref name="haystack"/> is a <B>null</B> reference (Warning).</exception>
        public static bool in_array(PhpValue needle, PhpArray haystack, bool strict = false)
        {
            var b = array_search(needle, haystack, strict);
            return !b.IsBoolean || b.Boolean;
        }

        /// <summary>
        /// Searches the array for a given value and returns the corresponding key if successful.
        /// </summary>
        /// <param name="needle">The value to search for.</param>
        /// <param name="haystack">The <see cref="PhpArray"/> where to search.</param>
        /// <param name="strict">Whether strict comparison method (operator ===) is used for comparing values.</param>
        /// <returns>The key associated with the <paramref name="needle"/> or <B>false</B> if there is no such key.</returns>
        /// <exception cref="PhpException"><paramref name="haystack"/> is a <B>null</B> reference (Warning).</exception>
        public static PhpValue array_search(PhpValue needle, PhpArray haystack, bool strict = false)
        {
            // result needn't to be deeply copied because it is a key of an array //

            if (haystack == null)
            {
                PhpException.ArgumentNull(nameof(haystack));
                return PhpValue.False;
            }

            // using operator ===:
            if (strict)
            {
                var enumerator = haystack.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    // TODO: dereferences value (because of StrictEquality operator):
                    if (needle.StrictEquals(enumerator.CurrentValue))
                        return PhpValue.Create(enumerator.CurrentKey);
                }
            }
            else
            {
                // using operator ==:

                var enumerator = haystack.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    // note: comparator manages references well:
                    if (needle.Equals(enumerator.CurrentValue))
                        return PhpValue.Create(enumerator.CurrentKey);
                }
            }

            // not found:
            return PhpValue.False;
        }

        #endregion

        #region array_fill, array_fill_keys, array_pad

        /// <summary>
		/// Creates a new array filled with a specified value.
		/// </summary>
		/// <param name="startIndex">The value of the key of the first item in the array.</param>
		/// <param name="count">The number of items in the array.</param>
		/// <param name="value">The value copied to all items in the array.</param>
		/// <returns>The array.</returns>
		/// <exception cref="PhpException">Thrown if <paramref name="count"/> is not positive.</exception>
		public static PhpArray array_fill(int startIndex, int count, PhpValue value)
        {
            if (count < 0)
            {
                PhpException.InvalidArgument("count", LibResources.arg_negative);
                return null;
            }
            else if (count == 0)
            {
                return PhpArray.NewEmpty();
            }
            else
            {
                var result = new PhpArray(count);
                int last = startIndex + count;
                for (int i = startIndex; i < last; i++)
                {
                    result.Add(i, value);
                }

                return result;
            }
        }

        public static PhpArray array_fill_keys(PhpArray keys, PhpValue value)
        {
            if (keys == null)
            {
                PhpException.ArgumentNull("keys");
                return null;
            }

            var result = new PhpArray(keys.Count);
            var iterator = keys.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                if (Core.Convert.TryToIntStringKey(iterator.CurrentValue, out var key) && !result.ContainsKey(key))
                {
                    result[key] = value;
                }
            }

            // makes deep copies of all added items:
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        /// <summary>
        /// Pads array to the specified length with a value.
        /// If the length is negative adds |length| elements at beginning otherwise adds elements at the end.
        /// Values with integer keys that are contained in the source array are inserted to the resulting one with new 
        /// integer keys counted from zero (or from |length| if length negative).
        /// </summary>
        /// <param name="array">The source array.</param>
        /// <param name="length">The length of the resulting array.</param>
        /// <param name="value">The value to add in array.</param>
        /// <returns>Padded array.</returns>
        /// <exception cref="PhpException">The <paramref name="array"/> argument is a <B>null</B> reference.</exception>
        public static PhpArray array_pad(PhpArray array, int length, PhpValue value)
        {
            if (array == null)
            {
                PhpException.ArgumentNull("array");
                return null;
            }

            // number of items to add:
            int remains = Math.Abs(length) - array.Count;

            // returns unchanged array (or its deep copy if called from PHP):
            if (remains <= 0) return array;

            PhpArray result = new PhpArray(array.Count + remains);

            // prepends items:
            if (length < 0)
            {
                while (remains-- > 0) result.Add(value);
            }

            // inserts items from source array
            // if a key is a string inserts it unchanged otherwise inserts value with max. integer key:  
            var iterator = array.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                var key = iterator.CurrentKey;
                if (key.IsString)
                    result.Add(key, iterator.CurrentValue);
                else
                    result.Add(iterator.CurrentValue);
            }

            // appends items:
            if (length > 0)
            {
                while (remains-- > 0) result.Add(value);
            }

            // the result is inplace deeply copied on return to PHP code:
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        #endregion

        #region array_key_first, array_key_last 

        /// <summary>
        /// Get the first key of the given array without affecting the internal array pointer.
        /// </summary>
        /// <returns>Returns the first key of array if the array is not empty. Otherwise <c>NULL</c>.</returns>
        public static PhpValue array_key_first(PhpArray array)
        {
            var enumerator = array.GetFastEnumerator();
            if (enumerator.MoveNext())
            {
                return PhpValue.Create(enumerator.CurrentKey);
            }
            else
            {
                return PhpValue.Null;
            }
        }

        /// <summary>
        /// Get the last key of the given array without affecting the internal array pointer.
        /// </summary>
        /// <returns>Returns the first key of array if the array is not empty. Otherwise <c>NULL</c>.</returns>
        public static PhpValue array_key_last(PhpArray array)
        {
            var enumerator = array.GetFastEnumerator();
            if (enumerator.MovePrevious())
            {
                return PhpValue.Create(enumerator.CurrentKey);
            }
            else
            {
                return PhpValue.Null;
            }
        }

        #endregion

        #region range

        /// <summary>
        /// Creates an array containing range of long integers from the [low;high] interval with arbitrary step.
        /// </summary>
        /// <param name="low">Lower bound of the interval.</param>
        /// <param name="high">Upper bound of the interval.</param>
        /// <param name="step">The step. An absolute value is taken if step is zero.</param>
        /// <returns>The array.</returns>
        private static PhpArray RangeOfLongInts(long low, long high, long step)
        {
            if (step == 0)
            {
                //PhpException.InvalidArgument("step", LibResources.GetString("arg_zero"));
                //return null;
                throw new ArgumentException();
            }

            if (step < 0) step = -step;

            PhpArray result = new PhpArray(unchecked((int)(Math.Abs(high - low) / step + 1)));

            if (high >= low)
            {
                for (int i = 0; low <= high; i++, low += step) result.Add(i, low);
            }
            else
            {
                for (int i = 0; low >= high; i++, low -= step) result.Add(i, low);
            }

            return result;
        }

        /// <summary>
        /// Creates an array containing range of doubles from the [low;high] interval with arbitrary step.
        /// </summary>
        /// <param name="low">Lower bound of the interval.</param>
        /// <param name="high">Upper bound of the interval.</param>
        /// <param name="step">The step. An absolute value is taken if step is less than zero.</param>
        /// <returns>The array.</returns>
        /// <exception cref="PhpException">Thrown if the <paramref name="step"/> argument is zero.</exception>
        private static PhpArray RangeOfDoubles(double low, double high, double step)
        {
            if (step == 0)
            {
                //PhpException.InvalidArgument("step", LibResources.GetString("arg_zero"));
                //return null;
                throw new ArgumentException();
            }

            if (step < 0) step = -step;

            PhpArray result = new PhpArray(System.Convert.ToInt32(Math.Abs(high - low) / step) + 1);

            if (high >= low)
            {
                for (int i = 0; low <= high; i++, low += step) result.Add(i, low);
            }
            else
            {
                for (int i = 0; low >= high; i++, low -= step) result.Add(i, low);
            }

            return result;
        }

        /// <summary>
        /// Creates an array containing range of characters from the [low;high] interval with arbitrary step.
        /// </summary>
        /// <param name="low">Lower bound of the interval.</param>
        /// <param name="high">Upper bound of the interval.</param>
        /// <param name="step">The step.</param>
        /// <returns>The array.</returns>
        /// <exception cref="PhpException">Thrown if the <paramref name="step"/> argument is zero.</exception>
        private static PhpArray RangeOfChars(char low, char high, int step)
        {
            if (step == 0)
            {
                //PhpException.InvalidArgument("step", LibResources.GetString("arg_zero"));
                //step = 1;
                throw new ArgumentException();
            }

            if (step < 0) step = -step;

            PhpArray result = new PhpArray(Math.Abs(high - low) / step + 1);
            if (high >= low)
            {
                for (int i = 0; low <= high; i++, low = unchecked((char)(low + step))) result.Add(i, low.ToString());
            }
            else
            {
                for (int i = 0; low >= high; i++, low = unchecked((char)(low - step))) result.Add(i, low.ToString());
            }

            return result;
        }

        /// <summary>
        /// Creates an array containing range of elements with step 1.
        /// </summary>
        /// <param name="low">Lower bound of the interval.</param>
        /// <param name="high">Upper bound of the interval.</param>
        /// <returns>The array.</returns>
        public static PhpArray range(long low, long high) => RangeOfLongInts(low, high, 1L);

        /// <summary>
        /// Creates an array containing range of elements with step 1.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="low">Lower bound of the interval.</param>
        /// <param name="high">Upper bound of the interval.</param>
        /// <returns>The array.</returns>
        public static PhpArray range(Context ctx, PhpValue low, PhpValue high) => range(ctx, low, high, PhpValue.Create(1));

        /// <summary>
        /// Creates an array containing range of elements with arbitrary step.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="low">Lower bound of the interval.</param>
        /// <param name="high">Upper bound of the interval.</param>
        /// <param name="step">The step.</param>
        /// <returns>The array.</returns>
        /// <remarks>
        /// Implements PHP awful range function. The result depends on types and 
        /// content of parameters under the following rules:
        /// <list type="number">
        /// <item>
        ///   <description>
        ///   If at least one parameter (low, high or step) is of type double or is a string wholly representing 
        ///       double value (i.e. whole string is converted to a number and no chars remains, 
        ///       e.g. "1.5" is wholly representing but the value "1.5x" is not)
        ///    than
        ///       range of double values is generated with a step treated as a double value
        ///       (e.g. <c>range("1x","2.5x","0.5") = array(1.0, 1.5, 2.0, 2.5)</c> etc.)
        ///    otherwise 
        ///   </description>
        /// </item>
        /// <item>
        ///   <description>
        ///    if at least one bound (i.e. low or high parameter) is of type int or is a string wholly representing
        ///       integer value 
        ///    than 
        ///       range of integer values is generated with a step treated as integer value
        ///       (e.g. <c>range("1x","2","1.5") = array(1, 2, 3, 4)</c> etc.)
        ///    otherwise
        ///   </description>
        /// </item>
        /// <item>
        ///   <description>
        ///    low and high are both non-empty strings (otherwise one of the two previous conditions would be true),
        ///    so the first characters of these strings are taken and a sequence of characters is generated.
        ///   </description>     
        /// </item>
        /// </list>
        /// Moreover, if <paramref name="low"/> is greater than <paramref name="high"/> then descending sequence is generated 
        /// and ascending one otherwise. If <paramref name="step"/> is less than zero than an absolute value is used.
        /// </remarks>
        /// <exception cref="PhpException">Thrown if the <paramref name="step"/> argument is zero (or its absolute value less than 1 in the case 2).</exception>
        public static PhpArray range(Context ctx, PhpValue low, PhpValue high, PhpValue step)
        {
            PhpNumber num_low, num_high, num_step;

            // converts each parameter to a number, determines what type of number it is (int/double)
            // and whether it wholly represents that number:
            var info_step = step.ToNumber(out num_step);
            var info_low = low.ToNumber(out num_low);
            var info_high = high.ToNumber(out num_high);

            var is_step_double = (info_step & Core.Convert.NumberInfo.Double) != 0;
            var is_low_double = (info_low & Core.Convert.NumberInfo.Double) != 0;
            var is_high_double = (info_high & Core.Convert.NumberInfo.Double) != 0;

            var w_step = (info_step & Core.Convert.NumberInfo.IsNumber) != 0;
            var w_low = (info_low & Core.Convert.NumberInfo.IsNumber) != 0;
            var w_high = (info_high & Core.Convert.NumberInfo.IsNumber) != 0;

            // at least one parameter is a double or its numeric value is wholly double:
            if (is_low_double && w_low || is_high_double && w_high || is_step_double && w_step)
            {
                return RangeOfDoubles(num_low.ToDouble(), num_high.ToDouble(), num_step.ToDouble());
            }

            // at least one bound is wholly integer (doesn't matter what the step is):
            if (!is_low_double && w_low || !is_high_double && w_high)
            {
                // at least one long integer:
                return RangeOfLongInts(num_low.ToLong(), num_high.ToLong(), num_step.ToLong());
            }

            // both bounds are strings which are not wholly representing numbers (other types wholly represents a number):

            string slow = low.ToString(ctx);
            string shigh = high.ToString(ctx);

            // because each string doesn't represent a number it isn't empty:
            Debug.Assert(slow != "" && shigh != "");

            return RangeOfChars(slow[0], shigh[0], (int)num_step.ToLong());
        }

        #endregion

        #region GetComparer

        /// <summary>
        /// Gets an instance of PHP comparer parametrized by specified method, order, and compared item type.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="method">The <see cref="ComparisonMethod"/>.</param>
        /// <param name="order">The <see cref="SortingOrder"/>.</param>
        /// <param name="keyComparer">Whether to compare keys (<B>false</B> for value comparer).</param>
        /// <returns>A comparer (either a new instance or existing singleton instance).</returns>
        internal static IComparer<KeyValuePair<IntStringKey, PhpValue>>/*!*/ GetComparer(Context ctx, ComparisonMethod method, SortingOrder order, bool keyComparer)
        {
            if (keyComparer)
            {
                switch (method)
                {
                    case ComparisonMethod.Numeric:
                        return (order == SortingOrder.Descending) ? KeyComparer.ReverseNumeric : KeyComparer.Numeric;

                    case ComparisonMethod.String:
                        return (order == SortingOrder.Descending) ? KeyComparer.ReverseString(ctx) : KeyComparer.String(ctx);

                    case ComparisonMethod.String | ComparisonMethod.FlagCase:
                        //return (order == SortingOrder.Descending) ? KeyComparer.ReverseStringIgnoreCase(ctx) : KeyComparer.StringIgnoreCase(ctx);
                        throw new NotImplementedException();

                    case ComparisonMethod.LocaleString:
                        return new KeyComparer(Locale.GetStringComparer(ctx, false), order == SortingOrder.Descending);

                    case ComparisonMethod.Natural:
                        return new KeyComparer(new PhpNaturalComparer(ctx, false), order == SortingOrder.Descending);

                    case ComparisonMethod.Natural | ComparisonMethod.FlagCase:
                        return new KeyComparer(new PhpNaturalComparer(ctx, caseInsensitive: true), order == SortingOrder.Descending);

                    default:
                        return (order == SortingOrder.Descending) ? KeyComparer.Reverse : KeyComparer.Default;
                }
            }
            else
            {
                switch (method)
                {
                    case ComparisonMethod.Numeric:
                        return (order == SortingOrder.Descending) ? ValueComparer.ReverseNumeric : ValueComparer.Numeric;

                    case ComparisonMethod.String:
                        return (order == SortingOrder.Descending) ? ValueComparer.ReverseString(ctx) : ValueComparer.String(ctx);

                    case ComparisonMethod.String | ComparisonMethod.FlagCase:
                        //return (order == SortingOrder.Descending) ? ValueComparer.ReverseStringIgnoreCase(ctx) : ValueComparer.StringIgnoreCase(ctx);
                        throw new NotImplementedException();

                    case ComparisonMethod.LocaleString:
                        return new ValueComparer(Locale.GetStringComparer(ctx, false), order == SortingOrder.Descending);

                    case ComparisonMethod.Natural:
                        return new ValueComparer(new PhpNaturalComparer(ctx, false), order == SortingOrder.Descending);

                    case ComparisonMethod.Natural | ComparisonMethod.FlagCase:
                        return new ValueComparer(new PhpNaturalComparer(ctx, caseInsensitive: true), order == SortingOrder.Descending);

                    default:
                        return (order == SortingOrder.Descending) ? ValueComparer.Reverse : ValueComparer.Default;
                }
            }
        }

        #endregion

        #region sort,asort,ksort,rsort,arsort,krsort

        /// <summary>
        /// Sorts an array using specified comparison method for comparing values.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool sort(Context ctx, [In, Out, PhpRw] PhpArray array, ComparisonMethod comparisonMethod = ComparisonMethod.Regular)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }

            array.Sort(GetComparer(ctx, comparisonMethod, SortingOrder.Ascending, false));
            array.ReindexAll();
            array.RestartIntrinsicEnumerator();

            return true;
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing values preserving key-value associations.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool asort(Context ctx, [In, Out, PhpRw] PhpArray array, ComparisonMethod comparisonMethod = ComparisonMethod.Regular)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }

            array.Sort(GetComparer(ctx, comparisonMethod, SortingOrder.Ascending, false));
            array.RestartIntrinsicEnumerator();

            return true;
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing keys.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of keys.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool ksort(Context ctx, [In, Out, PhpRw] PhpArray array, ComparisonMethod comparisonMethod = ComparisonMethod.Regular)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }

            array.Sort(GetComparer(ctx, comparisonMethod, SortingOrder.Ascending, true));
            array.RestartIntrinsicEnumerator();

            return true;
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing values in reverse order.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of keys.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool rsort(Context ctx, [In, Out, PhpRw] PhpArray array, ComparisonMethod comparisonMethod = ComparisonMethod.Regular)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }

            array.Sort(GetComparer(ctx, comparisonMethod, SortingOrder.Descending, false));
            array.ReindexAll();
            array.RestartIntrinsicEnumerator();

            return true;
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing values in reverse order
        /// preserving key-value associations.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool arsort(Context ctx, [In, Out, PhpRw] PhpArray array, ComparisonMethod comparisonMethod = ComparisonMethod.Regular)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }

            array.Sort(GetComparer(ctx, comparisonMethod, SortingOrder.Descending, false));
            array.RestartIntrinsicEnumerator();

            return true;
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing keys in reverse order.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of keys.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool krsort(Context ctx, [In, Out, PhpRw] PhpArray array, ComparisonMethod comparisonMethod = ComparisonMethod.Regular)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }

            array.Sort(GetComparer(ctx, comparisonMethod, SortingOrder.Descending, true));
            array.RestartIntrinsicEnumerator();

            return true;
        }

        #endregion

        #region usort,uasort,uksort

        /// <summary>
        /// Sorts an array using user comparison callback for comparing values.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="compare">The user callback to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool usort(Context ctx /*, caller*/, [In, Out, PhpRw] PhpArray array, IPhpCallable compare)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }
            if (!PhpVariable.IsValidCallback(compare)) return false;

            // sorts array using callback for comparisons:
            array.Sort(new ValueComparer(new PhpUserComparer(ctx, compare), false));

            array.ReindexAll();
            array.RestartIntrinsicEnumerator();

            return true;
        }

        /// <summary>
        /// Sorts an array user comparison callback method for comparing values preserving key-value associations.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="compare">The user callback to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool uasort(Context ctx /*, caller*/, [In, Out, PhpRw] PhpArray array, IPhpCallable compare)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }
            if (!PhpVariable.IsValidCallback(compare)) return false;

            // sorts array using callback for comparisons:
            array.Sort(new ValueComparer(new PhpUserComparer(ctx, compare), false));

            return true;
        }

        /// <summary>
        /// Sorts an array using user comparison callback for comparing keys.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="compare">The user callback to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool uksort(Context ctx /*, caller*/, [In, Out, PhpRw] PhpArray array, IPhpCallable compare)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }
            if (!PhpVariable.IsValidCallback(compare)) return false;

            array.Sort(new KeyComparer(new PhpUserComparer(ctx, compare), false));

            return true;
        }

        #endregion

        #region natsort,natcasesort

        /// <summary>
        /// Sorts an array using case sensitive natural comparison method for comparing 
        /// values preserving key-value association.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool natsort(Context ctx, [In, Out] PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }

            array.Sort(new ValueComparer(new PhpNaturalComparer(ctx, false), false));

            return true;
        }

        /// <summary>
        /// Sorts an array using case insensitive natural comparison method for 
        /// comparing values preserving key-value association.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool natcasesort(Context ctx, [In, Out] PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return false;
            }

            array.Sort(new ValueComparer(new PhpNaturalComparer(ctx, true), false));

            return true;
        }

        #endregion

        #region array_multisort

        /// <summary>
        /// Resolves arguments passed to <see cref="MultiSort"/> method according to PHP manual for <c>array_multisort</c> function.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="first">The first argument of <see cref="MultiSort"/>.</param>
        /// <param name="args">The rest of arguments of <see cref="MultiSort"/>.</param>
        /// <param name="arrays">An array to be filled with arrays passed in all arguments.</param>
        /// <param name="comparers">An array to be filled with comparers defined by arguments.</param>
        /// <remarks>
        /// Arrays and comparers can be a <B>null</B> reference. In such a case only number of arrays to be sorted
        /// is returned. Otherwise, <paramref name="arrays"/> is filled with these arrays and <paramref name="comparers"/>
        /// with comparers defined by appropriate arguments.
        /// </remarks>
        private static int MultiSortResolveArgs(
            Context ctx,
            PhpArray first,
            PhpValue[] args,
            PhpArray[] arrays,
            IComparer<KeyValuePair<IntStringKey, PhpValue>>[] comparers)
        {
            int col_count = 1;
            int row_count = first.Count;
            ComparisonMethod method = ComparisonMethod.Undefined;
            SortingOrder order = SortingOrder.Undefined;

            if (arrays != null)
            {
                arrays[0] = first;
            }

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.IsPhpArray(out var array))
                {
                    // checks whether the currently processed array has the same length as the first one:
                    if (array.Count != row_count)
                    {
                        //PhpException.Throw(PhpError.Warning, CoreResources.GetString("lengths_are_different", "the first array", string.Format("{0}-th array", col_count)));
                        //return 0;
                        throw new ArgumentException();
                    }
                    // sets next array:
                    if (arrays != null)
                        arrays[col_count] = array;

                    // sets comparer of the previous array:
                    if (comparers != null)
                        comparers[col_count - 1] = GetComparer(ctx, method, order, false);

                    // resets values:
                    method = ComparisonMethod.Undefined;
                    order = SortingOrder.Undefined;

                    col_count++;
                }
                else if (arg.IsLong(out var num))
                {
                    switch (num)
                    {
                        case (int)ComparisonMethod.Regular:
                        case (int)ComparisonMethod.Numeric:
                        case (int)ComparisonMethod.String:
                        case (int)ComparisonMethod.String | (int)ComparisonMethod.FlagCase:
                        case (int)ComparisonMethod.LocaleString:
                        case (int)ComparisonMethod.Natural:
                        case (int)ComparisonMethod.Natural | (int)ComparisonMethod.FlagCase:
                            if (method != ComparisonMethod.Undefined)
                            {
                                //PhpException.Throw(PhpError.Warning, LibResources.GetString("sorting_flag_already_specified", i));
                                //return 0;
                                throw new ArgumentException();
                            }
                            else
                            {
                                method = (ComparisonMethod)num;
                            }
                            break;

                        case (int)SortingOrder.Descending:
                        case (int)SortingOrder.Ascending:
                            if (order != SortingOrder.Undefined)
                            {
                                //PhpException.Throw(PhpError.Warning, LibResources.GetString("sorting_flag_already_specified", i));
                                //return 0;
                                throw new ArgumentException();
                            }
                            else
                            {
                                order = (SortingOrder)num;
                            }
                            break;

                        default:
                            //PhpException.Throw(PhpError.Warning, LibResources.GetString("argument_not_array_or_sort_flag", i));
                            //return 0;
                            throw new ArgumentException();
                    }
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, LibResources.argument_not_array_or_sort_flag, i.ToString());
                    return 0;
                }
            }

            // sets comparer of the previous array:
            if (comparers != null)
                comparers[col_count - 1] = GetComparer(ctx, method, order, false);

            return col_count;
        }

        /// <summary>
        /// Sort multiple arrays.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="first">The first array to be sorted.</param>
        /// <param name="args">Arrays to be sorted along with flags affecting sort order and 
        /// comparison methods to be used. See PHP manual for more details.</param>
        /// <returns>Whether arrays were sorted successfully.</returns>
        /// <remarks>Reindexes integer keys in the sorted arrays and restarts their intrinsic enumerators.</remarks>
        /// <exception cref="PhpException"><paramref name="first"/> is a <B>null</B> reference (Warning).</exception>
        /// <exception cref="PhpException">Arrays has different lengths (Warning).</exception>
        /// <exception cref="PhpException">Invalid sorting flags (Warning).</exception>
        /// <exception cref="PhpException">Multiple sorting flags applied on single array (Warning).</exception>
        public static bool array_multisort(Context ctx, [In, Out, PhpRw] PhpArray first, params PhpValue[] args)
        {
            // some "args" are also [PhpRw] but which ones is compile time unknown
            // but it is not neccessary to mark them since this attribute has no important effect

            if (first == null)
            {
                //TODO: PhpException.ArgumentNull("first");
                throw new ArgumentNullException();
            }

            IComparer<KeyValuePair<IntStringKey, PhpValue>>[] comparers;
            PhpArray[] arrays;
            int length = MultiSortResolveArgs(ctx, first, args, null, null);

            if (length == 0)
            {
                return false;
            }
            if (length == 1)
            {
                comparers = new IComparer<KeyValuePair<IntStringKey, PhpValue>>[1];
                MultiSortResolveArgs(ctx, first, args, null, comparers);
                first.Sort(comparers[0]);
                first.ReindexIntegers(0);
                first.RestartIntrinsicEnumerator();
                return true;
            }

            arrays = new PhpArray[length];
            comparers = new IComparer<KeyValuePair<IntStringKey, PhpValue>>[length];
            MultiSortResolveArgs(ctx, first, args, arrays, comparers);
            PhpHashtable.Sort(arrays, comparers); // + reindex + restart intrinsic

            return true;
        }

        #endregion

        #region array_u?(diff|intersect)(_u?assoc)?, array_(diff|intersect)_u?key

        /// <summary>
        /// Internal method common for all functions.
        /// </summary>
        private static PhpArray SetOperation(SetOperations op, PhpArray array, PhpArray[] arrays, IComparer<KeyValuePair<IntStringKey, PhpValue>> comparer)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            if (arrays == null || arrays.Length == 0)
            {
                //PhpException.InvalidArgumentCount(null, null);
                //return null;
                throw new ArgumentException();
            }

            Debug.Assert(comparer != null);

            return array.SetOperation(op, arrays, comparer);

            // the result is inplace deeply copied on return to PHP code:
            //result.InplaceCopyOnReturn = true;
        }

        // TODO: specific parameters instead of 'params PhpValue[] arraysAndComparer'

        /// <summary>
        /// There have to be at least 1 value in <paramref name="vars"/>.
        /// The last is converted to callback, the rest to arrays.
        /// </summary>
        private static bool SplitArraysAndComparers(int comparerCount, PhpArray array, PhpValue[] vars, out PhpArray[] arrays, out IPhpCallable cmp1, out IPhpCallable cmp2)
        {
            arrays = null;
            cmp2 = null;

            Debug.Assert(comparerCount >= 1 && comparerCount <= 2);

            if (vars == null || vars.Length == 0)
            {
                //PhpException.InvalidArgumentCount(null, null);
                //return false;
                throw new ArgumentException();
            }

            // the first callback:
            cmp1 = vars[vars.Length - comparerCount].AsCallable(default);
            if (!PhpVariable.IsValidCallback(cmp1)) return false;

            // the second callback:
            if (comparerCount > 1)
            {
                cmp2 = vars[vars.Length - 1].AsCallable(default);
                if (!PhpVariable.IsValidCallback(cmp2)) return false;
            }

            // remaining arguments should be arrays:
            arrays = new PhpArray[vars.Length - comparerCount + 1];
            arrays[0] = array;
            for (int i = 1; i < arrays.Length; i++)
            {
                if ((arrays[i] = vars[i - 1].AsArray()) == null)
                {
                    PhpException.Throw(PhpError.Warning, LibResources.argument_not_array, (i + 2).ToString());
                    return false;
                }
            }

            //
            return true;
        }

        /// <summary>
        /// Computes the difference of arrays.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array from which to take items away.</param>
        /// <param name="arrays">The arrays to be differentiated.</param>
        /// <returns>The array containing all the entries of <paramref name="array"/> that are not present 
        /// in any of the <paramref name="arrays"/>.</returns>
        /// <remarks>Keys are preserved. Entries are considered to be equal iff values compared by  
        /// by string comparison method are the same (see <see cref="ValueComparer.String"/>).</remarks>
        /// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
        /// <exception cref="PhpException"><paramref name="arrays"/> is a <B>null</B> reference or an empty array.</exception>
        //[return: PhpDeepCopy]
        public static PhpArray array_diff(Context ctx, PhpArray array, params PhpArray[] arrays)
        {
            return SetOperation(SetOperations.Difference, array, arrays, ValueComparer.String(ctx));
        }

        /// <summary>
        /// Computes the intersection of arrays.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_intersect(Context ctx, PhpArray array, params PhpArray[] arrays)
        {
            return SetOperation(SetOperations.Intersection, array, arrays, ValueComparer.String(ctx));
        }

        /// <summary>
        /// Computes the difference of arrays.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array from which to take items away.</param>
        /// <param name="arrays">The arrays to be differentiated.</param>
        /// <returns>The array containing all the entries of <paramref name="array"/> that are not present 
        /// in any of the <paramref name="arrays"/>.</returns>
        /// <remarks>Keys are preserved. Entries are considered to be equal iff they has the same keys and values
        /// according to string method comparison (see <see cref="EntryComparer"/> and <see cref="PhpStringComparer"/>).</remarks>
        /// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
        /// <exception cref="PhpException"><paramref name="arrays"/> is a <B>null</B> reference or an empty array.</exception>
        //[return: PhpDeepCopy]
        public static PhpArray array_diff_assoc(Context ctx, PhpArray array, params PhpArray[] arrays)
        {
            return SetOperation(SetOperations.Difference, array, arrays,
                new EntryComparer(new PhpStringComparer(ctx), false, new PhpStringComparer(ctx), false));
        }

        /// <summary>
        /// Computes the intersection of arrays.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_intersect_assoc(Context ctx, PhpArray array, params PhpArray[] arrays)
        {
            return SetOperation(SetOperations.Intersection, array, arrays,
                new EntryComparer(new PhpStringComparer(ctx), false, new PhpStringComparer(ctx), false));
        }

        /// <summary>
        /// Computes the difference of arrays.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array from which to take items away.</param>
        /// <param name="arrays">The arrays to be differentiated.</param>
        /// <returns>The array containing all the entries of <paramref name="array"/> that are not present 
        /// in any of the <paramref name="arrays"/>.</returns>
        /// <remarks>Entries are considered to be equal iff keys compared by  
        /// by string comparison method are the same (see <see cref="KeyComparer.String"/>).</remarks>
        /// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
        /// <exception cref="PhpException"><paramref name="arrays"/> is a <B>null</B> reference or an empty array.</exception>
        //[return: PhpDeepCopy]
        public static PhpArray array_diff_key(Context ctx, PhpArray array, params PhpArray[] arrays)
        {
            return SetOperation(SetOperations.Difference, array, arrays, KeyComparer.String(ctx));
        }

        /// <summary>
        /// Computes the intersection of arrays.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_intersect_key(Context ctx, PhpArray array, params PhpArray[] arrays)
        {
            return SetOperation(SetOperations.Intersection, array, arrays, KeyComparer.String(ctx));
        }

        /// <summary>
        /// Computes the difference of arrays using a specified key comparer.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_diff_ukey(Context ctx, PhpArray array, PhpArray array0, params PhpValue[] arraysAndComparer)
        {
            if (SplitArraysAndComparers(1, array0, arraysAndComparer, out var arrays, out var key_comparer, out var cmp))
            {
                return SetOperation(SetOperations.Difference, array, arrays, new KeyComparer(new PhpUserComparer(ctx, key_comparer), false));
            }

            return null;
        }

        /// <summary>
        /// Computes the intersection of arrays using a specified key comparer.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_intersect_ukey(Context ctx, PhpArray array, PhpArray array0, params PhpValue[] arraysAndComparer)
        {
            if (SplitArraysAndComparers(1, array0, arraysAndComparer, out var arrays, out var key_comparer, out var cmp))
            {
                return SetOperation(SetOperations.Intersection, array, arrays, new KeyComparer(new PhpUserComparer(ctx, key_comparer), false));
            }

            return null;
        }

        /// <summary>
        /// Computes the difference of arrays using a specified comparer.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_udiff(Context ctx, PhpArray array1, PhpArray array2, params PhpValue[] arraysAndComparer)
        {
            if (SplitArraysAndComparers(1, array2, arraysAndComparer, out var arrays, out var value_comparer, out var cmp))
            {
                return SetOperation(SetOperations.Difference, array1, arrays, new ValueComparer(new PhpUserComparer(ctx, value_comparer), false));
            }

            return null;
        }

        /// <summary>
        /// Computes the intersection of arrays using a specified comparer.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_uintersect(Context ctx, PhpArray array, PhpArray array0, params PhpValue[] arraysAndComparer)
        {
            if (SplitArraysAndComparers(1, array0, arraysAndComparer, out var arrays, out var value_comparer, out var cmp))
            {
                return SetOperation(SetOperations.Intersection, array, arrays, new ValueComparer(new PhpUserComparer(ctx, value_comparer), false));
            }

            return null;
        }

        /// <summary>
        /// Computes the difference of arrays using a specified comparer.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_udiff_assoc(Context ctx, PhpArray array, PhpArray array0, params PhpValue[] arraysAndComparer)
        {
            if (SplitArraysAndComparers(1, array0, arraysAndComparer, out var arrays, out var value_comparer, out var cmp))
            {
                return SetOperation(SetOperations.Difference, array, arrays, new EntryComparer(new PhpStringComparer(ctx), false, new PhpUserComparer(ctx, value_comparer), false));
            }

            return null;
        }

        /// <summary>
        /// Computes the intersection of arrays using a specified comparer.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_uintersect_assoc(Context ctx, PhpArray array, PhpArray array0, params PhpValue[] arraysAndComparer)
        {
            if (SplitArraysAndComparers(1, array0, arraysAndComparer, out var arrays, out var value_comparer, out var cmp))
            {
                return SetOperation(SetOperations.Intersection, array, arrays, new EntryComparer(new PhpStringComparer(ctx), false, new PhpUserComparer(ctx, value_comparer), false));
            }

            return null;
        }


        /// <summary>
        /// Computes the difference of arrays using a specified comparer.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_diff_uassoc(Context ctx, PhpArray array, PhpArray array0, params PhpValue[] arraysAndComparer)
        {
            if (SplitArraysAndComparers(1, array0, arraysAndComparer, out var arrays, out var key_comparer, out var cmp))
            {
                return SetOperation(SetOperations.Difference, array, arrays, new EntryComparer(new PhpUserComparer(ctx, key_comparer), false, new PhpStringComparer(ctx), false));
            }

            return null;
        }

        /// <summary>
        /// Computes the intersection of arrays using a specified comparer.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_intersect_uassoc(Context ctx, PhpArray array, PhpArray array0, params PhpValue[] arraysAndComparer)
        {
            if (SplitArraysAndComparers(1, array0, arraysAndComparer, out var arrays, out var key_comparer, out var cmp))
            {
                return SetOperation(SetOperations.Intersection, array, arrays, new EntryComparer(new PhpUserComparer(ctx, key_comparer), false, new PhpStringComparer(ctx), false));
            }

            return null;
        }

        /// <summary>
        /// Computes the difference of arrays using specified comparers.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_udiff_uassoc(Context ctx, PhpArray array, PhpArray array0, params PhpValue[] arraysAndComparers)
        {
            if (SplitArraysAndComparers(2, array0, arraysAndComparers, out var arrays, out var value_comparer, out var key_comparer))
            {
                return SetOperation(SetOperations.Difference, array, arrays, new EntryComparer(new PhpUserComparer(ctx, key_comparer), false, new PhpUserComparer(ctx, value_comparer), false));
            }

            return null;
        }

        /// <summary>
        /// Computes the intersection of arrays using specified comparers.
        /// </summary>
        //[return: PhpDeepCopy]
        public static PhpArray array_uintersect_uassoc(Context ctx, PhpArray array, PhpArray array0, params PhpValue[] arraysAndComparers)
        {
            if (SplitArraysAndComparers(2, array0, arraysAndComparers, out var arrays, out var value_comparer, out var key_comparer))
            {
                return SetOperation(SetOperations.Intersection, array, arrays, new EntryComparer(new PhpUserComparer(ctx, key_comparer), false, new PhpUserComparer(ctx, value_comparer), false));
            }

            return null;
        }

        #endregion

        #region array_column

        /// <summary>
        /// Return the values from a single column in the input array.
        /// </summary>
        /// <param name="input">A multi-dimensional array or an array of objects from which to pull a column of values from.</param>
        /// <param name="column_key">The column of values to return.
        /// This value may be an integer key of the column you wish to retrieve, or it may be a string key name
        /// for an associative array or property name.
        /// It may also be NULL to return complete arrays or objects (this is useful together with index_key to reindex the array).</param>
        /// <param name="index_key">The column to use as the index/keys for the returned array.
        /// This value may be the integer key of the column, or it may be the string key name.</param>
        /// <returns>Returns an array of values representing a single column from the input array.</returns>
        public static PhpArray array_column(PhpArray input, PhpValue column_key, PhpValue index_key = default)
        {
            if (input == null) throw new ArgumentException();

            var result = new PhpArray(input.Count);

            var key = Operators.IsSet(column_key)
                ? column_key.ToIntStringKey()
                : default(IntStringKey?);

            var ikey = Operators.IsSet(index_key)
                ? index_key.ToIntStringKey()
                : default(IntStringKey?);

            var enumerator = input.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                var columnn = (key.HasValue
                        ? enumerator.CurrentValue[key.Value] // TODO: Object's property
                        : enumerator.CurrentValue)
                    .DeepCopy();

                if (ikey.HasValue)
                {
                    var targetkey = enumerator.CurrentValue[ikey.Value].ToIntStringKey();
                    result[targetkey] = columnn;
                }
                else
                {
                    result.Add(columnn);
                }
            }

            //

            return result;
        }

        #endregion

        #region array_merge, array_merge_recursive

        /// <summary>
        /// Merges one or more arrays. Integer keys are changed to new ones, string keys are preserved.
        /// Values associated with existing string keys are be overwritten.
        /// </summary>
        /// <param name="arrays">Arrays to be merged.</param>
        /// <returns>
        /// The <see cref="PhpArray"/> containing items from all <paramref name="arrays"/>.
        /// Returns <c>null</c> in case of error.</returns>
        //[return: PhpDeepCopy]
        public static PhpArray array_merge(params PhpArray[] arrays)
        {
            // "arrays" argument is PhpArray[] => compiler generates code converting any value to PhpArray.
            // Note, PHP does reject non-array arguments.

            if (arrays == null || arrays.Length == 0)
            {
                return PhpArray.NewEmpty();
            }

            var result = new PhpArray(arrays[0] != null ? arrays[0].Count : 0);

            for (int i = 0; i < arrays.Length; i++)
            {
                if (arrays[i] == null)
                {
                    PhpException.Throw(PhpError.Warning, Resources.Resources.argument_not_array, (i + 1).ToString());
                    return null;
                }

                var enumerator = arrays[i].GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    var value = enumerator.CurrentValue.DeepCopy();

                    if (enumerator.CurrentKey.IsString)
                        result[enumerator.CurrentKey] = value;
                    else
                        result.Add(value);
                }
            }

            // results is inplace deeply copied if returned to PHP code:
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        /// <summary>
        /// Merges arrays recursively.
        /// </summary>
        /// <param name="array">The first array to merge.</param>
        /// <param name="arrays">The next arrays to merge.</param>
        /// <returns>An array containing items of all specified arrays.</returns>
        /// <remarks>
        /// Integer keys are reset so there cannot be a conflict among them. 
        /// Conflicts among string keys are resolved by merging associated values into arrays. 
        /// Merging is propagated recursively. Merged values are dereferenced. References are 
        /// preserved in non-merged values.
        /// </remarks>
        /// <exception cref="PhpException">Some array is a <B>null</B> reference (Warning).</exception>
        public static PhpArray array_merge_recursive(PhpArray array, params PhpArray[] arrays)
        {
            if (array == null || arrays == null)
            {
                //PhpException.ArgumentNull((array == null) ? "array" : "arrays");
                //return null;
                throw new ArgumentException();
            }

            for (int i = 0; i < arrays.Length; i++)
            {
                if (arrays[i] == null)
                {
                    //PhpException.Throw(PhpError.Warning, LibResources.GetString("argument_not_array", i + 2));
                    //return null;
                    throw new ArgumentException();
                }
            }

            return MergeRecursive(array, true, arrays);
        }

        /// <summary>
        /// Merges arrays recursively.
        /// </summary>
        /// <param name="array">The first array to merge.</param>
        /// <param name="arrays">The next arrays to merge.</param>
        /// <param name="deepCopy">Whether to deep copy merged items.</param>
        /// <returns>An array containing items of all specified arrays.</returns>
        private static PhpArray MergeRecursive(PhpArray array, bool deepCopy, params PhpArray[] arrays)
        {
            if (array == null) return null;

            PhpArray result = new PhpArray();
            array.AddTo(result, deepCopy);

            if (arrays != null)
            {
                for (int i = 0; i < arrays.Length; i++)
                {
                    if (arrays[i] != null)
                    {
                        if (!MergeRecursiveInternal(result, arrays[i], deepCopy))
                        {
                            //PhpException.Throw(PhpError.Warning, LibResources.GetString("recursion_detected"));
                            throw new ArgumentException();
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Adds items of "array" to "result" merging those whose string keys are the same.
        /// </summary>
        private static bool MergeRecursiveInternal(PhpArray/*!*/ result, PhpArray/*!*/ array, bool deepCopy)
        {
            var visited = new HashSet<object>();    // marks arrays that are being visited

            var iterator = array.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                var entry = iterator.Current;
                if (entry.Key.IsString)
                {
                    if (result.ContainsKey(entry.Key))
                    {
                        // the result array already contains the item => merging take place
                        var xv = result[entry.Key];
                        var y = entry.Value.GetValue();

                        // source item:
                        PhpValue x = xv.GetValue();

                        // if x is not a reference then we can reuse the ax array for the result
                        // since it has been deeply copied when added to the resulting array:
                        PhpArray item_result = (deepCopy && x.IsArray && !xv.IsAlias) ? x.Array : new PhpArray();

                        if (x.IsArray && y.IsArray)
                        {
                            var ax = x.Array;
                            var ay = y.Array;

                            if (ax != item_result)
                                ax.AddTo(item_result, deepCopy);

                            if (visited.Add(ax) == false && visited.Add(ay) == false)
                                return false;

                            // merges ay to the item result (may lead to stack overflow, 
                            // but only with both arrays recursively referencing themselves - who cares?):
                            bool finite = MergeRecursiveInternal(item_result, ay, deepCopy);

                            visited.Remove(ax);
                            visited.Remove(ay);

                            if (!finite) return false;
                        }
                        else
                        {
                            if (x.IsArray)
                            {
                                if (x.Array != item_result)
                                    x.Array.AddTo(item_result, deepCopy);
                            }
                            else
                            {
                                /*if (x != null)*/
                                item_result.Add(deepCopy ? x.DeepCopy() : x);
                            }

                            if (y.IsArray) y.Array.AddTo(item_result, deepCopy);
                            else /*if (y != null)*/ item_result.Add(deepCopy ? y.DeepCopy() : y);
                        }

                        result[entry.Key] = PhpValue.Create(item_result);
                    }
                    else
                    {
                        // PHP does no dereferencing when items are not merged:
                        result.Add(entry.Key, (deepCopy) ? entry.Value.DeepCopy() : entry.Value);
                    }
                }
                else
                {
                    // PHP does no dereferencing when items are not merged:
                    result.Add((deepCopy) ? entry.Value.DeepCopy() : entry.Value);
                }
            }

            return true;
        }

        #endregion

        #region array_change_key_case

        /// <summary>
        /// Converts string keys in <see cref="PhpArray"/> to lower case.
        /// </summary>
        /// <param name="array">The <see cref="PhpArray"/> to be converted.</param>
        /// <returns>The copy of <paramref name="array"/> with all string keys lower cased.</returns>
        /// <remarks>Integer keys as well as all values remain unchanged.</remarks>
        internal static PhpArray StringKeysToLower(PhpArray/*!*/ array)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            PhpArray result = new PhpArray();

            var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo; // cache current culture to avoid repetitious CurrentCulture.get

            var iterator = array.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                var entry = iterator.Current;
                if (entry.Key.IsString)
                {
                    result[textInfo.ToLower(entry.Key.String)] = entry.Value;
                }
                else
                    result[entry.Key] = entry.Value;
            }
            return result;
        }

        /// <summary>
        /// Converts string keys in <see cref="PhpArray"/> to upper case.
        /// </summary>
        /// <param name="array">The <see cref="PhpArray"/> to be converted.</param>
        /// <returns>The copy of <paramref name="array"/> with all string keys upper cased.</returns>
        /// <remarks>Integer keys as well as all values remain unchanged.</remarks>
        internal static PhpArray StringKeysToUpper(PhpArray/*!*/ array)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo; // cache current culture to avoid repetitious CurrentCulture.get

            var result = new PhpArray(array.Count);
            var iterator = array.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                var entry = iterator.Current;
                if (entry.Key.IsString)
                    result[textInfo.ToUpper(entry.Key.String)] = entry.Value;
                else
                    result[entry.Key] = entry.Value;
            }
            return result;
        }

        /// <summary>
        /// Converts string keys in <see cref="PhpArray"/> to lower case.
        /// </summary>
        /// <param name="array">The <see cref="PhpArray"/> to be converted.</param>
        /// <returns>The copy of <paramref name="array"/> with all string keys lower cased.</returns>
        /// <remarks>Integer keys as well as all values remain unchanged.</remarks>
        //[return: PhpDeepCopy]
        public static PhpArray array_change_key_case(PhpArray/*!*/ array)
        {
            PhpArray result = StringKeysToLower(array);
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        /// <summary>
        /// Converts string keys in <see cref="PhpArray"/> to specified case.
        /// </summary>
        /// <param name="array">The <see cref="PhpArray"/> to be converted.</param>
        /// <param name="keyCase">The <see cref="LetterCase"/> to convert keys to.</param>
        /// <returns>The copy of <paramref name="array"/> with all string keys lower cased.</returns>
        /// <remarks>Integer keys as well as all values remain unchanged.</remarks>
        //[return: PhpDeepCopy]
        public static PhpArray array_change_key_case(PhpArray array, LetterCase keyCase)
        {
            PhpArray result;
            switch (keyCase)
            {
                case LetterCase.Lower: result = StringKeysToLower(array); break;
                case LetterCase.Upper: result = StringKeysToUpper(array); break;

                default:
                    //PhpException.InvalidArgument("keyCase");
                    //goto case LetterCase.Upper;
                    throw new ArgumentException(nameof(keyCase));
            }
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        #endregion

        #region array_chunk

        /// <summary>
        /// Splits an array into chunks.
        /// </summary>
        /// <param name="array">The array to be split.</param>
        /// <param name="size">The number of items in each chunk (except for the last one where can be lesser items).</param>
        /// <param name="preserveKeys">Whether to preserve keys in chunks.</param>
        /// <returns>The array containing chunks indexed by integers starting from zero.</returns>
        /// <remarks>Chunks will contain deep copies of <paramref name="array"/> items.</remarks>
        public static PhpArray array_chunk(PhpArray array, int size, bool preserveKeys = false)
        {
            return ChunkInternal(array, size, preserveKeys, true);
        }

        /// <summary>
        /// Splits an array into chunks.
        /// </summary>
        /// <param name="array">The array to be split.</param>
        /// <param name="size">The number of items in each chunk (except for the last one where can be lesser items).</param>
        /// <param name="preserveKeys">Whether to preserve keys in chunks.</param>
        /// <returns>The array containing chunks indexed by integers starting from zero.</returns>
        internal static PhpArray Chunk(PhpArray array, int size, bool preserveKeys = false)
        {
            return ChunkInternal(array, size, preserveKeys, false);
        }

        /// <summary>
        /// Internal version of <see cref="Chunk"/> with deep-copy option.
        /// </summary>
        internal static PhpArray ChunkInternal(PhpArray array, int size, bool preserveKeys, bool deepCopy)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException(nameof(array));
            }
            if (size <= 0)
            {
                //PhpException.InvalidArgument("array", LibResources.GetString("arg_negative_or_zero"));
                //return null;
                throw new ArgumentException(nameof(size));
            }

            // nothing to do:
            if (array.Count == 0)
                return new PhpArray();

            // number of chunks:
            int count = (array.Count - 1) / size + 1; // = ceil(Count/size):

            PhpArray chunk;
            PhpArray result = new PhpArray(count);

            IEnumerator<KeyValuePair<IntStringKey, PhpValue>> iterator = array.GetEnumerator();

            // if deep-copies are required, wrapp iterator by enumerator making deep copies:
            if (deepCopy)
                iterator = PhpVariable.EnumerateDeepCopies(iterator);

            iterator.MoveNext();

            // all chunks except for the last one:
            for (int i = 0; i < count - 1; i++)
            {
                chunk = new PhpArray(size);

                if (preserveKeys)
                {
                    for (int j = 0; j < size; j++, iterator.MoveNext())
                        chunk.Add(iterator.Current.Key, iterator.Current.Value);
                }
                else
                {
                    for (int j = 0; j < size; j++, iterator.MoveNext())
                        chunk.Add(iterator.Current.Value);
                }

                result.Add(chunk);
            }

            // the last chunk:
            chunk = new PhpArray((size <= array.Count) ? size : array.Count);

            if (preserveKeys)
            {
                do { chunk.Add(iterator.Current.Key, iterator.Current.Value); } while (iterator.MoveNext());
            }
            else
            {
                do { chunk.Add(iterator.Current.Value); } while (iterator.MoveNext());
            }

            result.Add(chunk);

            // no deep copy is needed since it has already been done on chunks:
            return result;
        }

        #endregion

        #region array_count_values, array_unique

        /// <summary>
        /// Counts frequency of each value in an array.
        /// </summary>
        /// <param name="array">The array which values to count.</param>
        /// <returns>The array which keys are values of <paramref name="array"/> and values are their frequency.</returns>
        /// <remarks>
        /// Only <see cref="string"/> and <see cref="int"/> values are counted.
        /// Note, string numbers (e.g. "10") and their integer equivalents (e.g. 10) are counted separately.
        /// </remarks>
        /// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
        /// <exception cref="PhpException">A value is neither <see cref="string"/> nor <see cref="int"/>.</exception>
        public static PhpArray array_count_values(PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            PhpArray result = new PhpArray();

            var iterator = array.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                // dereferences value:
                var val = iterator.CurrentValue.GetValue();
                if (val.TryToIntStringKey(out IntStringKey key))
                {
                    var countval = result[key].ToLong();  // 0 for nonexisting entry
                    result[key] = PhpValue.Create(countval + 1L);
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, LibResources.neither_string_nor_integer_value, "count");
                }
            }

            // no need to deep copy (values are ints):
            return result;
        }

        /// <summary>
        /// Removes duplicate values from an array.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array which duplicate values to remove.</param>
        /// <param name="sortFlags">Specifies how the values are compared to be identical.</param>
        /// <returns>A copy of <paramref name="array"/> without duplicated values.</returns>
        /// <remarks>
        /// Values are compared using string comparison method (<see cref="ValueComparer.String"/>).  
        /// </remarks>
        /// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
        //[return: PhpDeepCopy]
        public static PhpArray array_unique(Context ctx, PhpArray array, ComparisonMethod sortFlags = ComparisonMethod.String)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            IComparer<PhpValue> comparer;
            switch (sortFlags)
            {
                case ComparisonMethod.Regular:
                    comparer = PhpComparer.Default; break;
                case ComparisonMethod.Numeric:
                    comparer = PhpNumericComparer.Default; break;
                case ComparisonMethod.String:
                    comparer = new PhpStringComparer(ctx); break;
                case ComparisonMethod.String | ComparisonMethod.FlagCase:
                    goto default;   // NOT IMPLEMENTED
                case ComparisonMethod.Natural:
                    comparer = new PhpNaturalComparer(ctx, false); break;
                case ComparisonMethod.Natural | ComparisonMethod.FlagCase:
                    comparer = new PhpNaturalComparer(ctx, caseInsensitive: true); break;
                case ComparisonMethod.LocaleString:
                    throw new NotImplementedException("array_unique( sortFlags: SORT_NATURAL )");
                //comparer = new PhpLocaleStringComparer(ctx); break;
                default:
                    //PhpException.ArgumentValueNotSupported("sortFlags", (int)sortFlags);
                    //return null;
                    throw new ArgumentException(nameof(sortFlags));
            }

            var result = new PhpArray(array.Count);

            var/*!*/identitySet = new HashSet<object>();

            // get only unique values - first found
            var enumerator = array.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                if (identitySet.Add(enumerator.CurrentValue.GetValue()))
                {
                    result.Add(enumerator.Current);
                }
            }

            //result.InplaceCopyOnReturn = true;
            return result;
        }

        #endregion

        #region array_flip

        /// <summary>
        /// Swaps all keys and their associated values in an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>An array containing entries which keys are values from the <paramref name="array"/>
        /// and which values are the corresponding keys.</returns>
        /// <remarks>
        /// <para>
        /// Values which are not of type <see cref="string"/> nor <see cref="int"/> are skipped 
        /// and for each such value a warning is reported. If there are more entries with the same 
        /// value in the <paramref name="array"/> the last key is considered others are ignored.
        /// String keys are converted using <see cref="Core.Convert.StringToArrayKey"/>.
        /// </para>
        /// <para>
        /// Unlike PHP this method doesn't return <B>false</B> on failure but a <B>null</B> reference.
        /// This is because it fails only if <paramref name="array"/> is a <B>null</B> reference.
        /// </para>
        /// </remarks>
        /// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference (Warning).</exception>
        /// <exception cref="PhpException">A value is neither <see cref="string"/> nor <see cref="int"/> (Warning).</exception>     
        public static PhpArray array_flip(PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            PhpArray result = new PhpArray(array.Count);

            var iterator = array.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                var entry = iterator.Current;
                // dereferences value:
                var val = entry.Value.GetValue();
                if (val.TryToIntStringKey(out IntStringKey key))
                {
                    result[key] = PhpValue.Create(entry.Key);
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, LibResources.neither_string_nor_integer_value, "flip");
                }
            }

            // no need to deep copy because values are ints/strings only (<= keys were int/strings only):
            return result;
        }

        #endregion

        #region array_keys, array_values, array_combine

        /// <summary>
        /// Retrieves an array of keys contained in a given array.
        /// </summary>
        /// <param name="array">An array which keys to get.</param>
        /// <returns><see cref="PhpArray"/> of <paramref name="array"/>'s keys.
        /// Keys in returned array are successive integers starting from zero.</returns>
        /// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
        public static PhpArray array_keys(PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return null; // NULL
            }

            // no need to make a deep copy since keys are immutable objects (strings, ints):
            var result = new PhpArray(array.Count);

            var enumerator = array.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                result.Add(PhpValue.Create(enumerator.CurrentKey));
            }

            return result;
        }

        /// <summary>
        /// Retrieves an array of some keys contained in a given array.
        /// </summary>
        /// <param name="array">An array which keys to get.</param>
        /// <param name="searchValue">Only the keys for this value are returned. 
        /// Values are compared using regular comparison method (<see cref="PhpComparer.CompareEq"/>).</param>
        /// <param name="strict">If true, uses strict comparison method (operator "===").</param>
        /// <returns>An array of keys being associated with specified value. 
        /// Keys in returned array are successive integers starting from zero.</returns>
        /// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
        public static PhpArray array_keys(PhpArray array, PhpValue searchValue, bool strict = false)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return null; // NULL
            }

            var result = new PhpArray();
            var enumerator = array.GetFastEnumerator();

            if (strict)
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.CurrentValue.StrictEquals(searchValue))
                        result.AddValue(PhpValue.Create(enumerator.CurrentKey));
                }
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.CurrentValue.Equals(searchValue))
                        result.AddValue(PhpValue.Create(enumerator.CurrentKey));
                }
            }

            // no need to make a deep copy since keys are immutable objects (strings, ints):
            return result;
        }

        /// <summary>
        /// Retrieves an array of values contained in a given array.
        /// </summary>
        /// <param name="array">An array which values to get.</param>
        /// <returns>A copy of <paramref name="array"/> with all keys indexed starting from zero.</returns>
        /// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
        /// <remarks>Doesn't dereference PHP references.</remarks>
        //[return: PhpDeepCopy]
        public static PhpArray array_values(PhpArray array)
        {
            if (array == null)
            {
                PhpException.ArgumentNull(nameof(array));
                return null;
            }

            // references are not dereferenced:
            var result = new PhpArray(array.Count);
            var enumerator = array.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.CurrentValue.DeepCopy());
            }

            // result is inplace deeply copied on return to PHP code:
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        /// <summary>
        /// Creates an array using one array for its keys and the second for its values.
        /// </summary>
        /// <param name="keys">The keys of resulting array.</param>
        /// <param name="values">The values of resulting array.</param>
        /// <returns>An array with keys from <paramref name="keys"/> values and values 
        /// from <paramref name="values"/> values.</returns>
        /// <remarks>
        /// <paramref name="keys"/> and <paramref name="values"/> should have the same length (zero is 
        /// adminssible - an empty array is returned).
        /// Keys are converted using <see cref="PHP.Core.Convert.ObjectToArrayKey"/> before hashed to resulting array.
        /// If more keys has the same value after conversion the last one is used.
        /// If a key is not a legal array key it is skipped.
        /// </remarks>
        /// <exception cref="PhpException"><paramref name="keys"/> or <paramref name="values"/> is a <B>null</B> reference.</exception>
        /// <exception cref="PhpException"><paramref name="keys"/> and <paramref name="values"/> has different length.</exception>
        /// <remarks>Doesn't dereference PHP references.</remarks>
        //[return: PhpDeepCopy]
        public static PhpArray array_combine(PhpArray keys, PhpArray values)
        {
            if (keys == null)
            {
                //PhpException.ArgumentNull("keys");
                //return null;
                throw new ArgumentNullException(nameof(keys));
            }

            if (values == null)
            {
                //PhpException.ArgumentNull("values");
                //return null;
                throw new ArgumentNullException(nameof(values));
            }

            if (keys.Count != values.Count)
            {
                //PhpException.Throw(PhpError.Warning, CoreResources.GetString("lengths_are_different", "keys", "values"));
                //return null;
                throw new ArgumentException();
            }

            IntStringKey key;

            PhpArray result = new PhpArray();
            var k_iterator = keys.GetFastEnumerator();
            var v_iterator = values.GetFastEnumerator();
            while (k_iterator.MoveNext())
            {
                v_iterator.MoveNext();

                // invalid keys are skipped, values are not dereferenced:
                if (Core.Convert.TryToIntStringKey(k_iterator.CurrentValue, out key))
                {
                    result[key] = v_iterator.CurrentValue;
                }
            }

            // result is inplace deeply copied on return to PHP code:
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        #endregion

        #region array_sum, array_product, array_reduce

        /// <summary>
        /// Sums all values in an array. Each value is converted to a number in the same way it is done by PHP.
        /// </summary>
        /// <exception cref="PhpException">Thrown if the <paramref name="array"/> argument is null.</exception>
        /// <returns>
        /// An integer, if all items are integers or strings converted to integers and the result is in integer range.
        /// A double, otherwise.
        /// </returns>
        public static PhpNumber array_sum(PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return 0.0;
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Count == 0)
            {
                return PhpNumber.Default;
            }

            PhpNumber result = PhpNumber.Default;
            PhpNumber num;

            var iterator = array.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                iterator.CurrentValue.ToNumber(out num);
                result += num;
            }

            //
            return result;
        }

        /// <summary>
        /// Computes a product of all values in an array. 
        /// Each value is converted to a number in the same way it is done by PHP.
        /// </summary>
        /// <exception cref="PhpException">Thrown if the <paramref name="array"/> argument is null.</exception>
        /// <returns>
        /// An integer, if all items are integers or strings converted to integers and the result is in integer range.
        /// A double, otherwise.
        /// </returns>
        public static PhpNumber array_product(PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return 0;
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Count == 0)
            {
                return PhpNumber.Default;
            }

            PhpNumber result = PhpNumber.Create(1L);
            PhpNumber num;

            var iterator = array.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                iterator.CurrentValue.ToNumber(out num);

                result *= num;
            }

            //
            return result;
        }

        public static PhpValue array_reduce(Context ctx, [In, Out] PhpArray array, IPhpCallable function)
        {
            return array_reduce(ctx, array, function, PhpValue.Null);
        }

        public static PhpValue array_reduce(Context ctx, [In, Out] PhpArray array, IPhpCallable function, PhpValue initialValue)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return PhpValue.Null;
                throw new ArgumentNullException(nameof(array));
            }

            //if (!PhpArgument.CheckCallback(function, caller, "function", 0, false)) return null;

            if (array.Count == 0)
            {
                return initialValue;
            }

            var args = new PhpValue[] { initialValue.DeepCopy(), PhpValue.Null };

            var iterator = array.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                args[1] = iterator.CurrentValueAliased;
                args[0] = function.Invoke(ctx, args);

                // CONSIDER: dereference the item if it wasn't alias before the operation
            }

            // dereferences the last returned value:
            return args[0].GetValue();
        }

        #endregion

        #region array_walk, array_walk_recursive

        struct ArrayWalker // : PhpVariableVisitor
        {
            readonly Context _ctx;
            readonly PhpValue[] _args; // [ &value, key, data ]
            readonly IPhpCallable _callback;
            readonly bool _recursive;

            HashSet<object> _visited;
            object _self;

            public ArrayWalker(Context ctx, IPhpCallable callback, PhpValue data = default, bool recursive = false)
            {
                _ctx = ctx;
                _callback = callback ?? throw new ArgumentNullException(nameof(callback));
                _recursive = recursive;
                _args = PrepareArgs(data);

                _visited = null;
                _self = null;
            }

            static PhpValue[] PrepareArgs(PhpValue data = default)
            {
                // prepares an array of callback's arguments (no deep copying needed because it is done so in callback):
                return (Operators.IsSet(data))
                    ? new PhpValue[] { PhpValue.CreateAlias(), PhpValue.Null, data }
                    : new PhpValue[] { PhpValue.CreateAlias(), PhpValue.Null };
            }

            public void Accept(PhpArray obj)
            {
                // recursion prevention
                if (_self == null)
                {
                    // do not allocate {_visited} for self
                    _self = obj;
                }
                else
                {
                    if (obj == _self)
                    {
                        // recursion
                        return;
                    }

                    // regular recursion prevention,
                    // allocate HashSet with visited arrays
                    if (_visited == null)
                    {
                        _visited = new HashSet<object>();
                    }

                    if (_visited.Add(obj) == false)
                    {
                        // recursion
                        return;
                    }
                }

                PhpArray tmp;
                var enumerator = obj.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    var value = enumerator.CurrentValue;

                    if (_recursive && (tmp = value.AsArray()) != null)
                    {
                        Accept(tmp);
                    }
                    else
                    {
                        // fills arguments for the callback:
                        _args[0].Alias.Value = value.GetValue();
                        _args[1] = PhpValue.Create(enumerator.CurrentKey);

                        // invoke callback:
                        _callback.Invoke(_ctx, _args);

                        // copy arg[0] back:
                        var newvalue = _args[0].Alias.Value;

                        if (value.IsAlias)
                        {
                            value.Alias.Value = newvalue;
                        }
                        else
                        {
                            //enumerator.CurrentValue = _args[0].Alias.Value;
                            obj.SetItemValue(enumerator.CurrentKey, newvalue); // ensures array is writeable and not shared
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies a user function or method on each element (value) of a specified dictionary.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array (or generic dictionary) to walk through.</param>
        /// <param name="callback">
        /// The callback called for each element of <paramref name="array"/>.
        /// The callback is assumed to have two or three parameters:
        /// <list type="number">
        ///   <item>
        ///     <term>
        ///       A value of dictionary entry. Can be specified with &amp; modifier which propagates any changes
        ///       make to the argument back to the entry. The dictionary can be changed in this way.
        ///     </term>
        ///   </item>
        ///   <item>A key of dictionary entry.</item>
        ///   <item>
        ///     Value of <paramref name="data"/> parameter if it is not a <B>null</B> reference.
        ///     Otherwise, the callback is assumed to have two parameters only.
        ///   </item>
        /// </list>
        /// </param>
        /// <param name="data">An additional parameter passed to <paramref name="callback"/> as its third parameter.</param>
        /// <returns><B>true</B>.</returns>
        /// <exception cref="PhpException"><paramref name="callback"/> or <paramref name="array"/> are <B>null</B> references.</exception>
        public static bool array_walk(Context ctx, [In, Out] PhpArray array, IPhpCallable callback, PhpValue data = default)
        {
            new ArrayWalker(ctx, callback, data, recursive: false).Accept(array);

            return true;
        }

        /// <summary>
        /// Applies a user function or method on each element (value) of a specified dictionary recursively.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to walk through.</param>
        /// <param name="callback">The callback called for each element of <paramref name="array"/>.</param>
        /// <param name="data">An additional parameter passed to <paramref name="callback"/> as its third parameter.</param>
        /// <exception cref="PhpException"><paramref name="callback"/> or <paramref name="array"/> are <B>null</B> references.</exception>
        /// <remarks><seealso cref="Walk"/>.</remarks>
        public static bool array_walk_recursive(Context ctx, [In, Out] PhpArray array, IPhpCallable callback, PhpValue data = default)
        {
            new ArrayWalker(ctx, callback, data, recursive: true).Accept(array);

            return true;
        }

        /// <summary>
        /// Prepares a walk for <see cref="array_walk(Context, PhpArray, IPhpCallable, PhpValue)"/> and <see cref="array_walk_recursive(Context, PhpArray, IPhpCallable, PhpValue)"/> methods.
        /// </summary>
        /// <exception cref="PhpException"><paramref name="callback"/> or <paramref name="array"/> are <B>null</B> references.</exception>
        private static PhpValue[] PrepareWalk(IDictionary array, IPhpCallable callback, PhpValue data = default)
        {
            if (callback == null)
            {
                //PhpException.ArgumentNull("callback");
                // return null;
                throw new ArgumentNullException(nameof(callback));
            }

            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException(nameof(array));
            }

            // prepares an array of callback's arguments (no deep copying needed because it is done so in callback):
            return (Operators.IsSet(data))
                ? new PhpValue[] { PhpValue.CreateAlias(), PhpValue.Null, data }
                : new PhpValue[] { PhpValue.CreateAlias(), PhpValue.Null };
        }

        /// <summary>
        /// Visits an entry of array which <see cref="array_walk(Context, PhpArray, IPhpCallable, PhpValue)"/> or <see cref="array_walk_recursive(Context, PhpArray, IPhpCallable, PhpValue)"/> is walking through.
        /// </summary>
        private static void VisitEntryOnWalk(Context ctx,
            KeyValuePair<IntStringKey, PhpValue> entry,
            IDictionary<IntStringKey, PhpValue> array,
            IPhpCallable callback, PhpValue[] args)
        {
            Debug.Assert(args[0].IsAlias);

            // fills arguments for the callback:
            args[0].Alias.Value = entry.Value.GetValue();
            args[1] = PhpValue.Create(entry.Key);

            // invoke callback:
            callback.Invoke(ctx, args);

            // loads a new value from a reference:
            if (entry.Value.IsAlias)
            {
                entry.Value.Alias.Value = args[0].Alias.Value;
            }
            else
            {
                array[entry.Key] = args[0].Alias.Value;
            }
        }

        #endregion

        #region array_filter

        /// <summary>
        /// <see cref="array_filter(Context, PhpArray, IPhpCallable, ArrayFilterFlags)"/> options.
        /// </summary>
        public enum ArrayFilterFlags
        {
            UseValue = 0,
            UseBoth = 1,
            UseKey = 2,
        }

        /// <summary>
        /// Pass key as the only argument to callback instead of the value.
        /// </summary>
        public const int ARRAY_FILTER_USE_KEY = (int)ArrayFilterFlags.UseKey;

        /// <summary>
        /// Pass both value and key as arguments to callback instead of the value.
        /// </summary>
        public const int ARRAY_FILTER_USE_BOTH = (int)ArrayFilterFlags.UseBoth;

        /// <summary>
        /// Retuns the specified array.
        /// see http://php.net/manual/en/function.array-filter.php
        /// </summary>
        public static PhpArray array_filter(PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException(nameof(array));
            }

            var result = new PhpArray();

            var enumerator = array.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                var entry = enumerator.Current;
                if (entry.Value.ToBoolean())
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        /// <summary>
        /// Filters an array using a specified callback.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="array">The array to be filtered.</param>
        /// <param name="callback">
        /// The callback called on each value in the <paramref name="array"/>. 
        /// If the callback returns value convertible to <B>true</B> the value is copied to the resulting array.
        /// Otherwise, it is ignored.
        /// </param>
        /// <param name="flag">Optional. Flag determining what arguments are sent to <paramref name="callback"/>.</param>
        /// <returns>An array of unfiltered items.</returns>
        //[return: PhpDeepCopy]
        public static PhpArray array_filter(Context ctx /*, caller*/, PhpArray array, IPhpCallable callback, ArrayFilterFlags flag = ArrayFilterFlags.UseValue)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException(nameof(array));
            }

            if (callback == null)
            {
                //PhpException.ArgumentNull("callback");
                //return null;
                throw new ArgumentNullException(nameof(callback));
            }

            var result = new PhpArray(array.Count);
            var args = new PhpValue[(flag == ArrayFilterFlags.UseBoth) ? 2 : 1];

            var iterator = array.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                var entry = iterator.Current;

                // no deep copying needed because it is done so in callback:

                switch (flag)
                {
                    case ArrayFilterFlags.UseBoth:
                        args[0] = entry.Value;
                        args[1] = PhpValue.Create(entry.Key);
                        break;
                    case ArrayFilterFlags.UseKey:
                        args[0] = PhpValue.Create(entry.Key);
                        break;
                    default:
                        args[0] = entry.Value;
                        break;
                }

                // adds entry to the resulting array if callback returns true:
                if (callback.Invoke(ctx, args).ToBoolean())
                {
                    result.Add(entry);
                }
            }

            // values should be inplace deeply copied:
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        #endregion

        #region array_map


        /// <summary>
        /// Default callback for <see cref="Map"/>.
        /// </summary>
        /// <returns>A delegate returning <see cref="PhpArray"/> containing items on the stack (passed as arguments).</returns>
        private static readonly IPhpCallable _mapIdentity = PhpCallback.Create((ctx, args) =>
        {
            var result = new PhpArray(args.Length);

            for (int i = 0; i < args.Length; i++)
            {
                result.Add(args[i].DeepCopy());
            }

            return PhpValue.Create(result);
        });

        /// <summary>
        /// Applies a callback function on specified tuples one by one storing its results to an array.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="map">
        /// A callback to be called on tuples. The number of arguments should be the same as
        /// the number of arrays specified by <pramref name="arrays"/>.
        /// Arguments passed by reference modifies elements of <pramref name="arrays"/>.
        /// A <B>null</B> means default callback which makes integer indexed arrays from the tuples is used. 
        /// </param>
        /// <param name="arrays">Arrays where to load tuples from. </param>
        /// <returns>An array of return values of the callback
        /// keyed by keys of the <paramref name="arrays"/> if it
        /// is a single array or by integer keys starting from 0.</returns>
        /// <remarks>
        /// <para>
        /// In the <I>i</I>-th call the <I>j</I>-th parameter of the callback will be 
        /// the <I>i</I>-th value of the <I>j</I>-the array or a <B>null</B> if that array 
        /// has less then <I>i</I> entries.
        /// </para>
        /// <para>
        /// If the callback assigns a value to a parameter passed by reference in the <I>i</I>-the call 
        /// and the respective array contains at least <I>i</I> elements the assigned value is propagated 
        /// to the array.
        /// </para>
        /// </remarks>
        public static PhpArray array_map(Context ctx /*, caller*/, IPhpCallable map, [In, Out] params PhpArray[] arrays)
        {
            if (map != null && !PhpVariable.IsValidBoundCallback(ctx, map))
            {
                PhpException.InvalidArgument(nameof(map));
                return null;
            }

            //if (!PhpArgument.CheckCallback(map, caller, "map", 0, true)) return null;
            if (arrays == null || arrays.Length == 0)
            {
                PhpException.InvalidArgument(nameof(arrays), LibResources.arg_null_or_empty);
                return null;
            }

            // if callback has not been specified uses the default one:
            if (map == null)
            {
                map = _mapIdentity;
            }

            int count = arrays.Length;
            bool preserve_keys = count == 1;
            var args = new PhpValue[count];
            var iterators = new OrderedDictionary.FastEnumerator[count];
            PhpArray result;

            // initializes iterators and args array, computes length of the longest array:
            int max_count = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                var array = arrays[i];

                if (array == null)
                {
                    PhpException.Throw(PhpError.Warning, LibResources.argument_not_array, (i + 2).ToString());// +2 (first arg is callback) 
                    return null;
                }

                iterators[i] = array.GetFastEnumerator();
                if (array.Count > max_count) max_count = array.Count;
            }

            // keys are preserved in a case of a single array and re-indexed otherwise:
            result = new PhpArray(arrays[0].Count);

            for (; ; )
            {
                bool hasvalid = false;

                // fills args[] with items from arrays:
                for (int i = 0; i < arrays.Length; i++)
                {
                    if (!iterators[i].IsDefault)
                    {
                        if (iterators[i].MoveNext())
                        {
                            hasvalid = true;

                            // note: deep copy is not necessary since a function copies its arguments if needed:
                            args[i] = iterators[i].CurrentValue;
                            // TODO: throws if the CurrentValue is an alias
                        }
                        else
                        {
                            args[i] = PhpValue.Null;
                            iterators[i] = default;   // IsDefault, !IsValid
                        }
                    }
                }

                if (!hasvalid) break;

                // invokes callback:
                var return_value = map.Invoke(ctx, args);

                // return value is not deeply copied:
                if (preserve_keys)
                {
                    result.Add(iterators[0].CurrentKey, return_value);
                }
                else
                {
                    result.Add(return_value);
                }

                // loads new values (callback may modify some by ref arguments):
                for (int i = 0; i < arrays.Length; i++)
                {
                    if (iterators[i].IsValid)
                    {
                        var item = iterators[i].CurrentValue;
                        if (item.IsAlias)
                        {
                            item.Alias.Value = args[i].GetValue();
                        }
                        else
                        {
                            iterators[i].CurrentValue = args[i].GetValue();
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region array_replace, array_replace_recursive

        /// <summary>
        /// array_replace() replaces the values of the first array with the same values from
        /// all the following arrays. If a key from the first array exists in the second array,
        /// its value will be replaced by the value from the second array. If the key exists in
        /// the second array, and not the first, it will be created in the first array. If a key
        /// only exists in the first array, it will be left as is. If several arrays are passed
        /// for replacement, they will be processed in order, the later arrays overwriting the
        /// previous values.
        ///  
        /// array_replace() is not recursive : it will replace values in the first array by
        /// whatever type is in the second array. 
        /// </summary>
        /// <param name="array">The array in which elements are replaced. </param>
        /// <param name="arrays">The arrays from which elements will be extracted. </param>
        /// <returns>Deep copy of array with replacements. Returns an array, or NULL if an error occurs. </returns>
        //[return: PhpDeepCopy]
        public static PhpArray array_replace([In, Out] PhpArray array, params PhpArray[] arrays)
        {
            return ArrayReplaceImpl(array, arrays, false);
        }

        /// <summary>
        ///  array_replace_recursive() replaces the values of the first array with the same values
        ///  from all the following arrays. If a key from the first array exists in the second array,
        ///  its value will be replaced by the value from the second array. If the key exists in the
        ///  second array, and not the first, it will be created in the first array. If a key only
        ///  exists in the first array, it will be left as is. If several arrays are passed for
        ///  replacement, they will be processed in order, the later array overwriting the previous
        ///  values.
        ///  
        /// array_replace_recursive() is recursive : it will recurse into arrays and apply the same
        /// process to the inner value.
        /// 
        /// When the value in array is scalar, it will be replaced by the value in array1, may it be
        /// scalar or array. When the value in array and array1 are both arrays, array_replace_recursive()
        /// will replace their respective value recursively. 
        /// </summary>
        /// <param name="array">The array in which elements are replaced. </param>
        /// <param name="arrays">The arrays from which elements will be extracted.</param>
        /// <returns>Deep copy of array with replacements. Returns an array, or NULL if an error occurs. </returns>
        //[return: PhpDeepCopy]
        public static PhpArray array_replace_recursive([In, Out] PhpArray array, params PhpArray[] arrays)
        {
            return ArrayReplaceImpl(array, arrays, true);
        }

        /// <remarks>Performs deep copy of array, return array with replacements.</remarks>
        internal static PhpArray ArrayReplaceImpl(PhpArray array, PhpArray[] arrays, bool recursive)
        {
            PhpArray result = array.DeepCopy();

            if (arrays != null)
            {
                for (int i = 0; i < arrays.Length; i++)
                {
                    ArrayReplaceImpl(result, arrays[i], recursive);
                }
            }

            //// if called by PHP language then all items in the result should be in place deeply copied:
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        /// <summary>
        /// Performs replacements on deeply-copied array. Performs deep copies of replace values.
        /// </summary>
        internal static void ArrayReplaceImpl(PhpArray array, PhpArray replaceWith, bool recursive)
        {
            if (array != null && replaceWith != null)
            {
                var iterator = replaceWith.GetFastEnumerator();
                while (iterator.MoveNext())
                {
                    PhpValue tmp;
                    var entry = iterator.Current;
                    if (recursive && entry.Value.IsArray && (tmp = array[entry.Key]).IsArray)
                    {
                        ArrayReplaceImpl(tmp.Array, entry.Value.Array, true);
                    }
                    else
                    {
                        array[entry.Key] = entry.Value.DeepCopy();
                    }
                }
            }
        }

        #endregion
    }
}
