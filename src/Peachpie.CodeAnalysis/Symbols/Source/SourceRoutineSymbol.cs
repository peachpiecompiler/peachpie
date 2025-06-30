﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
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
using Devsense.PHP.Text;
using System.Globalization;
using System.Threading;
using Peachpie.CodeAnalysis.Semantics;
using Devsense.PHP.Ast.DocBlock;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Base symbol representing a method or a function from source.
    /// </summary>
    internal abstract partial class SourceRoutineSymbol : MethodSymbol
    {
        [Flags]
        protected enum CommonFlags
        {
            OverriddenMethodResolved = 1,
        }

        /// <summary>Internal true/false values. Initially all false.</summary>
        protected CommonFlags _commonflags;

        ControlFlowGraph _cfg;
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
                    // build control flow graph
                    var cfg = new ControlFlowGraph(this.Statements, CreateBinder());

                    // create initial flow state
                    cfg.Start.FlowState = StateBinder.CreateInitialState(this);

                    //
                    Interlocked.CompareExchange(ref _cfg, cfg, null);
                }

                //
                return _cfg;
            }
        }

        protected SemanticsBinder CreateBinder()
        {
            return SemanticsBinder.Create(DeclaringCompilation, ContainingFile.SyntaxTree, LocalsTable, ContainingType as SourceTypeSymbol);
        }

        protected SemanticsBinder CreateBinderNoLocals()
        {
            return new SemanticsBinder(DeclaringCompilation, ContainingFile.SyntaxTree, self: ContainingType as SourceTypeSymbol);
        }

        /// <summary>
        /// Sets the new value of <see cref="ControlFlowGraph"/>.
        /// </summary>
        internal void UpdateControlFlowGraph(ControlFlowGraph newcfg)
        {
            _cfg = newcfg ?? throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(null);
        }

        /// <summary>
        /// Gets table of local variables.
        /// Variables are lazily added to the table.
        /// </summary>
        internal LocalsTable LocalsTable
        {
            get
            {
                if (_locals == null)
                {
                    Interlocked.CompareExchange(ref _locals, new LocalsTable(this), null);
                }

                return _locals;
            }
        }

        internal abstract IList<Statement> Statements { get; }

        protected abstract TypeRefContext CreateTypeRefContext();

        internal abstract Signature SyntaxSignature { get; }

        /// <summary>
        /// Specified return type.
        /// </summary>
        internal abstract TypeRef SyntaxReturnType { get; }

        /// <summary>
        /// Gets routine declaration syntax.
        /// </summary>
        internal abstract AstNode Syntax { get; }

        /// <summary>
        /// Optionaly gets routines PHP doc block.
        /// </summary>
        internal abstract IDocBlock PHPDocBlock { get; }

        /// <summary>
        /// Reference to a containing file symbol.
        /// </summary>
        internal abstract SourceFileSymbol ContainingFile { get; }

        ImmutableArray<AttributeData> _lazyAttributes;

        public override ImmutableArray<Location> Locations =>
            ImmutableArray.Create(
                Location.Create(
                    ContainingFile.SyntaxTree,
                    Syntax is ILangElement element ? element.Span.ToTextSpan() : default
            ));

        public override bool IsUnreachable => (Flags & RoutineFlags.IsUnreachable) != 0;

        protected ImmutableArray<ParameterSymbol> _implicitParameters;
        private SourceParameterSymbol[] _srcParams;

        /// <summary>Implicitly declared [params] parameter if the routine allows access to its arguments. This allows more arguments to be passed than declared.</summary>
        private SynthesizedParameterSymbol _implicitVarArg; // behaves like a stack of optional parameters

        /// <summary>
        /// Builds implicit parameters before source parameters.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<ParameterSymbol> BuildImplicitParams()
        {
            var index = 0;

            if (IsStatic)  // instance methods have <ctx> in <this>.<ctx> field, see SourceNamedTypeSymbol._lazyContextField
            {
                // Context <ctx>
                yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++, notNull: true);
            }
        }

        /// <summary>
        /// Gets value indicating this routine requires a special {PhpTypeInfo static} parameter to resolve `static` reserved type inside the routine body.
        /// </summary>
        internal virtual bool RequiresLateStaticBoundParam => false;

        /// <summary>
        /// Collects declaration diagnostics.
        /// </summary>
        public virtual void GetDiagnostics(DiagnosticBag diagnostic)
        {
            // check mandatory behind and optional parameter
            bool foundopt = false;
            var ps = SyntaxSignature.FormalParamsFixed();
            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                if (p == null)
                {
                    continue;
                }

                if (p.InitValue == null)
                {
                    if (foundopt && !p.IsVariadic)
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, p.Span, Devsense.PHP.Errors.Warnings.MandatoryBehindOptionalParam, "$" + p.Name.Name.Value));
                    }
                }
                else
                {
                    foundopt = true;
                }

                //
                if (p.IsVariadic && i < ps.Length - 1)
                {
                    // Fatal Error: variadic parameter (...) must be the last parameter
                    diagnostic.Add(this, p, Errors.ErrorCode.ERR_VariadicParameterNotLast);
                }

                // type hint
                if (p.TypeHint != null)
                {
                    if (p.TypeHint.IsVoid() || p.TypeHint.IsNever())
                    {
                        // not allowed types
                        diagnostic.Add(this, p, Errors.ErrorCode.ERR_ParameterTypeNotAllowed, p.Name.Name.Value, p.TypeHint);
                    }
                }

                // constructor property
                if (p.IsConstructorProperty)
                {
                    if (!string.Equals(this.Name, Devsense.PHP.Syntax.Name.SpecialMethodNames.Construct.Value, StringComparison.InvariantCultureIgnoreCase) ||
                        !(this is SourceMethodSymbol))
                    {
                        // ERR: not a constructor
                        diagnostic.Add(this, p, Errors.ErrorCode.ERR_CtorPropertyNotCtor);
                    }
                    else if (this.IsAbstract || this.ContainingType.IsInterface)
                    {
                        diagnostic.Add(this, p, Errors.ErrorCode.ERR_CtorPropertyAbstractCtor);
                    }
                    else if (this.IsStatic) // function, static method, static ctor
                    {
                        diagnostic.Add(this, p, Errors.ErrorCode.ERR_CtorPropertyStaticCtor);
                    }
                    else if (p.TypeHint is PrimitiveTypeRef pt && (
                        pt.PrimitiveTypeName == PrimitiveTypeRef.PrimitiveType.callable ||
                        //pt.PrimitiveTypeName == PrimitiveTypeRef.PrimitiveType.iterable || // it seems it's allowed now
                        pt.PrimitiveTypeName == PrimitiveTypeRef.PrimitiveType.never))
                    {
                        diagnostic.Add(this, p, Errors.ErrorCode.ERR_PropertyTypeNotAllowed, this.ContainingType.PhpName(), p.Name, pt.PrimitiveTypeName.ToString());
                    }
                    else if (p.TypeHint is ReservedTypeRef rt && (
                        rt.Type == ReservedTypeRef.ReservedType.@static))
                    {
                        diagnostic.Add(this, p, Errors.ErrorCode.ERR_PropertyTypeNotAllowed, this.ContainingType.PhpName(), p.Name, rt.Type.ToString());
                    }
                    else if (p.IsVariadic)
                    {
                        diagnostic.Add(this, p, Errors.ErrorCode.ERR_CtorPropertyVariadic);
                    }
                }
            }
        }

        /// <summary>
        /// Constructs routine source parameters.
        /// </summary>
        protected IEnumerable<SourceParameterSymbol> BuildSrcParams(IEnumerable<FormalParam> formalparams)
        {
            var pindex = 0; // zero-based relative index

            foreach (var p in formalparams)
            {
                if (p == null)
                {
                    continue;
                }

                yield return new SourceParameterSymbol(this, p, relindex: pindex++);
            }
        }

        protected virtual IEnumerable<SourceParameterSymbol> BuildSrcParams(Signature signature)
        {
            return BuildSrcParams(signature.FormalParams);
        }

        internal virtual ImmutableArray<ParameterSymbol> ImplicitParameters
        {
            get
            {
                if (_implicitParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _implicitParameters, BuildImplicitParams().ToImmutableArray());
                }

                var currentImplicitParameters = _implicitParameters;
                if (RequiresLateStaticBoundParam && !currentImplicitParameters.Any(SpecialParameterSymbol.IsLateStaticParameter))
                {
                    // PhpTypeInfo <static>
                    var implicitParameters = currentImplicitParameters.Add(
                        new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.PhpTypeInfo, SpecialParameterSymbol.StaticTypeName, currentImplicitParameters.Length));
                    ImmutableInterlocked.InterlockedCompareExchange(ref _implicitParameters, implicitParameters, currentImplicitParameters);
                }

                //
                return _implicitParameters;
            }
        }

        internal SourceParameterSymbol[] SourceParameters
        {
            get
            {
                if (_srcParams == null)
                {
                    var srcParams = BuildSrcParams(this.SyntaxSignature).ToArray();
                    Interlocked.CompareExchange(ref _srcParams, srcParams, null);
                }

                return _srcParams;
            }
        }

        SourceParameterSymbol SourceVarargsParam
        {
            get
            {
                var srcparams = this.SourceParameters;
                if (srcparams.Length != 0)
                {
                    var last = srcparams.Last();
                    if (last.IsParams)
                    {
                        return last;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Implicitly added parameter corresponding to <c>params PhpValue[] {arguments}</c>. Replaces all the optional parameters.
        /// !!IMPORTANT!! Its <see cref="ParameterSymbol.Ordinal"/> specifies its position - all the source parameters with the same or higher ordinal are ignored.
        /// Can be <c>null</c> if not needed.
        /// </summary>
        protected ParameterSymbol VarargsParam
        {
            get
            {
                // declare implicit [... varargs] parameter if needed and not defined as source parameter
                if ((Flags & RoutineFlags.RequiresVarArg) != 0 && !IsGlobalScope)
                {
                    if (_implicitVarArg == null)
                    {
                        var srcparams = SourceVarargsParam;

                        // is there is params (...) already and no optional parameters, we can stick with it
                        if (srcparams != null && SourceParameters.All(p => p.Initializer == null))
                        {
                            return null;
                        }

                        // create implicit [... params]
                        var implicitVarArg = new SynthesizedParameterSymbol( // IsImplicitlyDeclared, IsParams
                            this,
                            ArrayTypeSymbol.CreateSZArray(this.ContainingAssembly, this.DeclaringCompilation.CoreTypes.PhpValue),
                            0,
                            RefKind.None,
                            SpecialParameterSymbol.ParamsName, isParams: true);
                        Interlocked.CompareExchange(ref _implicitVarArg, implicitVarArg, null);
                    }
                }

                if (_implicitVarArg != null)
                {
                    // implicit params replaces all the optional arguments!!
                    int mandatory = ImplicitParameters.Length + this.SourceParameters.TakeWhile(p => p.Initializer == null).Count();
                    _implicitVarArg.UpdateOrdinal(mandatory);
                }

                return _implicitVarArg;
            }
        }

        /// <summary>
        /// Gets params parameter or null.
        /// </summary>
        internal ParameterSymbol GetParamsParameter()
        {
            var p = VarargsParam ?? SourceVarargsParam;
            Debug.Assert(p == null || p.Type.IsSZArray());

            return p;
        }

        public override bool IsExtern => false;

        public override bool IsOverride => false;

        public override bool IsVirtual => !IsSealed && !IsStatic;

        public override bool CastToFalse => false;  // source routines never cast special values to FALSE

        public override bool HasNotNull => !ReturnsNull;

        public override MethodKind MethodKind
        {
            get
            {
                // TODO: ctor, dtor, props, magic, ...

                return MethodKind.Ordinary;
            }
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                // [implicit parameters], [source parameters], [...varargs]

                var srcparams = SourceParameters;
                var implicitVarArgs = VarargsParam;

                var result = new List<ParameterSymbol>(ImplicitParameters.Length + srcparams.Length);

                result.AddRange(ImplicitParameters);

                if (implicitVarArgs == null)
                {
                    result.AddRange(srcparams);
                }
                else
                {
                    // implicitVarArgs replaces optional srcparams
                    for (int i = 0; i < srcparams.Length && srcparams[i].Ordinal < implicitVarArgs.Ordinal; i++)
                    {
                        result.Add(srcparams[i]);
                    }

                    result.Add(implicitVarArgs);
                }

                return result.AsImmutableOrEmpty();
            }
        }

        public sealed override int ParameterCount
        {
            get
            {
                // [implicit parameters], [source parameters], [...varargs]

                var implicitVarArgs = VarargsParam;
                if (implicitVarArgs != null)
                {
                    return implicitVarArgs.Ordinal + 1;
                }
                else
                {
                    return ImplicitParameters.Length + SourceParameters.Length;
                }
            }
        }

        public override bool ReturnsVoid => ReturnType.SpecialType == SpecialType.System_Void;

        /// <summary>
        /// Gets value indicating the routine can return <c>null</c>.
        /// </summary>
        public bool ReturnsNull
        {
            get
            {
                var thint = SyntaxReturnType;
                if (thint != null)
                {
                    // if type hint is provided,
                    // only can be NULL if specified
                    return thint.CanBeNull();
                }
                else if (this.IsOverrideable())
                {
                    // a virtual method can be overriden with anything
                    // always possible it may return null
                    return true;
                }
                else if (SyntaxSignature.AliasReturn)
                {
                    // returns an aliased value,
                    // can be anything
                    return true;
                }

                // use the result of type analysis
                var tmask = ResultTypeMask;

                return tmask.IsAnyType || tmask.IsRef || this.TypeRefContext.IsNullOrVoid(tmask);
            }
        }

        public override RefKind RefKind => RefKind.None;

        public override TypeSymbol ReturnType => PhpRoutineSymbolExtensions.ConstructClrReturnType(this);

        /// <summary>
        /// Gets enumeration of function attributes.
        /// </summary>
        public IReadOnlyList<SourceCustomAttribute> SourceAttributes => this.GetAttributes().OfTypeToReadOnlyList<AttributeData, SourceCustomAttribute>();

        ImmutableArray<AttributeData> PopulateSourceAttributes()
        {
            var attrs = ImmutableArray<AttributeData>.Empty;

            var phpattrs = Syntax.GetAttributes();
            if (phpattrs.Count != 0)
            {
                attrs = attrs.AddRange(CreateBinderNoLocals().BindAttributes(phpattrs));
            }

            // attributes from PHPDoc
            var deprecated = PHPDocBlock.GetDocEntry(AstUtils.DeprecatedTagName);
            if (deprecated != null)
            {
                // [ObsoleteAttribute(message, false)]
                attrs = attrs.Add(DeclaringCompilation.CreateObsoleteAttribute(deprecated));
            }

            // [DoesNotReturnAttribute]
            if (SyntaxReturnType.IsNever())
            {
                var attr = DeclaringCompilation.TryCreateDoesNotReturnAttribute();
                if (attr != null)
                {
                    attrs = attrs.Add(attr);
                }
            }

            return attrs;
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            if (_lazyAttributes.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyAttributes, PopulateSourceAttributes());
            }

            //
            return _lazyAttributes;
        }

        public override ImmutableArray<AttributeData> GetReturnTypeAttributes()
        {
            if (!ReturnsNull)
            {
                // [return: Nullable(1)] - does not return null
                var returnType = this.ReturnType;
                if (returnType != null && (returnType.IsReferenceType || returnType.Is_PhpValue())) // only if it makes sense to check for NULL
                {
                    return ImmutableArray.Create<AttributeData>(DeclaringCompilation.CreateNullableAttribute(NullableContextUtils.NotAnnotatedAttributeValue));
                }
            }

            //
            return ImmutableArray<AttributeData>.Empty;
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                var deprecated = this.PHPDocBlock.GetDocEntry(AstUtils.DeprecatedTagName);
                if (deprecated != null)
                {
                    return new ObsoleteAttributeData(
                        ObsoleteAttributeKind.Deprecated,
                        deprecated.GetEntryText(out var text) ? text : string.Empty,
                        isError: false,
                        diagnosticId: null,
                        urlFormat: null
                    );
                }

                return null;
            }
        }

        /// <summary>
        /// virtual = IsVirtual AND NewSlot 
        /// override = IsVirtual AND !NewSlot
        /// </summary>
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => !IsOverride && IsMetadataVirtual(ignoreInterfaceImplementationChanges);

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return IsVirtual && (!ContainingType.IsSealed || IsOverride || IsAbstract || OverrideOfMethod() != null);  // do not make method virtual if not necessary
        }

        /// <summary>
        /// Gets value indicating the method is an override of another virtual method.
        /// In such a case, this method MUST be virtual.
        /// </summary>
        private MethodSymbol OverrideOfMethod()
        {
            var overrides = ContainingType.ResolveOverrides(DiagnosticBag.GetInstance());   // Gets override resolution matrix. This is already resolved and does not cause an overhead.

            for (int i = 0; i < overrides.Length; i++)
            {
                if (overrides[i].Override == this)
                {
                    return overrides[i].Method;
                }
            }

            return null;
        }

        internal override bool IsMetadataFinal => base.IsMetadataFinal && IsMetadataVirtual(); // altered IsMetadataVirtual -> causes change to '.final' metadata as well

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (PHPDocBlock != null)
            {
                using (var output = new System.IO.StringWriter())
                {
                    DocumentationComments.DocumentationCommentCompiler.WriteRoutine(output, this);
                    return output.ToString();
                }
            }
            //
            return string.Empty;
        }
    }
}
