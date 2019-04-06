using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Peachpie.CodeAnalysis.Utilities
{
    /// <summary>
    /// PriorityQueue provides a stack-like interface, except that objects
    /// "pushed" in arbitrary order are "popped" in order of priority, i.e., 
    /// from least to greatest as defined by the specified comparer.
    /// </summary>
    /// <remarks>
    /// Push and Pop are each O(log N). Pushing N objects and them popping
    /// them all is equivalent to performing a heap sort and is O(N log N).
    /// Multiple different items of the same priority are allowed, but their
    /// order is not guaranteed to be stable.
    /// 
    /// The original implementation was taken from the Microsoft .NET
    /// Framework Reference Source, author: Niklas Borson (niklasb).
    /// Fixes from https://stackoverflow.com/q/44221454/2105235 were applied.
    /// </remarks>
    internal class PriorityQueue<T>
    {
        private T[] _heap;
        private int _count;
        private readonly IComparer<T> _comparer;
        private const int DefaultCapacity = 6;

        public PriorityQueue(IComparer<T> comparer)
        {
            _heap = new T[DefaultCapacity];
            _count = 0;
            _comparer = comparer ?? throw ExceptionUtilities.ArgumentNull(nameof(comparer));
        }

        /// <summary>
        /// Gets the number of items in the priority queue.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets the first or topmost object in the priority queue, which is the
        /// object with the minimum value.
        /// </summary>
        public T Top
        {
            get
            {
                if (_count <= 0) throw new InvalidOperationException("The queue is empty.");

                return _heap[0];
            }
        }

        /// <summary>
        /// Adds an object to the priority queue.
        /// </summary>
        public void Push(T value)
        {
            // Increase the size of the array if necessary.
            if (_count == _heap.Length)
            {
                Array.Resize(ref _heap, _count * 2);
            }

            // Find a place for the new value and insert it, starting at the bottom-rightmost node
            InsertValue(_count, value);

            _count++;
        }

        /// <summary>
        /// Removes the first node (i.e., the logical root) from the heap.
        /// </summary>
        public void Pop()
        {
            if (_count <= 0) throw new InvalidOperationException("The queue is empty.");

            if (_count > 1)
            {
                // Loop invariants:
                //
                //  1.  parent is the index of a gap in the logical tree
                //  2.  leftChild is
                //      (a) the index of parent's left child if it has one, or
                //      (b) a value >= _count if parent is a leaf node
                //
                int parent = 0;
                int leftChild = HeapLeftChild(parent);

                while (leftChild < _count)
                {
                    int rightChild = HeapRightFromLeft(leftChild);
                    int bestChild =
                        (rightChild < _count && _comparer.Compare(_heap[rightChild], _heap[leftChild]) < 0) ?
                        rightChild : leftChild;

                    // Promote bestChild to fill the gap left by parent.
                    _heap[parent] = _heap[bestChild];

                    // Restore invariants, i.e., let parent point to the gap.
                    parent = bestChild;
                    leftChild = HeapLeftChild(parent);
                }

                int gapIndex = parent;

                // Rebalance the heap and fill the gap by moving the last (i.e., bottom-rightmost) node.
                // (Unless it was eliminated by moving the gap there.)
                if (gapIndex != _count - 1)
                {
                    InsertValue(gapIndex, _heap[_count - 1]);
                }
            }

            _heap[_count - 1] = default(T); // Prevent memory leaks
            _count--;
        }

        private void InsertValue(int startGapIndex, T value)
        {
            // Loop invariant:
            //
            //  1.  index is a gap where we might insert the new node.
            //
            int index = startGapIndex;

            while (index > 0)
            {
                int parentIndex = HeapParent(index);
                if (_comparer.Compare(value, _heap[parentIndex]) < 0)
                {
                    // value is a better match than the parent node so exchange
                    // places to preserve the "heap" property.
                    _heap[index] = _heap[parentIndex];
                    index = parentIndex;
                }
                else
                {
                    // Heap is balanced
                    break;
                }
            }

            // Insert the value to the gap
            _heap[index] = value;
        }

        /// <summary>
        /// Calculate the parent node index given a child node's index, taking advantage
        /// of the "shape" property.
        /// </summary>
        private static int HeapParent(int i)
        {
            return (i - 1) / 2;
        }

        /// <summary>
        /// Calculate the left child's index given the parent's index, taking advantage of
        /// the "shape" property. If there is no left child, the return value is >= _count.
        /// </summary>
        private static int HeapLeftChild(int i)
        {
            return (i * 2) + 1;
        }

        /// <summary>
        /// Calculate the right child's index from the left child's index, taking advantage
        /// of the "shape" property (i.e., sibling nodes are always adjacent). If there is
        /// no right child, the return value >= _count.
        /// </summary>
        private static int HeapRightFromLeft(int i)
        {
            return i + 1;
        }
    }
}
