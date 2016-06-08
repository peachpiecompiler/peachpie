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
using Pchp.Syntax;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Base symbol representing a method or a function from source.
    /// </summary>
    internal abstract partial class SourceRoutineSymbol : MethodSymbol
    {
        [Flags]
        public enum RoutineFlags
        {
            None = 0,

            HasEval = 1,
            HasInclude = 2,
            HasIndirectVar = 4,
            UsesLocals = 8,

            /// <summary>
            /// The routine uses <c>static::</c> construct to access late static bound type.
            /// </summary>
            UsesLateStatic = 16,

            /// <summary>
            /// Whether the routine has to define local variables as an array instead of native local variables.
            /// </summary>
            RequiresLocalsArray = HasEval | HasInclude | HasInclude | UsesLocals,
        }

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

        /// <summary>
        /// Routine flags lazily initialized during code analysis.
        /// </summary>
        internal RoutineFlags Flags { get; set; }

        internal abstract IList<Statement> Statements { get; }

        internal TypeRefContext TypeRefContext => _typeCtx ?? (_typeCtx = CreateTypeRefContext());

        protected abstract TypeRefContext CreateTypeRefContext();

        public abstract ParameterSymbol ThisParameter { get; }

        /// <summary>
        /// Gets routine declaration syntax.
        /// </summary>
        internal abstract AstNode Syntax { get; }

        /// <summary>
        /// Optionaly gets routines PHP doc block.
        /// </summary>
        internal abstract PHPDocBlock PHPDocBlock { get; }

        /// <summary>
        /// Reference to a containing file symbol.
        /// </summary>
        internal abstract SourceFileSymbol ContainingFile { get; }

        protected ImmutableArray<ParameterSymbol> _params;

        /// <summary>
        /// Builds CLR method parameters.
        /// </summary>
        /// <remarks>(Context, arg1, arg2, ...)</remarks>
        protected virtual IEnumerable<ParameterSymbol> BuildParameters(Signature signature, PHPDocBlock phpdocOpt = null)
        {
            int index = 0;

            if (this.IsStatic)  // instance methods have <ctx> in <this>.<ctx> field, see SourceNamedTypeSymbol._lazyContextField
            {
                yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++);
            }

            int pindex = 0;

            foreach (var p in signature.FormalParams)
            {
                var ptag = (phpdocOpt != null) ? PHPDoc.GetParamTag(phpdocOpt, pindex, p.Name.Value) : null;

                yield return new SourceParameterSymbol(this, p, index++, ptag);

                pindex++;
            }
        }

        protected virtual TypeSymbol BuildReturnType(Signature signature, PHPDocBlock phpdocOpt, TypeRefMask return_tmask)
        {
            if (signature.AliasReturn)
                return DeclaringCompilation.CoreTypes.PhpAlias;

            // TODO: PHP7 return type
            //signature.ReturnTypeHint

            //
            if (phpdocOpt != null)
            {
                var returnTag = phpdocOpt.Returns;
                if (returnTag != null && returnTag.TypeNames.Length != 0)
                {
                    var typeCtx = this.TypeRefContext;
                    var tmask = PHPDoc.GetTypeMask(typeCtx, returnTag.TypeNamesArray);
                    if (!tmask.IsVoid && !tmask.IsAnyType)
                    {
                        return DeclaringCompilation.GetTypeFromTypeRef(typeCtx, tmask);
                    }
                }
            }

            //
            return DeclaringCompilation.GetTypeFromTypeRef(this.TypeRefContext, return_tmask);
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

        public override bool ReturnsVoid => ReturnType.SpecialType == SpecialType.System_Void;

        //public override TypeSymbol ReturnType { get; }
        //{
        //    get
        //    {
        //        throw new InvalidOperationException("To be overriden in derived class!");
        //        //return DeclaringCompilation.GetTypeFromTypeRef(this, this.ControlFlowGraph.ReturnTypeMask);
        //    }
        //}

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;   // TODO: from PHPDoc

        /// <summary>
        /// virtual = IsVirtual && NewSlot 
        /// override = IsVirtual && !NewSlot
        /// </summary>
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => !IsOverride && !IsStatic;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => IsVirtual;
    }
}
