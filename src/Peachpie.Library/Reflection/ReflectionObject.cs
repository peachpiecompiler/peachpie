using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Reflection
{
    [PhpType("[name]"), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionObject : ReflectionClass
    {
        public ReflectionObject(object instance)
            :base(instance.GetPhpTypeInfo())
        {
        }
    }
}
