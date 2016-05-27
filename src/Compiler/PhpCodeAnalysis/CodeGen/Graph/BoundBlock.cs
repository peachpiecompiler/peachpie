using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class BoundBlock : IGenerator
    {
        internal virtual void Emit(CodeGenerator cg)
        {
            // emit contained statements
            if (_statements.Count != 0)
            {
                _statements.ForEach(cg.Generate);
            }

            //
            cg.Generate(this.NextEdge);
        }

        void IGenerator.Generate(CodeGenerator cg) => Emit(cg);
    }

    partial class StartBlock
    {
        internal override void Emit(CodeGenerator cg)
        {
            // first brace sequence point
            var body = cg.Routine.Syntax.BodySpanOrInvalid();
            if (body.IsValid && cg.IsDebug)
            {
                cg.EmitSequencePoint(new Syntax.Text.Span(body.Start, 1));
                cg.EmitOpCode(ILOpCode.Nop);
            }
            else
            {
                cg.Builder.DefineInitialHiddenSequencePoint();
            }

            //
            if (cg.IsDebug)
            {
                // emit Debug.Assert(<context> != null);
                // emit parameters checks
            }

            //
            var locals = cg.Routine.ControlFlowGraph.FlowContext.Locals;

            // in case of script, declare the script, functions and types
            if (cg.Routine is Symbols.SourceGlobalMethodSymbol)
            {
                // <ctx>.OnInclude<TScript>()
                cg.EmitLoadContext();
                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.OnInclude_TScript.Symbol.Construct(cg.Routine.ContainingType));

                // <ctx>.DeclareFunction()
                cg.Routine.ContainingFile.Functions.Where(f => !f.IsConditional).ForEach(cg.EmitDeclareFunction);
            }
            else
            {
                if (cg.HasUnoptimizedLocals)
                {
                    // <locals> = new PhpArray(HINTCOUNT)
                    cg.LocalsPlaceOpt.EmitStorePrepare(cg.Builder);
                    cg.Builder.EmitIntConstant(locals.Length);    // HINTCOUNT
                    cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpArray_int);
                    cg.LocalsPlaceOpt.EmitStore(cg.Builder);
                }
            }

            // variables/parameters initialization
            foreach (var loc in locals)
            {
                loc.EmitInit(cg);
            }

            //
            base.Emit(cg);
        }
    }

    partial class ExitBlock
    {
        internal override void Emit(CodeGenerator cg)
        {
            // note: ILBuider removes eventual unreachable .ret opcode

            // return <default>;
            cg.EmitRetDefault();
            cg.Builder.AssertStackEmpty();
        }
    }
}
