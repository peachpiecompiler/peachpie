using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    /// <summary>
    /// Implementation of elastic bit array.
    /// The internal array is resized automatically within the set value operation.
    /// </summary>
    struct ElasticBitArray
    {
        int[] _bits;

        const int IntSize = sizeof(int) * 8;

        public ElasticBitArray(int capacity = 0)
        {
            if (capacity < 0)
                throw new ArgumentException();

            _bits = new int[capacity / IntSize + 1];
        }

        /// <summary>
        /// Copies to bit array to new <see cref="BitArray"/> object.
        /// </summary>
        public BitArray ToBitArray() => new BitArray(_bits);

        /// <summary>
        /// Gets or sets index-th bit in the array.
        /// </summary>
        public bool this[int index]
        {
            get
            {
                Debug.Assert(_bits != null);

                var num = index / IntSize;
                var bits = _bits;

                return
                    (index >= 0 && num < bits.Length) &&
                    (bits[num] & (1 << (index % IntSize))) != 0;
            }
        }

        public static void SetTrue(ref ElasticBitArray array, int index)
        {
            Debug.Assert(array._bits != null);

            if (index < 0)
            {
                throw new ArgumentException();
            }

            var num = index / IntSize;
            if (num >= array._bits.Length)
            {
                Array.Resize(ref array._bits, (num + 1) * 2);
            }

            array._bits[num] |= 1 << index % IntSize;
        }

        public static void SetFalse(ref ElasticBitArray array, int index)
        {
            Debug.Assert(array._bits != null);

            var num = index / IntSize;
            if (index >= 0 && num < array._bits.Length)
            {
                array._bits[num] &= ~(1 << index % IntSize);
            }

            // otherwise no value means false
        }
    }
}
