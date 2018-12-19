using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Provides a type reference and binding to <see cref="ITypeSymbol"/>.
    /// </summary>
    interface IBoundTypeRef : IEquatable<IBoundTypeRef>
    {
        /// <summary>
        /// Gets value indicting that the type allows a <c>NULL</c> reference.
        /// </summary>
        bool IsNullable { get; }

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
        /// Gets value indicating whether the type represents a lambda function.
        /// </summary>
        bool IsLambda { get; }

        /// <summary>
        /// Gets type information of lambda return value.
        /// This value is valid for callables.
        /// </summary>
        TypeRefMask LambdaReturnType { get; }

        /// <summary>
        /// Gets merged type information of array items values.
        /// </summary>
        TypeRefMask ElementType { get; }

        /// <summary>
        /// In case of generic type reference, gets its bound type arguments.
        /// </summary>
        ImmutableArray<IBoundTypeRef> TypeArguments { get; }

        /// <summary>
        /// Transfers this type reference to the target type context.
        /// The method may return <c>this</c> instance, it cannot return <c>null</c>.
        /// </summary>
        IBoundTypeRef/*!*/Transfer(TypeRefContext/*!*/source, TypeRefContext/*!*/target);

        /// <summary>
        /// Emits load of <c>PhpTypeInfo</c>.
        /// </summary>
        ITypeSymbol/*!*/EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false);

        /// <summary>
        /// Resolve <see cref="ITypeSymbol"/> if possible.
        /// Can be <c>null</c>.
        /// </summary>
        ITypeSymbol ResolveTypeSymbol(SourceRoutineSymbol/*!*/routine, SourceTypeSymbol self);
    }
}
