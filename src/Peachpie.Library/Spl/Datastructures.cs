using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Spl
{
    #region SplFixedArray

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
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

            var result = new SplFixedArray(array.Count);

            using (var enumerator = array.GetFastEnumerator())
            {
                if (save_indexes)
                {
                    while (enumerator.MoveNext())
                    {
                        var key = enumerator.CurrentKey;
                        if (key.IsString) throw new ArgumentException();

                        if (key.Integer >= result.SizeInternal())
                        {
                            result.ReallocArray(key.Integer);
                        }

                        result._array[key.Integer] = enumerator.CurrentValue.DeepCopy();
                    }
                }
                else
                {
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

        public virtual long getSize() => count();

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

    #endregion

    #region SplDoublyLinkedList

    /// <summary>
    /// The SplDoublyLinkedList class provides the main functionalities of a doubly linked list.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplDoublyLinkedList : Iterator, ArrayAccess, Countable
    {
        public void __construct() => throw new NotImplementedException();
        public virtual void add(PhpValue index, PhpValue newval) => throw new NotImplementedException();
        public virtual PhpValue bottom() => throw new NotImplementedException();
        public virtual int getIteratorMode() => throw new NotImplementedException();
        public virtual bool isEmpty() => throw new NotImplementedException();
        public virtual PhpValue pop() => throw new NotImplementedException();
        public virtual void prev() => throw new NotImplementedException();
        public virtual void push(PhpValue value) => throw new NotImplementedException();
        public virtual string serialize() => throw new NotImplementedException();
        public virtual void setIteratorMode(long mode) => throw new NotImplementedException();
        public virtual PhpValue shift() => throw new NotImplementedException();
        public virtual PhpValue top() => throw new NotImplementedException();
        public virtual void unserialize(string serialized) => throw new NotImplementedException();
        public virtual void unshift(PhpValue value) => throw new NotImplementedException();

        public virtual long count() => throw new NotImplementedException();

        public PhpValue offsetGet(PhpValue offset)
        {
            throw new NotImplementedException();
        }

        public void offsetSet(PhpValue offset, PhpValue value)
        {
            throw new NotImplementedException();
        }

        public void offsetUnset(PhpValue offset)
        {
            throw new NotImplementedException();
        }

        public bool offsetExists(PhpValue offset)
        {
            throw new NotImplementedException();
        }

        public void rewind()
        {
            throw new NotImplementedException();
        }

        public void next()
        {
            throw new NotImplementedException();
        }

        public bool valid()
        {
            throw new NotImplementedException();
        }

        public PhpValue key()
        {
            throw new NotImplementedException();
        }

        public PhpValue current()
        {
            throw new NotImplementedException();
        }
    }


    #endregion

    #region SplQueue

    /// <summary>
    /// The SplQueue class provides the main functionalities of a queue implemented using a doubly linked list.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplQueue : SplDoublyLinkedList, Iterator, ArrayAccess, Countable
    {
        public const int IT_MODE_LIFO = 2;
        public const int IT_MODE_FIFO = 0;
        public const int IT_MODE_DELETE = 1;
        public const int IT_MODE_KEEP = 0;

        public virtual PhpValue dequeue() => throw new NotImplementedException();
        public virtual void enqueue(PhpValue value) => throw new NotImplementedException();
        public virtual void setIteratorMode(int mode) => throw new NotImplementedException();
    }

    #endregion

    #region SplStack

    /// <summary>
    /// The SplStack class provides the main functionalities of a stack implemented using a doubly linked list.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplStack : SplDoublyLinkedList, Iterator, ArrayAccess, Countable
    {
        public virtual void setIteratorMode(int mode) => throw new NotImplementedException();
    }

    #endregion

    #region SplPriorityQueue

    /// <summary>
    /// The SplPriorityQueue class provides the main functionalities of a prioritized queue, implemented using a max heap.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplPriorityQueue : Iterator, Countable
    {
        public void __construct() => throw new NotImplementedException();
        public virtual long compare(PhpValue priority1, PhpValue priority2) => throw new NotImplementedException();
        public virtual long count() => throw new NotImplementedException();
        public virtual PhpValue current() => throw new NotImplementedException();
        public virtual PhpValue extract() => throw new NotImplementedException();
        public virtual int getExtractFlags() => throw new NotImplementedException();
        public virtual void insert(PhpValue value, PhpValue priority) => throw new NotImplementedException();
        public virtual bool isCorrupted() => throw new NotImplementedException();
        public virtual bool isEmpty() => throw new NotImplementedException();
        public virtual PhpValue key() => throw new NotImplementedException();
        public virtual void next() => throw new NotImplementedException();
        public virtual void recoverFromCorruption() => throw new NotImplementedException();
        public virtual void rewind() => throw new NotImplementedException();
        public virtual void setExtractFlags(int flags) => throw new NotImplementedException();
        public virtual PhpValue top() => throw new NotImplementedException();
        public virtual bool valid() => throw new NotImplementedException();
    }

    #endregion

    #region SplHeap, SplMinHeap, SplMaxHeap 

    /// <summary>
    /// The SplHeap class provides the main functionalities of a Heap.
    /// </summary>
    public abstract class SplHeap : Iterator, Countable
    {
        public virtual void __construct() => throw new NotImplementedException();
        protected abstract long compare(PhpValue value1, PhpValue value2);
        public virtual long count() => throw new NotImplementedException();
        public virtual PhpValue current() => throw new NotImplementedException();
        public virtual PhpValue extract() => throw new NotImplementedException();
        public virtual void insert(PhpValue value) => throw new NotImplementedException();
        public virtual bool isCorrupted() => throw new NotImplementedException();
        public virtual bool isEmpty() => throw new NotImplementedException();
        public virtual PhpValue key() => throw new NotImplementedException();
        public virtual void next() => throw new NotImplementedException();
        public virtual void recoverFromCorruption() => throw new NotImplementedException();
        public virtual void rewind() => throw new NotImplementedException();
        public virtual PhpValue top() => throw new NotImplementedException();
        public virtual bool valid() => throw new NotImplementedException();
    }

    /// <summary>
    /// The SplMinHeap class provides the main functionalities of a heap, keeping the minimum on the top.
    /// </summary>
    public class SplMinHeap : SplHeap
    {
        /// <summary>
        /// Compare elements in order to place them correctly in the heap while sifting up
        /// </summary>
        protected override long compare(PhpValue value1, PhpValue value2)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// The SplMaxHeap class provides the main functionalities of a heap, keeping the maximum on the top.
    /// </summary>
    public class SplMaxHeap : SplHeap
    {
        /// <summary>
        /// Compare elements in order to place them correctly in the heap while sifting up
        /// </summary>
        protected override long compare(PhpValue value1, PhpValue value2)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
