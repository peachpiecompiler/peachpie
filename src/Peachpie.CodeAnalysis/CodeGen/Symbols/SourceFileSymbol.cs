using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceFileSymbol
    {
        internal void SynthesizeInit(Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            // module.EmitBootstrap(this); // unnecessary
        }
    }
}
