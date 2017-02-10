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
    }
}
