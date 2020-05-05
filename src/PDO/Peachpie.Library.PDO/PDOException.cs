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
        /// </summary>
        public PhpArray errorInfo => _error.ToPhpErrorInfo();

        readonly PDO.ErrorInfo _error;

        /// <summary>
        /// Empty constructor used when overriding class in PHP.
        /// </summary>
        [PhpFieldsOnlyCtor]
        protected PDOException() { }

        internal PDOException(PDO.ErrorInfo error, Throwable previous = null)
            : base(error.Message, error.CodeOrZero(), previous)
        {
            _error = error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PDOException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="code">The code.</param>
        /// <param name="previous">The previous.</param>
        public PDOException(string message = "", long code = 0, Throwable previous = null)
            : this(PDO.ErrorInfo.Create(string.Empty, code.ToString(), message), previous)
        {
        }
    }
}
