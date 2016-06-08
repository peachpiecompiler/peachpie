using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Syntax.AST;

namespace Pchp.CodeAnalysis.FlowAnalysis.Visitors
{
    class GraphWalker : GraphVisitor
    {
        readonly int _color;

        private GraphWalker(int color, OperationVisitor opvisitor)
            : base(opvisitor)
        {
            _color = color;
        }

        public static void Walk(ControlFlowGraph cfg, OperationVisitor opvisitor)
        {
            var walker = new GraphWalker(cfg.NewColor(), opvisitor);
            walker.VisitCFG(cfg);
        }

        public override void VisitCFGCatchBlock(CatchBlock x)
        {


            base.VisitCFGCatchBlock(x);
        }

        public override void VisitCFGBlock(BoundBlock x)
        {
            if (x.Tag != _color)
            {
                x.Tag = _color;

                base.VisitCFGBlock(x);
            }
        }
    }
}
