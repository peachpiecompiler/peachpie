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
        public PhpArray errorInfo = PhpArray.NewEmpty();    // TODO
        
        /// <summary>
        /// Empty constructor used when overriding class in PHP.
        /// </summary>
        [PhpFieldsOnlyCtor]
        protected PDOException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PDOException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="code">The code.</param>
        /// <param name="previous">The previous.</param>
        public PDOException(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }
}
