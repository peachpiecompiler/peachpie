using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Syntax;
using System.Collections.Immutable;
using Devsense.PHP.Text;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed partial class SourceLambdaSymbol : SourceRoutineSymbol
    {
        readonly NamedTypeSymbol _container;
        readonly LambdaFunctionExpr _syntax;
        readonly bool _useThis;

        /// <summary>
        /// Whether <c>$this</c> is pased to the routine (non static lambda).
        /// </summary>
        internal bool UseThis => _useThis;

        FieldSymbol _lazyRoutineInfoField;    // internal static RoutineInfo !name;

        public SourceLambdaSymbol(LambdaFunctionExpr syntax, NamedTypeSymbol containing, bool useThis)
        {
            _container = containing;
            _syntax = syntax;
            _useThis = useThis;
        }

        /// <summary>
        /// A field representing the function info at runtime.
        /// Lazily associated with index by runtime.
        /// </summary>
        internal FieldSymbol EnsureRoutineInfoField(Emit.PEModuleBuilder module)
        {
            if (_lazyRoutineInfoField == null)
            {
                _lazyRoutineInfoField = module.SynthesizedManager
                    .GetOrCreateSynthesizedField(_container, this.DeclaringCompilation.CoreTypes.RoutineInfo, $"[routine]{this.MetadataName}", Accessibility.Private, true, true, true);
            }

            return _lazyRoutineInfoField;
        }

        protected override IEnumerable<ParameterSymbol> BuildImplicitParams()
        {
            var index = 0;

            // Context ctx
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++);

            // System.Object @this
            if (_useThis)
            {
                yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Object, SpecialParameterSymbol.ThisName, index++);
            }
        }

        protected override IEnumerable<SourceParameterSymbol> BuildSrcParams(Signature signature, PHPDocBlock phpdocOpt = null)
        {
            // [use params], [formal params]
            return base.BuildSrcParams(_syntax.UseParams.Concat(signature.FormalParams), phpdocOpt);
        }

        internal override IList<Statement> Statements => _syntax.Body.Statements;

        public override ParameterSymbol ThisParameter => null;

        internal override Signature SyntaxSignature => _syntax.Signature;

        internal override TypeRef SyntaxReturnType => _syntax.ReturnType;

        internal override AstNode Syntax => _syntax;

        internal override PHPDocBlock PHPDocBlock => _syntax.PHPDoc;

        internal override SourceFileSymbol ContainingFile => (_container as SourceTypeSymbol)?.ContainingFile ?? (_container as SourceFileSymbol);

        public override string Name => "anonymous@function";

        public override Symbol ContainingSymbol => _container;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(Location.Create(ContainingFile.SyntaxTree, _syntax.Span.ToTextSpan()));
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Accessibility DeclaredAccessibility => Accessibility.Private;

        public override bool IsStatic => true;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        protected override TypeRefContext CreateTypeRefContext() => new TypeRefContext(_syntax.ContainingSourceUnit, _container as SourceTypeSymbol);
    }
}
