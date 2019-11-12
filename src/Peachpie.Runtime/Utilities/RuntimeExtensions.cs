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
    }
}
