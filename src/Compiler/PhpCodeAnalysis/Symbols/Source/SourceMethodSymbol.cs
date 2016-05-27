using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.Syntax.AST;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.Syntax;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a PHP class method.
    /// </summary>
    internal sealed partial class SourceMethodSymbol : SourceRoutineSymbol
    {
        readonly SourceNamedTypeSymbol _type;
        readonly MethodDecl/*!*/_syntax;

        ParameterSymbol _lazyThisSymbol;
        MethodSymbol _lazyOverridenMethod;

        public SourceMethodSymbol(SourceNamedTypeSymbol/*!*/type, MethodDecl/*!*/syntax)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(syntax);

            _type = type;
            _syntax = syntax;

            // TODO: lazily; when using late static binding in a static method, add special <static> parameter, where runtime passes late static bound type
            _params = BuildParameters(syntax.Signature, syntax.PHPDoc).AsImmutable();
        }

        public override ParameterSymbol ThisParameter
        {
            get
            {
                if (_lazyThisSymbol == null && this.HasThis)
                    _lazyThisSymbol = new SpecialParameterSymbol(this, _type, SpecialParameterSymbol.ThisName, -1);

                return _lazyThisSymbol;
            }
        }

        public override IMethodSymbol OverriddenMethod
        {
            get
            {
                if (_lazyOverridenMethod == null)
                {
                    _lazyOverridenMethod = ResolveOverridenMethod();
                }

                return _lazyOverridenMethod;
            }
        }

        MethodSymbol ResolveOverridenMethod()
        {
            if (this.IsStatic || this.DeclaredAccessibility == Accessibility.Private)
                return null;    // static or private methods can't be overrides

            // TODO: if overriden method's signature does not match, we have to synthesize ghost stub

            // 
            //var overriden = new HashSet<MethodSymbol>();
            var type = ContainingType.BaseType;
            while (type != null)
            {
                var candidate = type.GetMembers(this.Name).OfType<MethodSymbol>().FirstOrDefault(Overrides);
                if (candidate != null)
                    return candidate;

                type = type.BaseType;
            }

            // interfaces
            var ifaces = this.ContainingType.AllInterfaces;
            foreach (var t in ifaces)
            {
                // TODO: override interface member
            }

            //
            return null;
        }

        bool Overrides(MethodSymbol basem)
        {
            if (basem != null && base.IsVirtual && !basem.IsSealed && !basem.IsStatic && basem.DeclaredAccessibility != Accessibility.Private)
            {
                if (this.Name.EqualsOrdinalIgnoreCase(basem.Name))
                {
                    // TODO: signature does not have to match exactly in PHP, parameters can be added, type can be converted,
                    // see ghost stubs

                    var p1 = this.ParametersType();
                    var p2 = basem.ParametersType();
                    if (p1.Length == p2.Length)
                    {
                        for (int i = 0; i < p1.Length; i++)
                        {
                            if (p1[i] != p2[i])
                                return false;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        internal override AstNode Syntax => _syntax;

        internal override PHPDocBlock PHPDocBlock => _syntax.PHPDoc;

        internal override IList<Statement> Statements => _syntax.Body;

        protected override TypeRefContext CreateTypeRefContext() => TypeRefFactory.CreateTypeRefContext(_type);

        internal override SourceFileSymbol ContainingFile => _type.ContainingFile;

        public override string Name => _syntax.Name.Value;

        public override Symbol ContainingSymbol => _type;

        public override Accessibility DeclaredAccessibility => _syntax.Modifiers.GetAccessibility();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => _syntax.Modifiers.IsAbstract();

        public override bool IsOverride => this.OverriddenMethod != null;

        public override bool IsSealed => _syntax.Modifiers.IsSealed();

        public override bool IsStatic => _syntax.Modifiers.IsStatic();

        public override bool IsVirtual => !IsSealed && !_type.IsSealed && !IsStatic
            && Pchp.Syntax.Name.SpecialMethodNames.Construct != _syntax.Name;   // __construct is not overridable

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                var tmask = TypeRefMask.AnyType;
                if (this.IsStatic || this.DeclaredAccessibility == Accessibility.Private)
                {
                    // allow flow analysed type to be used as method return type
                    tmask = this.ControlFlowGraph.ReturnTypeMask;
                }

                return BuildReturnType(_syntax.Signature, _syntax.PHPDoc, tmask);
            }
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => !IsSealed && !IsStatic;

        internal override bool IsMetadataFinal => base.IsMetadataFinal && !IsStatic;
    }
}
