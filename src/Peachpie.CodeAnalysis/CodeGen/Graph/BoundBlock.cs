using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Text;
using Pchp.CodeAnalysis.Symbols;
using Cci = Microsoft.Cci;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class BoundBlock : IGenerator
    {
        internal override void Emit(CodeGenerator cg)
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

            // in case of script, declare the script, functions and types
            if (cg.Routine is SourceGlobalMethodSymbol)
            {
                // <ctx>.OnInclude<TScript>()
                cg.EmitLoadContext();
                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.OnInclude_TScript.Symbol.Construct(cg.Routine.ContainingType));
            }

            //
            var locals = cg.Routine.LocalsTable;

            if (!cg.InitializedLocals)
            {
                // If it has unoptimized locals and they're not initilized externally -> need to initialize them
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

            // emit dummy locals showing indirect (unoptimized) locals in debugger's Watch and Locals window
            if (cg.HasUnoptimizedLocals && cg.EmitPdbSequencePoints && cg.IsDebug &&
                cg.CoreTypes.IndirectLocal.Symbol != null && // only if runtime provides this type (v1.0.0+)
                cg.IsGlobalScope == false && // don't bother in global code
                cg.Routine.LocalsTable.Count < 16) // emit IndirectLocal only there is not so many locals, otherwise it might use significant portion of stack
            {
                EmitIndirectLocalsDebugWatch(cg);
            }

            // remember array of arguments:
            if ((cg.Routine.Flags & FlowAnalysis.RoutineFlags.UsesArgs) != 0)
            {
                // <>args = cg.Emit_ArgsArray()
                var arrtype = cg.Emit_ArgsArray(cg.CoreTypes.PhpValue);
                cg.FunctionArgsArray = cg.GetTemporaryLocal(arrtype);
                cg.Builder.EmitLocalStore(cg.FunctionArgsArray);
            }

            // first brace sequence point
            var body = cg.Routine.Syntax.BodySpanOrInvalid();
            if (body.IsValid && cg.IsDebug)
            {
                cg.EmitSequencePoint(new Span(body.Start, 1));
            }

            // if generator method: emit switch table for continuation & change state to -1 (running)
            if (cg.Routine.IsGeneratorMethod())
            {
                EmitStateMachineMethodStart(cg);
            }

            //
            base.Emit(cg);
        }

        /// <summary>
        /// For debugger purposes; emits dummy variable that shows value of indirect locals.
        /// Using these dummy locals, user can see variables in Watch or Locals window even all the variables are stored indirectly in <see cref="CodeGenerator.LocalsPlaceOpt"/>.
        /// </summary>
        static void EmitIndirectLocalsDebugWatch(CodeGenerator cg)
        {
            Debug.Assert(cg.LocalsPlaceOpt != null);

            // variables/parameters initialization
            foreach (var loc in cg.Routine.LocalsTable.Variables)
            {
                if (loc.VariableKind == VariableKind.LocalTemporalVariable ||
                    loc.VariableKind == VariableKind.ThisParameter ||
                    loc is SuperglobalVariableReference || // VariableKind.GlobalVariable ?
                    string.IsNullOrEmpty(loc.Name) || // indirect variable
                    loc.Symbol as ILocalSymbolInternal == null)
                {
                    continue;
                }

                // Template: var {name} = new IndirectLocal(<locals>, name)

                // declare variable
                var def = cg.Builder.LocalSlotManager.DeclareLocal(
                        cg.CoreTypes.IndirectLocal.Symbol, loc.Symbol as ILocalSymbolInternal,
                        loc.Name, SynthesizedLocalKind.UserDefined,
                        Microsoft.CodeAnalysis.CodeGen.LocalDebugId.None, 0, LocalSlotConstraints.None, ImmutableArray<bool>.Empty, ImmutableArray<string>.Empty, false);

                cg.Builder.AddLocalToScope(def);

                var place = new LocalPlace(def);

                place.EmitStorePrepare(cg.Builder);

                // new IndirectLocal(locals : PhpArray, name : IntStringKey)
                loc.LoadIndirectLocal(cg);

                // store
                place.EmitStore(cg.Builder);
            }
        }

        private static void EmitStateMachineMethodStart(CodeGenerator cg)
        {
            // local <state> = g._state that is switched on (can't switch on remote field)
            cg.EmitGeneratorInstance();
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetGeneratorState_Generator);

            var stateLocal = cg.GeneratorStateLocal = cg.GetTemporaryLocal(cg.CoreTypes.Int32);
            cg.Builder.EmitLocalStore(stateLocal);

            // g._state = -1 : running
            cg.EmitGeneratorInstance();
            cg.Builder.EmitIntConstant(-1);
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorState_Generator_int);

            // create label for situation when state doesn't correspond to continuation: 0 -> didn't run to first yield
            var noContinuationLabel = new NamedLabel("noStateContinuation");

            // prepare jump table from yields
            var yieldExLabels = new List<KeyValuePair<ConstantValue, object>>();
            foreach (var yield in cg.Routine.ControlFlowGraph.Yields)
            {
                // labels have 1-based index (zero is reserved for run to first yield)
                // label object is the BoundYieldStatement itself, it is Marked at the proper place within its Emit method
                Debug.Assert(yield.YieldIndex >= 1);

                // yield statements inside "try" block are handled at beginning of try block itself (we cannot branch directly inside "try" from outside)
                var target = (object)yield.ContainingTryScopes.First?.Value ?? yield;

                // case YieldIndex: goto target;
                yieldExLabels.Add(new KeyValuePair<ConstantValue, object>(ConstantValue.Create(yield.YieldIndex), target));
            }

            // emit switch table that based on g._state jumps to appropriate continuation label
            cg.Builder.EmitIntegerSwitchJumpTable(yieldExLabels.ToArray(), noContinuationLabel, stateLocal, Cci.PrimitiveTypeCode.Int32);

            cg.Builder.MarkLabel(noContinuationLabel);
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
        /// Rethrow label.
        /// Marks code that rethrows <see cref="CodeGenerator.ExceptionToRethrowVariable"/>.
        /// </summary>
        private object _throwlbl;

        internal object GetReturnLabel()
        {
            return _retlbl ??= new NamedLabel("<return>");
        }

        internal object GetRethrowLabel()
        {
            return _throwlbl ??= new NamedLabel("<rethrow>");
        }

        /// <summary>
        /// Stores value from top of the evaluation stack to a temporary variable which will be returned from the exit block.
        /// </summary>
        internal void EmitTmpRet(CodeGenerator cg, TypeSymbol stack, bool yielding)
        {
            if (_rettmp == null && !cg.Routine.IsGeneratorMethod())
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
                cg.EmitConvert(stack, 0, (TypeSymbol)_rettmp.Type);
                cg.Builder.EmitLocalStore(_rettmp);
            }
            else
            {
                cg.EmitPop(stack);
            }

            //
            if (cg.ExtraFinallyBlock != null && !yielding)
            {
                // state = 1;
                // goto _finally;
                cg.Builder.EmitIntConstant((int)CodeGenerator.ExtraFinallyState.Return); // 1: return
                cg.ExtraFinallyStateVariable.EmitStore();
                cg.Builder.EmitBranch(ILOpCode.Br, cg.ExtraFinallyBlock);
                return;
            }

            //
            cg.Builder.EmitBranch(ILOpCode.Br, GetReturnLabel());
        }

        /// <summary>
        /// set generator state to -2 (closed)
        /// </summary>
        internal void EmitGeneratorEnd(CodeGenerator cg)
        {
            Debug.Assert(cg.Routine.IsGeneratorMethod());

            // g._state = -2 (closed): got to the end of the generator method
            cg.EmitGeneratorInstance();
            cg.Builder.EmitIntConstant(-2);
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorState_Generator_int);
        }

        internal override void Emit(CodeGenerator cg)
        {
            // note: ILBuider removes eventual unreachable .ret opcode

            // at the end of generator method:
            // set state to -2 (closed)
            if (cg.Routine.IsGeneratorMethod())
            {
                // g._state = -2 (closed): got to the end of the generator method
                EmitGeneratorEnd(cg);

                if (_retlbl != null)
                {
                    cg.Builder.MarkLabel(_retlbl);
                }

                cg.Builder.EmitRet(true);
            }
            else
            {
                //
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

            // rethrow <ExceptionToRethrowVariable>
            if (_throwlbl != null)
            {
                cg.Builder.MarkLabel(_throwlbl);

                if (cg.ExceptionToRethrowVariable != null)
                {
                    cg.ExceptionToRethrowVariable.EmitLoad(cg.Builder);
                }
                else
                {
                    cg.Builder.EmitNullConstant();
                }

                cg.Builder.EmitThrow(isRethrow: false); // rethrow's not working out of tryCatchFinally scope :/
                cg.Builder.AssertStackEmpty();
            }
        }
    }
}
