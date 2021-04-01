using System;
using System.Collections.Generic;
using System.Text;

namespace Pchp.Core.Collections
{
    /// <summary>
    /// Extension method for <see cref="ValueList{T}"/>.
    /// </summary>
    public static class ValueListExtensions
    {
        /// <summary>
        /// Concatenates all the elements of a string list, using the specified separator between each element.
        /// </summary>
        public static string Join(this ValueList<string> values, string separator)
        {
            values.GetArraySegment(out var array, out var count);
            return string.Join(separator, array, 0, count);
        }
    }
}
