using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    partial class BoundExpression
    {
        internal virtual TypeSymbol Emit(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }

    partial class BoundBinaryEx
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            var ltype = Left.Emit(il);
            var rtype = Right.Emit(il);

            if (UsesOperatorMethod)
            {
                throw new NotImplementedException();    // call this.Operator(ltype, rtype)
            }

            switch (this.BinaryOperationKind)
            {
                default:
                    throw new NotImplementedException();
            }
        }
    }

    partial class BoundLiteral
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }

    partial class BoundVariableRef
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            if (this.Variable == null)
                throw new InvalidOperationException(); // variable was not resolved

            if (Access == AccessType.Read)
            {
                this.Variable.GetPlace(il.IL).EmitLoad(il.IL);
            }

            throw new NotImplementedException();
        }
    }
}
