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

        #endregion

        #region Expressions

        protected override BoundOperation DefaultVisitOperation(BoundOperation x)
        {
            return x;
        }

        protected override BoundOperation VisitRoutineCall(BoundRoutineCall x)
        {
            // It must be updated in the visits of non-abstract subclassess
            return x;
        }

        public override BoundOperation VisitLiteral(BoundLiteral x)
        {
            return x;
        }

        public override BoundOperation VisitArgument(BoundArgument x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Value),
                x.ArgumentKind);
        }

        public override BoundOperation VisitTypeRef(BoundTypeRef x)
        {
            return x.Update(
                (BoundExpression)Accept(x.TypeExpression),
                x.TypeRef,
                x.ObjectTypeInfoSemantic,
                x.HasClassNameRestriction); ;
        }

        public override BoundOperation VisitMultipleTypeRef(BoundMultipleTypeRef x)
        {
            var typeRefs = VisitImmutableArray(x.BoundTypes);
            if (typeRefs == x.BoundTypes)
            {
                return x;
            }
            else if (typeRefs.Length == 1)
            {
                return typeRefs[0];
            }
            else
            {
                return x.Update(
                    typeRefs,
                    x.TypeRef,
                    x.ObjectTypeInfoSemantic,
                    x.HasClassNameRestriction);
            }
        }

        public override BoundOperation VisitRoutineName(BoundRoutineName x)
        {
            return x.Update(
                x.NameValue,
                (BoundExpression)Accept(x.NameExpression));
        }

        public override BoundOperation VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            return x.Update(
                (BoundRoutineName)Accept(x.Name),
                x.NameOpt,
                VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override BoundOperation VisitInstanceFunctionCall(BoundInstanceFunctionCall x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Instance),
                (BoundRoutineName)Accept(x.Name),
                VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override BoundOperation VisitStaticFunctionCall(BoundStaticFunctionCall x)
        {
            return x.Update(
                (BoundTypeRef)Accept(x.TypeRef),
                (BoundRoutineName)Accept(x.Name),
                VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override BoundOperation VisitEcho(BoundEcho x)
        {
            return x.Update(VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override BoundOperation VisitConcat(BoundConcatEx x)
        {
            return x.Update(VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override BoundOperation VisitNew(BoundNewEx x)
        {
            return x.Update(
                (BoundTypeRef)Accept(x.TypeRef),
                VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override BoundOperation VisitInclude(BoundIncludeEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.ArgumentsInSourceOrder[0].Value),
                x.InclusionType);
        }

        public override BoundOperation VisitExit(BoundExitEx x)
        {
            return x.Update(VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override BoundOperation VisitAssert(BoundAssertEx x)
        {
            return x.Update(VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override BoundOperation VisitBinaryExpression(BoundBinaryEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Left),
                (BoundExpression)Accept(x.Right),
                x.Operation);
        }

        public override BoundOperation VisitUnaryExpression(BoundUnaryEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Operand),
                x.Operation);
        }

        public override BoundOperation VisitIncDec(BoundIncDecEx x)
        {
            return x.Update(
                (BoundReferenceExpression)Accept(x.Target),
                x.IsIncrement,
                x.IsPostfix);
        }

        public override BoundOperation VisitConditional(BoundConditionalEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Condition),
                (BoundExpression)Accept(x.IfTrue),
                (BoundExpression)Accept(x.IfFalse));
        }

        public override BoundOperation VisitAssign(BoundAssignEx x)
        {
            return x.Update(
                (BoundReferenceExpression)Accept(x.Target),
                (BoundExpression)Accept(x.Value));
        }

        public override BoundOperation VisitCompoundAssign(BoundCompoundAssignEx x)
        {
            return x.Update(
                (BoundReferenceExpression)Accept(x.Target),
                (BoundExpression)Accept(x.Value));
        }

        public override BoundOperation VisitVariableName(BoundVariableName x)
        {
            return x.Update(
                x.NameValue,
                (BoundExpression)Accept(x.NameExpression));
        }

        public override BoundOperation VisitVariableRef(BoundVariableRef x)
        {
            return x.Update((BoundVariableName)Accept(x.Name));
        }

        public override BoundOperation VisitTemporalVariableRef(BoundTemporalVariableRef x)
        {
            Debug.Assert(x.Name.IsDirect);
            return x;
        }

        public override BoundOperation VisitList(BoundListEx x)
        {
            return x.Update(VisitImmutableArrayPairs(x.Items));
        }

        public override BoundOperation VisitFieldRef(BoundFieldRef x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Instance),
                (BoundTypeRef)Accept(x.ContainingType),
                (BoundVariableName)Accept(x.FieldName));
        }

        public override BoundOperation VisitArray(BoundArrayEx x)
        {
            return x.Update(VisitImmutableArrayPairs(x.Items));
        }

        public override BoundOperation VisitArrayItem(BoundArrayItemEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Array),
                (BoundExpression)Accept(x.Index));
        }

        public override BoundOperation VisitInstanceOf(BoundInstanceOfEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Operand),
                (BoundTypeRef)Accept(x.AsType));
        }

        public override BoundOperation VisitGlobalConstUse(BoundGlobalConst x)
        {
            return x;
        }

        public override BoundOperation VisitGlobalConstDecl(BoundGlobalConstDeclStatement x)
        {
            return x.Update(
                x.Name,
                (BoundExpression)Accept(x.Value));
        }

        public override BoundOperation VisitPseudoConstUse(BoundPseudoConst x)
        {
            return x;
        }

        public override BoundOperation VisitPseudoClassConstUse(BoundPseudoClassConst x)
        {
            return x.Update(
                (BoundTypeRef)Accept(x.TargetType),
                x.ConstType);
        }

        public override BoundOperation VisitIsEmpty(BoundIsEmptyEx x)
        {
            return x.Update((BoundExpression)Accept(x.Operand));
        }

        public override BoundOperation VisitIsSet(BoundIsSetEx x)
        {
            return x.Update((BoundReferenceExpression)Accept(x.VarReference));
        }

        public override BoundOperation VisitLambda(BoundLambda x)
        {
            return x.Update(VisitImmutableArray(x.UseVars));
        }

        public override BoundOperation VisitEval(BoundEvalEx x)
        {
            return x.Update((BoundExpression)Accept(x.CodeExpression));
        }


        public override BoundOperation VisitYieldEx(BoundYieldEx x)
        {
            return x;
        }

        public override BoundOperation VisitYieldFromEx(BoundYieldFromEx x)
        {
            return x.Update((BoundExpression)Accept(x.Operand));
        }

        #endregion

        #region Statements

        public override BoundOperation VisitUnset(BoundUnset x)
        {
            return x.Update((BoundReferenceExpression)Accept(x.Variable));
        }

        public override BoundOperation VisitEmptyStatement(BoundEmptyStatement x)
        {
            return x;
        }

        public override BoundOperation VisitBlockStatement(Graph.BoundBlock x)
        {
            // TODO: Return a new block if any change was made (after this class is turned into GraphRewriter)
            for (int i = 0; i < x.Statements.Count; i++)
            {
                x.Statements[i] = (BoundStatement)Accept(x.Statements[i]);
            }

            return x;
        }

        public override BoundOperation VisitExpressionStatement(BoundExpressionStatement x)
        {
            return x.Update((BoundExpression)Accept(x.Expression));
        }

        public override BoundOperation VisitReturn(BoundReturnStatement x)
        {
            return x.Update((BoundExpression)Accept(x.Returned));
        }

        public override BoundOperation VisitThrow(BoundThrowStatement x)
        {
            return x.Update((BoundExpression)x.Thrown);
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
            return x.Update((BoundVariableRef)Accept(x.Variable));
        }

        public override BoundOperation VisitStaticStatement(BoundStaticVariableStatement x)
        {
            return x;
        }

        public override BoundOperation VisitYieldStatement(BoundYieldStatement x)
        {
            return x.Update(
                x.YieldIndex,
                (BoundExpression)Accept(x.YieldedValue),
                (BoundExpression)Accept(x.YieldedKey));
        }

        public override BoundOperation VisitDeclareStatement(BoundDeclareStatement x)
        {
            return x;
        }

        #endregion
    }
}
