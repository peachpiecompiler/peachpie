using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Bound type reference.
    /// </summary>
    internal partial interface IBoundTypeRef
    {
        /// <summary>
        /// Resolved symbol if possible.
        /// Can be <c>null</c>.
        /// </summary>
        ITypeSymbol Symbol { get; }
    }

    sealed class BoundTypeRefFromSymbol : IBoundTypeRef
    {
        readonly ITypeSymbol _symbol;

        public static IBoundTypeRef CreateOrNull(ITypeSymbol symbol) => symbol != null ? new BoundTypeRefFromSymbol(symbol) : null;

        public BoundTypeRefFromSymbol(ITypeSymbol symbol)
        {
            Contract.ThrowIfNull(symbol);
            _symbol = symbol;
        }

        public ITypeSymbol Symbol => _symbol;

        public ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false) => BoundTypeRef.EmitLoadPhpTypeInfo(cg, _symbol);
    }

    sealed class BoundTypeRefFromPlace : IBoundTypeRef
    {
        readonly IPlace _place;

        public BoundTypeRefFromPlace(IPlace place)
        {
            Contract.ThrowIfNull(place);
            _place = place;
        }

        public ITypeSymbol Symbol => null;

        public ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            return _place.EmitLoad(cg.Builder)
                .Expect(cg.CoreTypes.PhpTypeInfo);
        }
    }

    /// <summary>
    /// Bound <see cref="TypeRef"/>.
    /// </summary>
    [DebuggerDisplay("{DebugView,nq}")]
    public partial class BoundTypeRef : IBoundTypeRef
    {
        public TypeRef TypeRef => _typeRef;
        readonly TypeRef _typeRef;

        /// <summary>
        /// Gets value determining the indirect type reference can refer to an object instance which type is used to get the type info.
        /// </summary>
        public bool ObjectTypeInfoSemantic => _objectTypeInfoSemantic;
        readonly bool _objectTypeInfoSemantic;

        /// <summary>
        /// Whether the type can represents a class only (not a primitive type).
        /// </summary>
        public bool HasClassNameRestriction => _isClassName;
        readonly bool _isClassName;

        string DebugView
        {
            get
            {
                if (_typeRef.QualifiedName.HasValue)
                    return _typeRef.QualifiedName.Value.ToString();

                if (TypeExpression != null)
                    return TypeExpression.ToString();

                return base.ToString();
            }
        }

        /// <summary>
        /// Resolved <see cref="TypeRef"/> if possible.
        /// </summary>
        internal TypeSymbol ResolvedType { get; set; }

        /// <summary>
        /// Resolved type symbol if any.
        /// </summary>
        public ITypeSymbol Symbol { get { return this.ResolvedType; } }

        /// <summary>
        /// Expression getting type name.
        /// </summary>
        internal BoundExpression TypeExpression { get; set; }

        public virtual bool IsDirect => TypeExpression == null;

        ITypeSymbol IBoundTypeRef.Symbol => this.ResolvedType;

        public BoundTypeRef(TypeRef tref, bool objAsTypeInfo, bool isClassName)
        {
            _typeRef = tref;
            _objectTypeInfoSemantic = objAsTypeInfo;
            _isClassName = isClassName;
        }
    }

    /// <summary>
    /// Bound ultiple <see cref="TypeRef"/>.
    /// </summary>
    public sealed partial class BoundMultipleTypeRef : BoundTypeRef
    {
        /// <summary>
        /// Array of bound types.
        /// </summary>
        public ImmutableArray<BoundTypeRef> BoundTypes => _boundTypes;
        readonly ImmutableArray<BoundTypeRef> _boundTypes;

        public override bool IsDirect => false;

        internal static ImmutableArray<BoundTypeRef> Flattern(BoundTypeRef tref)
        {
            if (tref is BoundMultipleTypeRef mtref)
            {
                return mtref.BoundTypes;
            }
            else
            {
                return ImmutableArray.Create(tref);
            }
        }

        public BoundMultipleTypeRef(ImmutableArray<BoundTypeRef> boundTypes, TypeRef tref, bool objAsTypeInfo, bool isClassName)
            : base(tref, objAsTypeInfo, isClassName)
        {
            Debug.Assert(boundTypes.Length > 1);
            Debug.Assert(!boundTypes.Any(t => t is BoundMultipleTypeRef));

            _boundTypes = boundTypes;
        }
    }
}
