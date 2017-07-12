using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Reflection
{
    [PhpType("[name]"), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionParameter : Reflector
    {
        public string __toString()
        {
            throw new NotImplementedException();
        }
    }
}
