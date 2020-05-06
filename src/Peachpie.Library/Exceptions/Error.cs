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
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
    public class Error : System.Exception, Throwable
    {
        [PhpHidden]
        readonly PhpStackTrace/*!*/_stacktrace = new PhpStackTrace();

        [PhpHidden]
        private PhpArray _trace;

        protected string message;
        protected long code;
        protected string file;
        protected int line;

        /// <summary>
        /// This property is used by some PHP frameworks
        /// to "override" <see cref="getTrace"/> and <see cref="getTraceAsString"/> feature.
        /// </summary>
        private PhpArray/*!*/trace
        {
            get => _trace ?? (_trace = _stacktrace.GetBacktrace());
            set => _trace = value;
        }

        private Throwable previous;

        [PhpFieldsOnlyCtor]
        protected Error()
        {
        }

        public Error(string message = "", long code = 0, Throwable previous = null)
            : base(message, previous as System.Exception)
        {
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

            this.previous = previous;
        }

        public virtual int getCode() => (int)code;

        public virtual string getFile() => file;

        public virtual int getLine() => line;

        public virtual string getMessage() => message;

        public virtual Throwable getPrevious() => previous ?? this.InnerException as Throwable;

        public virtual PhpArray getTrace() => trace;

        public virtual string getTraceAsString() => _stacktrace.GetStackTraceString(); // TODO: _trace

        public void __wakeup() => throw new NotImplementedException();

        public virtual string __toString() => _stacktrace.FormatExceptionString(this.GetPhpTypeInfo().Name, this.Message);   // TODO: _trace

        public sealed override string ToString() => __toString();
    }

    /// <summary>
    /// Thrown when an error occurs while performing mathematical operations.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
    public class ArithmeticError : Error
    {
        [PhpFieldsOnlyCtor]
        protected ArithmeticError() { }

        public ArithmeticError(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    /// <summary>
    /// Thrown when an attempt is made to divide a number by zero.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
    public class DivisionByZeroError : ArithmeticError
    {
        [PhpFieldsOnlyCtor]
        protected DivisionByZeroError() { }

        public DivisionByZeroError(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    /// <summary>
    /// Thrown when <c>assert()</c> fails.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("standard")]
    public class AssertionError : Error
    {
        [PhpFieldsOnlyCtor]
        protected AssertionError() { }

        public AssertionError(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
    public class TypeError : Error
    {
        [PhpFieldsOnlyCtor]
        protected TypeError() { }

        public TypeError(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
    public class ArgumentCountError : TypeError
    {
        [PhpFieldsOnlyCtor]
        protected ArgumentCountError() { }

        public ArgumentCountError(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
    public class CompileError : Error
    {
        [PhpFieldsOnlyCtor]
        protected CompileError() { }

        public CompileError(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
    public class ParseError : CompileError
    {
        [PhpFieldsOnlyCtor]
        protected ParseError() { }

        public ParseError(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }
}
