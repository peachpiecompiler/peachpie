using System;
using System.Collections.Generic;
using System.Text;

namespace Pchp.Core.Utilities
{
    /// <summary>
    /// Provides runtime helpers and singletons.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Frequently used default value of <see cref="RuntimeTypeHandle"/>.
        /// </summary>
        public static readonly RuntimeTypeHandle EmptyRuntimeTypeHandle = default(RuntimeTypeHandle);

        /// <summary>
        /// Frequently used default value of <see cref="Nullable{T}"/>.
        /// </summary>
        public static Nullable<T> EmptyNullable_T<T>() where T : struct => null; // default(Nullable<T>)

        /// <summary>
        /// Gets value indicating the given user PHP type is declared in specified <see cref="Context"/>.
        /// </summary>
        public static bool IsUserTypeDeclared(Context ctx, Reflection.PhpTypeInfo tinfo) => ctx.IsUserTypeDeclared(tinfo);
    }
}
