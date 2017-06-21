using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// Exception thrown if a value does not match with a set of values. Typically this happens when a function calls
    /// another function and expects the return value to be of a certain type or value not including arithmetic or
    /// buffer related errors.
    /// </summary>
    [PhpType("[name]")]
    public class UnexpectedValueException : RuntimeException
    {
        [PhpFieldsOnlyCtor]
        protected UnexpectedValueException() { }

        public UnexpectedValueException(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }
    }
}
