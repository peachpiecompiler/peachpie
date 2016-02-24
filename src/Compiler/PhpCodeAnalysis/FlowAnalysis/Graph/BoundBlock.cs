using Pchp.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class BoundBlock
    {
        /// <summary>
        /// Initial block flow state.
        /// Can be <c>null</c> in case there is no flow into the block or the state was released.
        /// </summary>
        internal FlowState FlowState
        {
            get; set;
        }
    }

    partial class ExitBlock
    {
        // TODO: list of blocks (may be from another CFG!!!) waiting for return type of this function
    }
}
