#nullable enable

using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Reflection
{
    /// <summary>
    /// The reflection class.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class Reflection
    {
        public static string export(Reflector reflector, bool @return = false)
        {
            throw new NotImplementedException();
        }

        public static PhpArray getModifierNames(int modifiers)
        {
            throw new NotImplementedException();
        }
    }
}
