using System;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// <see cref="Exception"/> is the base class for all Exceptions in PHP 5, and the base class for all user exceptions in PHP 7.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class Exception : System.Exception, Throwable
    {
        protected string message;
        protected long code;
        protected string file;
        protected int line;

        [PhpFieldsOnlyCtor]
        protected Exception() { }

        public Exception(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }

        /// <summary>
        /// Exception message in CLR.
        /// </summary>
        public override string Message => this.message;

        public virtual void __construct(string message = "", long code = 0, Throwable previous = null)
        {
            this.message = message;
            this.code = code;
        }

        public virtual int getCode() => (int)this.code;

        public virtual string getFile() => this.file;

        public virtual int getLine() => this.line;

        public virtual string getMessage() => this.message;

        public virtual Throwable getPrevious()
        {
            throw new NotImplementedException();
        }

        public virtual PhpArray getTrace()
        {
            throw new NotImplementedException();
        }

        public virtual string getTraceAsString()
        {
            throw new NotImplementedException();
        }

        public virtual string __toString()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Exception thrown if an error which can only be found on runtime occurs.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
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
    /// Exception that represents error in the program logic.
    /// This kind of exception should lead directly to a fix in your code.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
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
    /// An Error Exception.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class ErrorException : Spl.Exception
    {
        protected long severity;

        [PhpFieldsOnlyCtor]
        protected ErrorException() { }

        public ErrorException(string message = "", long code = 0, int severity = (int)PhpError.E_ERROR, string filename = null, int lineno = -1, Exception previous = null)
        {
            __construct(message, code, severity, filename, lineno, previous);
        }

        public sealed override void __construct(string message = "", long code = 0, Throwable previous = null)
            => __construct(message, code, -1, null, -1, (Exception)previous);
        
        public virtual void __construct(string message = "", long code = 0, int severity = (int)PhpError.E_ERROR, string filename = null, int lineno = -1, Exception previous = null)
        {
            this.message = message;
            this.code = code;
            this.severity = severity;
            this.file = filename;
            this.line = lineno;
        }

        public long getSeverity() => severity;
    }

    /// <summary>
    /// Exception thrown if a value is not a valid key. This represents errors that cannot be detected at compile time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
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
    /// Exception thrown when an illegal index was requested. This represents errors that should be detected at compile time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
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