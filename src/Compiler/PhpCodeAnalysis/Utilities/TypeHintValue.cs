using Pchp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Utilities
{
    /// <summary>
    /// Wraps type hint <see cref="object"/> provided by parser and provides properties to access real pimitive type or generic qualified name.
    /// </summary>
    public struct TypeHintValue
    {
        /// <summary>
        /// Hint object.
        /// </summary>
        private readonly object _obj;

        /// <summary>
        /// Gets value indicating whether the hint does not define any type.
        /// </summary>
        public bool IsEmpty { get { return object.ReferenceEquals(_obj, null); } }

        /// <summary>
        /// Gets value indicating whether the value represents a primitive type.
        /// </summary>
        public bool IsPrimitiveType { get { return _obj is PrimitiveTypeName; } }

        /// <summary>
        /// Gets value indicating whether the value represents a class type.
        /// </summary>
        public bool IsGenericQualifiedName { get { return _obj is GenericQualifiedName; } }

        /// <summary>
        /// Gets value indicating whether the value represents a class type.
        /// </summary>
        public bool IsQualifiedName { get { return IsGenericQualifiedName; } }

        public PrimitiveTypeName PrimitiveTypeName { get { return (PrimitiveTypeName)_obj; } }

        public GenericQualifiedName GenericQualifiedName { get { return (GenericQualifiedName)_obj; } }

        public QualifiedName QualifiedName { get { return this.GenericQualifiedName.QualifiedName; } }

        /// <summary>
        /// Gets name of the type or <c>null</c>.
        /// </summary>
        public override string ToString()
        {
            if (!this.IsEmpty)
            {
                if (this.IsPrimitiveType) return this.PrimitiveTypeName.Name.Value;
                if (this.IsQualifiedName) return this.QualifiedName.ToString();
            }

            return null;
        }

        #region construction

        /// <summary>
        /// Wraps type hint object.
        /// </summary>
        /// <param name="hint">Boxed primitive type or generic qualified type.</param>
        public TypeHintValue(object hint)
        {
            Debug.Assert(hint == null || hint is PrimitiveTypeName || hint is GenericQualifiedName);
            _obj = hint;
        }

        public TypeHintValue(PrimitiveTypeName hint)
            : this((object)hint)
        { }

        public TypeHintValue(GenericQualifiedName hint)
            : this((object)hint)
        { }

        public TypeHintValue(QualifiedName hint)
            : this(new GenericQualifiedName(hint))
        { }

        #endregion
    }
}
