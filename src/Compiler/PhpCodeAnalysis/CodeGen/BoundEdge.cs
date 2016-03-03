using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class Edge : IEmittable
    {
        internal abstract void Emit(CodeGenerator il);

        void IEmittable.Emit(CodeGenerator il) => this.Emit(il);
    }

    partial class SimpleEdge
    {
        internal override void Emit(CodeGenerator il)
        {
            if (il.IsGenerated(this.Target))
            {
                // target was already emitte,
                // simply branch there
                il.IL.EmitBranch(ILOpCode.Br, this.Target);
            }
            else
            {
                // continue generating the next block
                il.Generate(this.Target);
            }
        }
    }

    partial class ConditionalEdge
    {
        internal override void Emit(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }

    partial class TryCatchEdge
    {
        internal override void Emit(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }

    partial class ForeachEnumereeEdge
    {
        internal override void Emit(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }

    partial class ForeachMoveNextEdge
    {
        internal override void Emit(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }

    partial class SwitchEdge
    {
        internal override void Emit(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }
}
