using System;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// <see cref="Exception"/> is the base class for all Exceptions in PHP 5, and the base class for all user exceptions in PHP 7.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class Exception : System.Exception, Throwable
    {
        /// <summary>
        /// Original stack trace created when the class was constructed.
        /// </summary>
        [PhpHidden]
        private PhpStackTrace/*!*/_stacktrace;

        [PhpHidden]
        private PhpArray _trace;

        private Throwable previous;

        /// <summary>
        /// This property is used by some PHP frameworks
        /// to "override" <see cref="getTrace"/> and <see cref="getTraceAsString"/> feature.
        /// </summary>
        private PhpArray/*!*/trace
        {
            get => _trace ?? (_trace = _stacktrace.GetBacktrace());
            set => _trace = value;
        }

        protected string message;
        protected long code;
        protected string file;
        protected int line;

        [PhpFieldsOnlyCtor]
        protected Exception()
        {
            _stacktrace = new PhpStackTrace();
        }

        public Exception(string message = "", long code = 0, Throwable previous = null)
            : base(message, innerException: previous as System.Exception)
        {
            _stacktrace = new PhpStackTrace();

            this.file = _stacktrace.GetFilename();
            this.line = _stacktrace.GetLine();

            __construct(message, code, previous);
        }

        /// <summary>
        /// Exception message in CLR.
        /// </summary>
        public override string Message => this.message ?? string.Empty;

        public void __construct(string message = "", long code = 0, Throwable previous = null)
        {
            this.message = message;
            this.code = code;

            this.previous = previous;
        }

        public virtual int getCode() => (int)this.code;

        public virtual string getFile() => this.file;

        public virtual int getLine() => this.line;

        public virtual string getMessage() => this.message;

        public virtual Throwable getPrevious() => previous ?? this.InnerException as Throwable;

        public virtual PhpArray getTrace() => trace;

        public virtual string getTraceAsString() => _stacktrace.GetStackTraceString(); // TODO: _trace

        public virtual string __toString() => _stacktrace.FormatExceptionString(this.GetPhpTypeInfo().Name, getMessage()); // TODO: _trace

        public sealed override string ToString() => __toString();
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

        //public sealed override void __construct(string message = "", long code = 0, Throwable previous = null)
        //{
        //    __construct(message, code, -1, null, -1, (Exception)previous);
        //}

        public void __construct(string message = "", long code = 0, int severity = (int)PhpError.E_ERROR, string filename = null, int lineno = -1, Exception previous = null)
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
    /// The PharException class provides a phar-specific exception class.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class PharException : Spl.Exception
    {
        [PhpFieldsOnlyCtor]
        protected PharException() { }

        public PharException(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }
}
