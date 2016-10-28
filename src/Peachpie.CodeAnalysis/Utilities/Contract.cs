using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    internal static class Contract
    {
        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if given value is <c>null</c>.
        /// </summary>
        /// <typeparam name="T">Value type. Must be a reference type.</typeparam>
        /// <param name="value">Argument value.</param>
        public static void ThrowIfNull<T>(T value) where T : class
        {
            if (value == null)
            {
                ThrowArgumentNull<T>();
            }
        }

        private static void ThrowArgumentNull<T>() where T : class
        {
            throw new ArgumentNullException(typeof(T).Name);
        }
    }
}
