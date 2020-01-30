using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// Exception thrown if an error which can only be found on runtime occurs.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class RuntimeException : Spl.Exception
    {
        [PhpFieldsOnlyCtor]
        protected RuntimeException() { }

        public RuntimeException(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)

        {
        }
    }

    /// <summary>
    /// Exception thrown if a value does not match with a set of values. Typically this happens when a function calls
    /// another function and expects the return value to be of a certain type or value not including arithmetic or
    /// buffer related errors.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class UnexpectedValueException : RuntimeException
    {
        [PhpFieldsOnlyCtor]
        protected UnexpectedValueException() { }

        public UnexpectedValueException(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }
    }

    /// <summary>
    /// Exception thrown if an argument is not of the expected type.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class InvalidArgumentException : RuntimeException
    {
        [PhpFieldsOnlyCtor]
        protected InvalidArgumentException() { }

        public InvalidArgumentException(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    /// <summary>
    /// Exception thrown if a value is not a valid key. This represents errors that cannot be detected at compile time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class OutOfBoundsException : RuntimeException
    {
        [PhpFieldsOnlyCtor]
        protected OutOfBoundsException() { }

        public OutOfBoundsException(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    /// <summary>
    /// Exception thrown when adding an element to a full container.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class OverflowException : RuntimeException
    {
        [PhpFieldsOnlyCtor]
        protected OverflowException() { }

        public OverflowException(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    /// <summary>
    /// Exception thrown when performing an invalid operation on an empty container, such as removing an element.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class UnderflowException : RuntimeException
    {
        [PhpFieldsOnlyCtor]
        protected UnderflowException() { }

        public UnderflowException(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    /// <summary>
    /// Exception thrown to indicate range errors during program execution.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class RangeException : RuntimeException
    {
        [PhpFieldsOnlyCtor]
        protected RangeException() { }

        public RangeException(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }
}
