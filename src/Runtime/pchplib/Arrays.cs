using Pchp.Core;
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
    public static class Arrays
    {
        #region Constants

        public const int SORT_REGULAR = (int)ComparisonMethod.Regular;
        public const int SORT_NUMERIC = (int)ComparisonMethod.Numeric;
        public const int SORT_STRING = (int)ComparisonMethod.String;
        public const int SORT_LOCALE_STRING = (int)ComparisonMethod.LocaleString;

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
        public static PhpValue current(IPhpEnumerable array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            if (array.IntrinsicEnumerator.AtEnd)
                return PhpValue.False;

            // TODO: dereferences result since enumerator doesn't do so:
            return array.IntrinsicEnumerator.CurrentValue;
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
        public static object pos(IPhpEnumerable array) => current(array);

        /// <summary>
        /// Retrieves a key being pointed by an array intrinsic enumerator.
        /// </summary>
        /// <param name="array">The array which current key to return.</param>
        /// <returns>
        /// <b>Null</b>, if the intrinsic enumerator is behind the last item of <paramref name="array"/>, 
        /// otherwise the key being pointed by the enumerator.
        /// </returns>
        public static PhpValue key(IPhpEnumerable array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            if (array.IntrinsicEnumerator.AtEnd)
                return PhpValue.Null;

            // note, key can't be of type PhpReference, hence no dereferencing follows:
            return array.IntrinsicEnumerator.CurrentKey.GetValue();
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
        /// <include file='Doc/Arrays.xml' path='docs/intrinsicEnumeration/*'/>
        public static PhpValue next(IPhpEnumerable array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            // moves to the next item and returns false if there is no such item:
            if (!array.IntrinsicEnumerator.MoveNext()) return PhpValue.False;

            // TODO: dereferences result since enumerator doesn't do so:
            return array.IntrinsicEnumerator.CurrentValue;
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
        public static PhpValue prev(IPhpEnumerable array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            // moves to the previous item and returns false if there is no such item:
            // TODO: dereferences result since enumerator doesn't do so:
            return (array.IntrinsicEnumerator.MovePrevious())
                ? array.IntrinsicEnumerator.CurrentValue
                : PhpValue.False;
        }

        /// <summary>
        /// Moves array intrinsic enumerator so it will point to the last item of the array.
        /// </summary>
        /// <param name="array">The array which intrinsic enumerator to move.</param>
        /// <returns>The last value in the <paramref name="array"/> or <b>false</b> if <paramref name="array"/> 
        /// is empty.</returns>
        /// <remarks>The value returned is dereferenced.</remarks>
        public static PhpValue end(IPhpEnumerable array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            // moves to the last item and returns false if there is no such item:
            // TODO: dereferences result since enumerator doesn't do so:
            return (array.IntrinsicEnumerator.MoveLast())
                ? array.IntrinsicEnumerator.CurrentValue
                : PhpValue.False;
        }

        /// <summary>
        /// Moves array intrinsic enumerator so it will point to the first item of the array.
        /// </summary>
        /// <param name="array">The array which intrinsic enumerator to move.</param>
        /// <returns>The first value in the <paramref name="array"/> or <b>false</b> if <paramref name="array"/> 
        /// is empty.</returns>
        /// <remarks>The value returned is dereferenced.</remarks>
        public static PhpValue reset(IPhpEnumerable array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                return PhpValue.Null;
            }

            // moves to the last item and returns false if there is no such item:
            // TODO: dereferences result since enumerator doesn't do so:
            return (array.IntrinsicEnumerator.MoveFirst())
                ? array.IntrinsicEnumerator.CurrentValue
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
        /// <include file='Doc/Arrays.xml' path='docs/intrinsicEnumeration/*'/>
        //[return: CastToFalse, PhpDeepCopy]
        public static PhpArray each(IPhpEnumerable array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                return null;
            }

            if (array.IntrinsicEnumerator.AtEnd)
                return null;

            var entry = array.IntrinsicEnumerator.Current;
            array.IntrinsicEnumerator.MoveNext();

            // dereferences result since enumerator doesn't do so:
            var key = entry.Key;
            var value = entry.Value; // PhpVariable.Dereference(entry.Value);

            // creates the resulting array:
            PhpArray result = new PhpArray(2);
            result.Add(1, value);
            result.Add("value", value);
            result.Add(0, key);
            result.Add("key", key);

            // keys and values should be inplace deeply copied:
            // TODO: result.InplaceCopyOnReturn = true;
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
        public static PhpValue array_pop(PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            if (array.Count == 0) return PhpValue.Null;

            // dereferences result since the array doesn't do so:
            var result = (array.RemoveLast().Value);    // TODO: PhpVariable.Dereference

            array.RefreshMaxIntegerKey();
            array.RestartIntrinsicEnumerator();

            return result;
        }

        /// <summary>
        /// Adds multiple items into an array.
        /// </summary>
        /// <param name="array">The array where to add values.</param>
        /// <param name="vars">The array of values to add.</param>
        /// <returns>The number of items in array after all items was added.</returns>
        public static int array_push(PhpArray array, params PhpValue[] vars)
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
                array.Add(vars[i]);
            }

            return array.Count;
        }

        /// <summary>
        /// Removes the first item of an array and reindex integer keys starting from zero.
        /// </summary>
        /// <param name="array">The array to be shifted.</param>
        /// <returns>The removed object.</returns>
        /// <remarks>Resets intrinsic enumerator.</remarks>
        public static PhpValue array_shift(PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            if (array.Count == 0) return PhpValue.Null;

            // dereferences result since the array doesn't do so:
            var result = array.RemoveFirst().Value;  // TODO: PhpVariable.Dereference

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
        public static int array_unshift(PhpArray array, params PhpValue[] vars)
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
                array.Prepend(i, vars[i]);
            }

            return array.Count;
        }

        /// <summary>
        /// Returns array which elements are taken from a specified one in reversed order.
        /// Integer keys are reindexed starting from zero.
        /// </summary>
        /// <param name="array">The array to be reversed.</param>
        /// <returns>The array <paramref name="array"/> with items in reversed order.</returns>
        public static PhpArray array_reverse(PhpArray array)
        {
            return array_reverse(array, false);
        }

        /// <summary>
        /// Returns array which elements are taken from a specified one in reversed order.
        /// </summary>
        /// <param name="array">The array to be reversed.</param>
        /// <param name="preserveKeys">Whether keys should be left untouched. 
        /// If set to <b>false</b> then integer keys are reindexed starting from zero.</param>
        /// <returns>The array <paramref name="array"/> with items in reversed order.</returns>
        public static PhpArray array_reverse(PhpArray array, bool preserveKeys)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            PhpArray result = new PhpArray();

            var e = array.GetFastEnumerator();

            if (preserveKeys)
            {
                // changes only the order of elements:
                while (e.MoveNext())
                {
                    result.Prepend(e.CurrentKey, e.CurrentValue);
                }
            }
            else
            {
                // changes the order of elements and reindexes integer keys:
                int i = array.IntegerCount;
                while (e.MoveNext())
                {
                    var key = e.CurrentKey;
                    result.Prepend(key.IsString ? key : new IntStringKey(--i), e.CurrentValue);
                }
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
        /// <param name="offset">The ordinal number of a first item of the slice.</param>
        /// <returns>The slice of <paramref name="array"/>.</returns>
        /// <remarks>
        /// The same as <see cref="array_slice(PhpArray,int,int)"/> where <c>length</c> is infinity. 
        /// <seealso cref="PhpMath.AbsolutizeRange"/>. Resets integer keys.
        /// </remarks>
        public static PhpArray array_slice(PhpArray array, int offset)
        {
            return array_slice(array, offset, int.MaxValue, false);
        }

        /// <summary>
        /// Retrieves a slice of specified array.
        /// </summary>
        /// <param name="array">The array which slice to get.</param>
        /// <param name="offset">The relativized offset of the first item of the slice.</param>
        /// <param name="length">The relativized length of the slice.</param>
        /// <returns>The slice of <paramref name="array"/>.</returns>
        /// <remarks>
        /// See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> and 
        /// <paramref name="length"/>. Resets integer keys.
        /// </remarks>
        public static PhpArray array_slice(PhpArray array, int offset, int length)
        {
            return array_slice(array, offset, length, false);
        }

        /// <summary>
        /// Retrieves a slice of specified array.
        /// </summary>
        /// <param name="array">The array which slice to get.</param>
        /// <param name="offset">The relativized offset of the first item of the slice.</param>
        /// <param name="length">The relativized length of the slice.</param>
        /// <param name="preserveKeys">Whether to preserve integer keys. If <B>false</B>, the integer keys are reset.</param>
        /// <returns>The slice of <paramref name="array"/>.</returns>
        /// <remarks>
        /// See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> and <paramref name="length"/>.
        /// </remarks>
        public static PhpArray array_slice(PhpArray array, int offset, int length, bool preserveKeys)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            // absolutizes range:
            PhpMath.AbsolutizeRange(ref offset, ref length, array.Count);

            var iterator = array.GetFastEnumerator();

            // moves iterator to the first item of the slice;
            // starts either from beginning or from the end (which one is more efficient):
            if (offset < array.Count - offset)
            {
                for (int i = -1; i < offset; i++)
                    if (iterator.MoveNext() == false)
                        break;
            }
            else
            {
                for (int i = array.Count; i > offset; i--)
                    if (iterator.MovePrevious() == false)
                        break;
            }

            // copies the slice:
            PhpArray result = new PhpArray(length);
            int ikey = 0;
            for (int i = 0; i < length; i++)
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
        /// Removes a slice of an array.
        /// </summary>
        /// <param name="array">The array which slice to remove.</param>
        /// <param name="offset">The relativized offset of a first item of the slice.</param>
        /// <remarks>
        /// <para>Items from <paramref name="offset"/>-th to the last one are removed from <paramref name="array"/>.</para>
        /// </remarks>
        /// <para>See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/>.</para>
        public static PhpArray array_splice(PhpArray array, int offset)
        {
            // Splice would be equivalent to SpliceDc if no replacelent is specified (=> no SpliceDc):
            return array_splice(array, offset, int.MaxValue, PhpValue.Null);
        }

        /// <summary>
        /// Removes a slice of an array.
        /// </summary>
        /// <param name="array">The array which slice to remove.</param>
        /// <param name="offset">The relativized offset of a first item of the slice.</param>
        /// <param name="length">The relativized length of the slice.</param>
        /// <remarks>
        /// <para><paramref name="length"/> items are removed from <paramref name="array"/> 
        /// starting with the <paramref name="offset"/>-th one.</para>
        /// </remarks>
        /// <para>See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/>.</para>
        public static PhpArray array_splice(PhpArray array, int offset, int length)
        {
            // Splice would be equivalent to SpliceDc if no replacement is specified (=> no SpliceDc):
            return array_splice(array, offset, length, PhpValue.Null);
        }

        /// <summary>
        /// Replaces a slice of an array with specified item(s).
        /// </summary>
        /// <remarks>
        /// <para>The same as <see cref="Splice(PhpArray,int,int,object)"/> except for that
        /// replacement items are deeply copied to the <paramref name="array"/>.</para>
        /// </remarks>
        public static PhpArray array_splice(PhpArray array, int offset, int length, PhpValue replacement)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return null;
                throw new ArgumentNullException();
            }

            return SpliceInternal(array, offset, length, replacement, true);
        }

        /// <summary>
        /// Replaces a slice of an array with specified item(s).
        /// </summary>
        /// <param name="array">The array which slice to replace.</param>
        /// <param name="offset">The relativized offset of a first item of the slice.</param>
        /// <param name="length">The relativized length of the slice.</param>
        /// <param name="replacement"><see cref="PhpArray"/> of items to replace the splice or a single item.</param>
        /// <returns>The <see cref="PhpArray"/> of replaced items indexed by integers starting from zero.</returns>
        /// <remarks>
        /// <para>See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> and <paramref name="length"/>.</para>
        /// <para>Reindexes all integer keys in resulting array.</para>
        /// </remarks>
        internal static PhpArray Splice(PhpArray array, int offset, int length, PhpValue replacement)
        {
            if (array == null)
            {
                //PhpException.Throw(
                //    PhpError.Warning,
                //    string.Format(Strings.unexpected_arg_given, "array", PhpArray.PhpTypeName, PhpVariable.TypeNameNull));
                //return null;
                throw new ArgumentNullException();
            }

            return SpliceInternal(array, offset, length, replacement, false);
        }

        /// <summary>
        /// Implementation of <see cref="array_splice(PhpArray,int,int,object)"/> and <see cref="array_splice(PhpArray,int,int,object)"/>.
        /// </summary>
        /// <remarks>Whether to make a deep-copy of items in the replacement.</remarks>
        internal static PhpArray SpliceInternal(PhpArray array, int offset, int length, PhpValue replacement, bool deepCopy)
        {
            Debug.Assert(array != null);
            int count = array.Count;

            // converts offset and length to interval [first,last]:
            PhpMath.AbsolutizeRange(ref offset, ref length, count);

            PhpArray result = new PhpArray(length);

            // replacement is an array:
            if (replacement.IsArray)
            {
                // provides deep copies:
                IEnumerable<PhpValue> e = replacement.Array.Values;

                if (deepCopy)
                {
                    e = e.Select(Operators.DeepCopy);
                }

                // does replacement:
                array.ReindexAndReplace(offset, length, e, result);
            }
            else if (replacement.IsNull)
            {
                // replacement is null:

                array.ReindexAndReplace(offset, length, null, result);
            }
            else
            {
                // replacement is another type //

                // creates a deep copy:
                if (deepCopy) replacement = replacement.DeepCopy();

                // does replacement:
                array.ReindexAndReplace(offset, length, new[] { replacement }, result);
            }

            return result;
        }

        #endregion

        #region shuffle, array_rand

        /// <summary>
        /// Randomizes the order of elements in the array using PhpMath random numbers generator.
        /// </summary>
        /// <exception cref="PhpException">Thrown if the <paramref name="array"/> argument is null.</exception>
        /// <remarks>Reindexes all keys in the resulting array.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool shuffle(PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ArgumentNull("array");
                //return false;
                throw new ArgumentNullException();
            }

            array.Shuffle(PhpMath.Generator);
            array.ReindexAll();

            return true;
        }

        /// <summary>
        /// Returns a key of an entry chosen at random using PhpMath random numbers generator.
        /// </summary>
        /// <param name="array">The array which to choose from.</param>
        /// <returns>The chosen key.</returns>
        /// <exception cref="System.NullReferenceException"><paramref name="array"/> is a <B>null</B> reference.</exception>
        public static PhpValue array_rand(PhpArray array)
        {
            return array_rand(array, 1);
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
        public static PhpValue array_rand(PhpArray array, int count)
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
                // TODO: PhpException.ArgumentNull("array");
                return false;
            }
            if (result == null)
            {
                // TODO: PhpException.ArgumentNull("result");
                return false;
            }
            if (generator == null)
            {
                // TODO: PhpException.ArgumentNull("generator");
                return false;
            }
            if (count < 1 || count > source.Count)
            {
                // TODO: PhpException.InvalidArgument("count", LibResources.GetString("number_of_items_not_between_one_and_item_count", count, source.Count));
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
                // TODO: PhpException.ArgumentNull("array");
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
        /// <returns>Whether there is the <paramref name="needle"/> value in the <see cref="PhpArray"/>.</returns>
        /// <remarks>Regular comparison (<see cref="PhpComparer.CompareEq(object,object)"/>) is used for comparing values.</remarks>
        /// <exception cref="PhpException"><paramref name="haystack"/> is a <B>null</B> reference (Warning).</exception>
        public static bool in_array(PhpValue needle, PhpArray haystack)
        {
            var b = array_search(needle, haystack, false);
            return !b.IsBoolean || b.Boolean;
        }

        /// <summary>
        /// Checks if a value exists in an array.
        /// </summary>
        /// <param name="needle">The value to search for.</param>
        /// <param name="haystack">The <see cref="PhpArray"/> where to search.</param>
        /// <param name="strict">Whether strict comparison method (operator ===) is used for comparing values.</param>
        /// <returns>Whether there is the <paramref name="needle"/> value in the <see cref="PhpArray"/>.</returns>
        /// <exception cref="PhpException"><paramref name="haystack"/> is a <B>null</B> reference (Warning).</exception>
        public static bool in_array(PhpValue needle, PhpArray haystack, bool strict)
        {
            var b = array_search(needle, haystack, strict);
            return !b.IsBoolean || b.Boolean;
        }

        /// <summary>
        /// Searches the array for a given value and returns the corresponding key if successful.
        /// </summary>
        /// <param name="needle">The value to search for.</param>
        /// <param name="haystack">The <see cref="PhpArray"/> where to search.</param>
        /// <returns>The key associated with the <paramref name="needle"/> or <B>false</B> if there is no such key.</returns>
        /// <remarks>Regular comparison (<see cref="PhpComparer.CompareEq(object,object)"/>) is used for comparing values.</remarks>
        /// <exception cref="PhpException"><paramref name="haystack"/> is a <B>null</B> reference (Warning).</exception>
        public static PhpValue array_search(PhpValue needle, PhpArray haystack)
        {
            return array_search(needle, haystack, false);
        }

        /// <summary>
        /// Searches the array for a given value and returns the corresponding key if successful.
        /// </summary>
        /// <param name="needle">The value to search for.</param>
        /// <param name="haystack">The <see cref="PhpArray"/> where to search.</param>
        /// <param name="strict">Whether strict comparison method (operator ===) is used for comparing values.</param>
        /// <returns>The key associated with the <paramref name="needle"/> or <B>false</B> if there is no such key.</returns>
        /// <exception cref="PhpException"><paramref name="haystack"/> is a <B>null</B> reference (Warning).</exception>
        public static PhpValue array_search(PhpValue needle, PhpArray haystack, bool strict)
        {
            // result needn't to be deeply copied because it is a key of an array //

            if (haystack == null)
            {
                // TODO: PhpException.ArgumentNull("haystack");
                return PhpValue.False;
            }

            // using operator ===:
            if (strict)
            {
                using (var enumerator = haystack.GetFastEnumerator())
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

                using (var enumerator = haystack.GetFastEnumerator())
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
            if (count <= 0)
            {
                // TODO: PhpException.InvalidArgument("count", LibResources.GetString("arg:negative_or_zero"));
                return null;
            }

            PhpArray result = new PhpArray(count, 0);
            int last = startIndex + count;
            for (int i = startIndex; i < last; i++)
                result.Add(i, value);

            // makes deep copies of all added items:
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        public static PhpArray array_fill_keys(PhpArray keys, PhpValue value)
        {
            if (keys == null)
            {
                // PhpException.ArgumentNull("keys");
                // return null;
                throw new ArgumentNullException();
            }

            var result = new PhpArray(keys.Count);
            var iterator = keys.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                IntStringKey key = iterator.CurrentValue.ToIntStringKey();
                //if (Core.Convert.ObjectToArrayKey(x.Value, out key) && !result.ContainsKey(key))
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
                // PhpException.ArgumentNull("array");
                // return null;
                throw new ArgumentNullException();
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
                //PhpException.InvalidArgument("step", LibResources.GetString("arg:zero"));
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
                //PhpException.InvalidArgument("step", LibResources.GetString("arg:zero"));
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
                //PhpException.InvalidArgument("step", LibResources.GetString("arg:zero"));
                //step = 1;
                throw new ArgumentException();
            }

            if (step < 0) step = -step;

            PhpArray result = new PhpArray(Math.Abs(high - low) / step + 1, 0);
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
        /// <param name="low">Lower bound of the interval.</param>
        /// <param name="high">Upper bound of the interval.</param>
        /// <returns>The array.</returns>
        public static PhpArray range(Context ctx, PhpValue low, PhpValue high) => range(ctx, low, high, PhpValue.Create(1));

        /// <summary>
        /// Creates an array containing range of elements with arbitrary step.
        /// </summary>
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

                    case ComparisonMethod.LocaleString:
                        return new KeyComparer(Locale.GetStringComparer(ctx, false), order == SortingOrder.Descending);

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

                    case ComparisonMethod.LocaleString:
                        return new ValueComparer(Locale.GetStringComparer(ctx, false), order == SortingOrder.Descending);

                    default:
                        return (order == SortingOrder.Descending) ? ValueComparer.Reverse : ValueComparer.Default;
                }
            }
        }

        #endregion

        #region sort,asort,ksort,rsort,arsort,krsort

        /// <summary>
        /// Sorts an array using regular comparison method for comparing values.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool sort(Context ctx, [In, Out]PhpArray array)
        {
            return sort(ctx, array, ComparisonMethod.Regular);
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing values.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool sort(Context ctx, [In, Out]PhpArray array, ComparisonMethod comparisonMethod)
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
        /// Sorts an array using regular comparison method for comparing values preserving key-value associations.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool asort(Context ctx, [In, Out]PhpArray array)
        {
            return asort(ctx, array, ComparisonMethod.Regular);
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing values preserving key-value associations.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool asort(Context ctx, [In, Out]PhpArray array, ComparisonMethod comparisonMethod)
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
        /// Sorts an array using regular comparison method for comparing keys.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool ksort(Context ctx, [In, Out]PhpArray array)
        {
            return ksort(ctx, array, ComparisonMethod.Regular);
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing keys.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of keys.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool ksort(Context ctx, [In, Out]PhpArray array, ComparisonMethod comparisonMethod)
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
        /// Sorts an array using regular comparison method for comparing values in reverse order.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool rsort(Context ctx, [In, Out]PhpArray array)
        {
            return rsort(ctx, array, ComparisonMethod.Regular);
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing values in reverse order.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of keys.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool rsort(Context ctx, [In, Out]PhpArray array, ComparisonMethod comparisonMethod)
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
        /// Sorts an array using regular comparison method for comparing values in reverse order 
        /// preserving key-value associations.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool arsort(Context ctx, [In, Out]PhpArray array)
        {
            return arsort(ctx, array, ComparisonMethod.Regular);
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing values in reverse order
        /// preserving key-value associations.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool arsort(Context ctx, [In, Out]PhpArray array, ComparisonMethod comparisonMethod)
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
        /// Sorts an array using regular comparison method for comparing keys in reverse order.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool krsort(Context ctx, [In, Out] PhpArray array)
        {
            return krsort(ctx, array, ComparisonMethod.Regular);
        }

        /// <summary>
        /// Sorts an array using specified comparison method for comparing keys in reverse order.
        /// </summary>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="comparisonMethod">The method to be used for comparison of keys.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool krsort(Context ctx, [In, Out] PhpArray array, ComparisonMethod comparisonMethod)
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
        /// <param name="caller">The class context used to bind the callback.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="compare">The user callback to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool usort(Context ctx, [In, Out] PhpArray array, IPhpCallable compare)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }
            if (compare == null || !compare.IsValid) return false;

            // sorts array using callback for comparisons:
            array.Sort(new ValueComparer(new PhpUserComparer(ctx, compare), false));

            array.ReindexAll();
            array.RestartIntrinsicEnumerator();

            return true;
        }

        /// <summary>
        /// Sorts an array user comparison callback method for comparing values preserving key-value associations.
        /// </summary>
        /// <param name="caller">The class context used to bind the callback.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="compare">The user callback to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool uasort(Context ctx, [In, Out] PhpArray array, IPhpCallable compare)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }
            if (compare == null || !compare.IsValid) return false;

            // sorts array using callback for comparisons:
            array.Sort(new ValueComparer(new PhpUserComparer(ctx, compare), false));

            return true;
        }

        /// <summary>
        /// Sorts an array using user comparison callback for comparing keys.
        /// </summary>
        /// <param name="caller">The class context used to bind the callback.</param>
        /// <param name="array">The array to be sorted.</param>
        /// <param name="compare">The user callback to be used for comparison of values.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool uksort(Context ctx, [In, Out] PhpArray array, IPhpCallable compare)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }
            if (compare == null || !compare.IsValid) return false;

            array.Sort(new KeyComparer(new PhpUserComparer(ctx, compare), false));

            return true;
        }

        #endregion

        #region natsort,natcasesort

        /// <summary>
        /// Sorts an array using case sensitive natural comparison method for comparing 
        /// values preserving key-value association.
        /// </summary>
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
        /// <param name="array">The array to be sorted.</param>
        /// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        public static bool natcasesort(Context ctx, [In, Out] PhpArray array)
        {
            if (array == null)
            {
                //PhpException.ReferenceNull("array");
                //return false;
                throw new ArgumentNullException();
            }

            array.Sort(new ValueComparer(new PhpNaturalComparer(ctx, true), false));

            return true;
        }

        #endregion

        #region array_multisort

        /// <summary>
        /// Resolves arguments passed to <see cref="MultiSort"/> method according to PHP manual for <c>array_multisort</c> function.
        /// </summary>
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

                if (arg.IsArray)
                {
                    var array = arg.Array;

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
                else if (arg.TypeCode == PhpTypeCode.Long)
                {
                    var num = (int)arg.ToLong();

                    switch (num)
                    {
                        case (int)ComparisonMethod.Numeric:
                        case (int)ComparisonMethod.Regular:
                        case (int)ComparisonMethod.String:
                        case (int)ComparisonMethod.LocaleString:
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

                        case (int)SortingOrder.Ascending:
                        case (int)SortingOrder.Descending:
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
                    //PhpException.Throw(PhpError.Warning, LibResources.GetString("argument_not_array_or_sort_flag", i));
                    //return 0;
                    throw new ArgumentException();
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
        /// <param name="first">The first array to be sorted.</param>
        /// <param name="args">Arrays to be sorted along with flags affecting sort order and 
        /// comparison methods to be used. See PHP manual for more details.</param>
        /// <returns>Whether arrays were sorted successfully.</returns>
        /// <remarks>Reindexes integer keys in the sorted arrays and restarts their intrinsic enumerators.</remarks>
        /// <exception cref="PhpException"><paramref name="first"/> is a <B>null</B> reference (Warning).</exception>
        /// <exception cref="PhpException">Arrays has different lengths (Warning).</exception>
        /// <exception cref="PhpException">Invalid sorting flags (Warning).</exception>
        /// <exception cref="PhpException">Multiple sorting flags applied on single array (Warning).</exception>
        public static bool array_multisort(Context ctx, [In, Out] PhpArray first, params PhpValue[] args)
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
            PhpHashtable.Sort(arrays, comparers);

            for (int i = 0; i < length; i++)
            {
                arrays[i].ReindexIntegers(0);
                arrays[i].RestartIntrinsicEnumerator();
            }

            return true;
        }

        #endregion
    }
}
