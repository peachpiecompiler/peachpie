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
        internal virtual void Emit(CodeGenerator il)
        {
            // emit contained statements
            _statements.ForEach(il.Generate);

            //
            il.Generate(this.NextEdge);
        }

        void IGenerator.Generate(CodeGenerator il) => Emit(il);
    }

    partial class StartBlock
    {
        internal override void Emit(CodeGenerator il)
        {
            if (il.IsDebug)
            {
                // emit Debug.Assert(<context> != null);
                // emit parameters checks
            }

            // parameters initialization
            // ...

            //
            base.Emit(il);
        }
    }

    partial class ExitBlock
    {
        internal override void Emit(CodeGenerator il)
        {
            // return default(RETURN_TYPE);

            // <DEBUG>
            
            il.IL.EmitNullConstant();
            il.IL.EmitRet(false);
            
            // </DEBUG>
        }
    }
}
