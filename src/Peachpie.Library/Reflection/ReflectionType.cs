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
        public string Name { get; set; }
        public bool AllowsNull { get; set; }
        public bool BuiltIn { get; set; }


        public virtual bool allowsNull() => AllowsNull;

        public virtual bool isBuiltin() => BuiltIn;

        public virtual string __toString() => Name;

        public override string ToString() => __toString();
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionNamedType : ReflectionType
    {
        public virtual string getName() => Name;
    }

}