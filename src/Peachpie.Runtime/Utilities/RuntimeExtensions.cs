#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    public static class RuntimeExtensions
    {
        /// <summary>
        /// Gets new instance of <see cref="stdClass"/> which runtime fields are passed from given array.
        /// Array is not copied, the <see cref="stdClass"/> will reference the same instance as given.
        /// </summary>
        /// <param name="array">Array to be used as the class's runtime fields. Can be <c>null</c>.</param>
        public static stdClass AsStdClass(this PhpArray array) => new stdClass { __peach__runtimeFields = array, };

        /// <summary>
        /// Gets value indicating the array's internal structure is "packed".
        /// This means there is no hash table internally, not "holes" in the underlaying array, it only consists of ordered set of items indexed from <c>0</c> to <c>Count - 1</c>.
        /// </summary>
        /// <param name="array">Non-null reference to array.</param>
        public static bool IsPacked(this PhpArray array) => array.table.IsPacked;

        /// <summary>
        /// Ensures the array is not shared among more <see cref="PhpArray"/> instances.
        /// Lazily clones if necessary.
        /// </summary>
        public static PhpArray AsWritable(this PhpArray array)
        {
            array.EnsureWritable();
            return array;
        }
    }
}
