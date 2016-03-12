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
        readonly SourceFileSymbol _file;
        readonly FunctionDecl _syntax;

        public SourceFunctionSymbol(SourceFileSymbol file, FunctionDecl syntax)
        {
            Contract.ThrowIfNull(file);

            _file = file;
            _syntax = syntax;
            _params = BuildParameters(syntax.Signature).AsImmutable();
        }

        internal override AstNode Syntax => _syntax;

        internal override IList<Statement> Statements => _syntax.Body;

        internal override SourceFileSymbol ContainingFile => _file;

        protected override TypeRefContext CreateTypeRefContext()
            => new TypeRefContext(NameUtils.GetNamingContext(_syntax.Namespace, _syntax.SourceUnit.Ast), _syntax.SourceUnit, null);

        public override string Name => NameUtils.MakeQualifiedName(_syntax.Name, _syntax.Namespace).ClrName();

        public override Symbol ContainingSymbol => _file.SourceModule;

        internal override IModuleSymbol ContainingModule => _file.SourceModule;

        public override AssemblySymbol ContainingAssembly => _file.DeclaringCompilation.SourceAssembly;

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
