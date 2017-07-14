using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Reflection
{
    /// <summary>
    /// The <see cref="ReflectionType"/> class reports information about a function's return type.
    /// </summary>
    [PhpType("[name]"), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionType
    {
        public bool allowsNull() { throw new NotImplementedException(); }

        public bool isBuiltin() { throw new NotImplementedException(); }

        public string __toString() { throw new NotImplementedException(); }
    }
}