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
            if (x.IfTrue.ConstantValue.TryConvertToBool(out bool trueVal) && x.IfFalse.ConstantValue.TryConvertToBool(out bool falseVal))
            {
                if (trueVal && !falseVal)
                {
                    // A ? true : false => (bool)A
                    WasTransformationPerformed = true;
                    return new BoundUnaryEx(x.Condition, Devsense.PHP.Syntax.Ast.Operations.BoolCast);
                }

                // TODO: Other possibilities
            }

            return x;
        }
    }
}
