using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Peachpie.CodeAnalysis.Utilities
{
    internal sealed class ReferenceEqualityComparer : IEqualityComparer, IEqualityComparer<object>
    {
        private ReferenceEqualityComparer() { }

        public readonly static ReferenceEqualityComparer Default = new ReferenceEqualityComparer();

        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }

}
