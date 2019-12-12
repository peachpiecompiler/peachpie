using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// Exception that represents error in the program logic.
    /// This kind of exception should lead directly to a fix in your code.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class LogicException : Spl.Exception
    {
        [PhpFieldsOnlyCtor]
        protected LogicException() { }

        public LogicException(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
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

    /// <summary>
    /// Exception thrown when an illegal index was requested. This represents errors that should be detected at compile time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class OutOfRangeException : LogicException
    {
        [PhpFieldsOnlyCtor]
        protected OutOfRangeException() { }

        public OutOfRangeException(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }
}
