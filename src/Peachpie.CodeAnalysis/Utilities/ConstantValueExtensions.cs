using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis
{
    internal static class ConstantValueExtensions
    {
        /// <summary>
        /// Tries to convert <paramref name="value"/> to a <see cref="ConstantValue"/> if possible.
        /// Argument that doesn't have value or values which cannot be represented as <see cref="ConstantValue"/> causes a <c>null</c> reference to be returned.
        /// </summary>
        /// <param name="value">Optional boced value.</param>
        /// <returns><see cref="ConstantValue"/> instance if possible. Otherwise a <c>null</c> reference.</returns>
        public static ConstantValue ToConstantValueOrNull(this Optional<object> value)
        {
            if (value.HasValue)
            {
                var obj = value.Value;
                if (obj == null) return ConstantValue.Null;
                if (obj is int) return ConstantValue.Create((int)obj);
                if (obj is long) return ConstantValue.Create((long)obj);
                if (obj is string) return ConstantValue.Create((string)obj);
                if (obj is bool) return ConstantValue.Create((bool)obj);
                if (obj is double) return ConstantValue.Create((double)obj);
                if (obj is float) return ConstantValue.Create((float)obj);
                if (obj is decimal) return ConstantValue.Create((decimal)obj);
                if (obj is ulong) return ConstantValue.Create((ulong)obj);
                if (obj is uint) return ConstantValue.Create((uint)obj);
                if (obj is sbyte) return ConstantValue.Create((sbyte)obj);
                if (obj is short) return ConstantValue.Create((short)obj);
                if (obj is DateTime) return ConstantValue.Create((DateTime)obj);
            }

            return null;
        }
    }
}
