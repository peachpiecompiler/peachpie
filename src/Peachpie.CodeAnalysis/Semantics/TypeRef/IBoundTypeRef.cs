using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics.TypeRef;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Provides a type reference and binding to <see cref="ITypeSymbol"/>.
    /// </summary>
    public interface IBoundTypeRef : IEquatable<IBoundTypeRef>, IPhpOperation
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
        /// Gets type mask of the type reference in given context.
        /// </summary>
        TypeRefMask GetTypeRefMask(TypeRefContext ctx);

        /// <summary>
        /// Transfers this type reference to the target type context.
        /// The method may return <c>this</c> instance, it cannot return <c>null</c>.
        /// </summary>
        IBoundTypeRef/*!*/Transfer(TypeRefContext/*!*/source, TypeRefContext/*!*/target);

        /// <summary>
        /// Resolve <see cref="ITypeSymbol"/> if possible.
        /// Can be <c>null</c>.
        /// </summary>
        ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation);
    }

    /// <summary>
    /// Common <see cref="IBoundTypeRef"/> implementation.
    /// </summary>
    internal abstract class BoundTypeRef : BoundOperation, IBoundTypeRef
    {
        public virtual bool IsNullable { get; set; }
        public virtual bool IsObject => false;
        public virtual bool IsArray => false;
        public virtual bool IsPrimitiveType => false;
        public virtual bool IsLambda => false;
        public virtual TypeRefMask LambdaReturnType => TypeRefMask.AnyType;
        public virtual TypeRefMask ElementType => TypeRefMask.AnyType;
        public virtual ImmutableArray<IBoundTypeRef> TypeArguments => ImmutableArray<IBoundTypeRef>.Empty;

        public abstract ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false);
        public abstract ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation);

        /// <summary>
        /// Gets type mask of the type reference in given context.
        /// </summary>
        public virtual TypeRefMask GetTypeRefMask(TypeRefContext ctx) => WithNullableMask(ctx.GetTypeMask(this, false), ctx);

        /// <summary>Add <c>NULL</c> type mask if <see cref="IsNullable"/> is set to <c>true</c>.</summary>
        protected TypeRefMask WithNullableMask(TypeRefMask mask, TypeRefContext ctx)
        {
            return IsNullable
                ? mask | ctx.GetNullTypeMask()
                : mask;
        }

        public virtual IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target) => this;
        public virtual bool Equals(IBoundTypeRef other) => ReferenceEquals(this, other);
        public override bool Equals(object obj) => obj is IBoundTypeRef t && Equals(t);
        public override int GetHashCode() => base.GetHashCode();

        public override OperationKind Kind => OperationKind.None;
        public LangElement PhpSyntax { get; set; }

        /// <summary>
        /// Lazily set type symbol if resolved.
        /// </summary>
        public TypeSymbol ResolvedType { get; set; }

        /// <summary>
        /// Alias to <see cref="ResolvedType"/>.
        /// </summary>
        public override ITypeSymbol Type => ResolvedType;

        public override void Accept(OperationVisitor visitor) => visitor.DefaultVisit(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this, argument);
        public virtual TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitTypeRef(this);
    }
}
