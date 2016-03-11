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
            if (this.Expression.Emit(il)
                .SpecialType != SpecialType.System_Void)
            {
                il.EmitOpCode(ILOpCode.Pop);
            }
        }
    }
}
