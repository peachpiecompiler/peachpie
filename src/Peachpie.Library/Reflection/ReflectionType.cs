using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Reflection
{
    /// <summary>
    /// The <see cref="ReflectionType"/> class reports information about a function's return type.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionType
    {
        public virtual bool allowsNull() { throw new NotImplementedException(); }

        public virtual bool isBuiltin() { throw new NotImplementedException(); }

        public virtual string __toString() { throw new NotImplementedException(); }

        public override string ToString() => __toString();
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionNamedType : ReflectionType
    {
        public virtual string getName() { throw new NotImplementedException(); }
    }

}