using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class BoundBlock : IEmittable
    {
        internal virtual void Emit(CodeGenerator il)
        {
            // emit contained statements
            _statements.ForEach(il.Emit);
        }

        void IEmittable.Emit(CodeGenerator il) => Emit(il);
    }
}
