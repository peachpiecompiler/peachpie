using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Manages map of known scripts and bit array of already included.
        /// </summary>
        class ScriptsMap
        {
            int[] _bits = new int[_count / IntSize];

            const int IntSize = sizeof(int) * 8;

            /// <summary>
            /// Gets or sets index-th bit in the array.
            /// </summary>
            private bool this[int index]
            {
                get
                {
                    if (index >= 0 && index / IntSize < _bits.Length)
                    {
                        return (_bits[index / IntSize] & (1 << (index % IntSize))) != 0;
                    }

                    return false;
                }
                set
                {
                    if (index < 0)
                    {
                        throw new ArgumentException();
                    }

                    if (index / IntSize >= _bits.Length)
                    {
                        Array.Resize(ref _bits, (index / IntSize + 1) * 2);
                    }

                    if (value)
                    {
                        _bits[index / IntSize] |= 1 << index % IntSize;
                    }
                    else
                    {
                        _bits[index / IntSize] &= ~(1 << index % IntSize);
                    }
                }
            }

            static int _count;
            //static Dictionary<string, int> _scriptMap;

            public bool IsIncluded(int script_id) => this[script_id - 1];

            public bool SetIncluded(int script_id) => this[script_id - 1] = true;

            public static int EnsureIndex(ref int script_id)
            {
                if (script_id <= 0)
                    script_id = Interlocked.Increment(ref _count);

                return script_id;
            }
        }
    }
}
