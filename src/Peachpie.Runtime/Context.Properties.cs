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
        /// </summary>
        public virtual string ServerApi => null;

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
                this.WorkingDirectory = string.Concat(RootPath, CurrentPlatform.DirectorySeparator.ToString(), PathUtils.DirectoryName(value.Path));
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
                if (value == null) throw new ArgumentNullException();
                _rootPath = CurrentPlatform.NormalizeSlashes(value).TrimEndSeparator();
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
        public virtual TargetPhpLanguageAttribute TargetPhpLanguage { get => _targetPhpLanguageAttribute; }
        static TargetPhpLanguageAttribute _targetPhpLanguageAttribute;
    }
}
