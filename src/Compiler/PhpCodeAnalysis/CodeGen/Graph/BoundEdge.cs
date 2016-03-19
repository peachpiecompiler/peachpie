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
        internal abstract void Generate(CodeGenerator il);

        void IGenerator.Generate(CodeGenerator il) => this.Generate(il);
    }

    partial class SimpleEdge
    {
        internal override void Generate(CodeGenerator il)
        {
            il.Scope.ContinueWith(NextBlock);
        }
    }

    partial class ConditionalEdge
    {
        internal override void Generate(CodeGenerator il)
        {
            Contract.ThrowIfNull(Condition);

            if (IsLoop) // perf
            {
                il.Builder.EmitBranch(ILOpCode.Br, this.Condition);

                // {
                il.GenerateScope(TrueTarget, NextBlock.Ordinal);
                // }

                // if (Condition)
                il.Builder.MarkLabel(this.Condition);
                il.EmitConvertToBool(this.Condition.Emit(il), this.Condition.TypeRefMask);
                il.Builder.EmitBranch(ILOpCode.Brtrue, TrueTarget);
            }
            else
            {
                // if (Condition)
                il.EmitConvertToBool(this.Condition.Emit(il), this.Condition.TypeRefMask);
                il.Builder.EmitBranch(ILOpCode.Brfalse, FalseTarget);

                // {
                il.GenerateScope(TrueTarget, NextBlock.Ordinal);
                // }
            }

            il.Scope.ContinueWith(FalseTarget);
        }
    }

    partial class TryCatchEdge
    {
        internal override void Generate(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }

    partial class ForeachEnumereeEdge
    {
        internal override void Generate(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }

    partial class ForeachMoveNextEdge
    {
        internal override void Generate(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }

    partial class SwitchEdge
    {
        internal override void Generate(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }
}
