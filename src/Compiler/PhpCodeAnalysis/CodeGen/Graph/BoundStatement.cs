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
        internal virtual void Emit(CodeGenerator cg)
        {
            throw new NotImplementedException();
        }

        void IGenerator.Generate(CodeGenerator cg) => Emit(cg);
    }

    partial class BoundExpressionStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);
            cg.EmitPop(this.Expression.Emit(cg));
        }
    }

    partial class BoundReturnStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);

            //
            if (this.Returned == null)
            {
                if (cg.Routine.ReturnsVoid)
                {
                    // return;
                    cg.EmitRet(true);
                }
                else
                {
                    // return <default>;
                    cg.EmitRetDefault();
                }
            }
            else
            {
                if (cg.Routine.ReturnsVoid)
                {
                    // <expr>;
                    // return;
                    cg.EmitPop(this.Returned.Emit(cg));
                    cg.EmitRet(true);
                }
                else
                {
                    // return (T)<expr>;
                    cg.EmitConvert(this.Returned, cg.Routine.ReturnType);
                    cg.EmitRet(false);
                }
            }
        }
    }
}
