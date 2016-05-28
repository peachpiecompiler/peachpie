using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    partial class BoundEmptyStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            // nop
        }
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

    partial class BoundFunctionDeclStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(Pchp.Syntax.Text.Span.FromBounds(this.FunctionDecl.EntireDeclarationSpan.Start, this.FunctionDecl.HeadingEndPosition));

            // <ctx>.DeclareFunction ...
            cg.EmitDeclareFunction(this.Function);
        }
    }

    partial class BoundStaticVariableStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            foreach (var v in _variables)
            {
                Debug.Assert(v._holderPlace.TypeOpt != null);

                var getmethod = cg.CoreMethods.Context.GetStatic_T.Symbol.Construct(v._holderPlace.TypeOpt);
                var place = v.Place(cg.Builder);

                // Template: x = ctx.GetStatic<holder_x>()
                place.EmitStorePrepare(cg.Builder);

                cg.EmitLoadContext();
                cg.EmitCall(ILOpCode.Callvirt, getmethod);

                place.EmitStore(cg.Builder);

            }
        }
    }
}
