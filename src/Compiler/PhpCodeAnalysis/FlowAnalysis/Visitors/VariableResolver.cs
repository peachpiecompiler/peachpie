using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis.Visitors
{
    /// <summary>
    /// Resolves <see cref="BoundVariableRef.Variable"/> within given context.
    /// </summary>
    class VariableResolver : OperationWalker
    {
        readonly FlowContext _ctx;

        public VariableResolver(FlowContext ctx)
        {
            Contract.ThrowIfNull(ctx);
            _ctx = ctx;
        }

        public override void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        {
            var vref = ((BoundVariableRef)operation);
            vref.Update(_ctx.GetVar(vref.Name));
        }
    }
}