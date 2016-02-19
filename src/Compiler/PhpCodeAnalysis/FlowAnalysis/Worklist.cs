using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Queue of work items to do.
    /// </summary>
    internal class Worklist
    {
        /// <summary>
        /// List of blocks to be processed.
        /// </summary>
        readonly DistinctQueue<BoundBlock> _queue = new DistinctQueue<BoundBlock>();
    }
}
