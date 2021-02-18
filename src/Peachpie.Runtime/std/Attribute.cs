using System;
using System.Collections.Generic;
using System.Text;

namespace Pchp.Core.Std
{
    /// <summary>
    /// The Attribute class denotating a custom PHP attribute.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName, MinimumLangVersion = "8.0"), PhpExtension("Core")]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class Attribute : System.Attribute
    {
    }
}
