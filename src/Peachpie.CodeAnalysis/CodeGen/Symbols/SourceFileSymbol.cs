using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceFileSymbol
    {
        /// <summary>
        /// Main method wrapper in case it does not return PhpValue.
        /// </summary>
        internal void SynthesizeMainMethodWrapper(Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            if (_mainMethod.ReturnType != DeclaringCompilation.CoreTypes.PhpValue)
            {
                // PhpValue <Main>`0(parameters)
                var wrapper = new SynthesizedMethodSymbol(
                    this, WellKnownPchpNames.GlobalRoutineName + "`0", true, false,
                    DeclaringCompilation.CoreTypes.PhpValue, Accessibility.Public);

                wrapper.SetParameters(_mainMethod.Parameters.Select(p =>
                    new SpecialParameterSymbol(wrapper, p.Type, p.Name, p.Ordinal)).ToArray());

                // save method symbol to module
                module.SynthesizedManager.AddMethod(this, wrapper);

                // generate method body
                module.CreateMainMethodWrapper(wrapper, _mainMethod, diagnostics);
            }
        }
    }
}
