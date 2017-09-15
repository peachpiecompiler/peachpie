using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.CodeGen;

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

        public bool IsDirect => TypeExpression == null;

        ITypeSymbol IBoundTypeRef.Symbol => throw new NotImplementedException();

        public BoundTypeRef(TypeRef tref)
        {
            _typeRef = tref;
        }
    }
}
