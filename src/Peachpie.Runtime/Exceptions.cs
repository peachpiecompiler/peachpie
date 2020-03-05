using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Thrown by exit/die language constructs to cause immediate termination of a script being executed.
    /// </summary>
    [DebuggerDisplay("died(reason={_status,nq})")]
    public sealed class ScriptDiedException : Exception
    {
        /// <summary>
        /// The exist status.
        /// </summary>
        public PhpValue Status => _status;
        PhpValue _status;

        /// <summary>
        /// Gets exit code from the status code.
        /// </summary>
        public int ExitCode => ProcessStatus(null);

        public ScriptDiedException(PhpValue status)
        {
            _status = status;
        }

        public ScriptDiedException(string status)
            :this(PhpValue.Create(status))
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

        public override string Message => _status.DisplayString;
        
        /// <summary>
        /// Status of a different type than integer is printed,
        /// exit code according to PHP semantic is returned.
        /// </summary>
        public int ProcessStatus(Context ctx) => ProcessStatus(ctx, ref _status);

        int ProcessStatus(Context ctx, ref PhpValue status)
        {
            switch (status.TypeCode)
            {
                case PhpTypeCode.Alias:
                    return ProcessStatus(ctx, ref status.Alias.Value);

                case PhpTypeCode.Long:
                    return (int)status.ToLong();

                default:
                    if (ctx != null)
                    {
                        ctx.Echo(status);
                    }
                    return 0;
            }
        }
    }

    /// <summary>
    /// Thrown when a script couldn't be included because it was not found.
    /// See <see cref="Path"/> for the script file path.
    /// </summary>
    public sealed class ScriptIncludeException : ArgumentException
    {
        /// <summary>
        /// Original path to the script that failed to be included.
        /// </summary>
        public string Path { get; }

        internal ScriptIncludeException(string path)
            : base(string.Format(Resources.ErrResources.script_not_found, path))
        {
            this.Path = path;
        }
    }
}
