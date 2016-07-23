using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using Pchp.Syntax;

namespace Pchp.CodeAnalysis.Symbols
{
    internal class SynthesizedCtorSymbol : SynthesizedMethodSymbol
    {
        public SynthesizedCtorSymbol(NamedTypeSymbol/*!*/container)
            :base(container, WellKnownMemberNames.InstanceConstructorName, false, false, container.DeclaringCompilation.CoreTypes.Void)
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

    /// <summary>
    /// CLR .ctor symbol wrapping a PHP constructor.
    /// </summary>
    internal class SynthesizedCtorWrapperSymbol : SynthesizedCtorSymbol
    {
        MethodSymbol _lazyPhpCtor;
        MethodSymbol _lazyBaseCtor;

        public SynthesizedCtorWrapperSymbol(SourceNamedTypeSymbol/*!*/container)
            :base(container)
        {

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
                    _lazyPhpCtor = this.ContainingType.ResolvePhpCtor();

                    if (_lazyPhpCtor != null && _lazyPhpCtor.IsStatic)
                    {
                        // TODO: Err
                    }
                }

                //
                return _lazyPhpCtor;
            }
        }

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
            var phpctor = this.PhpCtor;
            var basectors = this.ContainingType.BaseType.InstanceConstructors
                .Where(c => c.DeclaredAccessibility != Accessibility.Private)
                .OrderBy(c => c.ParameterCount)
                .AsImmutable();

            MethodSymbol ctortemplate = null;

            //
            var ps = new List<ParameterSymbol>(1)
            {
                new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, 0) // Context <ctx>
            };

            if (phpctor != null && !phpctor.IsStatic)
            {
                ctortemplate = phpctor;

                var givenparams = phpctor.Parameters.Where(p => !p.IsImplicitlyDeclared).ToArray();

                // find best matching basector
                foreach (var c in basectors.Reverse())
                {
                    var calledparams = c.Parameters.Where(p => !p.IsImplicitlyDeclared).ToArray();

                    if (CanBePassedTo(givenparams, calledparams))
                    {
                        // we have found base constructor with most parameters we can call with given parameters
                        _lazyBaseCtor = c;
                        break;
                    }
                }

                if (_lazyBaseCtor == null && basectors.Length != 0)
                {
                    // TODO: Err: cannot call base ctor
                    throw new InvalidOperationException();
                }
            }
            else
            {
                // resolve best fitting base constructor,
                // warning in case of ambiguity

                _lazyBaseCtor = ctortemplate = basectors.FirstOrDefault();
                
                if (basectors.Length > 1)
                {
                    // TODO: Warning
                }
            }

            //
            Debug.Assert(SpecialParameterSymbol.IsContextParameter(ps[0]));
            Debug.Assert(_lazyBaseCtor == null || !_lazyBaseCtor.IsStatic);

            if (ctortemplate != null)
            {
                foreach (var p in ctortemplate.Parameters)
                {
                    if (SpecialParameterSymbol.IsContextParameter(p))
                        continue;

                    ps.Add(new SynthesizedParameterSymbol(this, p.Type, ps.Count, p.RefKind, p.Name,
                        explicitDefaultConstantValue: p.ExplicitDefaultConstantValue));
                }
            }

            //
            _parameters = ps.AsImmutable();
        }

        /// <summary>
        /// Gets base .ctor matching PHP ctor parameters or gets .ctor without parameters.
        /// </summary>
        internal MethodSymbol BaseCtor
        {
            get
            {
                if (_lazyBaseCtor == null)
                {
                    ResolveBaseCtorAndParameters();
                }

                return _lazyBaseCtor;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_parameters.IsDefaultOrEmpty)
                {
                    ResolveBaseCtorAndParameters();
                }

                return _parameters;
            }
        }
    }
}
