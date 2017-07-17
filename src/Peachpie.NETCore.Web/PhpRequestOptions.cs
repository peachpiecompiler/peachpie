using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;

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

        /// <summary>
        /// Encoding to be used for
        /// - the conversion between Unicode string and byte string.
        /// - outputting Unicode string to response stream.
        /// </summary>
        public Encoding StringEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Event raised before processing the request within newly created <see cref="Context"/>.
        /// </summary>
        public event Action<Context> BeforeRequest;

        internal void InvokeBeforeRequest(Context ctx) => BeforeRequest?.Invoke(ctx);
        
        public PhpRequestOptions() { }

        public PhpRequestOptions(
            Encoding encoding = null,
            string scriptAssemblyName = null)
        {
            if (encoding != null) this.StringEncoding = encoding;
            if (scriptAssemblyName != null) this.ScriptAssembliesName = new[] { scriptAssemblyName };
        }
    }
}
