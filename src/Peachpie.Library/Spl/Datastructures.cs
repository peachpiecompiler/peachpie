using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Spl
{
    // TODO:
    //SplDoublyLinkedList
    //SplStack
    //SplQueue
    //SplHeap
    //SplMaxHeap
    //SplMinHeap
    //SplPriorityQueue
    //SplObjectStorage

    [PhpType("[name]")]
    public class SplFixedArray : ArrayAccess, Iterator, Countable
    {
        /// <summary>
        /// Internal array storage. <c>null</c> reference if the size is <c>0</c>.
        /// </summary>
        private PhpValue[] _array = null;

        /// <summary>
        /// Iterator position in the array.
        /// </summary>
        private long _position = 0;

        #region Helper methods

        protected void ReallocArray(long newsize)
        {
            Debug.Assert(newsize >= 0);

            // empty the array
            if (newsize <= 0)
            {
                _array = null;
                return;
            }

            // resize the array
            var newarray = new PhpValue[newsize];
            var oldsize = (_array != null) ? _array.Length : 0;

            if (_array != null)
            {
                Array.Copy(_array, newarray, Math.Min(_array.Length, newarray.Length));
            }

            _array = newarray;

            // mark new elements as not set
            for (int i = oldsize; i < _array.Length; i++)
            {
                _array[i] = PhpValue.Void;
            }
        }

        protected bool IsValidInternal()
        {
            return (_position >= 0 && _array != null && _position < _array.Length);
        }

        protected long SizeInternal()
        {
            return (_array != null) ? _array.Length : 0;
        }

        protected void IndexCheckHelper(long index)
        {
            if (index < 0 || _array == null || index >= _array.Length)
            {
                //Exception.ThrowSplException(
                //    _ctx => new RuntimeException(_ctx, true),
                //    context,
                //    CoreResources.spl_index_invalid, 0, null);
                throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region SplFixedArray

        public SplFixedArray(long size = 0)
        {
            __construct(size);
        }

        /// <summary>
        /// Constructs an <see cref="SplFixedArray"/> object.
        /// </summary>
        /// <param name="size">The initial array size.</param>
        /// <returns></returns>
        public virtual void __construct(long size = 0)
        {
            if (size < 0)
            {
                PhpException.InvalidArgument(nameof(size));
            }

            ReallocArray(size);
        }

        /// <summary>
        /// Import the PHP array array in a new SplFixedArray instance.
        /// </summary>
        /// <param name="array">Source array.</param>
        /// <param name="save_indexes">Whether to preserve integer indexes.</param>
        /// <returns>New instance of <see cref="SplFixedArray"/> with copies of elements from <paramref name="array"/>.</returns>
        public static SplFixedArray fromArray(PhpArray array, bool save_indexes = true)
        {
            if (array == null || array.Count == 0)
            {
                return new SplFixedArray();
            }

            SplFixedArray result;

            using (var enumerator = array.GetFastEnumerator())
            {
                if (save_indexes)
                {
                    result = new SplFixedArray(array.MaxIntegerKey + 1);
                    while (enumerator.MoveNext())
                    {
                        var key = enumerator.CurrentKey;
                        if (key.IsString) throw new ArgumentException();
                        result._array[key.Integer] = enumerator.CurrentValue.DeepCopy();
                    }
                }
                else
                {
                    result = new SplFixedArray(array.Count);
                    int i = 0;
                    while (enumerator.MoveNext())
                    {
                        result._array[i++] = enumerator.CurrentValue.DeepCopy();
                    }
                }
            }

            //
            return result;
        }

        public virtual PhpArray toArray()
        {
            if (_array == null) return PhpArray.NewEmpty();

            var result = new PhpArray(_array.Length);

            for (int i = 0; i < _array.Length; i++)
            {
                result[i] = _array[i];
            }

            return result;
        }

        public virtual object getSize() => count();

        public virtual void setSize(long size)
        {
            if (size < 0)
            {
                // TODO: error
            }
            else
            {
                ReallocArray(size);
            }
        }

        public virtual void __wakeup()
        {
            // TODO: wakeup all the elements
        }

        #endregion

        #region interface Iterator

        /// <summary>
        /// Rewinds the iterator to the first element.
        /// </summary>
        public void rewind() { _position = 0; }

        /// <summary>
        /// Moves forward to next element.
        /// </summary>
        public void next() { _position++; }

        /// <summary>
        /// Checks if there is a current element after calls to <see cref="rewind"/> or <see cref="next"/>.
        /// </summary>
        /// <returns><c>bool</c>.</returns>
        public bool valid() { return IsValidInternal(); }

        /// <summary>
        /// Returns the key of the current element.
        /// </summary>
        public PhpValue key() { return (PhpValue)_position; }

        /// <summary>
        /// Returns the current element (value).
        /// </summary>
        public PhpValue current() { return IsValidInternal() ? _array[_position] : PhpValue.Void; }

        #endregion

        #region interface ArrayAccess

        /// <summary>
        /// Returns the value at specified offset.
        /// </summary>
        public PhpValue offsetGet(PhpValue offset)
        {
            var i = offset.ToLong();
            IndexCheckHelper(i);
            return _array[i];
        }

        /// <summary>
        /// Assigns a value to the specified offset.
        /// </summary>
        public void offsetSet(PhpValue offset, PhpValue value)
        {
            var i = offset.ToLong();
            IndexCheckHelper(i);
            _array[i] = value;
        }

        /// <summary>
        /// Unsets an offset.
        /// </summary>
        public void offsetUnset(PhpValue offset) => offsetSet(offset, PhpValue.Void);

        /// <summary>
        /// Whether an offset exists.
        /// </summary>
        /// <remarks>This method is executed when using isset() or empty().</remarks>
        public bool offsetExists(PhpValue offset)
        {
            var i = offset.ToLong();
            return i >= 0 && _array != null && i < _array.Length && _array[i].IsSet;
        }

        #endregion

        #region interface Countable

        /// <summary>
        /// Count elements of an object.
        /// </summary>
        /// <returns>The custom count as an integer.</returns>
        /// <remarks>This method is executed when using the count() function on an object implementing <see cref="Countable"/>.</remarks>
        public long count() { return SizeInternal(); }

        #endregion
    }
}
