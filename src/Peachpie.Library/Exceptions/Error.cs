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
    [PhpType("Error")]
    public class Error : System.Exception, Throwable
    {
        protected string message;
        protected long code;
        protected string file;
        protected int line;

        public Error() { }

        public Error(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }

        public void __construct(string message = "", long code = 0, Throwable previous = null)
        {
            this.message = message;
            this.code = code;
        }

        public virtual int getCode()
        {
            throw new NotImplementedException();
        }

        public virtual string getFile()
        {
            throw new NotImplementedException();
        }

        public virtual int getLine()
        {
            throw new NotImplementedException();
        }

        public virtual string getMessage()
        {
            throw new NotImplementedException();
        }

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
}
