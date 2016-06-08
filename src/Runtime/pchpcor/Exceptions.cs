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
    public class ScriptDiedException : Exception
    {
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

        /// <summary>
        /// The exist status.
        /// </summary>
        public PhpValue Status => _status;
        PhpValue _status;
    }
}
