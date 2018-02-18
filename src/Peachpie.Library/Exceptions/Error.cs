using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// <see cref="Error"/> is the base class for all internal PHP errors.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class Error : System.Exception, Throwable
    {
        internal readonly PhpStackTrace/*!*/_stacktrace;
        internal Throwable _previous;

        protected string message;
        protected long code;
        protected string file;
        protected int line;

        [PhpFieldsOnlyCtor]
        protected Error()
        {
            _stacktrace = new PhpStackTrace();
        }

        public Error(string message = "", long code = 0, Throwable previous = null)
            : base(message, previous as System.Exception)
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

        public virtual void __construct(string message = "", long code = 0, Throwable previous = null)
        {
            this.message = message;
            this.code = code;

            _previous = previous;
        }

        public virtual int getCode() => (int)code;

        public virtual string getFile() => file;

        public virtual int getLine() => line;

        public virtual string getMessage() => message;

        public virtual Throwable getPrevious() => _previous ?? this.InnerException as Throwable;

        public virtual PhpArray getTrace() => _stacktrace.GetBacktrace();

        public virtual string getTraceAsString() => _stacktrace.GetStackTraceString();

        public virtual string __toString() => _stacktrace.FormatExceptionString(this.GetPhpTypeInfo().Name, getMessage());

        public sealed override string ToString() => __toString();
    }

    /// <summary>
    /// Thrown when <c>assert()</c> fails.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class AssertionError : Error
    {
        [PhpFieldsOnlyCtor]
        protected AssertionError() { }

        public AssertionError(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    [PhpType(PhpTypeAttribute.InheritName)]
    public class TypeError : Error
    {
        [PhpFieldsOnlyCtor]
        protected TypeError() { }

        public TypeError(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }
}
