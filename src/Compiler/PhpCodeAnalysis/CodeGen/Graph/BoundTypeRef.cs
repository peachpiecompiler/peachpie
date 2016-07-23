using Pchp.CodeAnalysis.CodeGen;
using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    partial class BoundTypeRef
    {
        /// <summary>
        /// Emits name of bound type.
        /// </summary>
        /// <param name="cg"></param>
        internal void EmitClassName(CodeGenerator cg)
        {
            if (TypeExpression != null)
            {
                cg.EmitConvert(TypeExpression, cg.CoreTypes.String);
            }
            else
            {
                Debug.Assert(_typeRef is DirectTypeRef);

                if (_typeRef is PrimitiveTypeRef)
                {
                    throw new InvalidOperationException();
                }

                else if (_typeRef is DirectTypeRef)
                {
                    var classname = ((DirectTypeRef)_typeRef).ClassName;
                    cg.Builder.EmitStringConstant(classname.ToString());
                }

                else
                {
                    throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(_typeRef);
                }
            }
        }
    }
}
