using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.Syntax.AST;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a global PHP function.
    /// </summary>
    internal sealed class SourceFunctionSymbol : SourceRoutineSymbol
    {
        readonly PhpCompilation _compilation;
        readonly FunctionDecl _syntax;

        public SourceFunctionSymbol(PhpCompilation compilation, FunctionDecl syntax)
            :base(syntax.Signature)
        {
            Contract.ThrowIfNull(compilation);
            
            _compilation = compilation;
            _syntax = syntax;
        }

        internal override AstNode Syntax => _syntax;

        internal override IList<Statement> Statements => _syntax.Body;

        protected override TypeRefContext CreateTypeRefContext()
            => new TypeRefContext(NameUtils.GetNamingContext(_syntax.Namespace, _syntax.SourceUnit.Ast), _syntax.SourceUnit, null);

        public override string Name => NameUtils.MakeQualifiedName(_syntax.Name, _syntax.Namespace).ClrName();

        public override Symbol ContainingSymbol => _compilation.SourceModule;

        internal override IModuleSymbol ContainingModule => _compilation.SourceModule;

        public override AssemblySymbol ContainingAssembly => _compilation.SourceAssembly;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => false;

        public override bool IsExtern => false;

        public override bool IsOverride => false;

        public override bool IsSealed => true;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
