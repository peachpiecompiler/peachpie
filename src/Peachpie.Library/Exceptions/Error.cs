using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// <see cref="Error"/> is the base class for all internal PHP errors.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class Error : System.Exception, Throwable
    {
        protected string message;
        protected long code;
        protected string file;
        protected int line;

        [PhpFieldsOnlyCtor]
        protected Error() { }

        public Error(string message = "", long code = 0, Throwable previous = null)
            : base(message, previous as System.Exception)
        {
            __construct(message, code, previous);
        }

        /// <summary>
        /// Exception message in CLR.
        /// </summary>
        public override string Message => this.message;

        public void __construct(string message = "", long code = 0, Throwable previous = null)
        {
            this.message = message;
            this.code = code;
        }

        public virtual int getCode() => (int)code;

        public virtual string getFile() => file;

        public virtual int getLine() => line;

        public virtual string getMessage() => Message;

        public virtual Throwable getPrevious() => this.InnerException as Throwable;

        public virtual PhpArray getTrace()
        {
            throw new NotImplementedException();
        }

        public virtual string getTraceAsString()
        {
            throw new NotImplementedException();
        }

        public virtual string __toString() => Message;
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
