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
            _params = BuildParameters().ToImmutableArray();
        }

        protected override IEnumerable<ParameterSymbol> BuildParameters(Signature signature, PHPDocBlock phpdocOpt = null)
        {
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
        }

        IEnumerable<ParameterSymbol> BuildParameters()
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

        public override ParameterSymbol ThisParameter
        {
            get
            {
                var ps = this.Parameters;
                return ps.First(p => p.Type.SpecialType == SpecialType.System_Object && p.Name == SpecialParameterSymbol.ThisName);
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
                throw new NotImplementedException();
            }
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                return BuildReturnType(default(Signature), null, null, this.ResultTypeMask);
            }
        }

        internal override IList<Statement> Statements => _file.Syntax.Statements;

        internal override AstNode Syntax => _file.Syntax;

        internal override PHPDocBlock PHPDocBlock => null;

        internal override PhpCompilation DeclaringCompilation => _file.DeclaringCompilation;

        protected override TypeRefContext CreateTypeRefContext() => new TypeRefContext(_file.Syntax.ContainingSourceUnit, null);
    }
}
