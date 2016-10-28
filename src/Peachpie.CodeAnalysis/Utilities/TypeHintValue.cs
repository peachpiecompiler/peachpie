using Pchp.CodeAnalysis.Symbols;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
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
        private readonly TypeRef _obj;

        /// <summary>
        /// Gets value indicating whether the hint does not define any type.
        /// </summary>
        public bool IsEmpty { get { return object.ReferenceEquals(_obj, null); } }

        /// <summary>
        /// Gets value indicating whether the value represents a primitive type.
        /// </summary>
        public bool IsPrimitiveType { get { return _obj is PrimitiveTypeRef; } }

        /// <summary>
        /// Gets value indicating whether the value represents a class type.
        /// </summary>
        public bool IsGenericQualifiedName { get { return _obj is GenericTypeRef; } }

        /// <summary>
        /// Gets value indicating whether the value represents a class type.
        /// </summary>
        public bool IsQualifiedName { get { return _obj != null && _obj.QualifiedName.HasValue; } }

        public PrimitiveTypeRef.PrimitiveType PrimitiveTypeName { get { return ((PrimitiveTypeRef)_obj).PrimitiveTypeName; } }

        public QualifiedName QualifiedName { get { return _obj.QualifiedName.Value; } }

        /// <summary>
        /// Gets <see cref="TypeSymbol"/> representing this type hint.
        /// </summary>
        /// <returns><see cref="TypeSymbol"/> or <c>null</c> in case the type hint is empty.</returns>
        internal TypeSymbol AsTypeSymbol(PhpCompilation compilation)
        {
            var ct = compilation.CoreTypes;
            if (IsPrimitiveType)
            {
                switch (PrimitiveTypeName)
                {
                    case PrimitiveTypeRef.PrimitiveType.@int: return ct.Long;
                    case PrimitiveTypeRef.PrimitiveType.@float:return ct.Double;
                    case PrimitiveTypeRef.PrimitiveType.array:return ct.PhpArray;
                    case PrimitiveTypeRef.PrimitiveType.@bool:return ct.Boolean;
                    case PrimitiveTypeRef.PrimitiveType.@string:return ct.String;
                    case PrimitiveTypeRef.PrimitiveType.@void:return ct.Void;
                    case PrimitiveTypeRef.PrimitiveType.iterable:
                    default:
                        break;
                }

                throw new NotImplementedException(PrimitiveTypeName.ToString() + " AsTypeSymbol");
            }
            else if (IsQualifiedName)
            {
                return (TypeSymbol)compilation.GetTypeByMetadataName(this.QualifiedName.ClrName()) ?? ct.Object;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets name of the type or <c>null</c>.
        /// </summary>
        public override string ToString()
        {
            if (!this.IsEmpty)
            {
                if (this.IsPrimitiveType) return this.PrimitiveTypeName.ToString();
                if (this.IsQualifiedName) return this.QualifiedName.ToString();
            }

            return null;
        }

        #region construction

        /// <summary>
        /// Wraps type hint object.
        /// </summary>
        /// <param name="hint">Boxed primitive type or generic qualified type.</param>
        public TypeHintValue(TypeRef hint)
        {
            _obj = hint;
        }

        #endregion
    }
}
