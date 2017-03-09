using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhpException = global::Exception;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// Exception raised by PDO objects
    /// </summary>
    /// <seealso cref="Exception" />
    public class PDOException : PhpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PDOException"/> class.
        /// </summary>
        /// <param name="message">Message décrivant l'erreur.</param>
        public PDOException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PDOException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="code">The code.</param>
        /// <param name="previous">The previous.</param>
        public PDOException(string message, long code, Throwable previous) : base(message, code, previous)
        {
        }
    }
}
