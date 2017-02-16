using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core.std
{
    /// <summary>
    /// Gets declarations built-in directly in the runtime instead of the library.
    /// Such types are used by runtime directly giving special meaning to their object instances.
    /// </summary>
    public static class StdTable
    {
        static readonly Type[] _types = new[]
        {
            typeof(stdClass), typeof(__PHP_Incomplete_Class),
            typeof(Traversable), typeof(Iterator), typeof(IteratorAggregate),
            typeof(ArrayAccess), typeof(Serializable),
            typeof(Closure),
        };

        /// <summary>
        /// Enumeration of built-in types.
        /// </summary>
        public static string[] Types => _types.Select(t => t.FullName).ToArray();
    }
}
