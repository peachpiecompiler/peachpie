using Microsoft.CodeAnalysis;
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

            var return_type = il.Routine.ReturnType;
            switch (return_type.SpecialType)
            {
                case SpecialType.System_Void:
                    break;
                case SpecialType.System_Double:
                    il.Builder.EmitDoubleConstant(0.0);
                    break;
                case SpecialType.System_Int64:
                    il.Builder.EmitLongConstant(0);
                    break;
                case SpecialType.System_Boolean:
                    il.Builder.EmitBoolConstant(false);
                    break;
                case SpecialType.System_String:
                    il.Builder.EmitStringConstant(string.Empty);
                    break;
                default:
                    if (return_type.IsReferenceType)
                    {
                        il.Builder.EmitNullConstant();
                    }
                    else
                    {
                        throw new NotImplementedException();    // default(T)
                    }
                    break;
            }

            //
            il.Builder.EmitRet(return_type.SpecialType == SpecialType.System_Void);

            //
            il.Builder.AssertStackEmpty();
        }
    }
}
