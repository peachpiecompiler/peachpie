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

            var enumerator = array.GetFastEnumerator();

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

        /// <summary>
        /// See <see cref="stdClass"/>.
        /// Allows for storing runtime fields to this object.
        /// </summary>
        [System.Runtime.CompilerServices.CompilerGenerated]
        internal PhpArray __peach__runtimeFields;

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

        public virtual PhpArray __serialize()
        {
            var elements = new PhpArray(_baseList.Count);
            foreach (var item in _baseList)
            {
                elements.Add(item);
            }

            var array = new PhpArray(3);
            array.AddValue((int)iteratorMode);
            array.AddValue(PhpValue.FromClr(elements));
            array.AddValue(__peach__runtimeFields ?? PhpArray.NewEmpty());

            return array;
        }

        public virtual void __unserialize(PhpArray array)
        {
            iteratorMode = array.TryGetValue(0, out var flagsVal) && flagsVal.IsLong(out long flags)
                ? (SplIteratorMode)flags : throw new InvalidDataException();

            var elements = array.TryGetValue(1, out var elementsVal) && elementsVal.IsPhpArray(out var elementsArray)
                ? elementsArray : throw new InvalidDataException();
            var e = elements.GetFastEnumerator();
            while (e.MoveNext())
            {
                this.push(e.CurrentValue);
            }

            __peach__runtimeFields = array.TryGetValue(2, out var propsVal) && propsVal.IsPhpArray(out var propsArray)
                ? propsArray : throw new InvalidDataException();
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

    #region PriorityQueue<T>

    /// <summary>
    /// Generic priority queue for PHP's <see cref="SplPriorityQueue"/> and <see cref="SplHeap"/>.
    /// </summary>
    [PhpHidden]
    sealed class PriorityQueue<T>
    {
        /// <summary>
        /// Comparer.
        /// </summary>
        readonly IComparer<T> _comparer;

        /// <summary>
        /// Binary heap nodes implemented as a list 
        /// </summary>
        readonly List<T> _list = new List<T>();

        /// <summary>
        /// Indicator if the heap might be corrupted (true only when bubbling up or down hasn't finished correctly)
        /// </summary>
        public bool IsCorrupted { get; private set; }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        public PriorityQueue(IComparer<T> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Count of items.
        /// </summary>
        public int Count => _list.Count;

        /// <summary>
        /// Gets the current top of the heap
        /// </summary>
        /// <returns>value on top of the heap</returns>
        public T Peek() => _list.Count != 0 ? _list[0] : default;

        /// <summary>
        /// Gets the current top of the heap, and removes it
        /// </summary>
        /// <returns>value on top of the heap</returns>
        public bool TryPop(out T value)
        {
            if (_list.Count != 0)
            {
                IsCorrupted = true;

                value = _list[0];

                _list[0] = _list[LastIndex];
                _list.RemoveAt(LastIndex);

                BubbleDown();

                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Inserts a value into a heap, and bubbles it into correct place
        /// </summary>
        /// <param name="value">node to be inserted</param>
        public void Insert(T value)
        {
            IsCorrupted = true;

            _list.Add(value);

            BubbleUp();
        }

        /// <summary>
        /// Index of last node of a heap, or -1 if empty
        /// </summary>
        int LastIndex => _list.Count - 1;

        /// <summary>
        /// Gets the parent node index if exists, otherwise -1
        /// </summary>
        /// <param name="childIndex">index of the child node</param>
        /// <returns>parent node index if exists, otherwise -1</returns>
        int ParentIndex(int childIndex)
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
        int LeftChildIndex(int parentIndex)
        {
            int childIndex = (parentIndex * 2) + 1;
            if (childIndex < _list.Count)
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
        int RightChildIndex(int parentIndex)
        {
            int childIndex = (parentIndex * 2) + 2;
            if (childIndex < _list.Count)
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
        void Swap(int index1, int index2)
        {
            var tmp = _list[index1];
            _list[index1] = _list[index2];
            _list[index2] = tmp;
        }

        /// <summary>
        /// Bubble the last item of the heap up, to get it into correct position
        /// </summary>
        void BubbleUp() => BubbleUp(LastIndex);

        /// <summary>
        /// Bubble the item of the heap up, to get it into correct position
        /// </summary>
        void BubbleUp(int index)
        {
            Debug.Assert(index >= 0 && index < Count);
            int parentIndex = ParentIndex(index);

            while (parentIndex >= 0 && _comparer.Compare(_list[parentIndex], _list[index]) < 0)
            {
                Swap(index, parentIndex);
                index = parentIndex;
                parentIndex = ParentIndex(index);
            }

            IsCorrupted = false;
        }

        /// <summary>
        /// Bubble the first item of the heap down, to get it into the correct position
        /// </summary>
        void BubbleDown()
        {
            int currentIndex = 0;
            int leftChild = LeftChildIndex(currentIndex);

            while (leftChild > 0)
            {
                int rightChild = RightChildIndex(currentIndex);

                if (rightChild > 0 && _comparer.Compare(_list[rightChild], _list[leftChild]) > 0 && _comparer.Compare(_list[rightChild], _list[currentIndex]) > 0)
                {
                    Swap(currentIndex, rightChild);
                    currentIndex = rightChild;
                }
                else if (_comparer.Compare(_list[leftChild], _list[currentIndex]) > 0)
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

            IsCorrupted = false;
        }

        /// <summary>
        /// Reorders the queue if <see cref="IsCorrupted"/>.
        /// </summary>
        public void Recover()
        {
            if (IsCorrupted)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    BubbleUp(i);
                }

                IsCorrupted = false;
            }
        }
    }

    #endregion

    #region SplPriorityQueue

    /// <summary>
    /// The SplPriorityQueue class provides the main functionalities of a prioritized queue, implemented using a max heap.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplPriorityQueue : Iterator, Countable, IComparer<SplPriorityQueue.Pair>
    {
        /// <summary>
        /// Shortcut for (PhpValue priority, PhpValue value).
        /// </summary>
        internal struct Pair
        {
            public PhpValue
                priority,
                value;
        }

        readonly PriorityQueue<Pair> _queue;

        #region IComparer

        int IComparer<Pair>.Compare(Pair x, Pair y)
            => (int)this.compare(x.priority, y.priority);

        #endregion

        /// <summary>
        /// SplPriorityQueue extraction mode
        /// </summary>
        [Flags]
        internal enum SplExtractionMode
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

        public SplPriorityQueue()
        {
            _queue = new PriorityQueue<Pair>(this);
        }

        public void __construct() { /* Nothing */ }

        public virtual long compare(PhpValue priority1, PhpValue priority2) => priority1.Compare(priority2);

        public virtual long count() => _queue.Count;

        public virtual PhpValue current() => _queue.Count != 0 ? ForExtractionMode(_queue.Peek()) : PhpValue.Null;

        public virtual PhpValue extract() => _queue.TryPop(out var x) ? ForExtractionMode(x) : PhpValue.Null;

        public virtual int getExtractFlags() => (int)_extractionMode;

        public virtual void insert(PhpValue value, PhpValue priority)
        {
            _queue.Insert(new Pair { priority = priority.DeepCopy(), value = value.DeepCopy() });
        }

        public virtual bool isCorrupted() => _queue.IsCorrupted;

        public virtual bool isEmpty() => _queue.Count == 0;

        public virtual PhpValue key()
        {
            // The key is the theoretical index of the iterater, which is always the last node for the heap
            return _queue.Count - 1;
        }

        public virtual void next()
        {
            // The only thing moving the iterator of a heap does is deletes the top and corrects the heap
            _queue.TryPop(out var _);
        }

        public virtual void recoverFromCorruption() => _queue.Recover();

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
            return _queue.Count != 0;
        }

        /// <summary>
        /// Chooses which part of KeyValuePair to return according to current extraction mode
        /// </summary>
        /// <param name="pair">Node to return</param>
        /// <returns>pair's value, priority, or both in a PhpArray accordint to <see cref="_extractionMode"/></returns>
        private PhpValue ForExtractionMode(Pair pair)
        {
            switch (_extractionMode)
            {
                case SplExtractionMode.ExtrData:
                    return pair.value;

                case SplExtractionMode.ExtrPriority:
                    return pair.priority;

                case SplExtractionMode.ExtrBoth:
                    return new PhpArray(2)
                    {
                        { "priority", pair.priority },
                        { "data", pair.value },
                    };

                default:
                    throw new InvalidOperationException();
            }
        }
    }

    #endregion

    #region SplHeap, SplMinHeap, SplMaxHeap 

    /// <summary>
    /// The SplHeap class provides the main functionalities of a Heap.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public abstract class SplHeap : Iterator, Countable, IComparer<PhpValue>
    {
        readonly PriorityQueue<PhpValue> _queue;

        protected SplHeap()
        {
            _queue = new PriorityQueue<PhpValue>(this);

            __construct();
        }

        public void __construct() {/* nothing */}

        protected abstract long compare(PhpValue value1, PhpValue value2);

        public virtual long count() => _queue.Count;

        /// <summary>
        /// Gets the current top of the heap
        /// </summary>
        /// <returns>value on top of the heap</returns>
        public virtual PhpValue current() => _queue.Peek();

        /// <summary>
        /// Gets the current top of the heap, and removes it
        /// </summary>
        /// <returns>value on top of the heap</returns>
        public virtual PhpValue extract() => _queue.TryPop(out var x) ? x : PhpValue.Null;

        public virtual void insert(PhpValue value) => _queue.Insert(value.DeepCopy());

        public virtual bool isCorrupted() => _queue.IsCorrupted;

        public virtual bool isEmpty() => _queue.Count == 0;

        public virtual PhpValue key()
        {
            // The key is the theoretical index of the iterater, which is always the last node for the heap
            return _queue.Count - 1;
        }

        public virtual void next()
        {
            // The only thing moving the iterator of a heap does is deletes the top and corrects the heap
            _queue.TryPop(out var _);
        }
        public virtual void recoverFromCorruption() => _queue.Recover();

        public virtual void rewind() { /*nothing*/ }

        public virtual PhpValue top()
        {
            // Top of the heap is also the current position of the iterator
            return _queue.Peek();
        }

        public virtual bool valid()
        {
            // Only checks if the heap contains any nodes
            return _queue.Count != 0;
        }

        int IComparer<PhpValue>.Compare(PhpValue x, PhpValue y) => (int)compare(x, y);
    }

    /// <summary>
    /// The SplMinHeap class provides the main functionalities of a heap, keeping the minimum on the top.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplMinHeap : SplHeap
    {
        /// <summary>
        /// Compare elements in order to place them correctly in the heap while sifting up
        /// </summary>
        protected override long compare(PhpValue value1, PhpValue value2) => -value1.Compare(value2);
    }

    /// <summary>
    /// The SplMaxHeap class provides the main functionalities of a heap, keeping the maximum on the top.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplMaxHeap : SplHeap
    {
        /// <summary>
        /// Compare elements in order to place them correctly in the heap while sifting up
        /// </summary>
        protected override long compare(PhpValue value1, PhpValue value2) => value1.Compare(value2);
    }

    #endregion
}
