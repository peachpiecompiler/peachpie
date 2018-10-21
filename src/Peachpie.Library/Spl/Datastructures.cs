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
        /// <summary>
        /// Runtime context.
        /// </summary>
        protected readonly Context/*!*/_ctx;

        /// <summary>
        /// SPL collections Iterator Mode constants
        /// </summary>
        [PhpHidden]
        [Flags]
        public enum SplIteratorMode
        {
            Lifo = 2,
            Fifo = 0,
            Delete = 1,
            Keep = 0
        }

        public const int IT_MODE_LIFO = (int)SplIteratorMode.Lifo;
        public const int IT_MODE_FIFO = (int)SplIteratorMode.Fifo;
        public const int IT_MODE_DELETE = (int)SplIteratorMode.Delete;
        public const int IT_MODE_KEEP = (int)SplIteratorMode.Keep;

        /// <summary>
        /// The underlying LinkedList holding the values of the PHP doubly linked list.
        /// </summary>
        private readonly LinkedList<PhpValue> _baseList = new LinkedList<PhpValue>();

        /// <summary>
        /// The current node used for iteration, and its index.
        /// </summary>
        LinkedListNode<PhpValue> currentNode;
        private int index = -1;

        /// <summary>
        /// Current iteration mode.
        /// </summary>
        protected SplIteratorMode iteratorMode = SplIteratorMode.Keep;

        public SplDoublyLinkedList(Context ctx)
        {
            _ctx = ctx;
        }

        public virtual void __construct() { /* nothing */ }

        public virtual void add(PhpValue index, PhpValue newval)
        {
            var indexval = GetValidIndex(index);

            // Special cases of addin the first or last item have to be taken care of separately
            if (indexval == 0)
            {
                _baseList.AddFirst(newval);
            }
            else if (indexval == _baseList.Count())
            {
                _baseList.AddLast(newval);
            }
            else
            {
                var nodeBefore = GetNodeAtIndex(indexval - 1);
                _baseList.AddAfter(nodeBefore, newval);
            }
        }

        public virtual PhpValue bottom()
        {
            if (_baseList.Count == 0)
                throw new RuntimeException("The list is empty");

            return _baseList.First();
        }

        public virtual int getIteratorMode()
        {
            return (int)iteratorMode;
        }

        public virtual bool isEmpty()
        {
            return _baseList.Count == 0;
        }

        public virtual PhpValue pop()
        {
            if (_baseList.Count == 0)
                throw new RuntimeException("The list is empty");

            var value = _baseList.Last();
            _baseList.RemoveLast();
            return value;
        }

        public virtual void prev()
        {
            if (valid())
            {
                MoveCurrentPointer(false);
            }
        }

        public virtual void push(PhpValue value)
        {
            _baseList.AddLast(value);
        }

        public virtual void setIteratorMode(long mode)
        {
            // mode is within all possible combinations of SplIteratorMode
            if (mode >= IT_MODE_KEEP && mode <= (IT_MODE_LIFO & IT_MODE_DELETE))
            {
                iteratorMode = (SplIteratorMode)mode;
            }
            else
            {
                throw new ArgumentException("Argument value is not an iterator mode.");
            }
        }

        public virtual PhpValue shift()
        {
            if (_baseList.Count == 0)
                throw new RuntimeException("The list is empty");

            var value = _baseList.First();
            _baseList.RemoveFirst();
            return value;
        }

        public virtual PhpValue top()
        {
            if (_baseList.Count == 0)
                throw new RuntimeException("The list is empty");

            return _baseList.Last();
        }

        public virtual void unshift(PhpValue value)
        {
            _baseList.AddFirst(value);
        }

        public virtual long count()
        {
            return _baseList.Count;
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
                _baseList.Remove(node);
        }

        public bool offsetExists(PhpValue offset)
        {
            if (!offset.TryToIntStringKey(out var key) || key.IsString)
                throw new OutOfRangeException("Offset could not be parsed as an integer.");

            var offsetInt = key.Integer;

            return offsetInt >= 0 && offsetInt < count();
        }

        public void rewind()
        {
            if (_baseList.Count != 0)
            {
                if ((iteratorMode & SplIteratorMode.Lifo) != 0)
                {
                    currentNode = _baseList.Last;
                    index = _baseList.Count - 1;
                }
                else
                {
                    currentNode = _baseList.First;
                    index = 0;
                }
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
                MoveCurrentPointer(true);
            }
        }

        public bool valid()
        {
            return _baseList.Count != 0 && currentNode != null;
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
        public virtual PhpString serialize()
        {
            // {i:iterator_mode};:{item0};:{item1},...;

            var result = new PhpString.Blob();
            var serializer = PhpSerialization.PhpSerializer.Instance;

            // i:(iterator_mode};
            result.Append(serializer.Serialize(_ctx, (int)this.iteratorMode, default));

            // :{item}
            foreach (var item in _baseList)
            {
                result.Append(":");
                result.Append(serializer.Serialize(_ctx, item, default));
            }

            //
            return new PhpString(result);
        }

        /// <summary>
        /// Constructs the SplDoublyLinkedList out of a serialized string representation
        /// </summary>
        public virtual void unserialize(PhpString serialized)
        {
            // {i:iterator_mode};:{item0};:{item1};...

            if (serialized.Length < 4) throw new ArgumentException(nameof(serialized)); // quick check

            var stream = new MemoryStream(serialized.ToBytes(_ctx));
            try
            {
                var reader = new PhpSerialization.PhpSerializer.ObjectReader(_ctx, stream, default);

                // i:iteratormode
                var tmp = reader.Deserialize();
                if (!tmp.IsLong(out var imode))
                {
                    throw new InvalidDataException();
                }
                this.iteratorMode = (SplIteratorMode)imode;

                // :{item}
                while (stream.ReadByte() == ':')
                {
                    this.push(reader.Deserialize());
                }
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Notice,
                    Resources.LibResources.deserialization_failed, e.Message, stream.Position.ToString(), stream.Length.ToString());
            }
        }

        private void MoveCurrentPointer(bool forwardDirection)
        {
            LinkedListNode<PhpValue> newNode = null;

            if (((iteratorMode & SplIteratorMode.Lifo) != 0) && forwardDirection)
            {
                newNode = currentNode.Previous;
            }
            else
            {
                if (forwardDirection)
                    newNode = currentNode.Next;
                else
                    newNode = currentNode.Previous;
            }

            if ((iteratorMode & SplIteratorMode.Delete) != 0)
            {
                _baseList.Remove(currentNode);
                currentNode = newNode;
            }
            else
            {
                currentNode = newNode;

                if (((iteratorMode & SplIteratorMode.Lifo) != 0) == forwardDirection)
                    index--;
                else
                    index++;
            }
        }

        /// <summary>
        /// Gets index in valid range from given value or throws.
        /// </summary>
        /// <returns>Node index.</returns>
        /// <exception cref="OutOfRangeException">Given index is out of range or invalid.</exception>
        long GetValidIndex(PhpValue index)
        {
            if (index.TryToIntStringKey(out var key) && key.IsInteger && key.Integer >= 0 && key.Integer <= count()) // PHP's key conversion // == count() allowed
            {
                return key.Integer;
            }
            else
            {
                throw new OutOfRangeException(); // Offset invalid or out of range
            }
        }

        private LinkedListNode<PhpValue> GetNodeAtIndex(PhpValue index)
        {
            return GetNodeAtIndex(GetValidIndex(index));
        }

        private LinkedListNode<PhpValue>/*!*/GetNodeAtIndex(long index)
        {
            var node = _baseList.First;
            while (index-- > 0 && node != null)
            {
                node = node.Next;
            }

            return node ?? throw new OutOfRangeException();
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
        public SplQueue(Context ctx) : base(ctx) { }

        public override void __construct()
        {
            iteratorMode = SplIteratorMode.Keep | SplIteratorMode.Fifo;
        }
        public virtual PhpValue dequeue() => shift();
        public virtual void enqueue(PhpValue value) => push(value);
        public virtual void setIteratorMode(int mode)
        {
            if (((SplIteratorMode)mode & SplIteratorMode.Lifo) != 0)
                throw new RuntimeException("Iteration direction of SplQueue can not be changed to LIFO.");

            // mode can only be set to values with SplIteratorMode.Lifo unset
            if (mode >= IT_MODE_KEEP && mode <= IT_MODE_DELETE)
            {
                iteratorMode = ((SplIteratorMode)mode & SplIteratorMode.Delete);
            }
            else
            {
                throw new ArgumentException("Argument value is not an iterator mode.");
            }
        }
    }

    #endregion

    #region SplStack

    /// <summary>
    /// The SplStack class provides the main functionalities of a stack implemented using a doubly linked list.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplStack : SplDoublyLinkedList, Iterator, ArrayAccess, Countable
    {
        public SplStack(Context ctx) : base(ctx) { }

        public override void __construct()
        {
            iteratorMode = SplIteratorMode.Keep | SplIteratorMode.Lifo;
        }

        public virtual void setIteratorMode(int mode)
        {
            if (((SplIteratorMode)mode & SplIteratorMode.Lifo) == 0)
                    throw new RuntimeException("Iteration direction of SplQueue can not be changed to FIFO.");

            // mode can only be set to values with SplIteratorMode.Lifo set
            if (mode >= IT_MODE_KEEP && mode <= (IT_MODE_LIFO + IT_MODE_DELETE))
            {
                iteratorMode = ((SplIteratorMode)mode | SplIteratorMode.Lifo);
            }
            else
            {
                throw new ArgumentException("Argument value is not an iterator mode.");
            }
        }
    }

    #endregion

    #region AbstractHeapImplementation

    /// <summary>
    /// Generic heap functionality abstraction used for <see cref="SplHeap"/> and <see cref="SplPriorityQueue"/>
    /// </summary>
    /// <typeparam name="T">Heap type parameter, PhpValue for a <see cref="SplHeap"/>, or KeyValuePair of PhpValues for <see cref="SplPriorityQueue"/>/typeparam>
    public abstract class AbstractHeap<T>
    {
        /// <summary>
        /// Binary heap nodes implemented as a list 
        /// </summary>
        protected readonly List<T> _heap = new List<T>();

        /// <summary>
        /// Indicator if the heap might be corrupted (true only when bubbling up or down hasnl finished correctly)
        /// </summary>
        protected bool _corrupted = false;

        /// <summary>
        /// Compare elements in order to place them correctly in the heap while sifting up
        /// </summary>
        /// <returns>positive integer if value1 is greater than value2, 0 if they are equal, negative integer otherwise</returns>
        protected abstract long compare(T value1, T value2);

        /// <summary>
        /// Gets the current top of the heap
        /// </summary>
        /// <returns>value on top of the heap</returns>
        protected T Peek
        {
            get
            {
                if (_heap.Count > 0)
                {
                    return _heap[0];
                }
                else
                {
                    throw new RuntimeException("The heap is empty.");
                }
            }
        }

        /// <summary>
        /// Gets the current top of the heap, and removes it
        /// </summary>
        /// <returns>value on top of the heap</returns>
        protected T Pop()
        {
            if (_heap.Count > 0)
            {
                _corrupted = true;

                var top = _heap[0];

                _heap[0] = _heap[LastIndex];
                _heap.RemoveAt(LastIndex);

                BubbleDown();

                return top;
            }
            else
            {
                throw new RuntimeException("The heap is empty.");
            }
        }

        /// <summary>
        /// Inserts a value into a heap, and bubbles it into correct place
        /// </summary>
        /// <param name="value">node to be inserted</param>
        protected void Insert(T value)
        {
            _corrupted = true;

            _heap.Add(value);

            BubbleUp();
        }

        /// <summary>
        /// Index of last node of a heap, or -1 if empty
        /// </summary>
        protected int LastIndex
        {
            get
            {
                int index = _heap.Count - 1;
                if (index >= 0)
                {
                    return index;
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// Gets the parent node index if exists, otherwise -1
        /// </summary>
        /// <param name="childIndex">index of the child node</param>
        /// <returns>parent node index if exists, otherwise -1</returns>
        protected int ParentIndex(int childIndex)
        {
            if (childIndex > 0)
            {
                return (childIndex - 1) / 2;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Gets the left child's node index if exists, otherwise -1
        /// </summary>
        /// <param name="parentIndex">index of the parent node</param>
        /// <returns>left child's index, if exists, otherwise -1</returns>
        protected int LeftChildIndex(int parentIndex)
        {
            int childIndex = (parentIndex * 2) + 1;
            if (childIndex < _heap.Count)
            {
                return childIndex;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Gets the right child's node index if exists, otherwise -1
        /// </summary>
        /// <param name="parentIndex">index of the parent node</param>
        /// <returns>right child's index, if exists, otherwise -1</returns>
        protected int RightChildIndex(int parentIndex)
        {
            int childIndex = (parentIndex * 2) + 2;
            if (childIndex < _heap.Count)
            {
                return childIndex;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Swaps the items in the heap, throws OutOfRange if one of indexes is invalid
        /// </summary>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <exception cref="OutOfRangeException">Given index is out of range or invalid.</exception>
        protected void Swap(int index1, int index2)
        {
            var tmp = _heap[index1];
            _heap[index1] = _heap[index2];
            _heap[index2] = tmp;
        }

        /// <summary>
        /// Bubble the last item of the heap up, to get it into correct position
        /// </summary>
        protected void BubbleUp()
        {
            int currentIndex = LastIndex;
            int parentIndex = ParentIndex(currentIndex);

            while (parentIndex >= 0 && compare(_heap[parentIndex], _heap[currentIndex]) < 0)
            {
                Swap(currentIndex, parentIndex);
                currentIndex = parentIndex;
                parentIndex = ParentIndex(currentIndex);
            }

            _corrupted = false;
        }

        /// <summary>
        /// Bubble the first item of the heap down, to get it into the correct position
        /// </summary>
        protected void BubbleDown()
        {
            int currentIndex = 0;
            int leftChild = LeftChildIndex(currentIndex);

            while (leftChild > 0)
            {
                int rightChild = RightChildIndex(currentIndex);

                if (rightChild > 0 && compare(_heap[rightChild], _heap[leftChild]) > 0 && compare(_heap[rightChild], _heap[currentIndex]) > 0)
                {
                    Swap(currentIndex, rightChild);
                    currentIndex = rightChild;
                }
                else if (compare(_heap[leftChild], _heap[currentIndex]) > 0)
                {
                    Swap(currentIndex, leftChild);
                    currentIndex = leftChild;
                }
                else
                {
                    currentIndex = LastIndex;
                }

                leftChild = LeftChildIndex(currentIndex);
            }
            _corrupted = false;
        }
    }

    #endregion

    #region SplPriorityQueue

    /// <summary>
    /// The SplPriorityQueue class provides the main functionalities of a prioritized queue, implemented using a max heap.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplPriorityQueue : AbstractHeap<KeyValuePair<PhpValue, PhpValue>>, Iterator, Countable
    {
        /// <summary>
        /// Runtime context.
        /// </summary>
        protected readonly Context/*!*/_ctx;

        /// <summary>
        /// SplPriorityQueue extraction mode
        /// </summary>
        [PhpHidden]
        [Flags]
        public enum SplExtractionMode
        {
            ExtrData = 1,
            ExtrPriority = 2,
            ExtrBoth = 3
        }

        public const int EXTR_DATA = (int)SplExtractionMode.ExtrData;
        public const int EXTR_PRIORITY = (int)SplExtractionMode.ExtrPriority;
        public const int EXTR_BOTH = (int)SplExtractionMode.ExtrBoth;

        /// <summary>
        /// The current extraction mode;
        /// </summary>
        private SplExtractionMode _extractionMode = SplExtractionMode.ExtrData;

        public SplPriorityQueue(Context ctx) : base()
        {
            _ctx = ctx;
        }

        public void __construct() { /* Nothing */ }

        protected override long compare(KeyValuePair<PhpValue, PhpValue> pair1, KeyValuePair<PhpValue, PhpValue> pair2)
        {
            if (pair1.Key > pair2.Key)
            {
                return 1;
            }
            else if (pair2.Key > pair1.Key)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
        public virtual long count()
        {
            return _heap.Count;
        }
        public virtual PhpValue current()
        {
            return ForExtractionMode(Peek);
        }
        public virtual PhpValue extract()
        {
            return ForExtractionMode(Pop());
        }
        public virtual int getExtractFlags()
        {
            return (int)_extractionMode;
        }
        public virtual void insert(PhpValue value, PhpValue priority)
        {
            Insert(new KeyValuePair<PhpValue, PhpValue>(priority, value));
        }
        public virtual bool isCorrupted()
        {
            return _corrupted;
        }
        public virtual bool isEmpty()
        {
            return count() == 0;
        }
        public virtual PhpValue key()
        {
            // The key is the theoretical index of the iterater, which is always the last node for the heap
            return count() - 1;
        }
        public virtual void next()
        {
            // The only thing moving the iterator of a heap does is deletes the top and corrects the heap
            Pop();
        }
        public virtual void recoverFromCorruption()
        {
            var backupList = new List<KeyValuePair<PhpValue, PhpValue>>();
            foreach (var item in _heap)
            {
                backupList.Add(item);
            }
            foreach (var item in backupList)
            {
                insert(item.Value, item.Key);
            }

            _corrupted = false;
        }
        public virtual void rewind() { /* Nothing */ }
        public virtual void setExtractFlags(int flags)
        {
            // flags are within all possible combinations of SplExtractionMode
            if (flags >= EXTR_DATA && flags <= EXTR_BOTH)
            {
                _extractionMode = (SplExtractionMode)flags;
            }
            else
            {
                throw new ArgumentException("Argument value is not an iterator mode.");
            }
        }
        public virtual PhpValue top()
        {
            // Top of the heap is also the current position of the iterator
            return current();
        }
        public virtual bool valid()
        {
            // Only checks if the priorityQueue contains any nodes
            return !isEmpty();
        }

        /// <summary>
        /// Chooses which part of KeyValuePair to return according to current extraction mode
        /// </summary>
        /// <param name="pair">Node to return</param>
        /// <returns>pair's value, priority, or both in a PhpArray accordint to <see cref="_extractionMode"/></returns>
        private PhpValue ForExtractionMode (KeyValuePair<PhpValue, PhpValue> pair)
        {
            if (_extractionMode == SplExtractionMode.ExtrData)
            {
                return pair.Value;
            }
            else if (_extractionMode == SplExtractionMode.ExtrPriority)
            {
                return pair.Key;
            }
            else if (_extractionMode == SplExtractionMode.ExtrBoth)
            {
                var arr = new PhpArray(2);
                arr.Add("priority", pair.Key);
                arr.Add("data", pair.Value);
                return arr;
            }
            else
            {
                throw new RuntimeException("SplPriorityQueue has unknown extraction mode set.");
            }
        }
    }

    #endregion

    #region SplHeap, SplMinHeap, SplMaxHeap 

    /// <summary>
    /// The SplHeap class provides the main functionalities of a Heap.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public abstract class SplHeap : AbstractHeap<PhpValue>, Iterator, Countable
    {
        /// <summary>
        /// Runtime context.
        /// </summary>
        protected readonly Context/*!*/_ctx;

        protected SplHeap(Context ctx) : base()
        {
            _ctx = ctx;
            __construct();
        }

        public virtual void __construct() {/* nothing */}
        public virtual long count()
        {
            return _heap.Count;
        }

        /// <summary>
        /// Gets the current top of the heap
        /// </summary>
        /// <returns>value on top of the heap</returns>
        public virtual PhpValue current()
        {
            return Peek;
        }

        /// <summary>
        /// Gets the current top of the heap, and removes it
        /// </summary>
        /// <returns>value on top of the heap</returns>
        public virtual PhpValue extract()
        {
            return Pop();
        }
        public virtual void insert(PhpValue value)
        {
            Insert(value);
        }
        public virtual bool isCorrupted()
        {
            return _corrupted;
        }
        public virtual bool isEmpty()
        {
            return count() == 0;
        }
        public virtual PhpValue key()
        {
            // The key is the theoretical index of the iterater, which is always the last node for the heap
            return count() - 1;
        }
        public virtual void next()
        {
            // The only thing moving the iterator of a heap does is deletes the top and corrects the heap
            Pop();
        }
        public virtual void recoverFromCorruption()
        {
            List<PhpValue> backupList = new List<PhpValue>();
            foreach (var item in _heap)
            {
                backupList.Add(item);
            }
            foreach (var item in backupList)
            {
                insert(item);
            }

            _corrupted = false;
        }
        public virtual void rewind() { /*nothing*/ }
        public virtual PhpValue top()
        {
            // Top of the heap is also the current position of the iterator
            return Peek;
        }
        public virtual bool valid()
        {
            // Only checks if the heap contains any nodes
            return !isEmpty();
        }
    }

    /// <summary>
    /// The SplMinHeap class provides the main functionalities of a heap, keeping the minimum on the top.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplMinHeap : SplHeap
    {
        public SplMinHeap(Context ctx) : base(ctx)
        {
        }

        /// <summary>
        /// Compare elements in order to place them correctly in the heap while sifting up
        /// </summary>
        protected override long compare(PhpValue value1, PhpValue value2)
        {
            if (value1 < value2)
            {
                return 1;
            }
            else if (value2 < value1)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// The SplMaxHeap class provides the main functionalities of a heap, keeping the maximum on the top.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplMaxHeap : SplHeap
    {
        public SplMaxHeap(Context ctx) : base(ctx)
        {
        }

        /// <summary>
        /// Compare elements in order to place them correctly in the heap while sifting up
        /// </summary>
        protected override long compare(PhpValue value1, PhpValue value2)
        {
            if(value1 > value2)
            {
                return 1;
            }
            else if (value2 > value1)
            {
                return -1;
            } else
            {
                return 0;
            }
        }
    }

    #endregion
}
