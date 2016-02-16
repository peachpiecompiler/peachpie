using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Symbols
{
    internal abstract partial class ParameterSymbol : Symbol, IParameterSymbol
    {
        public ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public virtual object ExplicitDefaultValue => null;

        public virtual bool HasExplicitDefaultValue => false;

        public virtual bool IsOptional => false;

        public virtual bool IsParams => false;

        public virtual bool IsThis => false;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        public override bool IsExtern => false;

        /// <summary>
        /// Gets the ordinal position of the parameter. The first parameter has ordinal zero.
        /// The "'this' parameter has ordinal -1.
        /// </summary>
        public abstract int Ordinal { get; }

        public virtual RefKind RefKind => RefKind.None;

        public virtual ITypeSymbol Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        protected override Symbol OriginalSymbolDefinition => this;

        IParameterSymbol IParameterSymbol.OriginalDefinition => (IParameterSymbol)OriginalSymbolDefinition;
    }
}
