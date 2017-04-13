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
    /// Signature: <code>static &lt;&gt;sm_(Context &lt;ctx&gt;, T @this, PhpArray &lt;locals&gt;, Generator generator)</code>
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
            base.SetParameters(getParams(originalRoutine));
        }

        /// <summary>
        ///  Parameters for SourceGeneratorSymbol method are defined by <see cref="GeneratorStateMachineDelegate"/>
        /// </summary>
        ParameterSymbol[] getParams(SourceRoutineSymbol originalRoutine)
        {
            // resolve type of $this
            TypeSymbol thisType = originalRoutine.ThisParameter?.Type ?? (TypeSymbol)originalRoutine.DeclaringCompilation.ObjectType;

            Debug.Assert(thisType != null);

            // yield sm method signature
            var index = 0;
            return new[]
            {
                new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++),
                new SpecialParameterSymbol(this, thisType, SpecialParameterSymbol.ThisName, index++),
                new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.PhpArray, SpecialParameterSymbol.LocalsName, index++),
                new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Generator, "generator", index++),
            };

        }

    }
}
