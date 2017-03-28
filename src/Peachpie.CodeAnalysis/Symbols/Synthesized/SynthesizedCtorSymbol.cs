using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using Devsense.PHP.Syntax;

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

        private SynthesizedPhpCtorSymbol(SourceTypeSymbol containingType, Accessibility accessibility,
            bool isInitFieldsOnly,
            MethodSymbol basector, MethodSymbol __construct, int paramsLimit = int.MaxValue)
            : base(containingType, WellKnownMemberNames.InstanceConstructorName, false, false, containingType.DeclaringCompilation.CoreTypes.Void, accessibility)
        {
            if (basector == null) throw new ArgumentNullException(nameof(basector));

            _basector = basector;
            _phpconstruct = __construct;

            this.IsInitFieldsOnly = isInitFieldsOnly;

            // clone parameters from __construct ?? basector
            var template = (__construct ?? basector).Parameters;
            var ps = new List<ParameterSymbol>(template.Length)
            {
                new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, 0) // Context <ctx>
            };

            // same parameters as PHP constructor
            for (int i = 0; i < template.Length && i < paramsLimit; i++)
            {
                var p = template[i];
                if (!SpecialParameterSymbol.IsContextParameter(p))
                {
                    ps.Add(new SynthesizedParameterSymbol(this, p.Type, ps.Count, p.RefKind, p.Name, p.IsParams,
                        explicitDefaultConstantValue: p.ExplicitDefaultConstantValue));
                }
            }
            
            _parameters = ps.ToImmutableArray();
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
                    .OrderByDescending(c => c.ParameterCount)   // longest ctors first
                    .AsImmutable();

            // what parameters are provided
            var givenparams = (phpconstruct != null) ? phpconstruct.Parameters.Where(p => !p.IsImplicitlyDeclared).ToArray() : Array.Empty<ParameterSymbol>();

            // first declare .ctor that initializes fields only and calls base .ctor
            var basector = ResolveBaseCtor(givenparams, basectors);
            if (basector == null)
            {
                // type.BaseType was not resolved, reported by type.BaseType
                // TODO: Err & ErrorMethodSymbol
                yield break;
            }

            // create .ctor(s)
            if (phpconstruct == null)
            {
                yield return new SynthesizedPhpCtorSymbol(type, Accessibility.Public, false, basector, null);
            }
            else
            {
                var fieldsinitctor = new SynthesizedPhpCtorSymbol(type, Accessibility.Protected, true, basector, null);
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

                yield return new SynthesizedPhpCtorSymbol(type, phpconstruct.DeclaredAccessibility, false, fieldsinitctor, phpconstruct);
            }

            yield break;
        }

        static bool CanBePassedTo(ParameterSymbol[] givenparams, ParameterSymbol[] calledparams)
        {
            for (int i = 0; i < calledparams.Length; i++)
            {
                if (i >= givenparams.Length)
                {
                    if (!calledparams[i].IsOptional)
                    {
                        return false;
                    }
                }
                else if (!givenparams[i].CanBePassedTo(calledparams[i]))
                {
                    return false;
                }
            }

            return true;
        }

        static MethodSymbol ResolveBaseCtor(ParameterSymbol[] givenparams, ImmutableArray<MethodSymbol> candidates)
        {
            // find best matching basector
            foreach (var c in candidates)
            {
                var calledparams = c.Parameters.Where(p => !p.IsImplicitlyDeclared).ToArray();

                if (CanBePassedTo(givenparams, calledparams) && !c.IsStatic && c.DeclaredAccessibility != Accessibility.Private)
                {
                    // we have found base constructor with most parameters we can call with given parameters
                    return c;
                }
            }

            return null;
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
                    _lazyPhpFieldsOnlyCtorAttribute = new SynthesizedAttributeData(
                        DeclaringCompilation.CoreMethods.Ctors.PhpFieldsOnlyCtorAttribute,
                        ImmutableArray<TypedConstant>.Empty,
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                }

                attrs = attrs.Add(_lazyPhpFieldsOnlyCtorAttribute);
            }

            return attrs;
        }
    }

    #endregion
}
