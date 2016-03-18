using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    partial class EdgesAnalysis
    {
        /// <summary>
        /// Visits single expressions and project transformations to flow state.
        /// </summary>
        class ExpressionAnalysis : OperationVisitor
        {
            #region Fields

            EdgesAnalysis _analysis;

            /// <summary>
            /// Gets current type context for type masks resolving.
            /// </summary>
            protected TypeRefContext TypeRefContext => _analysis.TypeRefContext;

            /// <summary>
            /// Current flow state.
            /// </summary>
            protected FlowState State => _analysis._state;

            #endregion

            #region Construction

            public ExpressionAnalysis()
            {
            }

            internal void SetAnalysis(EdgesAnalysis analysis)
            {
                _analysis = analysis;
            }

            #endregion
        }
    }
}
