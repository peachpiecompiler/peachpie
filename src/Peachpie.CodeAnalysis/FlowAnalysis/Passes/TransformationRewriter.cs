using System;
using System.Collections.Generic;
using System.Text;
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    internal class TransformationRewriter : PhpOperationRewriter
    {
        public bool WasTransformationPerformed { get; private set; }

        public override BoundOperation VisitConditional(BoundConditionalEx x)
        {
            base.VisitConditional(x);

            // TODO: Check that there were no side effects of these expressions
            if (x.IfTrue != null
                && x.IfTrue.ConstantValue.HasValue && x.IfTrue.ConstantValue.Value is bool trueVal
                && x.IfFalse.ConstantValue.HasValue && x.IfFalse.ConstantValue.Value is bool falseVal)
            {
                if (trueVal && !falseVal)
                {
                    // A ? true : false => (bool)A
                    WasTransformationPerformed = true;
                    return new BoundUnaryEx(x.Condition, Devsense.PHP.Syntax.Ast.Operations.BoolCast)
                        .CopyContextFrom(x);
                }

                // TODO: Other possibilities
            }

            return x;
        }
    }
}
