using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Web
{
    /// <summary>
    /// PHP scripts hosting options.
    /// </summary>
    public class PhpRequestOptions
    {
        /// <summary>
        /// Set of assemblies name containing compiled PHP scripts to be sideloaded.
        /// </summary>
        public string[] ScriptAssembliesName { get; set; } = new[] { "website" };
    }
}
