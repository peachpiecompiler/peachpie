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
    internal sealed partial class SourceGeneratorSymbol : SourceRoutineSymbol
    {
        readonly NamedTypeSymbol _container;
        readonly FunctionDecl _syntax;

        ParameterSymbol _lazyThisSymbol;
        FieldSymbol _lazyRoutineInfoField;    // internal static RoutineInfo !name;

        public SourceGeneratorSymbol(FunctionDecl syntax, NamedTypeSymbol containing)
        {
            _container = containing;
            _syntax = syntax;
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
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Object, "generator", 0);
        }

        protected override IEnumerable<SourceParameterSymbol> BuildSrcParams(Signature signature, PHPDocBlock phpdocOpt = null)
        {
            yield break;
        }

        internal override IList<Statement> Statements => _syntax.Body.Statements;

        public override ParameterSymbol ThisParameter
        {
            get
            {
                if (_lazyThisSymbol == null && this.HasThis)
                    _lazyThisSymbol = new SpecialParameterSymbol(this, _container, SpecialParameterSymbol.ThisName, -1);

                return _lazyThisSymbol;
            }
        }

        internal override Signature SyntaxSignature
        {
            get
            {
                return new Signature(false, new List<FormalParam>());

                throw new NotImplementedException("IMPLEMENT");
            }
        }

        internal override AstNode Syntax => _syntax;

        internal override PHPDocBlock PHPDocBlock => null;

        internal override SourceFileSymbol ContainingFile => (_container as SourceTypeSymbol)?.ContainingFile ?? (_container as SourceFileSymbol);

        public override string Name => "generator@function";

        public override TypeSymbol ReturnType => DeclaringCompilation.CoreTypes.Void;

        public override Symbol ContainingSymbol => _container;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
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

        public override bool IsStatic => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        protected override TypeRefContext CreateTypeRefContext() => new TypeRefContext(_syntax.ContainingSourceUnit, _container as SourceTypeSymbol);

        internal override TypeRef SyntaxReturnType
        {
            get
            {
                throw new NotImplementedException("IMPLEMENT");
            }
        }
    }
}
