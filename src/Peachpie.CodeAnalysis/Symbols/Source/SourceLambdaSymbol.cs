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
    internal sealed class SourceLambdaSymbol : SourceRoutineSymbol
    {
        readonly SourceRoutineSymbol _routine;
        readonly LambdaFunctionExpr _syntax;

        FieldSymbol _lazyRoutineInfoField;    // internal static RoutineInfo !name;

        public SourceLambdaSymbol(LambdaFunctionExpr syntax, SourceRoutineSymbol containingroutine)
        {
            _routine = containingroutine;
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
                    .GetOrCreateSynthesizedField(_routine.ContainingType, this.DeclaringCompilation.CoreTypes.RoutineInfo, $"[routine]{this.MetadataName}", Accessibility.Private, true, true, true);
            }

            return _lazyRoutineInfoField;
        }

        protected override IEnumerable<ParameterSymbol> BuildParameters(Signature signature, PHPDocBlock phpdocOpt = null)
        {
            int index = 0;

            // Context ctx
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++);

            // System.Object @this
            yield return new SourceParameterSymbol(this,
                new FormalParam(
                    Span.Invalid,
                    SpecialParameterSymbol.ThisName,
                    Span.Invalid,
                    new Devsense.PHP.Syntax.Ast.ClassTypeRef(Span.Invalid, NameUtils.SpecialNames.System_Object),
                    FormalParam.Flags.Default,
                    null,
                    new List<CustomAttribute>()),
                index++, null);

            // @static + parameters
            int pindex = 0;

            foreach (var p in _syntax.UseParams.Concat(signature.FormalParams))
            {
                var ptag = (phpdocOpt != null) ? PHPDoc.GetParamTag(phpdocOpt, pindex - _syntax.UseParams.Count, p.Name.Name.Value) : null;

                yield return new SourceParameterSymbol(this, p, index++, ptag);

                pindex++;
            }
        }

        internal override IList<Statement> Statements => _syntax.Body.Statements;

        public override ParameterSymbol ThisParameter => null;

        internal override Signature SyntaxSignature => _syntax.Signature;

        internal override AstNode Syntax => _syntax;

        internal override PHPDocBlock PHPDocBlock => _syntax.PHPDoc;

        internal override SourceFileSymbol ContainingFile => _routine.ContainingFile;

        public override string Name => "anonymous@function";

        public override TypeSymbol ReturnType
        {
            get
            {
                return BuildReturnType(_syntax.Signature, _syntax.ReturnType, _syntax.PHPDoc, this.ResultTypeMask);
            }
        }

        public override Symbol ContainingSymbol => _routine;

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

        public override bool IsStatic => true;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        protected override TypeRefContext CreateTypeRefContext() => new TypeRefContext(_syntax.ContainingSourceUnit, _routine.ContainingType as SourceTypeSymbol);
    }
}
