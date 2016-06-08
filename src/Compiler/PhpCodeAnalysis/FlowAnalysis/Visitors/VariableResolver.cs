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

        public override void DefaultVisit(IOperation operation)
        {
            if (operation is BoundPseudoConst)
            {
                //VisitPseudoConst((BoundPseudoConst)operation);
            }
            else if (operation is BoundGlobalConst)
            {
                //VisitGlobalConst((BoundGlobalConst)operation);
            }
            else if (operation is BoundIsSetEx)
            {
                ((BoundIsSetEx)operation).VarReferences.ForEach(Visit);
            }
            else if (operation is BoundUnset)
            {
                ((BoundUnset)operation).VarReferences.ForEach(Visit);
            }
            else if (operation is BoundListEx)
            {
                throw new NotImplementedException("list");  // TODO: visit vars
            }
            else
            {
                throw new NotImplementedException(operation.GetType().Name);
            }
        }

        public override void VisitCatch(ICatch operation)
        {
            base.VisitCatch(operation);
        }

        public override void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        {
            var vref = ((BoundVariableRef)operation);
            vref.Variable = _ctx.GetVar(vref.Name);
        }
    }
}