using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pchp.Core;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Reference to a type.
    /// </summary>
    public interface ITypeRef : IEquatable<ITypeRef>
    {
        /// <summary>
        /// Full type name.
        /// </summary>
        QualifiedName QualifiedName { get; }

        /// <summary>
        /// Gets value indicating whether the type represents an object (class or interface) and not a primitive type.
        /// </summary>
        bool IsObject { get; }

        /// <summary>
        /// Gets value indicating whether the type represents an array.
        /// </summary>
        bool IsArray { get; }

        /// <summary>
        /// Gets value indicating whether the type represents a primitive type.
        /// </summary>
        bool IsPrimitiveType { get; }

        /// <summary>
        /// Gets value indicating whether the type represents a lambda function or <c>callable</c> primitive type;
        /// </summary>
        bool IsLambda { get; }

        /// <summary>
        /// Gets known keys of the array. Each value is either <c>int</c> or <c>string</c>. Cannot be <c>null</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">In case the type does not represent an array.</exception>
        IEnumerable<object> Keys { get; }

        /// <summary>
        /// Gets merged type information of array items values.
        /// </summary>
        TypeRefMask ElementType { get; }

        /// <summary>
        /// Gets type information of callable return value. This value is valid for lambda functions and instances of closures.
        /// </summary>
        /// <exception cref="InvalidOperationException">In case the type does not represent a callable.</exception>
        TypeRefMask LambdaReturnType { get; }

        /// <summary>
        /// Gets lambda function signature.
        /// </summary>
        /// <exception cref="InvalidOperationException">In case the type does not represent a callable.</exception>
        Signature LambdaSignature { get; }

        /// <summary>
        /// Gets the type code.
        /// </summary>
        PhpTypeCode TypeCode { get; }

        /// <summary>
        /// Transfers this type reference to the target type context.
        /// The method may return <c>this</c>, it cannot return <c>null</c>.
        /// </summary>
        ITypeRef/*!*/Transfer(TypeRefContext/*!*/source, TypeRefContext/*!*/target);
    }
}
