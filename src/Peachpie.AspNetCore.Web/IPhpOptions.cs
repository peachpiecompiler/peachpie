using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Pchp.Core;
using Pchp.Library;

namespace Peachpie.AspNetCore.Web
{
    /// <summary>
    /// Configurable options for a PHP request.
    /// </summary>
    public interface IPhpOptions : IPhpConfigurationService
    {
        /// <summary>
        /// Encoding to be used for<br/>
        /// - the conversion between Unicode string and byte string.<br/>
        /// - outputting Unicode string to response stream.<br/>
        /// Recommended value is <see cref="Encoding.UTF8"/>.
        /// </summary>
        Encoding StringEncoding { get; set; }

        ///// <summary>
        ///// Collection of assemblies containing compiled PHP scripts to be loaded.
        ///// If the collection is empty, all the referenced assemblies are loaded.
        ///// </summary>
        //ICollection<Assembly> ScriptAssemblyCollection { get; }

        /// <summary>
        /// Application's root directory.
        /// All the scripts are resolved relatively to this path.
        /// </summary>
        string RootPath { get; set; }

        /// <summary>
        /// Gets session configuration.
        /// </summary>
        /// <remarks>
        /// Can be <c>null</c> in case the application is not built with the session extension (<c>Peachpie.Library</c>).
        /// </remarks>
        IPhpSessionConfiguration Session { get; }

        /// <summary>
        /// Allows additional configuration of the request,
        /// right before the requested script is executed.
        /// </summary>
        event Action<Context> RequestStart;

        /// <summary>
        /// Name the logger category. Default is <c>"PHP"</c>.
        /// </summary>
        string LoggerCategory { get; set; }
    }
}
