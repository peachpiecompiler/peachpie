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
        /// Enqueues next blocks to the worklist.
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
            // emit condition
            // .brfalse FalseBranch
            // GenerateScope(TrueBranch)

            throw new NotImplementedException();
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
