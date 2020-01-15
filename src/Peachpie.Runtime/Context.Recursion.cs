using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        struct RecursionCheckKey : IEquatable<RecursionCheckKey>
        {
            readonly object _key;
            readonly int _subkey;

            public RecursionCheckKey(object key, int subkey)
            {
                Debug.Assert(key != null);
                _key = key;
                _subkey = subkey;
            }

            public bool Equals(RecursionCheckKey other) => _subkey == other._subkey && _key.Equals(other._key);
            public override int GetHashCode() => _key.GetHashCode() ^ _subkey;
            public override bool Equals(object obj) => obj is RecursionCheckKey key && Equals(key);
        }

        public struct RecursionCheckToken : IDisposable
        {
            Stack<RecursionCheckKey> Pending { get; }

            public RecursionCheckToken(Context ctx, object key, int subkey = 0)
                : this(ctx, new RecursionCheckKey(key, subkey))
            { }

            private RecursionCheckToken(Context ctx, RecursionCheckKey key)
            {
                Pending = (ctx._lazyRecursionPrevention ??= new Stack<RecursionCheckKey>());

                IsInRecursion = Pending.Contains(key);
                Pending.Push(key);
            }

            /// <summary>
            /// Gets value indicating whether the key is in recursion.
            /// </summary>
            public bool IsInRecursion { get; }

            /// <summary>
            /// Exits recursion check.
            /// Must be called once.
            /// </summary>
            public void Dispose()
            {
                Pending.Pop();
            }
        }

        /// <summary>
        /// Set of scopes we are entered into.
        /// Recursion prevention.
        /// </summary>
        Stack<RecursionCheckKey> _lazyRecursionPrevention;
    }
}
