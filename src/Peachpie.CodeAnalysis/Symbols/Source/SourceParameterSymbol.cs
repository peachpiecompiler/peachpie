using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Syntax;
using Pchp.CodeAnalysis.Semantics;
using System.Threading;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a PHP function parameter.
    /// </summary>
    internal sealed class SourceParameterSymbol : ParameterSymbol
    {
        readonly SourceRoutineSymbol _routine;
        readonly FormalParam _syntax;

        /// <summary>
        /// Index of the source parameter, relative to the first source parameter.
        /// </summary>
        readonly int _relindex;
        readonly PHPDocBlock.ParamTag _ptagOpt;

        internal PHPDocBlock.ParamTag PHPDocOpt => _ptagOpt;

        TypeSymbol _lazyType;

        /// <summary>
        /// Optional. The parameter initializer expression i.e. bound <see cref="FormalParam.InitValue"/>.
        /// </summary>
        public override BoundExpression Initializer => _initializer;
        readonly BoundExpression _initializer;

        /// <summary>
        /// Whether the parameter needs to be copied when passed by value.
        /// Can be set to <c>false</c> by analysis (e.g. unused parameter or only delegation to another method).
        /// </summary>
        public bool CopyOnPass { get; set; } = true;

        public override FieldSymbol DefaultValueField
        {
            get
            {
                if (_lazyDefaultValueField == null && Initializer != null && ExplicitDefaultConstantValue == null)
                {
                    TypeSymbol fldtype; // type of the field
                    
                    if (Initializer is BoundArrayEx arr)
                    {
                        // special case: empty array
                        if (arr.Items.Length == 0 && !_syntax.PassedByRef)
                        {
                            // OPTIMIZATION: reference the singleton field directly, the called routine is responsible to perform copy if necessary
                            // parameter MUST NOT be `PassedByRef` https://github.com/peachpiecompiler/peachpie/issues/591
                            // PhpArray.Empty
                            return DeclaringCompilation.CoreMethods.PhpArray.Empty;
                        }

                        //   
                        fldtype = DeclaringCompilation.CoreTypes.PhpArray;
                    }
                    else if (Initializer is BoundPseudoClassConst)
                    {
                        fldtype = DeclaringCompilation.GetSpecialType(SpecialType.System_String);
                    }
                    else
                    {
                        fldtype = DeclaringCompilation.CoreTypes.PhpValue;
                    }

                    // The construction of the default value may require a Context, cannot be created as a static singletong
                    // Additionally; default values of REF parameter must be created every time from scratch! https://github.com/peachpiecompiler/peachpie/issues/591
                    if (Initializer.RequiresContext ||
                        (_syntax.PassedByRef && fldtype.IsReferenceType && fldtype.SpecialType != SpecialType.System_String))  // we can cache the default value even for Refs if it is an immutable value
                    {
                        // Func<Context, PhpValue>
                        fldtype = DeclaringCompilation.GetWellKnownType(WellKnownType.System_Func_T2).Construct(
                            DeclaringCompilation.CoreTypes.Context,
                            DeclaringCompilation.CoreTypes.PhpValue);
                    }

                    // determine the field container:
                    NamedTypeSymbol fieldcontainer = ContainingType; // by default in the containing class/trait/file
                    string fieldname = $"<{ContainingSymbol.Name}.{Name}>_DefaultValue";

                    //if (fieldcontainer.IsInterface)
                    //{
                    //    fieldcontainer = _routine.ContainingFile;
                    //    fieldname = ContainingType.Name + "." + fieldname;
                    //}

                    // public static readonly T ..;
                    var field = new SynthesizedFieldSymbol(
                        fieldcontainer,
                        fldtype,
                        fieldname,
                        accessibility: Accessibility.Public,
                        isStatic: true, isReadOnly: true);

                    //
                    Interlocked.CompareExchange(ref _lazyDefaultValueField, field, null);
                }
                return _lazyDefaultValueField;
            }
        }
        FieldSymbol _lazyDefaultValueField;

        public SourceParameterSymbol(SourceRoutineSymbol routine, FormalParam syntax, int relindex, PHPDocBlock.ParamTag ptagOpt)
        {
            Contract.ThrowIfNull(routine);
            Contract.ThrowIfNull(syntax);
            Debug.Assert(relindex >= 0);

            _routine = routine;
            _syntax = syntax;
            _relindex = relindex;
            _ptagOpt = ptagOpt;
            _initializer = (syntax.InitValue != null)
                ? new SemanticsBinder(DeclaringCompilation, routine.ContainingFile.SyntaxTree, locals: null, routine: null, self: routine.ContainingType as SourceTypeSymbol)
                    .BindWholeExpression(syntax.InitValue, BoundAccess.Read)
                    .SingleBoundElement()
                : null;
        }

        /// <summary>
        /// Containing routine.
        /// </summary>
        internal SourceRoutineSymbol Routine => _routine;

        public override Symbol ContainingSymbol => _routine;

        internal override PhpCompilation DeclaringCompilation => _routine.DeclaringCompilation;

        internal override IModuleSymbol ContainingModule => _routine.ContainingModule;

        public override NamedTypeSymbol ContainingType => _routine.ContainingType;

        public override string Name => _syntax.Name.Name.Value;

        public override bool IsThis => false;

        public FormalParam Syntax => _syntax;

        internal sealed override TypeSymbol Type
        {
            get
            {
                if (_lazyType == null)
                {
                    Interlocked.CompareExchange(ref _lazyType, ResolveType(), null);
                }

                return _lazyType;
            }
        }

        /// <summary>
        /// Gets value indicating that if the parameters type is a reference type,
        /// it is not allowed to pass a null value.
        /// </summary>
        public override bool HasNotNull
        {
            get
            {
                // when providing type hint, only allow null if explicitly specified:
                if (_syntax.TypeHint == null || _syntax.TypeHint is NullableTypeRef || DefaultsToNull)
                {
                    return false;
                }

                //
                return true;
            }
        }

        internal bool DefaultsToNull => _initializer != null && _initializer.ConstantValue.IsNull();

        /// <summary>
        /// Gets value indicating whether the parameter has been replaced with <see cref="SourceRoutineSymbol.VarargsParam"/>.
        /// </summary>
        internal bool IsFake => (Routine.GetParamsParameter() != null && Routine.GetParamsParameter() != this && Ordinal >= Routine.GetParamsParameter().Ordinal);

        TypeSymbol ResolveType()
        {
            if (IsThis)
            {
                // <this> parameter
                if (_routine is SourceGlobalMethodSymbol)
                {
                    // "AnyType" in case of $this in global scope
                    return DeclaringCompilation.CoreTypes.PhpValue;
                }

                return ContainingType;
            }

            //return DeclaringCompilation.GetTypeFromTypeRef(_routine, _routine.ControlFlowGraph.GetParamTypeMask(this));

            // determine parameter type from the signature:

            // aliased parameter:
            if (_syntax.IsOut || _syntax.PassedByRef)
            {
                if (_syntax.IsVariadic)
                {
                    // PhpAlias[]
                    return ArrayTypeSymbol.CreateSZArray(this.ContainingAssembly, DeclaringCompilation.CoreTypes.PhpAlias);
                }
                else
                {
                    // PhpAlias
                    return DeclaringCompilation.CoreTypes.PhpAlias;
                }
            }

            // 1. specified type hint
            var typeHint = _syntax.TypeHint;
            if (typeHint is ReservedTypeRef rtref)
            {
                // workaround for https://github.com/peachpiecompiler/peachpie/issues/281
                // remove once it gets updated in parser
                if (rtref.Type == ReservedTypeRef.ReservedType.self) return _routine.ContainingType; // self
            }
            var result = DeclaringCompilation.GetTypeFromTypeRef(typeHint, _routine.ContainingType as SourceTypeSymbol, nullable: DefaultsToNull);

            // 2. optionally type specified in PHPDoc
            if (result == null && _ptagOpt != null && _ptagOpt.TypeNamesArray.Length != 0
                && (DeclaringCompilation.Options.PhpDocTypes & PhpDocTypes.ParameterTypes) != 0)
            {
                var typectx = _routine.TypeRefContext;
                var tmask = FlowAnalysis.PHPDoc.GetTypeMask(typectx, _ptagOpt.TypeNamesArray, _routine.GetNamingContext());
                if (!tmask.IsVoid && !tmask.IsAnyType)
                {
                    result = DeclaringCompilation.GetTypeFromTypeRef(typectx, tmask);
                }
            }

            // 3 default:
            if (result == null)
            {
                // TODO: use type from overriden method

                result = DeclaringCompilation.CoreTypes.PhpValue;
            }

            // variadic (result[])
            if (_syntax.IsVariadic)
            {
                result = ArrayTypeSymbol.CreateSZArray(this.ContainingAssembly, result);
            }

            //
            return result;
        }

        public override RefKind RefKind
        {
            get
            {
                //if (_syntax.IsOut)
                //    return RefKind.Out;

                return RefKind.None;
            }
        }

        public override bool IsParams => _syntax.IsVariadic;

        public override int Ordinal => _relindex + _routine.ImplicitParameters.Length;

        /// <summary>
        /// Zero-based index of the source parameter.
        /// </summary>
        public int ParameterIndex => _relindex;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(Location.Create(Routine.ContainingFile.SyntaxTree, _syntax.Name.Span.ToTextSpan()));
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override IEnumerable<AttributeData> GetCustomAttributesToEmit(CommonModuleCompilationState compilationState)
        {
            // [param]   
            if (IsParams)
            {
                yield return DeclaringCompilation.CreateParamsAttribute();
            }

            // [NotNull]
            if (HasNotNull && Type.IsReferenceType)
            {
                yield return DeclaringCompilation.CreateNotNullAttribute();
            }

            // [DefaultValue]
            if (DefaultValueField != null)
            {
                yield return DeclaringCompilation.CreateDefaultValueAttribute(ContainingType, DefaultValueField);
            }

            //
            yield break;
        }

        public override bool IsOptional => this.HasExplicitDefaultValue;

        internal override ConstantValue ExplicitDefaultConstantValue
        {
            get
            {
                ConstantValue value = null;

                if (Initializer != null)
                {
                    // NOTE: the constant does not have to have the exact same type as the parameter, it is up to the caller of the method to process DefaultValue and convert it if necessary

                    value = Initializer.ConstantValue.ToConstantValueOrNull();
                    if (value != null)
                    {
                        return value;
                    }

                    // NOTE: non-literal default values (like array()) must be handled by creating a ghost method overload calling this method:

                    // Template:
                    // foo($a = [], $b = [1, 2, 3]) =>
                    // + foo($a, $b){ /* this routine */ }
                    // + foo($a) => foo($a, [1, 2, 3])
                    // + foo() => foo([], [1, 2, 3)
                }

                //
                return value;
            }
        }
    }
}
