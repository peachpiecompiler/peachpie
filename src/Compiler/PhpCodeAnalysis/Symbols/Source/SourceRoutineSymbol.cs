using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Base symbol representing a method or a function from source.
    /// </summary>
    internal abstract class SourceRoutineSymbol : MethodSymbol
    {
        ImmutableArray<ControlFlowGraph> _cfg;
        TypeRefContext _typeCtx;

        #region ISemanticFunction

        /// <summary>
        /// Gets lazily bound block containing method semantics.
        /// Entry point of analysis and emitting.
        /// </summary>
        public override ImmutableArray<ControlFlowGraph> CFG
        {
            get
            {
                if (_cfg.IsDefault)
                    _cfg = ImmutableArray.Create(CreateCFG());

                return _cfg;
            }
        }

        #endregion

        #region Helpers
        
        /// <summary>
        /// Creates type context for a method within given type, determines naming, type context.
        /// </summary>
        protected static TypeRefContext/*!*/CreateTypeRefContext(TypeDecl/*!*/typeDecl)
        {
            Contract.ThrowIfNull(typeDecl);

            return new TypeRefContext(NameUtils.GetNamingContext(typeDecl), typeDecl.SourceUnit, typeDecl);
        }

        #endregion

        internal abstract IList<Statement> Statements { get; }

        internal TypeRefContext TypeRefContext => _typeCtx ?? (_typeCtx = CreateTypeRefContext());

        protected ControlFlowGraph CreateCFG() => new ControlFlowGraph(this.Statements);

        protected abstract TypeRefContext CreateTypeRefContext();

        /// <summary>
        /// Gets routine declaration syntax.
        /// </summary>
        internal abstract LangElement Syntax { get; }

        readonly protected ImmutableArray<ParameterSymbol> _params;

        public SourceRoutineSymbol(Signature signature)
        {
            _params = BuildParameters(signature).ToImmutableArray();
        }

        IEnumerable<ParameterSymbol> BuildParameters(Signature signature)
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
