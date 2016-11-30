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
        static readonly ILookup<string, Type> _types = new[]
        {
            typeof(stdClass), typeof(__PHP_Incomplete_Class), typeof(ArrayAccess)
        }.ToLookup(t => t.FullName);

        /// <summary>
        /// Enumeration of built-in types.
        /// </summary>
        public static ILookup<string, Type> Types => _types;
    }
}
