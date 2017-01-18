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
            public class EqualityComparer : IEqualityComparer<RecursionCheckKey>
            {
                public static readonly EqualityComparer Instance = new EqualityComparer();
                private EqualityComparer() { }
                public bool Equals(RecursionCheckKey x, RecursionCheckKey y) => x.Equals(y);
                public int GetHashCode(RecursionCheckKey obj) => obj.GetHashCode();
            }

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
            public override bool Equals(object obj) => obj is RecursionCheckKey && Equals((RecursionCheckKey)obj);
        }

        public struct RecursionCheckToken : IDisposable
        {
            readonly Stack<RecursionCheckKey> _list;
            readonly bool _isrecursion;

            public RecursionCheckToken(Context ctx, object key, int subkey = 0)
                : this(ctx, new RecursionCheckKey(key, subkey))
            { }

            private RecursionCheckToken(Context ctx, RecursionCheckKey key)
            {
                _isrecursion = (_list = ctx._recursionPrevention).Contains(key);
                _list.Push(key);
            }

            /// <summary>
            /// Gets value indicating whether the key is in recursion.
            /// </summary>
            public bool IsInRecursion => _isrecursion;

            /// <summary>
            /// Exits recursion check.
            /// Must be called once.
            /// </summary>
            public void Dispose()
            {
                _list.Pop();
            }
        }

        /// <summary>
        /// Set of scopes we are entered into.
        /// Recursion prevention.
        /// </summary>
        readonly Stack<RecursionCheckKey> _recursionPrevention = new Stack<RecursionCheckKey>();
    }
}
