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

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a PHP class method.
    /// </summary>
    internal sealed class SourceMethodSymbol : SourceBaseMethodSymbol
    {
        readonly SourceNamedTypeSymbol _type;
        readonly MethodDecl/*!*/_syntax;
        
        public SourceMethodSymbol(SourceNamedTypeSymbol/*!*/type, MethodDecl/*!*/syntax)
            :base(syntax.Signature)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(syntax);

            _type = type;
            _syntax = syntax;
        }

        protected override ControlFlowGraph CreateCFG()
            => new ControlFlowGraph(_syntax.Body);

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
