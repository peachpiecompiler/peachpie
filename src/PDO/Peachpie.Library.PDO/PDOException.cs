using Pchp.Library.Spl;
using System;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// Exception raised by PDO objects
    /// </summary>
    /// <seealso cref="Pchp.Library.Spl.Exception" />
    public class PDOException : Pchp.Library.Spl.Exception
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
