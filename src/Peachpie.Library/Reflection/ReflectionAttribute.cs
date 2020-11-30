#nullable enable

using Pchp.Core;
using Pchp.Core.Reflection;
using System;

namespace Pchp.Library.Reflection
{
    /// <summary>
    /// The reflection attribute.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionAttribute
    {
        public const int IS_INSTANCEOF = 1;

        readonly private protected Attribute _attribute;

        internal ReflectionAttribute(Attribute attribute)
        {
            _attribute = attribute;
        }

        /// <summary>
        /// Gets the attribute name.
        /// </summary>
        public string getName()
        {
            if (_attribute is PhpCustomAtribute phpattr)
            {
                return phpattr.TypeName;
            }
            else
            {
                return _attribute.GetPhpTypeInfo().Name;
            }
        }

        /// <summary>
        /// Creates instance of the attribute class.
        /// </summary>
        public object newInstance(Context ctx)
        {
            if (_attribute is PhpCustomAtribute phpattr)
            {
                // ctx.GetDeclaredType(phpattr.TypeName, true)
                // Invoke ctor with named arguments
                throw new NotImplementedException();
            }
            else
            {
                return _attribute;
            }
        }

        /// <summary>
        /// Get the array of attribute arguments.
        /// </summary>
        public PhpArray getArguments()
        {
            if (_attribute is PhpCustomAtribute phpattr)
            {
                return phpattr.Arguments.DeepCopy();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
