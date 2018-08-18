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
                case PhpTypeCode.Int32:
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
}
