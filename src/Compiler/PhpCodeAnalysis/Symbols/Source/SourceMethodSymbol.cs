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
        
        public SourceMethodSymbol(SourceNamedTypeSymbol/*!*/type, MethodDecl/*!*/syntax)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(syntax);

            _type = type;
            _syntax = syntax;
            _params = BuildParameters(syntax.Signature).AsImmutable();
        }

        public override ParameterSymbol ThisParameter
        {
            get
            {
                if (_lazyThisSymbol == null)
                    _lazyThisSymbol = new SpecialParameterSymbol(this, _type, SpecialParameterSymbol.ThisName, -1);

                return _lazyThisSymbol;
            }
        }

        internal override AstNode Syntax => _syntax;

        internal override PHPDocBlock PHPDocBlock => _syntax.PHPDoc;

        internal override IList<Statement> Statements => _syntax.Body;

        protected override TypeRefContext CreateTypeRefContext() => CreateTypeRefContext(_type.Syntax);

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

        public override bool IsOverride => false;

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

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => !IsSealed;
    }
}
