using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
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

    /// <summary>
    /// Exception thrown if a value does not adhere to a defined valid data domain.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class DomainException : LogicException
    {
        [PhpFieldsOnlyCtor]
        protected DomainException() { }

        public DomainException(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }
    }

    /// <summary>
    /// Exception thrown if a length is invalid.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class LengthException : LogicException
    {
        [PhpFieldsOnlyCtor]
        protected LengthException() { }

        public LengthException(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }
    }
}
