using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// Exception thrown if an argument is not of the expected type.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class InvalidArgumentException : RuntimeException
    {
        [PhpFieldsOnlyCtor]
        protected InvalidArgumentException() { }

        public InvalidArgumentException(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }
    }

    /// <summary>
    /// Exception thrown if a callback refers to an undefined function or if some arguments are missing.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class BadFunctionCallException : LogicException
    {
        [PhpFieldsOnlyCtor]
        protected BadFunctionCallException() { }

        public BadFunctionCallException(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }
    }

    /// <summary>
    /// Exception thrown if a callback refers to an undefined method or if some arguments are missing.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class BadMethodCallException : BadFunctionCallException
    {
        [PhpFieldsOnlyCtor]
        protected BadMethodCallException() { }

        public BadMethodCallException(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }
    }
}
