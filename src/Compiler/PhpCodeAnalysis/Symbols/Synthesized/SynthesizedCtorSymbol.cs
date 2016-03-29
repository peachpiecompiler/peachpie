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
    internal class SynthesizedCtorSymbol : MethodSymbol
    {
        protected readonly SourceNamedTypeSymbol _type;

        ImmutableArray<ParameterSymbol> _lazyParameters;

        MethodSymbol _lazyRealCtorMethod;

        public SynthesizedCtorSymbol(SourceNamedTypeSymbol/*!*/type)
        {
            Contract.ThrowIfNull(type);
            Debug.Assert(!type.IsStatic);
            _type = type;
        }

        public sealed override Symbol ContainingSymbol => _type;

        public sealed override INamedTypeSymbol ContainingType => _type;

        public override bool HidesBaseMethodsByName => false;

        internal override bool HasSpecialName => true;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public sealed override bool IsAbstract => false;

        public sealed override bool IsExtern => false;

        public sealed override bool IsOverride => false;

        public sealed override bool IsSealed => false;

        public override bool IsStatic => false;

        public sealed override bool IsVirtual => false;

        public sealed override MethodKind MethodKind => MethodKind.Constructor;

        public sealed override string Name => IsStatic ? WellKnownMemberNames.StaticConstructorName : WellKnownMemberNames.InstanceConstructorName;

        public sealed override bool ReturnsVoid => true;

        public sealed override TypeSymbol ReturnType => _type.DeclaringCompilation.CoreTypes.Void;

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

        /// <summary>
        /// Real constructor method or base .ctor to be called by this CLR .ctor.
        /// </summary>
        internal MethodSymbol RealCtorMethod
        {
            get
            {
                if (_lazyRealCtorMethod == null)
                    _lazyRealCtorMethod = ResolveRealCtorSymbol();

                //
                return _lazyRealCtorMethod;
            }
        }

        private MethodSymbol ResolveRealCtorSymbol()
        {
            var ctor = _type.GetMembers(Syntax.Name.SpecialMethodNames.Construct.Value).OfType<MethodSymbol>().FirstOrDefault();
            if (ctor != null)
            {
                if (ctor.IsStatic) { }  // TODO: ErrorCode
                return ctor;
            }
            
            // lookup base .ctor
            var btype = _type.BaseType;
            if (btype != null)
            {
                var ctors = btype.InstanceConstructors;
                if (ctors.Length == 1)
                    return ctors[0];

                var paramless = ctors.FirstOrDefault(m => m.ParameterCount == 0);
                if (paramless != null)
                    return paramless;

                throw new NotImplementedException("__construct with specific call to parent .ctor should be implemented");  // TODO: ErrorCode: missing __construct method
            }

            //
            Debug.Assert(false);
            return null;
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    var ctor = this.RealCtorMethod;
                    var ps = new List<ParameterSymbol>(1);

                    // Context <ctx>
                    ps.Add(new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, ps.Count));

                    if (ctor != null)
                    {
                        foreach (var p in ctor.Parameters)
                        {
                            if (p.IsImplicitlyDeclared) continue;   // Context <ctx>
                            ps.Add(new SpecialParameterSymbol(this, p.Type, p.Name, ps.Count));
                        }
                    }

                    //
                    _lazyParameters = ps.AsImmutable();
                }

                return _lazyParameters;
            }
        }
    }
}
