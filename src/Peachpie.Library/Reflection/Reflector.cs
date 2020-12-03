#nullable enable

using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Reflection
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public interface Reflector
    {
        // public static string export(void )

        /// <summary>
        /// The reflected object name.
        /// </summary>
        string __toString();
    }
}
