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
        /// <summary>
        /// Regular CLR assembly.
        /// </summary>
        None,

        /// <summary>
        /// Corresponds to system runtime library.
        /// </summary>
        CorLibrary,

        /// <summary>
        /// Corresponds to our runtime library (<c>Peachpie.Runtime</c>).
        /// </summary>
        PeachpieCorLibrary,

        /// <summary>
        /// Denotates an assembly that contains the library extension.
        /// Such assembly is marked with assembly attribute <c>PhpExtensionAttribute</c>, it contains PHP types, functions or PHP scripts.
        /// </summary>
        ExtensionLibrary,
    }
}
