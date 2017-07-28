using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    internal static class FuncExtensions
    {
        /// <summary>Cached <see cref="Func{T, TResult}"/> instance.</summary>
        public static readonly Func<object, bool> s_not_null = new Func<object, bool>((obj) => obj != null);

        sealed class IdentityHolder<T>
        {
            public readonly static Func<T, T> s_identity = new Func<T, T>(x => x);
        }

        public static Func<T, T> Identity<T>() => IdentityHolder<T>.s_identity;
    }
}
