#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    static class FuncExtensions
    {
        /// <summary>Cached <see cref="Func{T, TResult}"/> instance.</summary>
        public static readonly Func<object, bool> s_not_null = obj => obj != null;

        static class IdentityHolder<T>
        {
            public static readonly Func<T, T> s_identity = x => x;
        }

        public static Func<T, T> Identity<T>() => IdentityHolder<T>.s_identity;
    }
}
