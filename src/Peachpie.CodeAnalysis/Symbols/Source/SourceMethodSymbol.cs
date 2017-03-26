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

        internal override Signature SyntaxSignature => _syntax.Signature;

        internal override TypeRef SyntaxReturnType => _syntax.ReturnType;

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

        public override bool IsAbstract => !IsStatic && (_syntax.Modifiers.IsAbstract() || _type.IsInterface);

        public override bool IsOverride => this.OverriddenMethod != null && this.SignaturesMatch((MethodSymbol)this.OverriddenMethod);

        public override bool IsSealed => !IsStatic && _syntax.Modifiers.IsSealed();

        public override bool IsStatic => _syntax.Modifiers.IsStatic();

        public override bool IsVirtual => !IsStatic;    // every method in PHP is virtual except static methods

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(Location.Create(ContainingFile.SyntaxTree, _syntax.Span.ToTextSpan()));
            }
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => this.IsVirtual;

        internal override bool IsMetadataFinal => IsSealed && !IsStatic && base.IsMetadataFinal;
    }
}
