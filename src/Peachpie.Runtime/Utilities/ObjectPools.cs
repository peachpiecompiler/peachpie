using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace Pchp.Core.Utilities
{
    public static class ObjectPools
    {
        /// <summary>
        /// Gets object pool singleton.
        /// Uses <see cref="StringBuilderPooledObjectPolicy"/> policy (automatically clears the string builder upon return).
        /// </summary>
        public static ObjectPool<StringBuilder> StringBuilderPool { get; } = new DefaultObjectPoolProvider().Create(new StringBuilderPooledObjectPolicy());

        public static ObjectPool<List<T>> GetListPool<T>() => ListPool<T>.Pool;

        /// <summary>Gets pooled instance of <see cref="StringBuilder"/>.</summary>
        public static StringBuilder GetStringBuilder() => StringBuilderPool.Get();

        /// <summary>Returns instance to pool.</summary>
        public static void Return(this StringBuilder sb) => StringBuilderPool.Return(sb);

        /// <summary>
        /// Gets the <paramref name="sb"/> value as string and return the instance to the <see cref="StringBuilderPool"/>.
        /// </summary>
        /// <param name="sb">String builder instance.</param>
        /// <returns><paramref name="sb"/> string.</returns>
        public static string GetStringAndReturn(this StringBuilder sb)
        {
            Debug.Assert(sb != null);

            var value = sb.ToString();
            Return(sb);
            return value;
        }
    }
}
