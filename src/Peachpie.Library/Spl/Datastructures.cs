using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        protected readonly Context _ctx;

        /// <summary>
        /// SPL collections Iterator Mode constants
        /// </summary>
        [PhpHidden]
        public enum SPL_ITERATOR_MODE
        {
            IT_MODE_KEEP = 0,

            IT_MODE_FIFO = 0,

            IT_MODE_DELETE = 1,

            IT_MODE_LIFO = 2
        }

        public const int IT_MODE_KEEP = 0;
        public const int IT_MODE_FIFO = 0;
        public const int IT_MODE_DELETE = 1;
        public const int IT_MODE_LIFO = 2;

        // LinkedList holding the values for the doubly linked list
        LinkedList<PhpValue> baseList;

        // The current node used for iteration, and its index
        LinkedListNode<PhpValue> currentNode;
        private int index = -1;

        //Current iteration mode
        private SPL_ITERATOR_MODE iteratorMode = IT_MODE_KEEP;

        public SplDoublyLinkedList(Context ctx)
        {
            _ctx = ctx;
            __construct();
        }
        public SplDoublyLinkedList()
        {
            __construct();
        }

        public void __construct()
        {
            baseList = new LinkedList<PhpValue>();
        }
        public virtual void add(PhpValue index, PhpValue newval)
        {
            int indexBefore = -1;
            if (index.IsInteger())
                indexBefore = (int)index.ToLong();
            else
                if (!Int32.TryParse(index.ToString(), out indexBefore))
                throw new OutOfRangeException("Index could not be parsed as an integer.");

            //Special cases of addin the first or last item have to be taken care of separately
            if (index == 0)
            {
                baseList.AddFirst(newval);
                return;
            }
            else if (index == baseList.Count())
            {
                baseList.AddLast(newval);
                return;
            }
            else
                indexBefore--;

            var nodeBefore = GetNodeAtIndex(indexBefore);
            baseList.AddAfter(nodeBefore, newval);
        }
        public virtual PhpValue bottom()
        {
            if (baseList.Count == 0)
                throw new RuntimeException("The list is empty");

            return baseList.First();
        }
        public virtual int getIteratorMode()
        {
            return (int)iteratorMode;
        }
        public virtual bool isEmpty()
        {
            return baseList.Count == 0;
        }
        public virtual PhpValue pop()
        {
            if (baseList.Count == 0)
                throw new RuntimeException("The list is empty");

            var value = baseList.Last();
            baseList.RemoveLast();
            return value;
        }
        public virtual void prev()
        {
            if (valid())
            {
                if(iteratorMode == SPL_ITERATOR_MODE.IT_MODE_DELETE)
                {
                    var newNode = currentNode.Previous;
                    baseList.Remove(currentNode);
                    currentNode = newNode;
                } else
                {
                    currentNode = currentNode.Previous;
                    index--;
                }
            }
        }
        public virtual void push(PhpValue value)
        {
            baseList.AddLast(value);
        }
        public virtual void setIteratorMode(long mode)
        {
            if(Enum.IsDefined(typeof(SPL_ITERATOR_MODE), mode))
            {
                iteratorMode = (SPL_ITERATOR_MODE)mode;
            } else
            {
                throw new ArgumentException("Argument value is not an iterator mode.");
            }
        }
        public virtual PhpValue shift()
        {
            if (baseList.Count == 0)
                throw new RuntimeException("The list is empty");

            var value = baseList.First();
            baseList.RemoveFirst();
            return value;
        }
        public virtual PhpValue top()
        {
            if (baseList.Count == 0)
                throw new RuntimeException("The list is empty");

            return baseList.Last();
        }
        public virtual void unshift(PhpValue value)
        {
            baseList.AddFirst(value);
        }

        public virtual long count()
        {
            return baseList.Count;
        }

        public PhpValue offsetGet(PhpValue offset)
        {
            var node = GetNodeAtIndex(offset);

            Debug.Assert(node != null);

            if (node != null)
                return node.Value;
            else
                return PhpValue.Null;
        }

        public void offsetSet(PhpValue offset, PhpValue value)
        {
            var node = GetNodeAtIndex(offset);

            Debug.Assert(node != null);

            if (node != null)
                node.Value = value;
        }

        public void offsetUnset(PhpValue offset)
        {
            var node = GetNodeAtIndex(offset);

            Debug.Assert(node != null);

            if (node != null)
                baseList.Remove(node);
        }

        public bool offsetExists(PhpValue offset)
        {
            int offsetInt = -1;
            if (offset.IsInteger())
                offsetInt = (int)offset.ToLong();
            else
                if (!Int32.TryParse(offset.ToString(), out offsetInt))
                throw new OutOfRangeException("Offset could not be parsed as an integer.");

            if (offsetInt < 0 || offsetInt >= baseList.Count)
                return false;
            else
                return true;
        }

        public void rewind()
        {
            if (baseList.Count > 0)
            {
                currentNode = baseList.First;
                index = 0;
            }
            else
            {
                currentNode = null;
                index = -1;
            }
        }

        public void next()
        {
            if (valid())
            {
                if (iteratorMode == SPL_ITERATOR_MODE.IT_MODE_DELETE)
                {
                    var newNode = currentNode.Next;
                    baseList.Remove(currentNode);
                    currentNode = newNode;
                } else
                {
                    currentNode = currentNode.Next;
                    index++;
                }
            }
        }

        public bool valid()
        {
            return (baseList.Count > 0 && currentNode != null);
        }

        public PhpValue key()
        {
            return index;
        }

        public PhpValue current()
        {
            return valid() ? currentNode.Value : PhpValue.Null;
        }

        /// <summary>
        /// Gets a serialized string representation of the List.
        /// </summary>
        public virtual string serialize(Context _ctx)
        {
            // i:{iterator_mode};:i:{item0};:i:{item1},...;

            var result = new PhpString.Blob();
            var serializer = PhpSerialization.PhpSerializer.Instance;

            // x:i:{iterator_mode};
            result.Append("i:");
            result.Append(serializer.Serialize(_ctx, (int)this.iteratorMode, default(RuntimeTypeHandle)));
            result.Append(";");

            // :i:{item}
            foreach (var item in baseList)
            {
                result.Append(":i:");
                result.Append(serializer.Serialize(_ctx, item, default(RuntimeTypeHandle)));
                result.Append(";");
            }

            return result.ToString();
        }

        /// <summary>
        /// Constructs a new SplDoublyLinkedList out of a serialized string representation
        /// </summary>
        public virtual SplDoublyLinkedList unserialize(PhpString serialized)
        {
            // i:{iterator_mode};:i:{item0};:i:{item1},...;

            if (serialized.Length < 12) throw new ArgumentException(nameof(serialized)); // quick check

            SplDoublyLinkedList sdll = new SplDoublyLinkedList(_ctx);

            var stream = new MemoryStream(serialized.ToBytes(_ctx));
            try
            {
                PhpValue tmp;
                var reader = new PhpSerialization.PhpSerializer.ObjectReader(_ctx, stream, default(RuntimeTypeHandle));

                // i:
                if (stream.ReadByte() != 'i') throw new InvalidDataException();
                if (stream.ReadByte() != ':') throw new InvalidDataException();

                tmp = reader.Deserialize();
                if (tmp.TypeCode != PhpTypeCode.Long) throw new InvalidDataException();
                int iteratorMode = (int)tmp.ToLong();

                // skip the ';'
                if (stream.ReadByte() != ';') throw new InvalidDataException();

                // :i:{item}
                while (stream.ReadByte() != ':')
                {
                    if (stream.ReadByte() != 'i') throw new InvalidDataException();
                    if (stream.ReadByte() != ':') throw new InvalidDataException();

                    var obj = reader.Deserialize();

                    sdll.push(obj);

                    if (stream.ReadByte() != ';')
                        throw new InvalidDataException();
                }

            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Notice,
                    Resources.LibResources.deserialization_failed, e.Message, stream.Position.ToString(), stream.Length.ToString());
            }

            return sdll;
        }

        #endregion

        private LinkedListNode<PhpValue> GetNodeAtIndex(PhpValue index)
        {
            int indexInt = -1;
            if (index.IsInteger())
                indexInt = (int)index.ToLong();
            else
                if (!Int32.TryParse(index.ToString(), out indexInt))
                throw new OutOfRangeException("Index could not be parsed as an integer.");

            return GetNodeAtIndex(indexInt);
        }

        private LinkedListNode<PhpValue> GetNodeAtIndex(int index)
        {
            if (index < 0 || index > baseList.Count())
                throw new OutOfRangeException("Index out of range");

            LinkedListNode<PhpValue> element = baseList.First;
            for (int i = 0; i < index; i++)
                element = element.Next;

            return element;
        }
    }

    #region SplQueue

    /// <summary>
    /// The SplQueue class provides the main functionalities of a queue implemented using a doubly linked list.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplQueue : SplDoublyLinkedList, Iterator, ArrayAccess, Countable
    {
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
