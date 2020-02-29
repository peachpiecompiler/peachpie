using System;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// <see cref="Exception"/> is the base class for all Exceptions in PHP 5, and the base class for all user exceptions in PHP 7.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
    public class Exception : System.Exception, Throwable
    {
        /// <summary>
        /// Original stack trace created when the class was constructed.
        /// </summary>
        [PhpHidden]
        readonly PhpStackTrace/*!*/_stacktrace = new PhpStackTrace();

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

        protected PhpValue message; // laravel makes references to this variable hence it cannot be strictly `string` // https://github.com/peachpiecompiler/peachpie/issues/564 - can be typed as string once we allow making references to any type
        protected PhpValue code;    // should be `long` only, but laravel assigns string as well here
        protected string file;
        protected int line;

        [PhpFieldsOnlyCtor]
        protected Exception()
        {
            this.code = PhpValue.Null;
            this.message = PhpValue.Null;
        }

        public Exception(string message = "", long code = 0, Throwable previous = null)
            : base(message, innerException: previous as System.Exception)
        {
            this.file = _stacktrace.GetFilename();
            this.line = _stacktrace.GetLine();

            __construct(message, code, previous);
        }

        /// <summary>
        /// Exception message in CLR.
        /// </summary>
        public override string Message
        {
            get
            {
                if (Operators.IsSet(this.message))
                {
                    if (this.message.IsString(out var str))
                    {
                        return str;
                    }

                    return this.message.ToString(); // ASSERTION! no culture provided
                }

                return string.Empty;
            }
        }

        public void __construct(string message = "", long code = 0, Throwable previous = null)
        {
            this.message = message;
            this.code = code;

            this.previous = previous;
        }

        public virtual int getCode() => (int)this.code;

        public virtual string getFile() => this.file;

        public virtual int getLine() => this.line;

        public virtual string getMessage() => this.Message;

        public virtual Throwable getPrevious() => previous ?? this.InnerException as Throwable;

        public virtual PhpArray getTrace() => trace;

        public virtual string getTraceAsString() => _stacktrace.GetStackTraceString(); // TODO: _trace

        public void __wakeup() => throw new NotImplementedException();

        public virtual string __toString() => _stacktrace.FormatExceptionString(this.GetPhpTypeInfo().Name, this.Message); // TODO: _trace

        public sealed override string ToString() => __toString();
    }

    /// <summary>
    /// An Error Exception.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
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
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("phar")]
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
