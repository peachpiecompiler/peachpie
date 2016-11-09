using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Syntax;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Base symbol representing a method or a function from source.
    /// </summary>
    internal abstract partial class SourceRoutineSymbol : MethodSymbol
    {
        ControlFlowGraph _cfg;
        FlowState _state;
        LocalsTable _locals;

        /// <summary>
        /// Lazily bound semantic block.
        /// Entry point of analysis and emitting.
        /// </summary>
        public override ControlFlowGraph ControlFlowGraph
        {
            get
            {
                if (_cfg == null && this.Statements != null) // ~ Statements => non abstract method
                {
                    // create initial flow state
                    var state = StateBinder.CreateInitialState(this);
                    
                    //
                    var binder = new SemanticsBinder(this.LocalsTable);

                    // build control flow graph
                    _cfg = new ControlFlowGraph(this.Statements, binder, this.GetNamingContext());
                    _cfg.Start.FlowState = state;
                }

                return _cfg;
            }
        }

        /// <summary>
        /// Gets table of local variables.
        /// Variables are lazily added to the table.
        /// </summary>
        internal LocalsTable LocalsTable
        {
            get
            {
                var locals = _locals;
                if (locals == null)
                {
                    _locals = locals = new LocalsTable(this);
                }

                return locals;
            }
        }

        internal abstract IList<Statement> Statements { get; }

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
                var ptag = (phpdocOpt != null) ? PHPDoc.GetParamTag(phpdocOpt, pindex, p.Name.Name.Value) : null;

                yield return new SourceParameterSymbol(this, p, index++, ptag);

                pindex++;
            }
        }

        protected virtual TypeSymbol BuildReturnType(Signature signature, TypeRef tref, PHPDocBlock phpdocOpt, TypeRefMask rtype)
        {
            if (signature.AliasReturn)
            {
                return DeclaringCompilation.CoreTypes.PhpAlias;
            }

            // PHP7 return type
            if (tref != null)
            {
                return DeclaringCompilation.GetTypeFromTypeRef(tref);
            }

            //
            var typeCtx = this.TypeRefContext;

            //
            if (phpdocOpt != null)
            {
                var returnTag = phpdocOpt.Returns;
                if (returnTag != null && returnTag.TypeNames.Length != 0)
                {
                    var tmask = PHPDoc.GetTypeMask(typeCtx, returnTag.TypeNamesArray, this.GetNamingContext());
                    if (!tmask.IsVoid && !tmask.IsAnyType)
                    {
                        return DeclaringCompilation.GetTypeFromTypeRef(typeCtx, tmask);
                    }
                }
            }

            //
            return DeclaringCompilation.GetTypeFromTypeRef(typeCtx, rtype);
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
        /// virtual = IsVirtual AND NewSlot 
        /// override = IsVirtual AND !NewSlot
        /// </summary>
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => !IsOverride && !IsStatic;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => IsVirtual;
    }
}
