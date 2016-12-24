using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Helper service providing maintanance of recursion prevention set.
    /// </summary>
    public interface IRecursionPreventionService
    {
        /// <summary>
        /// Enters recursion check.
        /// </summary>
        /// <param name="key">Object entering the check.</param>
        /// <param name="subkey">Custom key.</param>
        /// <returns><c>true</c> if entering was successfull, <see cref="ExitRecursion(object, int)"/> must be called then.</returns>
        bool TryEnterRecursion(object key, int subkey = 0);

        /// <summary>
        /// Exits the recusrion previous entered using <see cref="TryEnterRecursion(object, int)"/>.
        /// </summary>
        void ExitRecursion(object key, int subkey = 0);
    }

    partial class Context : IRecursionPreventionService
    {
        public IRecursionPreventionService RecursionService => this;

        #region Nested struct: RecursionState

        /// <summary>
        /// Helper struct maintaining information about recursion prevention.
        /// </summary>
        struct RecursionState : IEquatable<RecursionState>
        {
            public class EqualityComparer : IEqualityComparer<RecursionState>
            {
                public static readonly EqualityComparer Instance = new EqualityComparer();
                private EqualityComparer() { }
                public bool Equals(RecursionState x, RecursionState y) => x.Equals(y);
                public int GetHashCode(RecursionState obj) => obj.GetHashCode();
            }

            readonly object _key;
            readonly int _subkey;

            public RecursionState(object key, int subkey)
            {
                Debug.Assert(key != null);
                _key = key;
                _subkey = subkey;
            }

            public bool Equals(RecursionState other) => _subkey == other._subkey && _key.Equals(other._key);
            public override int GetHashCode() => _key.GetHashCode() ^ _subkey;
            public override bool Equals(object obj) => obj is RecursionState && Equals((RecursionState)obj);
        }

        #endregion

        bool IRecursionPreventionService.TryEnterRecursion(object key, int subkey) => _recursionPrevention.Add(new RecursionState(key, subkey));

        void IRecursionPreventionService.ExitRecursion(object key, int subkey) => _recursionPrevention.Remove(new RecursionState(key, subkey));

        /// <summary>
        /// Set of scopes we are entered into.
        /// Recursion prevention.
        /// </summary>
        readonly HashSet<RecursionState> _recursionPrevention = new HashSet<RecursionState>(RecursionState.EqualityComparer.Instance);
    }
}
