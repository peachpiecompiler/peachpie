using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.Syntax.AST;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Global code as a static [Main] method.
    /// </summary>
    sealed class SourceGlobalMethodSymbol : SourceRoutineSymbol
    {
        public const string GlobalRoutineName = "<Main>";

        readonly SourceFileSymbol _file;

        public SourceGlobalMethodSymbol(SourceFileSymbol file)
            :base(new Signature(false, new FormalParam[0]))
        {
            Contract.ThrowIfNull(file);

            _file = file;
        }

        public override string Name => GlobalRoutineName;

        public override Symbol ContainingSymbol => _file;

        internal override SourceFileSymbol ContainingFile => _file;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override IList<Statement> Statements => _file.Syntax.Statements;

        internal override AstNode Syntax => _file.Syntax;

        internal override PhpCompilation DeclaringCompilation => _file.DeclaringCompilation;

        protected override TypeRefContext CreateTypeRefContext() => new TypeRefContext(new Syntax.NamingContext(null, 0), _file.Syntax.SourceUnit, null);
    }
}
