using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    partial class BoundStatement : IGenerator
    {
        internal virtual void Emit(CodeGenerator il)
        {
            throw new NotImplementedException();
        }

        void IGenerator.Generate(CodeGenerator il) => Emit(il);
    }

    partial class BoundExpressionStatement
    {
        internal override void Emit(CodeGenerator il)
        {
            il.EmitPop(this.Expression.Emit(il));
        }
    }

    partial class BoundReturnStatement
    {
        internal override void Emit(CodeGenerator il)
        {
            if (this.Returned == null)
            {
                if (il.Routine.ReturnsVoid)
                {
                    // return;
                    il.Builder.EmitRet(true);
                }
                else
                {
                    // return <default>;
                    il.EmitReturnDefault();
                }
            }
            else
            {
                if (il.Routine.ReturnsVoid)
                {
                    // <expr>;
                    // return;
                    il.EmitPop(this.Returned.Emit(il));
                }
                else
                {
                    // return (T)<expr>;
                    il.EmitConvert(this.Returned, il.Routine.ReturnType);
                    il.Builder.EmitRet(false);
                }
            }
        }
    }
}
