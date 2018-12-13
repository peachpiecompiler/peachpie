using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Ast = Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    internal class TransformationRewriter : GraphRewriter
    {
        private readonly DelayedTransformations _delayedTransformations;
        private readonly SourceRoutineSymbol _routine;

        public int TransformationCount { get; private set; }

        public static bool TryTransform(DelayedTransformations delayedTransformations, SourceRoutineSymbol routine)
        {
            if (routine.ControlFlowGraph == null)
            {
                // abstract method
                return false;
            }

            //
            var rewriter = new TransformationRewriter(delayedTransformations, routine);
            var currentCFG = routine.ControlFlowGraph;
            var updatedCFG = (ControlFlowGraph)rewriter.VisitCFG(currentCFG);

            routine.ControlFlowGraph = updatedCFG;

            Debug.Assert((rewriter.TransformationCount != 0) == (updatedCFG != currentCFG)); // transformations <=> cfg updated                                                                                 //
            return updatedCFG != currentCFG;
        }

        private TransformationRewriter(DelayedTransformations delayedTransformations, SourceRoutineSymbol routine)
        {
            _delayedTransformations = delayedTransformations;
            _routine = routine;
        }

        protected override void OnVisitCFG(ControlFlowGraph x)
        {
            Debug.Assert(_routine.ControlFlowGraph == x);
        }

        private protected override void OnUnreachableRoutineFound(SourceRoutineSymbol routine)
        {
            _delayedTransformations.UnreachableRoutines.Add(routine);
        }

        private protected override void OnUnreachableTypeFound(SourceTypeSymbol type)
        {
            _delayedTransformations.UnreachableTypes.Add(type);
        }

        public override object VisitConditional(BoundConditionalEx x)
        {
            x = (BoundConditionalEx)base.VisitConditional(x);

            if (x.IfTrue != null) // otherwise it is (A ?: B) operator
            {
                if (x.Condition.ConstantValue.TryConvertToBool(out var condVal))
                {
                    TransformationCount++;
                    return (condVal ? x.IfTrue : x.IfFalse).WithAccess(x);
                }

                if (x.IfTrue.ConstantValue.IsBool(out bool trueVal) &&
                    x.IfFalse.ConstantValue.IsBool(out bool falseVal))
                {
                    if (trueVal && !falseVal)
                    {
                        // A ? true : false => (bool)A
                        TransformationCount++;
                        return new BoundUnaryEx(x.Condition, Ast.Operations.BoolCast).WithAccess(x);
                    }
                    else if (!trueVal && falseVal)
                    {
                        // A ? false : true => !A
                        TransformationCount++;
                        return new BoundUnaryEx(x.Condition, Ast.Operations.LogicNegation).WithAccess(x);
                    }
                }
            }

            return x;
        }

        public override object VisitBinaryExpression(BoundBinaryEx x)
        {
            // AND, OR:
            if (x.Operation == Ast.Operations.And ||
                x.Operation == Ast.Operations.Or)
            {
                if (x.Left.ConstantValue.TryConvertToBool(out var bleft))
                {
                    if (x.Operation == Ast.Operations.And)
                    {
                        TransformationCount++;
                        // TRUE && Right => Right
                        // FALSE && Right => FALSE
                        return bleft ? x.Right : x.Left;
                    }
                    else if (x.Operation == Ast.Operations.Or)
                    {
                        TransformationCount++;
                        // TRUE || Right => TRUE
                        // FALSE || Right => Right
                        return bleft ? x.Left : x.Right;
                    }
                }

                if (x.Right.ConstantValue.TryConvertToBool(out var bright))
                {
                    if (x.Operation == Ast.Operations.And && bright == true)
                    {
                        TransformationCount++;
                        return x.Left; // Left && TRUE => Left
                    }
                    else if (x.Operation == Ast.Operations.Or && bright == false)
                    {
                        TransformationCount++;
                        // Left || FALSE => Left
                        return x.Left;
                    }
                }
            }

            //
            return base.VisitBinaryExpression(x);
        }

        public override object VisitCFGConditionalEdge(ConditionalEdge x)
        {
            if (x.Condition.ConstantValue.TryConvertToBool(out bool condValue))
            {
                TransformationCount++;
                NotePossiblyUnreachable(condValue ? x.FalseTarget : x.TrueTarget);
                var target = condValue ? x.TrueTarget : x.FalseTarget;
                return new SimpleEdge((BoundBlock)Accept(target));
            }

            return base.VisitCFGConditionalEdge(x);
        }

        public override object VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            // dirname( __FILE__ ) -> __DIR__
            if (x.Name.NameValue == NameUtils.SpecialNames.dirname &&
                x.ArgumentsInSourceOrder.Length == 1 &&
                x.ArgumentsInSourceOrder[0].Value is BoundPseudoConst pc &&
                pc.ConstType == Devsense.PHP.Syntax.Ast.PseudoConstUse.Types.File)
            {
                TransformationCount++;
                return new BoundPseudoConst(Devsense.PHP.Syntax.Ast.PseudoConstUse.Types.Dir).WithAccess(x.Access);
            }

            //
            return base.VisitGlobalFunctionCall(x);
        }
    }
}
