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
using Devsense.PHP.Text;

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

        internal override Signature SyntaxSignature => new Signature(false, Array.Empty<FormalParam>(), Span.Invalid);

        internal override TypeRef SyntaxReturnType => null;

        public override bool IsGlobalScope => true;

        internal override bool RequiresLateStaticBoundParam => false;   // not supported in global code

        protected override IEnumerable<ParameterSymbol> BuildImplicitParams()
        {
            int index = 0;

            // Context <ctx>
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++);

            // PhpArray <locals>
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.PhpArray, SpecialParameterSymbol.LocalsName, index++);

            // object @this
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Object, SpecialParameterSymbol.ThisName, index++);

            // RuntimeTypeHandle <self>
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.RuntimeTypeHandle, SpecialParameterSymbol.SelfName, index++);
        }

        protected override IEnumerable<SourceParameterSymbol> BuildSrcParams(Signature signature, PHPDocBlock phpdocOpt = null)
        {
            return Array.Empty<SourceParameterSymbol>();
        }

        public ParameterSymbol ThisParameter => this.ImplicitParameters.First(p => p.Name == SpecialParameterSymbol.ThisName);

        public ParameterSymbol SelfParameter => this.ImplicitParameters.First(SpecialParameterSymbol.IsSelfParameter);

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

        internal override IList<Statement> Statements => _file.SyntaxTree.Root.Statements;

        internal override AstNode Syntax => _file.SyntaxTree.Root;

        internal override PHPDocBlock PHPDocBlock => null;

        internal override PhpCompilation DeclaringCompilation => _file.DeclaringCompilation;

        protected override TypeRefContext CreateTypeRefContext() => new TypeRefContext(DeclaringCompilation, null);
    }
}
