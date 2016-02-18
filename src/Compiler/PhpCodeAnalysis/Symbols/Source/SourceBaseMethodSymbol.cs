using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics;
using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal abstract class SourceBaseMethodSymbol : MethodSymbol
    {
        BoundMethodBody _block;

        /// <summary>
        /// Gets lazily bound block containing method semantics.
        /// Entry point of analysis and emitting.
        /// </summary>
        public BoundMethodBody BoundBlock => _block ?? (_block = CreateBoundBlock());

        protected abstract BoundMethodBody CreateBoundBlock();

        readonly protected ImmutableArray<ParameterSymbol> _params;

        public SourceBaseMethodSymbol(Signature signature)
        {
            _params = BuildParameters(signature).ToImmutableArray();
        }

        private IEnumerable<ParameterSymbol> BuildParameters(Signature signature)
        {
            int index = 0;

            foreach (var p in signature.FormalParams)
            {
                yield return new SourceParameterSymbol(this, p, index++);
            }
        }

        public override bool IsExtern => false;

        public override bool IsOverride => false;

        public override bool IsVirtual => !IsSealed && !IsStatic;

        public override SymbolKind Kind => SymbolKind.Method;

        public override MethodKind MethodKind
        {
            get
            {
                // TODO: ctor, dtor, props, magic, ...

                return MethodKind.Ordinary;
            }
        }

        public override ImmutableArray<IParameterSymbol> Parameters => StaticCast<IParameterSymbol>.From(_params);

        public override bool ReturnsVoid
        {
            get
            {
                return ReturnType.SpecialType == SpecialType.System_Void;
            }
        }

        public override ITypeSymbol ReturnType
        {
            get
            {
                return this.DeclaringCompilation.GetSpecialType(SpecialType.System_Object);
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;   // TODO: from PHPDoc

        /// <summary>
        /// virtual = IsVirtual && NewSlot 
        /// override = IsVirtual && !NewSlot
        /// </summary>
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => !IsOverride;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => IsVirtual;
    }
}
