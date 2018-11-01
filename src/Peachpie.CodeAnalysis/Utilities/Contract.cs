using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Semantics.Graph;

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
            if (ReferenceEquals(value, null))
            {
                ThrowArgumentNull<T>();
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if given value is <c>null</c>.
        /// </summary>
        /// <typeparam name="T">Value type. Must be a reference type.</typeparam>
        /// <param name="value">Argument value.</param>
        /// <param name="message">Error message.</param>
        public static void ThrowIfNull<T>(T value, string message) where T : class
        {
            if (ReferenceEquals(value, null))
            {
                ThrowArgumentNull<T>(message);
            }
        }

        /// <summary>Throws <see cref="ArgumentNullException"/> if given value is <c>null</c>.</summary>
        public static void ThrowIfNull<T>(T value, string message, string arg0) where T : class
        {
            if (ReferenceEquals(value, null))
            {
                ThrowArgumentNull<T>(message, arg0);
            }
        }

        /// <summary>Throws <see cref="ArgumentNullException"/> if given value is <c>null</c>.</summary>
        public static void ThrowIfNull<T>(T value, string message, string arg0, string arg1) where T : class
        {
            if (ReferenceEquals(value, null))
            {
                ThrowArgumentNull<T>(message, arg0, arg1);
            }
        }

        private static void ThrowArgumentNull<T>() where T : class
        {
            throw new ArgumentNullException(typeof(T).Name);
        }

        private static void ThrowArgumentNull<T>(string messageFormat, params object[] messageParams) where T : class
        {
            throw new ArgumentNullException(typeof(T).Name, string.Format(messageFormat, messageParams));
        }

        internal static void ThrowIfDefault<T>(ImmutableArray<T> arr)
        {
            if (arr.IsDefault)
            {
                throw new ArgumentException(typeof(ImmutableArray<T>).Name);
            }
        }
    }
}
