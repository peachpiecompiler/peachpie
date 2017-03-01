using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Text;

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

        /// <summary>
        /// Helper comparer defining order in which are blocks emitted if there is more than one in the queue.
        /// Can be used for optimizing branches heuristically.
        /// </summary>
        internal sealed class EmitOrderComparer : IComparer<BoundBlock>
        {
            // TODO: blocks emit priority

            public static readonly EmitOrderComparer Instance = new EmitOrderComparer();
            private EmitOrderComparer() { }
            public int Compare(BoundBlock x, BoundBlock y) => x.Ordinal - y.Ordinal;
        }
    }

    partial class StartBlock
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.Builder.DefineInitialHiddenSequencePoint();

            // first brace sequence point
            var body = cg.Routine.Syntax.BodySpanOrInvalid();
            if (body.IsValid && cg.IsDebug)
            {
                cg.EmitSequencePoint(new Span(body.Start, 1));
                cg.EmitOpCode(ILOpCode.Nop);
            }

            //
            if (cg.IsDebug)
            {
                if (cg.Routine.IsStatic)
                {
                    // Debug.Assert(<context> != null);
                    cg.EmitDebugAssertNotNull(cg.ContextPlaceOpt, "Context cannot be null.");
                }

                // TODO: emit parameters checks
            }

            //
            var locals = cg.Routine.LocalsTable;

            // in case of script, declare the script, functions and types
            if (cg.Routine is Symbols.SourceGlobalMethodSymbol)
            {
                // <ctx>.OnInclude<TScript>()
                cg.EmitLoadContext();
                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.OnInclude_TScript.Symbol.Construct(cg.Routine.ContainingType));

                // <ctx>.DeclareFunction()
                cg.Routine.ContainingFile.Functions
                    .Where(f => !f.IsConditional)
                    .ForEach(cg.EmitDeclareFunction);
                // <ctx>.DeclareType()
                cg.DeclaringCompilation.SourceSymbolCollection.GetTypes()
                    .OfType<Symbols.SourceTypeSymbol>()
                    .Where(t => !t.Syntax.IsConditional && t.ContainingFile == cg.Routine.ContainingFile && !t.IsAnonymousType)   // non conditional declaration within this file
                    .ForEach(cg.EmitDeclareType);
            }
            else
            {
                if (cg.HasUnoptimizedLocals)
                {
                    // <locals> = new PhpArray(HINTCOUNT)
                    cg.LocalsPlaceOpt.EmitStorePrepare(cg.Builder);
                    cg.Builder.EmitIntConstant(locals.Count);    // HINTCOUNT
                    cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpArray_int);
                    cg.LocalsPlaceOpt.EmitStore(cg.Builder);
                }
            }

            // variables/parameters initialization
            foreach (var loc in locals.Variables)
            {
                loc.EmitInit(cg);
            }

            //
            base.Emit(cg);
        }
    }

    partial class ExitBlock
    {
        /// <summary>
        /// Temporary local variable for return.
        /// </summary>
        private Microsoft.CodeAnalysis.CodeGen.LocalDefinition _rettmp;

        /// <summary>
        /// Return label.
        /// </summary>
        private object _retlbl;

        /// <summary>
        /// Stores value from top of the evaluation stack to a temporary variable which will be returned from the exit block.
        /// </summary>
        internal void EmitTmpRet(CodeGenerator cg, Symbols.TypeSymbol stack)
        {
            // lazy initialize
            if (_retlbl == null)
            {
                _retlbl = new NamedLabel("<return>");
            }

            if (_rettmp == null)
            {
                var rtype = cg.Routine.ReturnType;
                if (rtype.SpecialType != SpecialType.System_Void)
                {
                    _rettmp = cg.GetTemporaryLocal(rtype);
                }
            }

            // <rettmp> = <stack>;
            if (_rettmp != null)
            {
                cg.EmitConvert(stack, 0, (Symbols.TypeSymbol)_rettmp.Type);
                cg.Builder.EmitLocalStore(_rettmp);
                cg.Builder.EmitBranch(ILOpCode.Br, _retlbl);
            }
            else
            {
                cg.EmitPop(stack);
            }
        }

        internal override void Emit(CodeGenerator cg)
        {
            // note: ILBuider removes eventual unreachable .ret opcode

            if (_retlbl != null && _rettmp == null)
            {
                cg.Builder.MarkLabel(_retlbl);
            }

            // return <default>;
            cg.EmitRetDefault();
            cg.Builder.AssertStackEmpty();

            // return <rettemp>;
            if (_rettmp != null)
            {
                Debug.Assert(_retlbl != null);
                cg.Builder.MarkLabel(_retlbl);

                // note: _rettmp is always initialized since we branch to _retlbl only after storing to _rettmp

                cg.Builder.EmitLocalLoad(_rettmp);
                cg.Builder.EmitRet(false);
                cg.Builder.AssertStackEmpty();
            }
        }
    }
}
