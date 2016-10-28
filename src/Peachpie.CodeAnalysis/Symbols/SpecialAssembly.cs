using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Anumeration of well known assemblies.
    /// </summary>
    enum SpecialAssembly
    {
        None,
        CorLibrary,
        PchpCorLibrary,
        ExtensionLibrary,
    }
}
