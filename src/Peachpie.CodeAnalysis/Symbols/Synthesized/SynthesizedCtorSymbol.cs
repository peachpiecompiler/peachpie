using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using Devsense.PHP.Syntax;
using System.Threading;

namespace Pchp.CodeAnalysis.Symbols
{
    #region SynthesizedCtorSymbol // generated .ctor method

    internal class SynthesizedCtorSymbol : SynthesizedMethodSymbol
    {
        public SynthesizedCtorSymbol(NamedTypeSymbol/*!*/container)
            : base(container, WellKnownMemberNames.InstanceConstructorName, false, false, container.DeclaringCompilation.CoreTypes.Void)
        {
            Debug.Assert(!container.IsStatic);
        }

        public override bool HidesBaseMethodsByName => false;

        internal override bool HasSpecialName => true;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public sealed override bool IsAbstract => false;

        public sealed override bool IsExtern => false;

        public sealed override bool IsOverride => false;

        public sealed override bool IsSealed => false;

        public sealed override bool IsVirtual => false;

        public sealed override MethodKind MethodKind => MethodKind.Constructor;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;
    }

    #endregion

    #region SynthesizedPhpCtorSymbol

    /// <summary>
    /// Synthesized <c>.ctor</c> method (instance constructor) for PHP classes.
    /// </summary>
    internal class SynthesizedPhpCtorSymbol : SynthesizedMethodSymbol
    {
        readonly MethodSymbol _basector;
        readonly MethodSymbol _phpconstruct;

        /// <summary>
        /// Base <c>.ctor</c> to be called by this method.
        /// </summary>
        public MethodSymbol BaseCtor => _basector;

        /// <summary>
        /// Optional. PHP constructor <c>__construct</c> to be called by this method.
        /// </summary>
        public MethodSymbol PhpConstructor => _phpconstruct;

        public bool IsInitFieldsOnly { get; private set; }

        protected SynthesizedPhpCtorSymbol(SourceTypeSymbol containingType, Accessibility accessibility,
            bool isInitFieldsOnly,
            MethodSymbol basector, MethodSymbol __construct, int paramsLimit = int.MaxValue)
            : base(containingType, WellKnownMemberNames.InstanceConstructorName, false, false, containingType.DeclaringCompilation.CoreTypes.Void, accessibility)
        {
            _basector = basector ?? throw new ArgumentNullException(nameof(basector));
            _phpconstruct = __construct;

            this.IsInitFieldsOnly = isInitFieldsOnly;

            // clone parameters from __construct ?? basector
            var template = (__construct ?? basector).Parameters;

            _parameters = CreateParameters(template.Take(paramsLimit)).ToImmutableArray();
        }

        protected virtual IEnumerable<ParameterSymbol> CreateParameters(IEnumerable<ParameterSymbol> baseparams)
        {
            int index = 0;

            // Context <ctx>
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++);

            // same parameters as PHP constructor
            foreach (var p in baseparams)
            {
                if (!SpecialParameterSymbol.IsContextParameter(p))
                {
                    yield return new SynthesizedParameterSymbol(this, p.Type, index++, p.RefKind, p.Name, p.IsParams,
                        explicitDefaultConstantValue: p.ExplicitDefaultConstantValue);
                }
            }
        }

        /// <summary>
        /// Creates CLS constructors for a PHP class.
        /// </summary>
        /// <param name="type">PHP class.</param>
        /// <returns>Enumeration of instance constructors for PHP class.</returns>
        /// <remarks>
        /// Constructors are created with respect to <c>base..ctor</c> and class PHP constructor function.
        /// At least a single <c>.ctor</c> is created which initializes fields and calls <c>base..ctor</c>. This is main constructor needed to properly initialize the class.
        /// In case there is a PHP constructor function:
        /// - The first ctor is marked as protected and is used only by other ctors and by derived classes to initialize class without calling the PHP constructor function.
        /// - Another ctor is created in order to call the main constructor and call PHP constructor function.
        /// - Ghost stubs of the other ctor are created in order to pass default parameter values which cannot be stored in metadata (e.g. array()).
        /// </remarks>
        public static IEnumerable<MethodSymbol> CreateCtors(SourceTypeSymbol type)
        {
            if (type.IsStatic || type.IsInterface)
            {
                yield break;
            }

            // resolve php constructor
            var phpconstruct = type.ResolvePhpCtor(true); // this tells us what parameters are provided so we can select best overload for base..ctor() call

            // resolve base .ctor that has to be called
            var btype = type.BaseType;
            var fieldsonlyctor = (MethodSymbol)(btype as IPhpTypeSymbol)?.InstanceConstructorFieldsOnly;   // base..ctor() to be called if provided
            var basectors = (fieldsonlyctor != null)
                ? ImmutableArray.Create(fieldsonlyctor)
                : btype.InstanceConstructors
                    .Where(c => c.DeclaredAccessibility != Accessibility.Private)   // ignore inaccessible .ctors
                    .OrderByDescending(c => c.ParameterCount)   // longest ctors first
                    .AsImmutable();

            // what parameters are provided
            var givenparams = (phpconstruct != null)
                ? phpconstruct.Parameters.Where(p => !p.IsImplicitlyDeclared && !p.IsParams).AsImmutable()
                : ImmutableArray<ParameterSymbol>.Empty;

            // first declare .ctor that initializes fields only and calls base .ctor
            var basector = ResolveBaseCtor(givenparams, basectors);
            if (basector == null)
            {
                // type.BaseType was not resolved, reported by type.BaseType
                // TODO: Err & ErrorMethodSymbol
                yield break;
            }

            MethodSymbol defaultctor; // .ctor to be used by default

            // create .ctor(s)
            if (phpconstruct == null)
            {
                yield return defaultctor = new SynthesizedPhpCtorSymbol(type, Accessibility.Public, false, basector, null);
            }
            else
            {
                var fieldsinitctor = new SynthesizedPhpCtorSymbol(type, Accessibility.ProtectedOrInternal, true, basector, null);
                yield return fieldsinitctor;

                // generate .ctor(s) calling PHP __construct with optional overloads in case there is an optional parameter
                var ps = phpconstruct.Parameters;
                for (int i = 0; i < ps.Length; i++)
                {
                    var p = ps[i] as SourceParameterSymbol;
                    if (p != null && p.Initializer != null && p.ExplicitDefaultConstantValue == null)   // => ConstantValue couldn't be resolved for optional parameter
                    {
                        yield return new SynthesizedPhpCtorSymbol(type, phpconstruct.DeclaredAccessibility, false, fieldsinitctor, phpconstruct, i);
                    }
                }

                yield return defaultctor = new SynthesizedPhpCtorSymbol(type, phpconstruct.DeclaredAccessibility, false, fieldsinitctor, phpconstruct);
            }

            // parameterless .ctor() with shared context
            if (defaultctor.DeclaredAccessibility == Accessibility.Public && !type.IsAbstract)
            {
                //// Template:
                //// void .ctor() : this(Context.Default) { }
                //yield return new SynthesizedParameterlessPhpCtorSymbol(type, Accessibility.Public, defaultctor);
                // TODO: uncomment once overload resolution will prioritize the overload with Context parameter over this one!
            }

            yield break;
        }

        static MethodSymbol ResolveBaseCtor(ImmutableArray<ParameterSymbol> givenparams, ImmutableArray<MethodSymbol> candidates)
        {
            MethodSymbol best = null;
            var bestcost = OverrideHelper.ConversionCost.MissingArgs; // the minimal errornous case

            // find best matching basector
            foreach (var c in candidates)
            {
                var calledparams = c.Parameters.Where(p => !p.IsImplicitlyDeclared && !p.IsParams).ToImmutableArray();

                if (!c.IsStatic &&
                    c.DeclaredAccessibility != Accessibility.Private &&
                    c.DeclaredAccessibility != Accessibility.Internal &&
                    c.DeclaredAccessibility != Accessibility.ProtectedAndInternal)
                {
                    var cost = OverrideHelper.OverrideCost(givenparams, calledparams);
                    if (cost < bestcost)
                    {
                        // we have found base constructor with most parameters we can call with given parameters
                        bestcost = cost;
                        best = c;
                    }
                }
            }

            //
            return best;
        }

        #region .ctor metadata

        public override bool HidesBaseMethodsByName => false;

        internal override bool HasSpecialName => true;

        public sealed override bool IsAbstract => false;

        public sealed override bool IsExtern => false;

        public sealed override bool IsOverride => false;

        public sealed override bool IsSealed => false;

        public sealed override bool IsVirtual => false;

        public sealed override MethodKind MethodKind => MethodKind.Constructor;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        BaseAttributeData _lazyPhpFieldsOnlyCtorAttribute;

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            var attrs = base.GetAttributes();

            // IsInitFieldsOnly => InitFieldsOnlyCtorAttribute
            if (IsInitFieldsOnly)
            {
                // [PhpFieldsOnlyCtorAttribute]
                if (_lazyPhpFieldsOnlyCtorAttribute == null)
                {
                    var lazyPhpFieldsOnlyCtorAttribute = new SynthesizedAttributeData(
                        DeclaringCompilation.CoreMethods.Ctors.PhpFieldsOnlyCtorAttribute,
                        ImmutableArray<TypedConstant>.Empty,
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                    Interlocked.CompareExchange(ref _lazyPhpFieldsOnlyCtorAttribute, lazyPhpFieldsOnlyCtorAttribute, null);
                }

                attrs = attrs.Add(_lazyPhpFieldsOnlyCtorAttribute);
            }

            return attrs;
        }
    }

    #endregion

    #region SynthesizedPhpTraitCtorSymbol

    internal class SynthesizedPhpTraitCtorSymbol : SynthesizedPhpCtorSymbol
    {
        public new SourceTraitTypeSymbol ContainingType => (SourceTraitTypeSymbol)base.ContainingType;

        public SynthesizedPhpTraitCtorSymbol(SourceTraitTypeSymbol containingType)
            : base(containingType, Accessibility.Public, false, containingType.BaseType.InstanceConstructors[0]/*Object..ctor()*/, null)
        {

        }

        /// <summary>.ctor parameter <c>object @this</c>.</summary>
        public ParameterSymbol ThisParameter => this.Parameters[1];

        protected override IEnumerable<ParameterSymbol> CreateParameters(IEnumerable<ParameterSymbol> baseparams)
        {
            Debug.Assert(baseparams.Count() == 0);

            int index = 0;

            // note: the signature is very similar to global code routine <Main>

            // Context <ctx>
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++);

            // !TSelf this
            yield return new SpecialParameterSymbol(this, ContainingType.TSelfParameter, SpecialParameterSymbol.ThisName, index++);
        }
    }

    #endregion

    #region SynthesizedParameterlessPhpCtorSymbol

    /// <summary>
    /// Context-less .ctor for a PHP class.
    /// </summary>
    internal sealed class SynthesizedParameterlessPhpCtorSymbol : SynthesizedPhpCtorSymbol
    {
        public SynthesizedParameterlessPhpCtorSymbol(
            SourceTypeSymbol containingType, Accessibility accessibility,
            MethodSymbol defaultctor)
            : base(containingType, accessibility, false, defaultctor, null)
        {
        }

        protected override IEnumerable<ParameterSymbol> CreateParameters(IEnumerable<ParameterSymbol> baseparams)
        {
            int index = 0;

            // same parameters as PHP constructor
            foreach (var p in baseparams)
            {
                if (!SpecialParameterSymbol.IsContextParameter(p))
                {
                    yield return new SynthesizedParameterSymbol(this, p.Type, index++, p.RefKind, p.Name, p.IsParams,
                        explicitDefaultConstantValue: p.ExplicitDefaultConstantValue);
                }
            }
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            // [CompilerGenerated]
            var compilergenerated = (MethodSymbol)DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor);

            return base.GetAttributes().Add(
                new SynthesizedAttributeData(compilergenerated, ImmutableArray<TypedConstant>.Empty, ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty)
            );
        }
    }

    #endregion
}
