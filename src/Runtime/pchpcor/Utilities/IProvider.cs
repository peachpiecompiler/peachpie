using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    /// <summary>
    /// Helper provider giving an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of object that the provider gets.</typeparam>
    interface IProvider<T>
    {
        /// <summary>
        /// Gets or creates object of type <typeparamref name="T"/>.
        /// </summary>
        T Create();
    }

    static class Providers
    {
        internal class OrdinalIgnoreCaseStringComparer : IProvider<IEqualityComparer<string>>
        {
            public IEqualityComparer<string> Create() => StringComparer.OrdinalIgnoreCase;
        }

        internal class OrdinalStringComparer : IProvider<IEqualityComparer<string>>
        {
            public IEqualityComparer<string> Create() => StringComparer.Ordinal;
        }

        internal class RuntimeMethodHandleComparer : IProvider<IEqualityComparer<RuntimeMethodHandle>>, IEqualityComparer<RuntimeMethodHandle>
        {
            public IEqualityComparer<RuntimeMethodHandle> Create() => new RuntimeMethodHandleComparer();

            public bool Equals(RuntimeMethodHandle x, RuntimeMethodHandle y) => x == y;

            public int GetHashCode(RuntimeMethodHandle obj) => obj.GetHashCode();
        }

        internal class TypeComparer : IProvider<IEqualityComparer<Type>>, IEqualityComparer<Type>
        {
            public IEqualityComparer<Type> Create() => new TypeComparer();

            public bool Equals(Type x, Type y) => x == y;

            public int GetHashCode(Type obj) => obj.GetHashCode();
        }
    }
}
