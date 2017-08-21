using Pchp.Core;
using Pchp.Library.Spl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Reflection
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionException : Spl.Exception
    {
        public ReflectionException(string message = "", long code = 0, Throwable previous = null)
            :base(message, code, previous)
        {
        }
    }
}
