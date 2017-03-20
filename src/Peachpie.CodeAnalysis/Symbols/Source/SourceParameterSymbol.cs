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

        TypeSymbol _lazyType;

        /// <summary>
        /// Optional. The parameter initializer expression i.e. bound <see cref="FormalParam.InitValue"/>.
        /// </summary>
        public BoundExpression Initializer => _initializer;
        readonly BoundExpression _initializer;

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
                ? new SemanticsBinder(null).BindExpression(syntax.InitValue, BoundAccess.Read)
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
                    _lazyType = ResolveType();
                }

                return _lazyType;
            }
        }

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
                return DeclaringCompilation.CoreTypes.PhpAlias;
            }

            // 1. specified type hint
            var result = DeclaringCompilation.GetTypeFromTypeRef(_syntax.TypeHint);

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

        public override int Ordinal => _relindex + _routine.ImplicitParameters.Count;

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
            if (IsParams)
            {
                yield return new SynthesizedAttributeData(
                    (MethodSymbol)DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_ParamArrayAttribute__ctor),
                    ImmutableArray<TypedConstant>.Empty, ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

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
