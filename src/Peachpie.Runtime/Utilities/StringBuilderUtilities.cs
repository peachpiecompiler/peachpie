using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Pchp.Core.Utilities
{
    /// <summary>
    /// <see cref="StringBuilder"/> extensions and pooling.
    /// </summary>
    public static class StringBuilderUtilities
    {
        /// <summary>
        /// Gets object pool singleton.
        /// Uses <see cref="StringBuilderPooledObjectPolicy"/> policy (automatically clears the string builder upon return).
        /// </summary>
        public static ObjectPool<StringBuilder> Pool { get; } = new DefaultObjectPoolProvider().Create(new StringBuilderPooledObjectPolicy());

        /// <summary>
        /// Gets the <paramref name="sb"/> value as string and return the instance to the <see cref="Pool"/>.
        /// </summary>
        /// <param name="sb">String builder instance.</param>
        /// <returns><paramref name="sb"/> string.</returns>
        public static string GetStringAndReturn(StringBuilder sb)
        {
            Debug.Assert(sb != null);

            var value = sb.ToString();
            Pool.Return(sb);
            return value;
        }
    }
}
