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
using System.Diagnostics;
using Pchp.CodeAnalysis.CodeGen;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Base symbol representing a method or a function from source.
    /// </summary>
    internal abstract partial class SourceRoutineSymbol : MethodSymbol
    {
        ControlFlowGraph _cfg;
        TypeRefContext _typeCtx;

        #region ISemanticFunction

        /// <summary>
        /// Gets lazily bound block containing method semantics.
        /// </summary>
        public override ImmutableArray<ControlFlowGraph> CFG
        {
            get
            {
                if (_cfg != null)
                    return ImmutableArray.Create(_cfg);

                return default(ImmutableArray<ControlFlowGraph>);
            }
        }

        /// <summary>
        /// Lazily bound semantic block, equivalent for CFG[0].
        /// Entry point of analysis and emitting.
        /// </summary>
        internal ControlFlowGraph ControlFlowGraph
        {
            get { return _cfg; }
            set { _cfg = value; }
        }

        public override TypeRefMask GetResultType(TypeRefContext ctx)
        {
            var cfg = this.ControlFlowGraph;
            return ctx.AddToContext(cfg.FlowContext.TypeRefContext, cfg.ReturnTypeMask);
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

        protected abstract TypeRefContext CreateTypeRefContext();

        /// <summary>
        /// Gets routine declaration syntax.
        /// </summary>
        internal abstract AstNode Syntax { get; }

        /// <summary>
        /// Reference to a containing file symbol.
        /// </summary>
        internal abstract SourceFileSymbol ContainingFile { get; }

        protected ImmutableArray<ParameterSymbol> _params;

        /// <summary>
        /// Builds CLR method parameters.
        /// </summary>
        /// <remarks>(Context, arg1, arg2, ...)</remarks>
        protected virtual IEnumerable<ParameterSymbol> BuildParameters(Signature signature)
        {
            int index = 0;

            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++);

            foreach (var p in signature.FormalParams)
            {
                yield return new SourceParameterSymbol(this, p, index++);
            }
        }

        public override bool IsExtern => false;

        public override bool IsOverride => false;

        public override bool IsVirtual => !IsSealed && !IsStatic;

        public override MethodKind MethodKind
        {
            get
            {
                // TODO: ctor, dtor, props, magic, ...

                return MethodKind.Ordinary;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters => _params;

        public override int ParameterCount => _params.Length;

        public override bool ReturnsVoid => this.ControlFlowGraph.ReturnTypeMask.IsVoid;

        public override TypeSymbol ReturnType => DeclaringCompilation.GetTypeFromTypeRef(this, this.ControlFlowGraph.ReturnTypeMask, false);

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;   // TODO: from PHPDoc

        /// <summary>
        /// virtual = IsVirtual && NewSlot 
        /// override = IsVirtual && !NewSlot
        /// </summary>
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => !IsOverride;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => IsVirtual;
    }
}
