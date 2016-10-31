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

    #region SynthesizedPhpNewMethodSymbol // generated .phpnew method

    /// <summary>
    /// <c>.phpnew</c> method instantiating the method without calling its constructor.
    /// </summary>
    internal class SynthesizedPhpNewMethodSymbol : SynthesizedMethodSymbol
    {
        MethodSymbol _lazyBaseCtor;

        public SynthesizedPhpNewMethodSymbol(SourceTypeSymbol container)
            : base(container, WellKnownPchpNames.PhpNewMethodName, false, false, container.DeclaringCompilation.CoreTypes.Void, Accessibility.Public)
        {
            Debug.Assert(!container.IsStatic);
            _parameters = default(ImmutableArray<ParameterSymbol>); // lazy initialized
        }

        public override bool IsAbstract => false;
        public override bool IsVirtual => false;
        public override bool IsStatic => false;
        public override MethodKind MethodKind => MethodKind.Ordinary;
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => true;

        /// <summary>
        /// <c>__construct</c> in current class. Can be <c>null</c>.
        /// </summary>
        private MethodSymbol PhpCtor => this.ContainingType.ResolvePhpCtor();

        static bool CanBePassedTo(ParameterSymbol[] givenparams, ParameterSymbol[] calledparams)
        {
            if (calledparams.Length > givenparams.Length)
                return false;

            for (int i = 0; i < calledparams.Length; i++)
            {
                if (!givenparams[i].CanBePassedTo(calledparams[i]))
                {
                    return false;
                }
            }

            return true;
        }

        void ResolveBaseCtorAndParameters()
        {
            if (!_parameters.IsDefaultOrEmpty)
                return;

            //
            var phpctor = this.PhpCtor; // this tells us what parameters are provided to resolve base .ctor that can be called
            var basephpnew = (this.ContainingType.BaseType as IPhpTypeSymbol)?.InitializeInstanceMethod;   // base..phpnew() to be called if provided
            var basectors = (basephpnew != null)
                ? ImmutableArray.Create(basephpnew)
                : this.ContainingType.BaseType.InstanceConstructors
                .Where(c => c.DeclaredAccessibility != Accessibility.Private)
                .OrderByDescending(c => c.ParameterCount)   // longest ctors first
                .AsImmutable();

            // Context <ctx>
            var ps = new List<ParameterSymbol>(1)
            {
                new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, 0) // Context <ctx>
            };

            var givenparams = (phpctor != null)
                ? phpctor.Parameters.Where(p => !p.IsImplicitlyDeclared).ToArray()
                : EmptyArray<ParameterSymbol>.Instance;

            // find best matching basector
            foreach (var c in basectors)
            {
                var calledparams = c.Parameters.Where(p => !p.IsImplicitlyDeclared).ToArray();

                if (CanBePassedTo(givenparams, calledparams))
                {
                    // we have found base constructor with most parameters we can call with given parameters
                    _lazyBaseCtor = c;
                    break;
                }
            }

            if (_lazyBaseCtor == null)
            {
                throw new InvalidOperationException("Base .ctor cannot be resolved with provided constructor.");
            }

            //
            Debug.Assert(SpecialParameterSymbol.IsContextParameter(ps[0]));
            Debug.Assert(_lazyBaseCtor != null && !_lazyBaseCtor.IsStatic);

            foreach (var p in _lazyBaseCtor.Parameters)
            {
                if (SpecialParameterSymbol.IsContextParameter(p))
                    continue;

                ps.Add(new SynthesizedParameterSymbol(this, p.Type, ps.Count, p.RefKind, p.Name,
                    explicitDefaultConstantValue: p.ExplicitDefaultConstantValue));
            }

            //
            _parameters = ps.AsImmutable();
        }

        /// <summary>
        /// Base <c>.phpnew</c> or <c>.ctor</c> to be called by this <c>.phpnew</c> method.
        /// </summary>
        internal MethodSymbol BasePhpNew
        {
            get
            {
                if (_lazyBaseCtor == null)
                {
                    ResolveBaseCtorAndParameters();
                }

                Debug.Assert(_lazyBaseCtor != null);
                return _lazyBaseCtor;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_parameters.IsDefault)
                {
                    ResolveBaseCtorAndParameters();
                }

                return _parameters;
            }
        }
    }

    #endregion

    /// <summary>
    /// CLR .ctor symbol calling .phpnew and PHP constructor.
    /// </summary>
    internal class SynthesizedPhpCtorSymbol : SynthesizedCtorSymbol
    {
        MethodSymbol _lazyPhpCtor;

        public SynthesizedPhpCtorSymbol(SourceTypeSymbol/*!*/container)
            : base(container)
        {
            _parameters = default(ImmutableArray<ParameterSymbol>); // lazy initialized
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                var phpctor = PhpCtor;

                return phpctor != null
                    ? phpctor.DeclaredAccessibility
                    : Accessibility.Public;
            }
        }

        /// <summary>
        /// PHP constructor method in this class.
        /// Can be <c>null</c>.
        /// </summary>
        internal MethodSymbol PhpCtor
        {
            get
            {
                if (_lazyPhpCtor == null)
                {
                    _lazyPhpCtor = this.ContainingType.ResolvePhpCtor(true);

                    if (_lazyPhpCtor != null && _lazyPhpCtor.IsStatic)
                    {
                        // TODO: Err
                    }
                }

                //
                return _lazyPhpCtor;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_parameters.IsDefault)
                {
                    // Context <ctx>
                    var ps = new List<ParameterSymbol>(1)
                    {
                        new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, 0) // Context <ctx>
                    };

                    if (this.PhpCtor != null)
                    {
                        // same parameters as PHP constructor
                        foreach (var p in this.PhpCtor.Parameters)
                        {
                            if (SpecialParameterSymbol.IsContextParameter(p))
                                continue;

                            ps.Add(new SynthesizedParameterSymbol(this, p.Type, ps.Count, p.RefKind, p.Name,
                                explicitDefaultConstantValue: p.ExplicitDefaultConstantValue));
                        }
                    }

                    _parameters = ps.AsImmutable();
                }

                return _parameters;
            }
        }
    }
}
