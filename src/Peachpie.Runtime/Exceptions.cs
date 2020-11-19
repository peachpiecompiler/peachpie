using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// PHP runtime exception.
    /// </summary>
    public class PhpErrorException : Exception
    {
        public PhpErrorException()
        {
        }

        public PhpErrorException(string message) : base(message)
        {
        }

        public PhpErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Fatal PHP error causing the script to be terminated.
    /// </summary>
    public sealed class PhpFatalErrorException : PhpErrorException
    {
        public PhpFatalErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }


    /// <summary>
    /// Thrown by exit/die language constructs to cause immediate termination of a script being executed.
    /// </summary>
    [DebuggerDisplay("died(reason={Status,nq})")]
    public sealed class ScriptDiedException : PhpErrorException
    {
        /// <summary>
        /// The exist status.
        /// </summary>
        public PhpValue Status { get; }

        /// <summary>
        /// Gets exit code from the status code.
        /// </summary>
        public int ExitCode => ProcessStatus(null);

        public ScriptDiedException(PhpValue status)
        {
            Status = status;
        }

        public ScriptDiedException(string status)
            : this(PhpValue.Create(status))
        {
        }

        public ScriptDiedException(long status)
            : this(PhpValue.Create(status))
        {
        }

        public ScriptDiedException()
            : this(PhpValue.Create(255))
        {
        }

        public override string Message => Status.DisplayString;

        /// <summary>
        /// Status of a different type than integer is printed,
        /// exit code according to PHP semantic is returned.
        /// </summary>
        public int ProcessStatus(Context ctx) => ProcessStatus(ctx, Status);

        static int ProcessStatus(Context ctx, PhpValue status)
        {
            switch (status.TypeCode)
            {
                case PhpTypeCode.Alias:
                    return ProcessStatus(ctx, status.Alias.Value);

                case PhpTypeCode.Long:
                    return (int)status.ToLong();

                default:
                    ctx?.Echo(status);
                    return 0;
            }
        }
    }

    /// <summary>
    /// Thrown when a script couldn't be included because it was not found.
    /// See <see cref="Path"/> for the script file path.
    /// </summary>
    public sealed class ScriptIncludeException : PhpErrorException
    {
        /// <summary>
        /// Original path to the script that failed to be included.
        /// </summary>
        public string Path { get; }

        internal ScriptIncludeException(string path)
        {
            Path = path;
        }

        public override string Message => string.Format(Resources.ErrResources.script_not_found, Path);
    }
}
