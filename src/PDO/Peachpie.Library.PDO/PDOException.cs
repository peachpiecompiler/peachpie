#nullable enable

using Pchp.Library.Spl;
using System;
using Pchp.Core;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// Exception raised by PDO objects
    /// </summary>
    /// <seealso cref="Pchp.Library.Spl.Exception" />
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(PDOConfiguration.PdoExtensionName)]
    public class PDOException : RuntimeException
    {
        /// <summary>
        /// Corresponds to <see cref="PDO.errorInfo"/> or <see cref="PDOStatement.errorInfo"/>.
        /// Can be changed.
        /// </summary>
        public PhpArray errorInfo
        {
            get => _errorInfo ?? _error.ToPhpErrorInfo();
            /*protected*/set => _errorInfo = value;
        }

        [PhpHidden]
        private PhpArray? _errorInfo;

        [PhpHidden]
        readonly PDO.ErrorInfo _error;

        /// <summary>
        /// SQLSTATE error code.
        /// </summary>
        public virtual new string getCode() => this.code.ToString(); // _error.SqlState

        /// <summary>
        /// Empty constructor used when overriding class in PHP.
        /// </summary>
        [PhpFieldsOnlyCtor]
        protected PDOException() { }

        internal PDOException(PDO.ErrorInfo error, Throwable? previous = null)
            : base(error.Message, 0, previous)
        {
            _error = error;
            this.code = error.SqlState;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PDOException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="code">The code.</param>
        /// <param name="previous">The previous.</param>
        public PDOException(string message = "", long code = 0, Throwable? previous = null)
            : this(PDO.ErrorInfo.Create(string.Empty, string.Empty, message), previous)
        {
            this.code = code;
        }
    }
}
