using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class Edge : IGenerator
    {
        /// <summary>
        /// Generates or enqueues next blocks to the worklist.
        /// </summary>
        internal abstract void Generate(CodeGenerator cg);

        void IGenerator.Generate(CodeGenerator cg) => this.Generate(cg);
    }

    partial class SimpleEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            cg.Scope.ContinueWith(NextBlock);
        }
    }

    partial class ConditionalEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            Contract.ThrowIfNull(Condition);

            if (IsLoop) // perf
            {
                cg.Builder.EmitBranch(ILOpCode.Br, this.Condition);

                // {
                cg.GenerateScope(TrueTarget, NextBlock.Ordinal);
                // }

                // if (Condition)
                cg.Builder.MarkLabel(this.Condition);
                cg.EmitConvert(this.Condition, cg.CoreTypes.Boolean);
                cg.Builder.EmitBranch(ILOpCode.Brtrue, TrueTarget);
            }
            else
            {
                // if (Condition)
                cg.EmitConvert(this.Condition, cg.CoreTypes.Boolean);
                cg.Builder.EmitBranch(ILOpCode.Brfalse, FalseTarget);

                // {
                cg.GenerateScope(TrueTarget, NextBlock.Ordinal);
                // }
            }

            cg.Scope.ContinueWith(FalseTarget);
        }
    }

    partial class TryCatchEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            throw new NotImplementedException();
        }
    }

    partial class ForeachEnumereeEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            throw new NotImplementedException();
        }
    }

    partial class ForeachMoveNextEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            throw new NotImplementedException();
        }
    }

    partial class SwitchEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            throw new NotImplementedException();
        }
    }
}
