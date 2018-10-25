using System;
using System.Collections.Generic;
using System.Text;
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    internal class TransformationRewriter : PhpOperationRewriter
    {
        public int TransformationCount { get; private set; }

        public override BoundOperation VisitConditional(BoundConditionalEx x)
        {
            x = (BoundConditionalEx)base.VisitConditional(x);

            if (x.IfTrue != null
                && x.IfTrue.ConstantValue.IsBool(out bool trueVal)
                && x.IfFalse.ConstantValue.IsBool(out bool falseVal))
            {
                if (trueVal && !falseVal)
                {
                    // A ? true : false => (bool)A
                    TransformationCount++;
                    return
                        new BoundUnaryEx(x.Condition, Devsense.PHP.Syntax.Ast.Operations.BoolCast)
                        .WithContext(x);
                }

                // TODO: Other possibilities
            }

            return x;
        }
    }
}
