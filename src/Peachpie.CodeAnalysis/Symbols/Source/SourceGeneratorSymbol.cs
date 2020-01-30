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
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Synthesized method representing the generator state machine's next function.
    /// Signature: <code>static &lt;&gt;sm_(Context &lt;ctx&gt;, T @this, PhpArray &lt;locals&gt;, PhpArray &lt;tmpLocals&gt;, Generator generator)</code>
    /// </summary>
    internal sealed partial class SourceGeneratorSymbol : SynthesizedMethodSymbol
    {
        public SourceGeneratorSymbol(SourceRoutineSymbol originalRoutine)
            : base(originalRoutine.ContainingType, string.Format(WellKnownPchpNames.GeneratorStateMachineNameFormatString, originalRoutine.RoutineName),
                  isstatic: true, isvirtual: false,
                  returnType: originalRoutine.DeclaringCompilation.CoreTypes.Void, ps: null)
        {
            Debug.Assert(originalRoutine.IsGeneratorMethod());

            // Need to postpone settings the params because I can't access 'this' in base constructor call
            base.SetParameters(CreateParameters(originalRoutine));
        }

        /// <summary>
        /// Parameters for <see cref="SourceGeneratorSymbol"/> method are defined by <c>GeneratorStateMachineDelegate</c>.
        /// </summary>
        ParameterSymbol[] CreateParameters(SourceRoutineSymbol originalRoutine)
        {
            // resolve type of $this
            var thisType = originalRoutine.GetPhpThisVariablePlaceWithoutGenerator()?.Type ?? (TypeSymbol)originalRoutine.DeclaringCompilation.ObjectType;

            // yield sm method signature
            var index = 0;
            return new[]
            {
                ContextParameter = new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++),
                ThisParameter = new SpecialParameterSymbol(this, thisType, SpecialParameterSymbol.ThisName, index++),
                LocalsParameter = new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.PhpArray, SpecialParameterSymbol.LocalsName, index++),
                TmpLocalsParameter = new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.PhpArray, SpecialParameterSymbol.TemporaryLocalsName, index++),
                GeneratorParameter = new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Generator, "generator", index++),
            };
        }

        public ParameterSymbol ContextParameter { get; private set; }
        public ParameterSymbol ThisParameter { get; private set; }
        public ParameterSymbol LocalsParameter { get; private set; }
        public ParameterSymbol TmpLocalsParameter { get; private set; }
        public ParameterSymbol GeneratorParameter { get; private set; }
    }
}
