using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Pchp.Core.Collections
{
    /// <summary>
    /// <see cref="IList{T}"/> implementation as a value type.
    /// </summary>
    [DebuggerDisplay("{typeof(T).FullName,nq}[{_count}]")]
    public struct ValueList<T> : IList<T>
    {
        int _count;
        T[] _array;

        public ValueList(IEnumerable<T> collection)
        {
            _count = 0;
            _array = null;

            AddRange(collection);
        }

        public ValueList(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _count = 0;
            _array = capacity <= 0 ? Array.Empty<T>() : new T[capacity];
        }

        public T this[int index]
        {
            get
            {
                return index >= 0 && index < _count
                    ? _array[index]
                    : throw new ArgumentOutOfRangeException(nameof(index));
            }
            set
            {
                if (index >= 0 && index < _count)
                {
                    _array[index] = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        public int Count => _count;

        public bool IsReadOnly => false;

        private int Capacity
        {
            get
            {
                return _array != null ? _array.Length : 0;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (value == 0)
                {
                    _array = Array.Empty<T>();
                }
                else if (_array == null || _array.Length == 0)
                {
                    _array = new T[value];
                }
                else
                {
                    Array.Resize(ref _array, value);
                }
            }
        }

        private void EnsureCapacity(int capacity)
        {
            if (Capacity < capacity)
            {
                Capacity = capacity;
            }
        }

        public void Add(T item)
        {
            if (Capacity == _count)
            {
                if (_count == 0)
                {
                    // optimized for list with 1 element
                    // and calling ToArray() afterwards
                    Capacity = 1;
                }
                else
                {
                    Capacity = (_count + 1) * 2;
                }
            }

            _array[_count++] = item;
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection col)
            {
                EnsureCapacity(Count + col.Count);
                col.CopyTo(_array, _count);
                _count += col.Count;
            }
            else if (enumerable != null)
            {
                foreach (var item in enumerable)
                {
                    Add(item);
                }
            }
        }

        public void AddRange(T[] array) => AddRange(array, 0, array.Length);

        public void AddRange(T[] array, int start, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            EnsureCapacity(Count + count);
            Array.Copy(array, start, _array, _count, count);
            _count += count;
        }

        public void Clear()
        {
            if (_count != 0)
            {
                Array.Clear(_array, 0, _count);
                _count = 0;
            }
        }

        public bool Contains(T item) => IndexOf(item) >= 0;

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_count != 0)
            {
                Array.Copy(_array, 0, array, arrayIndex, _count);
            }
        }

        public int IndexOf(T item) => _array != null ? Array.IndexOf(_array, item, 0, _count) : -1;

        public void Insert(int index, T item)
        {
            EnsureCapacity(_count + 1);
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _array[index] = default(T);
            Array.Copy(_array, index + 1, _array, index, --_count - index);
        }

        /// <summary>
        /// Alias to <see cref="Add(T)"/>.
        /// </summary>
        public void Push(T item) => this.Add(item);

        /// <summary>
        /// Pops an element from the end of collection.
        /// </summary>
        /// <returns>The last element.</returns>
        public T Pop()
        {
            if (_count > 0)
            {
                return _array[--_count];
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public ValueEnumerator GetEnumerator() => new ValueEnumerator(ref this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var count = _count;
            var array = _array;

            for (int i = 0; i < count; i++)
            {
                yield return array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        /// <summary>
        /// Returns a collection of items with duplicit <c>key</c> (according to provided <paramref name="keySelector"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of the key to be checned.</typeparam>
        /// <param name="keySelector">Gets the key to be used for the duplicity check.</param>
        /// <param name="predicate">Optional. Where predicate.</param>
        /// <returns>Set of duplicit items.</returns>
        public IReadOnlyList<T> SelectDuplicities<TKey>(Func<T, TKey> keySelector, Predicate<T> predicate = null)
        {
            if (this.Count <= 1)
            {
                return Array.Empty<T>();
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            var set = new HashSet<TKey>();
            var duplicities = new ValueList<T>();

            foreach (var item in this)
            {
                if (predicate != null && !predicate(item))
                {
                    continue;
                }

                if (set.Add(keySelector(item)))
                {
                    // ok
                }
                else
                {
                    duplicities.Add(item);
                }
            }

            //
            return duplicities.AsReadOnly();
        }

        /// <summary>
        /// Gets a readonly collection of the elements.
        /// Note the collection may reference the underlying array, so any changes will be reflected in both returned list and this list as well.
        /// </summary>
        public IReadOnlyList<T> AsReadOnly()
        {
            if (_count == 0)
            {
                return Array.Empty<T>();
            }

            Debug.Assert(_array != null);

            if (_count == _array.Length)
            {
                return _array;
            }

            return new ArraySegmentClass(_array, _count);
        }

        /// <summary>
        /// Gets array with the elements.
        /// Note the array may reference the underlying array, so any changes will be reflected in both returned list and this list as well.
        /// </summary>
        public T[] ToArray()
        {
            if (_count == 0)
            {
                return Array.Empty<T>();
            }

            Debug.Assert(_array != null);

            if (_count == _array.Length)
            {
                return _array;
            }

            return _array.AsSpan(0, _count).ToArray();
        }

        public Span<T> AsSpan()
        {
            if (_count == 0)
            {
                return Span<T>.Empty;
            }

            Debug.Assert(_array != null);

            return _array.AsSpan(0, _count);
        }

        internal void GetArraySegment(out T[] array, out int count)
        {
            count = _count;

            if (count == 0)
            {
                array = Array.Empty<T>();
            }
            else
            {
                array = _array;
            }
        }

        public struct ValueEnumerator
        {
            readonly T[] _array;
            readonly int _count;

            int index;

            internal ValueEnumerator(ref ValueList<T> list)
            {
                _array = list._array;
                _count = list._count;
                index = -1;
            }

            public bool MoveNext() => ++index < _count;

            public T Current => _array[index];
        }

        sealed class ArraySegmentClass : IReadOnlyList<T>
        {
            readonly T[] _array;
            readonly int _count;

            public ArraySegmentClass(T[] array, int count)
            {
                _array = array ?? throw new ArgumentNullException(nameof(array));
                _count = count;
            }

            public T this[int index] => _array[index];

            public int Count => _count;

            public IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < _count; i++)
                {
                    yield return _array[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
