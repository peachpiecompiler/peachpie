#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Utilities;

namespace Pchp.Core
{
    partial class Context : IEncodingProvider
    {
        /// <summary>
        /// Encoding used to convert between unicode strings and binary strings.
        /// </summary>
        public virtual Encoding StringEncoding => Encoding.UTF8;

        /// <summary>
        /// Gets name of the server API (aka <c>PHP_SAPI</c> PHP constant).
        /// Always a lowercase string. Cannot be <c>null</c>.
        /// </summary>
        public virtual string ServerApi => "isapi";

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
        public virtual IHttpPhpContext? HttpPhpContext => null;

        /// <summary>
        /// Gets or sets the initial script file.
        /// </summary>
        public ScriptInfo MainScriptFile
        {
            get
            {
                return _mainScriptFile;
            }

            protected set
            {
                Debug.Assert(_mainScriptFile.IsValid == false);
                Debug.Assert(value.IsValid);

                _mainScriptFile = value;

                // cwd = entering script directory
                // simple Path.Concat:
                var reldir = PathUtils.TrimFileName(value.Path);
                this.WorkingDirectory = reldir.IsEmpty
                    ? RootPath
                    : StringUtils.Concat(RootPath.AsSpan(), CurrentPlatform.DirectorySeparator, reldir);
            }
        }
        ScriptInfo _mainScriptFile;

        /// <summary>
        /// Root directory (web root or console app root) where loaded scripts are relative to.
        /// The root path does not end with directory separator.
        /// </summary>
        /// <remarks>
        /// - <c>__FILE__</c> and <c>__DIR__</c> magic constants are resolved as concatenation with this value.
        /// - <see cref="WorkingDirectory"/> is initialized with this value upon context is created.
        /// </remarks>
        public string RootPath
        {
            get
            {
                return _rootPath;
            }
            set
            {
                _rootPath = CurrentPlatform.NormalizeSlashes((value ?? throw new ArgumentNullException()).TrimEndSeparator());
            }
        }
        string _rootPath = string.Empty;

        /// <summary>
        /// Current working directory.
        /// </summary>
        public virtual string WorkingDirectory { get; set; }

        /// <summary>
        /// Set of include paths to be used to resolve full file path.
        /// </summary>
        public virtual string[] IncludePaths => this.Configuration.Core.IncludePathsArray;

        /// <summary>
        /// Gets target PHP language specification.
        /// By default, this is reflected from the compiled PHP script.
        /// </summary>
        public TargetPhpLanguageAttribute? TargetPhpLanguage { get => s_targetPhpLanguageAttribute; }
        static TargetPhpLanguageAttribute? s_targetPhpLanguageAttribute;

        /// <summary>
        /// Gets value indicating whether not defined classes should be automatically included when used for the first time.
        /// Does not apply when SPL autoloading gets enabled.
        /// This is intended for package distribution without the need of specifying autoload class map or a similar mechanism.
        /// </summary>
        /// <remarks>See <see cref="ImplicitAutoloadTypeByName"/> for more details.</remarks>
        public bool EnableImplicitAutoload { get; set; }
    }
}
