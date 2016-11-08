using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.FlowAnalysis;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Syntax;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a PHP class method.
    /// </summary>
    internal sealed partial class SourceMethodSymbol : SourceRoutineSymbol
    {
        readonly SourceTypeSymbol _type;
        readonly MethodDecl/*!*/_syntax;

        ParameterSymbol _lazyThisSymbol;
        MethodSymbol _lazyOverridenMethod;

        public SourceMethodSymbol(SourceTypeSymbol/*!*/type, MethodDecl/*!*/syntax)
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
                    _lazyOverridenMethod = this.ResolveOverride();
                }

                return _lazyOverridenMethod;
            }
        }

        internal override AstNode Syntax => _syntax;

        internal override PHPDocBlock PHPDocBlock => _syntax.PHPDoc;

        internal override IList<Statement> Statements => _syntax.Body?.Statements;

        protected override TypeRefContext CreateTypeRefContext() => TypeRefFactory.CreateTypeRefContext(_type);

        internal override SourceFileSymbol ContainingFile => _type.ContainingFile;

        public override string Name => _syntax.Name.Name.Value;

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

        public override bool IsOverride => this.OverriddenMethod != null && this.SignaturesMatch((MethodSymbol)this.OverriddenMethod);

        public override bool IsSealed => _syntax.Modifiers.IsSealed();

        public override bool IsStatic => _syntax.Modifiers.IsStatic();

        public override bool IsVirtual => !IsSealed && !_type.IsSealed && !IsStatic;

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
                // we can use analysed return type in case the method cannot be an override
                var propagateResultType = this.IsStatic || this.DeclaredAccessibility == Accessibility.Private || (this.IsSealed && !this.IsOverride);

                // TODO: in case of override, use return type of overriden method // in some cases

                //
                var t = BuildReturnType(_syntax.Signature, _syntax.ReturnType, _syntax.PHPDoc,
                    propagateResultType ? this.ResultTypeMask : TypeRefMask.AnyType);

                return t;
            }
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => !IsSealed && !IsStatic;

        internal override bool IsMetadataFinal => base.IsMetadataFinal && !IsStatic;
    }
}
