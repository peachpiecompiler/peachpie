using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Encoding used to convert between unicode strings and binary strings.
        /// </summary>
        public virtual Encoding StringEncoding => Encoding.UTF8;

        /// <summary>
        /// Gets number format used for converting <see cref="double"/> to <see cref="string"/>.
        /// </summary>
        public virtual NumberFormatInfo NumberFormat => NumberFormatInfo.InvariantInfo;

        /// <summary>
        /// Gets value indicating whether the application is a web application.
        /// </summary>
        public bool IsWebApplication => this.HttpPhpContext != null;

        /// <summary>
        /// In case of a context providing web features,
        /// gets instance of <see cref="IHttpPhpContext"/> for HTTP request manipulation.
        /// 
        /// If it is a console context or a class library context, the property gets a <c>null</c> reference.
        /// </summary>
        public virtual IHttpPhpContext HttpPhpContext => null;

        /// <summary>
        /// Gets the initial script file.
        /// </summary>
        public ScriptInfo MainScriptFile { get; protected set; }

        /// <summary>
        /// Root directory (web root or console app root) where loaded scripts are relative to.
        /// The root path does not end with directory separator.
        /// </summary>
        /// <remarks>
        /// - <c>__FILE__</c> and <c>__DIR__</c> magic constants are resolved as concatenation with this value.
        /// </remarks>
        public virtual string RootPath { get; } = string.Empty;

        /// <summary>
        /// Current working directory.
        /// </summary>
        public virtual string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Set of include paths to be used to resolve full file path.
        /// </summary>
        public virtual string[] IncludePaths => _defaultIncludePaths;   // TODO:  => this.Config.FileSystem.IncludePaths
        static readonly string[] _defaultIncludePaths = new[] { "." };
    }
}
