using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Pchp.CodeAnalysis.Semantics
{
    public class PhpOperationRewriter : PhpOperationVisitor<BoundOperation>
    {
        #region Helper methods

        private ImmutableArray<T> VisitImmutableArray<T>(ImmutableArray<T> arr) where T : BoundOperation, IPhpOperation
        {
            ImmutableArray<T>.Builder alternate = null;
            for (int i = 0; i < arr.Length; i++)
            {
                var orig = arr[i];
                var visited = orig?.Accept(this);

                if (visited != orig)
                {
                    if (alternate == null)
                    {
                        alternate = arr.ToBuilder();
                    }
                    alternate[i] = (T)visited;
                }
            }

            return alternate?.MoveToImmutable() ?? arr;
        }

        private ImmutableArray<KeyValuePair<T1, T2>> VisitImmutableArrayPairs<T1, T2>(ImmutableArray<KeyValuePair<T1, T2>> arr)
            where T1 : BoundOperation, IPhpOperation
            where T2 : BoundOperation, IPhpOperation
        {
            ImmutableArray<KeyValuePair<T1, T2>>.Builder alternate = null;
            for (int i = 0; i < arr.Length; i++)
            {
                var orig = arr[i];
                var visitedKey = orig.Key?.Accept(this);
                var visitedValue = orig.Value?.Accept(this);

                if (visitedKey != orig.Key || visitedValue != orig.Value)
                {
                    if (alternate == null)
                    {
                        alternate = arr.ToBuilder();
                    }
                    alternate[i] = new KeyValuePair<T1, T2>((T1)visitedKey, (T2)visitedValue);
                }
            }

            return alternate?.MoveToImmutable() ?? arr;
        }

        private void VisitAndUpdate<T>(T value, Action<T> setter) where T : BoundOperation, IPhpOperation
        {
            var visited = Accept(value);
            if (visited != value)
            {
                setter((T)visited);
            }
        }

        private void VisitAndUpdate<T>(ImmutableArray<T> value, Action<ImmutableArray<T>> setter) where T : BoundOperation, IPhpOperation
        {
            var visited = VisitImmutableArray(value);
            if (visited != value)
            {
                setter((ImmutableArray<T>)visited);
            }
        }

        private void VisitAndUpdate<T1, T2>(ImmutableArray<KeyValuePair<T1, T2>> value, Action<ImmutableArray<KeyValuePair<T1, T2>>> setter)
            where T1 : BoundOperation, IPhpOperation
            where T2 : BoundOperation, IPhpOperation
        {
            var visited = VisitImmutableArrayPairs(value);
            if (visited != value)
            {
                setter((ImmutableArray<KeyValuePair<T1, T2>>)visited);
            }
        }

        #endregion

        #region Expressions

        protected override BoundOperation VisitRoutineCall(BoundRoutineCall x)
        {
            VisitAndUpdate(x.ArgumentsInSourceOrder, v => x.ArgumentsInSourceOrder = v);

            return x;
        }

        public override BoundOperation VisitLiteral(BoundLiteral x)
        {
            return x;
        }

        public override BoundOperation VisitArgument(BoundArgument x)
        {
            VisitAndUpdate(x.Value, v => x.Value = v);

            return x;
        }

        public override BoundOperation VisitTypeRef(BoundTypeRef x)
        {
            // TODO
            return default;
        }

        public override BoundOperation VisitMultipleTypeRef(BoundMultipleTypeRef x)
        {
            // TODO
            return default;
        }

        public override BoundOperation VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            return VisitRoutineCall(x);
        }

        public override BoundOperation VisitInstanceFunctionCall(BoundInstanceFunctionCall x)
        {
            VisitAndUpdate(x.Instance, x.SetInstance);
            return VisitRoutineCall(x);
        }

        public override BoundOperation VisitStaticFunctionCall(BoundStaticFunctionCall x)
        {
            VisitTypeRef(x.TypeRef);    // TODO
            return VisitRoutineCall(x);
        }

        public override BoundOperation VisitEcho(BoundEcho x)
        {
            return VisitRoutineCall(x);
        }

        public override BoundOperation VisitConcat(BoundConcatEx x)
        {
            return VisitRoutineCall(x);
        }

        public override BoundOperation VisitNew(BoundNewEx x)
        {
            VisitTypeRef(x.TypeRef);    // TODO
            return VisitRoutineCall(x);
        }

        public override BoundOperation VisitInclude(BoundIncludeEx x)
        {
            return VisitRoutineCall(x);
        }

        public override BoundOperation VisitExit(BoundExitEx x)
        {
            return VisitRoutineCall(x);
        }

        public override BoundOperation VisitAssert(BoundAssertEx x)
        {
            return VisitRoutineCall(x);
        }

        public override BoundOperation VisitBinaryExpression(BoundBinaryEx x)
        {
            VisitAndUpdate(x.Left, v => x.Left = v);
            VisitAndUpdate(x.Right, v => x.Right = v);

            return x;
        }

        public override BoundOperation VisitUnaryExpression(BoundUnaryEx x)
        {
            VisitAndUpdate(x.Operand, v => x.Operand = v);

            return x;
        }

        public override BoundOperation VisitIncDec(BoundIncDecEx x)
        {
            VisitAndUpdate(x.Target, v => x.Target = v);
            VisitAndUpdate(x.Value, v => x.Value = v);      // TODO: Does it make sense?

            return x;
        }

        public override BoundOperation VisitConditional(BoundConditionalEx x)
        {
            VisitAndUpdate(x.Condition, v => x.Condition = v);
            VisitAndUpdate(x.IfTrue, v => x.IfTrue = v);
            VisitAndUpdate(x.IfFalse, v => x.IfFalse = v);

            return x;
        }

        public override BoundOperation VisitAssign(BoundAssignEx x)
        {
            VisitAndUpdate(x.Target, v => x.Target = v);
            VisitAndUpdate(x.Value, v => x.Value = v);

            return x;
        }

        public override BoundOperation VisitCompoundAssign(BoundCompoundAssignEx x)
        {
            VisitAndUpdate(x.Target, v => x.Target = v);
            VisitAndUpdate(x.Value, v => x.Value = v);

            return x;
        }

        public override BoundOperation VisitVariableRef(BoundVariableRef x)
        {
            return x;
        }

        public override BoundOperation VisitTemporalVariableRef(BoundTemporalVariableRef x)
        {
            // BoundSynthesizedVariableRef is based solely on BoundVariableRef so far 
            return VisitVariableRef(x);
        }

        public override BoundOperation VisitList(BoundListEx x)
        {
            VisitAndUpdate(x.Items, v => x.Items = v);

            return x;
        }

        public override BoundOperation VisitFieldRef(BoundFieldRef x)
        {
            VisitTypeRef(x.ContainingType);                     // TODO
            VisitAndUpdate(x.Instance, v => x.Instance = v);
            VisitAndUpdate(x.FieldName.NameExpression, v => x.FieldName = new BoundVariableName(v));

            return x;
        }

        public override BoundOperation VisitArray(BoundArrayEx x)
        {
            VisitAndUpdate(x.Items, v => x.Items = v);

            return x;
        }

        public override BoundOperation VisitArrayItem(BoundArrayItemEx x)
        {
            VisitAndUpdate(x.Array, v => x.Array = v);
            VisitAndUpdate(x.Index, v => x.Index = v);

            return x;
        }

        public override BoundOperation VisitInstanceOf(BoundInstanceOfEx x)
        {
            VisitAndUpdate(x.Operand, v => x.Operand = v);
            VisitTypeRef(x.AsType);                             // TODO

            return x;
        }

        public override BoundOperation VisitGlobalConstUse(BoundGlobalConst x)
        {
            return x;
        }

        public override BoundOperation VisitGlobalConstDecl(BoundGlobalConstDeclStatement x)
        {
            VisitAndUpdate(x.Value, v => x.Value = v);

            return x;
        }

        public override BoundOperation VisitPseudoConstUse(BoundPseudoConst x)
        {
            return x;
        }

        public override BoundOperation VisitPseudoClassConstUse(BoundPseudoClassConst x)
        {
            VisitTypeRef(x.TargetType);     // TODO

            return x;
        }

        public override BoundOperation VisitIsEmpty(BoundIsEmptyEx x)
        {
            VisitAndUpdate(x.Operand, v => x.Operand = v);

            return x;
        }

        public override BoundOperation VisitIsSet(BoundIsSetEx x)
        {
            VisitAndUpdate(x.VarReference, v => x.VarReference = v);

            return x;
        }

        public override BoundOperation VisitLambda(BoundLambda x)
        {
            return x;
        }

        public override BoundOperation VisitEval(BoundEvalEx x)
        {
            VisitAndUpdate(x.CodeExpression, v => x.CodeExpression = v);

            return x;
        }


        public override BoundOperation VisitYieldEx(BoundYieldEx x)
        {
            return x;
        }

        public override BoundOperation VisitYieldFromEx(BoundYieldFromEx x)
        {
            VisitAndUpdate(x.Operand, v => x.Operand = v);

            return x;
        }

        #endregion

        #region Statements

        public override BoundOperation VisitUnset(BoundUnset x)
        {
            VisitAndUpdate(x.Variable, v => x.Variable = v);

            return x;
        }

        public override BoundOperation VisitEmptyStatement(BoundEmptyStatement x)
        {
            return x;
        }

        public override BoundOperation VisitBlockStatement(Graph.BoundBlock x)
        {
            for (int i = 0; i < x.Statements.Count; i++)
            {
                VisitAndUpdate(x.Statements[i], v => x.Statements[i] = v);
            }

            return x;
        }

        public override BoundOperation VisitExpressionStatement(BoundExpressionStatement x)
        {
            VisitAndUpdate(x.Expression, v => x.Expression = v);

            return x;
        }

        public override BoundOperation VisitReturn(BoundReturnStatement x)
        {
            VisitAndUpdate(x.Returned, v => x.Returned = v);

            return x;
        }

        public override BoundOperation VisitThrow(BoundThrowStatement x)
        {
            VisitAndUpdate(x.Thrown, v => x.Thrown = v);

            return x;
        }

        public override BoundOperation VisitFunctionDeclaration(BoundFunctionDeclStatement x)
        {
            return x;
        }

        public override BoundOperation VisitTypeDeclaration(BoundTypeDeclStatement x)
        {
            return x;
        }

        public override BoundOperation VisitGlobalStatement(BoundGlobalVariableStatement x)
        {
            VisitAndUpdate(x.Variable, v => x.Variable = v);

            return x;
        }

        public override BoundOperation VisitStaticStatement(BoundStaticVariableStatement x)
        {
            return x;
        }

        public override BoundOperation VisitYieldStatement(BoundYieldStatement x)
        {
            VisitAndUpdate(x.YieldedValue, v => x.YieldedValue = v);
            VisitAndUpdate(x.YieldedKey, v => x.YieldedKey = v);

            return x;
        }

        public override BoundOperation VisitDeclareStatement(BoundDeclareStatement x)
        {
            return x;
        }

        #endregion
    }
}
