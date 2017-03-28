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
    /// <summary>
    /// Synthesized method representing the generator state machine's next function.
    /// </summary>
    internal sealed partial class SourceGeneratorSymbol : SynthesizedMethodSymbol
    {
        public SourceGeneratorSymbol(NamedTypeSymbol containingType)
            : base(containingType, "generator@function", 
                  isstatic: true, isvirtual: false, 
                  returnType: containingType.DeclaringCompilation.CoreTypes.Void, ps: null )
        {
            //Need to postpone settings the params because I can't access 'this' in base constructor call
            var parameters = getParams();
            base.SetParameters(null);
        }

        /// <summary>
        ///  Parameters for SourceGeneratorSymbol method are defined by <see cref="Core.std.GeneratorStateMachineDelegate"/>
        /// </summary>
        IEnumerable<ParameterSymbol> getParams()
        {
            int index = 0;

            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, index++);
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Object, SpecialParameterSymbol.ThisName, index++);
            yield return new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Generator, "generator", index++);

            
        }

    }
}
