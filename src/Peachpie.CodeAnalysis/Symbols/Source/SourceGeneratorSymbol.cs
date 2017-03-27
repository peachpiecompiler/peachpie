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
        readonly bool _useThis;

        ParameterSymbol _lazyThisSymbol;
        FieldSymbol _lazyRoutineInfoField;    // internal static RoutineInfo !name;

        internal bool UseThis => _useThis;

        public SourceGeneratorSymbol(FunctionDecl syntax, NamedTypeSymbol containing, bool useThis)
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
                // The last two parameters? @readonly and autoincrement?

                _lazyRoutineInfoField = module.SynthesizedManager
                    .GetOrCreateSynthesizedField(_container, this.DeclaringCompilation.CoreTypes.RoutineInfo, $"[routine]{this.MetadataName}", Accessibility.Private, true, true, true);
            }

            return _lazyRoutineInfoField;
        }

        protected override IEnumerable<ParameterSymbol> BuildImplicitParams()
        {
            int index = 0;
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Object, "generator", index++);

            if (_useThis)
            {
                //Isn't the type being mere object going to be a problem?

                yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Object, SpecialParameterSymbol.ThisName, index++);
            }
        }

        protected override IEnumerable<SourceParameterSymbol> BuildSrcParams(Signature signature, PHPDocBlock phpdocOpt = null)
        {
            yield break;
        }

        internal override IList<Statement> Statements => _syntax.Body.Statements;

        public override ParameterSymbol ThisParameter => null;

        internal override Signature SyntaxSignature
        {
            get
            {
                return new Signature(false, new List<FormalParam>());

                // Don't have AST corresponding to signature because the signature is synthesized.
                // Maybe choose different parent object (SynthesizedMethodSymbol)?
                // + Doesn't have these problems
                // - The method isn't completely synthesized, only it's signature 
                //   the inside is merely rewritten.

                throw new NotImplementedException("IMPLEMENT");
            }
        }


        internal override TypeRef SyntaxReturnType
        {
            // See Signature SyntaxSignature

            get
            {
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
            // See Signature SyntaxSignature
            // Might not be applicable as I rewrite the inside of the method
            get
            {
                throw new NotImplementedException("IMPLEMENT");
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
