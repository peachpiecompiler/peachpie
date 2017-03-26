using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using System.Diagnostics;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Global code as a static [Main] method.
    /// </summary>
    sealed partial class SourceGlobalMethodSymbol : SourceRoutineSymbol
    {
        readonly SourceFileSymbol _file;

        public SourceGlobalMethodSymbol(SourceFileSymbol file)
        {
            Contract.ThrowIfNull(file);

            _file = file;
        }

        internal override Signature SyntaxSignature => new Signature(false, Array.Empty<FormalParam>());

        internal override TypeRef SyntaxReturnType => null;

        protected override IEnumerable<ParameterSymbol> BuildImplicitParams()
        {
            int index = 0;

            // Context <ctx>
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++);

            // PhpArray <locals>
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.PhpArray, SpecialParameterSymbol.LocalsName, index++);

            // object this
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Object, SpecialParameterSymbol.ThisName, index++);

            // TODO: RuntimeTypeHandle <TypeContext>
        }

        protected override IEnumerable<SourceParameterSymbol> BuildSrcParams(Signature signature, PHPDocBlock phpdocOpt = null)
        {
            return Array.Empty<SourceParameterSymbol>();
        }

        public override ParameterSymbol ThisParameter
        {
            get
            {
                var ps = this.Parameters;
                return ps.First(p =>
                {
                    Debug.Assert(p.Type != null);
                    return (p.Name == SpecialParameterSymbol.ThisName && p.Type.SpecialType == SpecialType.System_Object);
                });
            }
        }

        public override string Name => WellKnownPchpNames.GlobalRoutineName;

        public override string RoutineName => string.Empty;

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

        public override bool IsSealed => false;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(Location.Create(ContainingFile.SyntaxTree, default(Microsoft.CodeAnalysis.Text.TextSpan)));
            }
        }

        internal override IList<Statement> Statements => _file.SyntaxTree.Source.Ast.Statements;

        internal override AstNode Syntax => _file.SyntaxTree.Source.Ast;

        internal override PHPDocBlock PHPDocBlock => null;

        internal override PhpCompilation DeclaringCompilation => _file.DeclaringCompilation;

        protected override TypeRefContext CreateTypeRefContext() => new TypeRefContext(_file.SyntaxTree.Source, null);
    }
}
