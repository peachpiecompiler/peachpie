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
    [Attribute(Attribute.TARGET_CLASS)]
    public sealed class Attribute : System.Attribute
    {
        public const int TARGET_CLASS = 1;
        public const int TARGET_FUNCTION = 2;
        public const int TARGET_METHOD = 4;
        public const int TARGET_PROPERTY = 8;
        public const int TARGET_CLASS_CONSTANT = 16;
        public const int TARGET_PARAMETER = 32;
        public const int TARGET_ALL = TARGET_CLASS|TARGET_FUNCTION|TARGET_METHOD|TARGET_PROPERTY|TARGET_CLASS_CONSTANT|TARGET_PARAMETER;

        public const int IS_REPEATABLE = 64;

        public int flags;

        public Attribute(int flags = TARGET_ALL)
        {
            __construct(flags);
        }

        public void __construct(int flags = TARGET_ALL)
        {
            this.flags = flags;
        }
    }
}
