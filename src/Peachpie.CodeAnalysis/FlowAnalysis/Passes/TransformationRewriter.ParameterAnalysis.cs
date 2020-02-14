using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.Utilities;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    partial class TransformationRewriter
    {
        /// <summary>
        /// The state denotes whether a potential modification of any parameter containing alias could have
        /// happened (e.g. by calling an external method).
        /// </summary>
        private enum ParameterAnalysisState
        {
            /// <summary>
            /// We haven't analysed this code.
            /// </summary>
            Unexplored = default,
            /// <summary>
            /// We have already analysed this code and haven't discovered any operation possibly modifying any alias
            /// outside the routine scope.
            /// </summary>
            Clean,
            /// <summary>
            /// Any parameter dereference encountered in this state on prevents its value passing from being skipped, because
            /// a possible alias modification might have happened.
            /// </summary>
            Dirty
        }

        /// <summary>
        /// Implements parameter value passing analysis using <see cref="ParameterAnalysisState"/>, retrieving information
        /// about on which parameters we don't need to call PassValue.
        /// </summary>
        private class ParameterAnalysis : AnalysisWalker<ParameterAnalysisState, VoidStruct>
        {
            #region Fields

            /// <summary>
            /// Records parameters which need to be deep copied and dealiased upon routine start.
            /// </summary>
            private BitMask64 _needPassValueParams;

            private FlowContext _flowContext;

            private Dictionary<BoundBlock, ParameterAnalysisState> _blockToStateMap = new Dictionary<BoundBlock, ParameterAnalysisState>();

            private DistinctQueue<BoundBlock> _worklist = new DistinctQueue<BoundBlock>(new BoundBlock.OrdinalComparer());

            #endregion

            #region Usage

            private ParameterAnalysis(FlowContext flowContext)
            {
                _flowContext = flowContext;
            }

            public static BitMask64 GetNeedPassValueParams(SourceRoutineSymbol routine)
            {
                if (routine.ParameterCount == 0)
                {
                    return default;
                }

                var cfg = routine.ControlFlowGraph;
                var analysis = new ParameterAnalysis(cfg.FlowContext);

                analysis._blockToStateMap[cfg.Start] = ParameterAnalysisState.Clean;
                analysis._worklist.Enqueue(cfg.Start);
                while (analysis._worklist.TryDequeue(out var block))
                {
                    analysis.Accept(block);
                }

                return analysis._needPassValueParams;
            }

            #endregion

            #region State handling

            protected override bool IsStateInitialized(ParameterAnalysisState state) => state != default;

            protected override bool AreStatesEqual(ParameterAnalysisState a, ParameterAnalysisState b) => a == b;

            protected override ParameterAnalysisState GetState(BoundBlock block) => _blockToStateMap.TryGetOrDefault(block);

            protected override void SetState(BoundBlock block, ParameterAnalysisState state) => _blockToStateMap[block] = state;

            protected override ParameterAnalysisState CloneState(ParameterAnalysisState state) => state;

            protected override ParameterAnalysisState MergeStates(ParameterAnalysisState a, ParameterAnalysisState b) => a > b ? a : b;

            protected override void SetStateUnknown(ref ParameterAnalysisState state) => state = ParameterAnalysisState.Dirty;

            protected override void EnqueueBlock(BoundBlock block) => _worklist.Enqueue(block);

            #endregion

            #region Visit expressions

            public override VoidStruct VisitArgument(BoundArgument x)
            {
                VariableHandle varindex;

                if (State != ParameterAnalysisState.Dirty
                    && x.Value is BoundVariableRef varRef
                    && varRef.Variable is ParameterReference
                    && varRef.Name.IsDirect
                    && !_flowContext.IsReference(varindex = _flowContext.GetVarIndex(varRef.Name.NameValue))
                    && !varRef.Access.MightChange)
                {
                    // Passing a parameter as an argument by value to another routine is a safe use which does not
                    // require it to be deeply copied (the called function will do it on its own if necessary)
                    return default;
                }
                else
                {
                    return base.VisitArgument(x);
                }
            }

            public override VoidStruct VisitVariableRef(BoundVariableRef x)
            {
                // Other usage than being passed as an argument to another function requires a parameter to be deeply copied
                if (!x.Name.IsDirect)
                {
                    // In the worst case, any variable can be targeted
                    _needPassValueParams.SetAll();
                }
                else
                {
                    var varindex = _flowContext.GetVarIndex(x.Name.NameValue);
                    if (!_flowContext.IsReference(varindex))
                    {
                        // Mark only the specific variable as possibly being changed
                        _needPassValueParams.Set(varindex);
                    }
                    else
                    {
                        // TODO: Mark only those that can be referenced
                        _needPassValueParams.SetAll();
                    }
                }

                return base.VisitVariableRef(x);
            }

            protected override VoidStruct VisitRoutineCall(BoundRoutineCall x)
            {
                // An external alias can be modified when the routine is actually called, after processing the arguments
                base.VisitRoutineCall(x);
                State = ParameterAnalysisState.Dirty;

                return default;
            }

            // Assert cannot modify any external alias, so just visit the arguments
            public override VoidStruct VisitAssert(BoundAssertEx x) => base.VisitRoutineCall(x);

            public override VoidStruct VisitConcat(BoundConcatEx x) => VisitStringConvertingArgs(x.ArgumentsInSourceOrder);

            public override VoidStruct VisitEcho(BoundEcho x) => VisitStringConvertingArgs(x.ArgumentsInSourceOrder);

            private VoidStruct VisitStringConvertingArgs(ImmutableArray<BoundArgument> args)
            {
                // Converting any object argument to string can cause a __toString call
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    arg.Accept(this);

                    var argTypeMask = arg.Value.TypeRefMask;
                    if (argTypeMask.IsAnyType || _flowContext.TypeRefContext.IsObject(argTypeMask))
                    {
                        State = ParameterAnalysisState.Dirty;
                    }
                }

                return default;
            }

            protected override void Visit(BoundUnaryEx x, ConditionBranch branch)
            {
                base.Visit(x, branch);

                // Cloning causes calling __clone with arbitrary code
                if (x.Operation == Devsense.PHP.Syntax.Ast.Operations.Clone)
                {
                    State = ParameterAnalysisState.Dirty;
                }
            }

            public override VoidStruct VisitConversion(BoundConversionEx x)
            {
                base.VisitConversion(x);

                // Custom conversion of an object can call any external code, conversion to string can call __toString
                var opTypeMask = x.Operand.TypeRefMask;
                var typeRefCtx = _flowContext.TypeRefContext;
                if (opTypeMask.IsAnyType || typeRefCtx.IsObject(opTypeMask)
                    && (x.Conversion.IsUserDefined || typeRefCtx.IsAString(x.TargetType.GetTypeRefMask(typeRefCtx))))
                {
                    State = ParameterAnalysisState.Dirty;
                }

                return default;
            }

            public override VoidStruct VisitFieldRef(BoundFieldRef x)
            {
                base.VisitFieldRef(x);
                State = ParameterAnalysisState.Dirty;

                return default;
            }

            public override VoidStruct VisitArrayItem(BoundArrayItemEx x)
            {
                base.VisitArrayItem(x);
                State = ParameterAnalysisState.Dirty;

                return default;
            }

            public override VoidStruct VisitOffsetExists(BoundOffsetExists x)
            {
                base.VisitOffsetExists(x);
                State = ParameterAnalysisState.Dirty;

                return default;
            }

            public override VoidStruct VisitEval(BoundEvalEx x)
            {
                // As anything can happen in eval, force value passing of all parameters
                base.VisitEval(x);
                State = ParameterAnalysisState.Dirty;
                _needPassValueParams.SetAll();

                return default;
            }

            #endregion
        }
    }
}
