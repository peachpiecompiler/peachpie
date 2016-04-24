using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class BoundBlock : IGenerator
    {
        internal virtual void Emit(CodeGenerator cg)
        {
            // emit contained statements
            _statements.ForEach(cg.Generate);

            //
            cg.Generate(this.NextEdge);
        }

        void IGenerator.Generate(CodeGenerator cg) => Emit(cg);
    }

    partial class StartBlock
    {
        internal override void Emit(CodeGenerator cg)
        {
            //
            cg.Builder.DefineInitialHiddenSequencePoint();

            //
            if (cg.IsDebug)
            {
                // emit Debug.Assert(<context> != null);
                // emit parameters checks
            }

            // variables/parameters initialization
            foreach (var loc in cg.Routine.ControlFlowGraph.FlowContext.Locals)
            {
                loc.EmitInit(cg);
            }

            //
            base.Emit(cg);
        }
    }

    partial class ExitBlock
    {
        internal override void Emit(CodeGenerator cg)
        {
            // note: ILBuider removes eventual unreachable .ret opcode

            // return <default>;
            cg.EmitReturnDefault();
            cg.Builder.AssertStackEmpty();
        }
    }
}
