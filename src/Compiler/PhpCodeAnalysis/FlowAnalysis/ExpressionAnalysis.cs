using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax;
using Pchp.Syntax.AST;
using Pchp.Syntax.Text;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Visits single expressions and project transformations to flow state.
    /// </summary>
    internal class ExpressionAnalysis : OperationVisitor
    {
        #region Fields

        CFGAnalysis _analysis;

        readonly ISemanticModel _model;

        /// <summary>
        /// Gets current type context for type masks resolving.
        /// </summary>
        protected TypeRefContext TypeCtx => _analysis.TypeRefContext;

        /// <summary>
        /// Current flow state.
        /// </summary>
        protected FlowState State
        {
            get { return _analysis.State; }
            set { _analysis.State = value; }
        }

        /// <summary>
        /// Reference to corresponding source routine.
        /// </summary>
        protected SourceRoutineSymbol Routine => State.Common.Routine;

        /// <summary>
        /// The worklist to be used to enqueue next blocks.
        /// </summary>
        protected Worklist<BoundBlock> Worklist => _analysis.Worklist;

        #endregion

        #region Helpers

        /// <summary>
        /// Gets value indicating the given type represents a double and nothing else.
        /// </summary>
        bool IsDoubleOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && this.TypeCtx.IsDouble(tmask);
        }

        /// <summary>
        /// Gets value indicating the given type represents a double and nothing else.
        /// </summary>
        bool IsDoubleOnly(BoundExpression x) => IsDoubleOnly(x.TypeRefMask);

        /// <summary>
        /// Gets value indicating the given type represents a long and nothing else.
        /// </summary>
        bool IsLongOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && this.TypeCtx.IsLong(tmask);
        }

        /// <summary>
        /// Gets value indicating the given type represents a long and nothing else.
        /// </summary>
        bool IsLongOnly(BoundExpression x) => IsLongOnly(x.TypeRefMask);

        /// <summary>
        /// Gets value indicating the given type is long or double or both but nothing else.
        /// </summary>
        /// <param name="tmask"></param>
        /// <returns></returns>
        bool IsNumberOnly(TypeRefMask tmask)
        {
            if (TypeCtx.IsLong(tmask) || TypeCtx.IsDouble(tmask))
            {
                if (tmask.IsSingleType)
                    return true;

                return !tmask.IsAnyType && TypeCtx.GetTypes(tmask)
                    .All(t => t.TypeCode == Core.PhpTypeCode.Long || t.TypeCode == Core.PhpTypeCode.Double);
            }

            return false;
        }

        /// <summary>
        /// Gets value indicating the given type represents only class types.
        /// </summary>
        bool IsClassOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsAnyType && TypeCtx.GetTypes(tmask).All(x => x.IsObject);
        }

        /// <summary>
        /// Gets value indicating the given type represents only array types.
        /// </summary>
        bool IsArrayOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsAnyType && TypeCtx.GetTypes(tmask).All(x => x.IsArray);
        }

        /// <summary>
        /// Gets value indicating the given type is long or double or both but nothing else.
        /// </summary>
        /// <param name="tmask"></param>
        /// <returns></returns>
        bool IsNumberOnly(BoundExpression x) => IsNumberOnly(x.TypeRefMask);

        /// <summary>
        /// Determines if given expression represents a variable which value is less than <c>Int64.Max</c> in current state.
        /// </summary>
        bool IsLTInt64Max(BoundReferenceExpression r)
        {
            var varname = AsVariableName(r);
            return varname != null && State.IsLTInt64Max(varname);
        }

        /// <summary>
        /// In case of a local variable or parameter, sets associated flag determining its value is less than Int64.Max.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="lt"></param>
        void LTInt64Max(BoundReferenceExpression r, bool lt)
        {
            var varname = AsVariableName(r);
            if (varname != null)
                State.LTInt64Max(varname, lt);
        }

        /// <summary>
        /// In case of a local variable or parameter, gets its name.
        /// </summary>
        string AsVariableName(BoundReferenceExpression r)
        {
            var vr = r as BoundVariableRef;
            if (vr != null && (vr.Variable is BoundLocal || vr.Variable is BoundParameter))
            {
                return vr.Variable.Name;
            }
            return null;
        }

        bool IsLongConstant(BoundExpression expr, long value)
        {
            var l = (expr as BoundLiteral);
            if (l != null && l.ConstantValue.HasValue)
            {
                if (l.ConstantValue.Value is long) return ((long)l.ConstantValue.Value) == value;
                if (l.ConstantValue.Value is int) return ((int)l.ConstantValue.Value) == value;
            }
            return false;
        }

        /// <summary>
        /// Updates the expression access and visits it.
        /// </summary>
        /// <param name="x">The expression.</param>
        /// <param name="access">New access.</param>
        void Visit(BoundExpression x, BoundAccess access)
        {
            x.Access = access;
            Visit(x);
        }

        internal TypeSymbol ResolveType(DirectTypeRef dtype)
        {
            return (TypeSymbol)_model.GetType(dtype.ClassName);
        }

        #endregion

        #region Short-Circuit Evaluation

        /// <summary>
        /// Visits condition used to branch execution to true or false branch.
        /// </summary>
        /// <remarks>
        /// Because of minimal evaluation there is different TFlowState for true and false branches,
        /// AND and OR operators have to take this into account.
        /// 
        /// Also some other constructs may have side-effect for known branch,
        /// eg. <c>($x instanceof X)</c> implies ($x is X) in True branch.
        /// </remarks>
        internal void VisitCondition(BoundExpression condition, ConditionBranch branch)
        {
            Contract.ThrowIfNull(condition);

            if (branch != ConditionBranch.AnyResult)
            {
                if (condition is BoundBinaryEx)
                {
                    VisitBinaryOperatorExpression((BoundBinaryEx)condition, branch);
                    return;
                }
                //if (condition is BoundUnaryEx)
                //{
                //    VisitUnaryEx((UnaryEx)condition, branch);
                //    return;
                //}
                //if (condition is DirectFcnCall)
                //{
                //    VisitDirectFcnCall((DirectFcnCall)condition, branch);
                //    return;
                //}
                if (condition is BoundInstanceOfEx)
                {
                    VisitInstanceOf((BoundInstanceOfEx)condition, branch);
                    return;
                }
                //if (condition is IssetEx)
                //{
                //    VisitIssetEx((IssetEx)condition, branch);
                //    return;
                //}
                //if (condition is EmptyEx)
                //{
                //    VisitEmptyEx((EmptyEx)condition, branch);
                //    return;
                //}
            }

            // no effect
            condition.Accept(this);
        }

        //protected virtual void VisitBinaryEx(BinaryEx x, ConditionBranch branch)
        //{
        //    // to be overriden in derived class
        //    base.VisitBinaryEx(x);
        //}

        //protected virtual void VisitUnaryEx(UnaryEx x, ConditionBranch branch)
        //{
        //    // to be overriden in derived class
        //    base.VisitUnaryEx(x);
        //}

        //protected virtual void VisitInstanceOfEx(InstanceOfEx x, ConditionBranch branch)
        //{
        //    // to be overriden in derived class
        //    base.VisitInstanceOfEx(x);
        //}

        //protected virtual void VisitDirectFcnCall(DirectFcnCall x, ConditionBranch branch)
        //{
        //    // to be overriden in derived class
        //    base.VisitDirectFcnCall(x);
        //}
        //public virtual void VisitIssetEx(IssetEx x, ConditionBranch branch)
        //{
        //    base.VisitIssetEx(x);
        //}

        //public virtual void VisitEmptyEx(EmptyEx x, ConditionBranch branch)
        //{
        //    base.VisitEmptyEx(x);
        //}

        ///// <summary>
        ///// Explicitly given R-Value for the list. Used when traversing <c>foreach</c> and nested <c>list</c>.
        ///// Standard <c>list() = expr;</c> fallbacks to this one as well.
        ///// </summary>
        //protected virtual void VisitListEx(ListEx list, TypeRefMask rValueType)
        //{
        //    // to be overriden in derived class            
        //}

        //public sealed override void VisitListEx(ListEx x)
        //{
        //    // standard list expression, not foreach or nested one
        //    Contract.ThrowIfNull(x.RValue);
        //    VisitElement(x.RValue);
        //    VisitListEx(x, (x.RValue != null) ? (TypeRefMask)x.RValue.TypeInfoValue : TypeRefMask.AnyType);
        //}

        //public sealed override void VisitUnaryEx(UnaryEx x)
        //{
        //    VisitUnaryEx(x, ConditionBranch.Default);
        //}

        //public sealed override void VisitBinaryEx(BinaryEx x)
        //{
        //    VisitBinaryEx(x, ConditionBranch.Default);
        //}

        //public sealed override void VisitInstanceOfEx(InstanceOfEx x)
        //{
        //    VisitInstanceOfEx(x, ConditionBranch.AnyResult);
        //}

        //public sealed override void VisitDirectFcnCall(DirectFcnCall x)
        //{
        //    VisitDirectFcnCall(x, ConditionBranch.AnyResult);
        //}

        //public sealed override void VisitIssetEx(IssetEx x)
        //{
        //    VisitIssetEx(x, ConditionBranch.AnyResult);
        //}

        //public sealed override void VisitEmptyEx(EmptyEx x)
        //{
        //    VisitEmptyEx(x, ConditionBranch.AnyResult);
        //}

        /// <summary>
        /// Handles use of variable as foreach iterator value.
        /// </summary>
        /// <param name="varuse"></param>
        /// <returns>Derivate type of iterated values.</returns>
        internal virtual TypeRefMask HandleTraversableUse(BoundExpression/*!*/varuse)
        {
            return TypeRefMask.AnyType;
        }

        #endregion

        #region Construction

        public ExpressionAnalysis(ISemanticModel model)
        {
            Contract.ThrowIfNull(model);
            _model = model;
        }

        internal void SetAnalysis(CFGAnalysis analysis)
        {
            _analysis = analysis;
        }

        #endregion

        #region Declaration Statements

        public override void VisitInvalidStatement(IStatement operation)
        {
            if (operation is BoundFunctionDeclStatement)
            {
                return;
            }

            base.VisitInvalidStatement(operation);
        }

        public sealed override void VisitVariableDeclarationStatement(IVariableDeclarationStatement operation)
        {
            if (operation is BoundStaticVariableStatement)
            {
                VisitStaticVariableStatement((BoundStaticVariableStatement)operation);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(operation);
            }
        }

        protected virtual void VisitStaticVariableStatement(BoundStaticVariableStatement x)
        {
            foreach (var v in x.Variables)
            {
                var name = v.Variable.Name;

                var oldtype = State.GetVarType(name);

                // set var
                if (v.InitialValue != null)
                {
                    // analyse initializer
                    Visit(v.InitialValue);

                    State.SetVarInitialized(name);
                    State.LTInt64Max(name, (v.InitialValue.ConstantValue.HasValue && v.InitialValue.ConstantValue.Value is long && (long)v.InitialValue.ConstantValue.Value < long.MaxValue));
                    State.SetVar(name, ((BoundExpression)v.InitialValue).TypeRefMask | oldtype);
                }
                else
                {
                    State.LTInt64Max(name, false);
                }
            }
        }

        #endregion

        #region Visit Literals

        public sealed override void VisitLiteralExpression(ILiteralExpression operation)
            => ((BoundExpression)operation).TypeRefMask = ((BoundLiteral)operation).ResolveTypeMask(TypeCtx);

        #endregion

        #region Visit Assignments

        public sealed override void VisitAssignmentExpression(IAssignmentExpression operation)
            => ((BoundExpression)operation).TypeRefMask = VisitAssignmentExpression((BoundAssignEx)operation);

        protected virtual TypeRefMask VisitAssignmentExpression(BoundAssignEx x)
        {
            Debug.Assert(x.Target.Access.IsWrite);
            Debug.Assert(x.Value.Access.IsRead);

            Visit(x.Value);

            x.Target.Access = x.Target.Access.WithWrite(x.Value.TypeRefMask);
            Visit(x.Target);

            //
            return x.Value.TypeRefMask;
        }

        static Operations CompoundOpToBinaryOp(Operations op)
        {
            switch (op)
            {
                case Operations.AssignAdd: return Operations.Add;
                case Operations.AssignAnd: return Operations.And;
                case Operations.AssignAppend: return Operations.Concat;
                case Operations.AssignDiv: return Operations.Div;
                case Operations.AssignMod: return Operations.Mod;
                case Operations.AssignMul: return Operations.Mul;
                case Operations.AssignOr: return Operations.Or;
                case Operations.AssignPow: return Operations.Pow;
                case Operations.AssignPrepend: return Operations.Concat;
                case Operations.AssignShiftLeft: return Operations.ShiftLeft;
                case Operations.AssignShiftRight: return Operations.ShiftRight;
                case Operations.AssignSub: return Operations.Sub;
                case Operations.AssignXor: return Operations.Xor;
                default:
                    throw ExceptionUtilities.UnexpectedValue(op);
            }
        }

        protected virtual TypeRefMask VisitCompoundAssignmentExpression(BoundCompoundAssignEx x)
        {
            Debug.Assert(x.Target.Access.IsRead && x.Target.Access.IsWrite);
            Debug.Assert(x.Value.Access.IsRead);

            // Target X Value
            var result = VisitBinaryOperatorExpression(new BoundBinaryEx(x.Target.WithAccess(BoundAccess.Read), x.Value, CompoundOpToBinaryOp(x.Operation)), ConditionBranch.AnyResult);

            // Target =
            x.Target.Access = BoundAccess.Write.WithWrite(result);
            Visit(x.Target);

            // put read access back
            x.Target.Access = x.Target.Access.WithRead();

            //
            return result;
        }

        public override void VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation)
            => ((BoundExpression)operation).TypeRefMask = VisitCompoundAssignmentExpression((BoundCompoundAssignEx)operation);

        public override void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        {
            var expr = (BoundExpression)operation;

            if (expr is BoundVariableRef)
            {
                VisitBoundVariableRef((BoundVariableRef)expr);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected virtual void VisitBoundVariableRef(BoundVariableRef x)
        {
            if (x.Access.IsRead)
            {
                State.SetVarUsed(x.Name);
                var vartype = State.GetVarType(x.Name);

                if (vartype.IsVoid)
                    vartype = TypeRefMask.AnyType;

                if (x.Access.IsEnsure)
                {
                    if (x.Access.IsReadRef)
                    {
                        State.SetVarRef(x.Name);
                        vartype.IsRef = true;
                    }
                    if (x.Access.EnsureObject && !IsClassOnly(vartype))
                    {
                        vartype |= TypeCtx.GetSystemObjectTypeMask();
                    }
                    if (x.Access.EnsureArray && !IsArrayOnly(vartype))
                    {
                        vartype |= TypeCtx.GetArrayTypeMask();
                    }

                    State.SetVarInitialized(x.Name);
                    State.SetVar(x.Name, vartype);
                }

                x.TypeRefMask = vartype;
            }

            if (x.Access.IsWrite)
            {
                State.SetVarInitialized(x.Name);
                State.SetVar(x.Name, x.Access.WriteMask);
                State.LTInt64Max(x.Name, false);

                x.TypeRefMask = x.Access.WriteMask;

                if (x.Access.IsWriteRef)
                {
                    State.SetVarRef(x.Name);
                    x.TypeRefMask = x.TypeRefMask.WithRefFlag;
                }

                //
                if (x.Variable is BoundStaticLocal)
                {
                    // analysis has to be started over // TODO: start from the block which declares the static local variable
                    var startBlock = Routine.ControlFlowGraph.Start;
                    var startState = startBlock.FlowState;

                    var oldVar = startState.GetVarType(x.Name);
                    if (oldVar != x.TypeRefMask)
                    {
                        startState.SetVar(x.Name, x.TypeRefMask);
                        this.Worklist.Enqueue(startBlock);
                    }
                }
            }

            if (x.Access.IsUnset)
            {
                State.SetVar(x.Name, 0);
                State.LTInt64Max(x.Name, false);
                x.TypeRefMask = 0;
            }
        }

        public sealed override void VisitIncrementExpression(IIncrementExpression operation)
            => VisitIncrementExpression((BoundIncDecEx)operation);

        protected virtual void VisitIncrementExpression(BoundIncDecEx x)
        {
            // <target> = <target> +/- 1L

            Debug.Assert(x.Access.IsRead || x.Access.IsNone);
            Debug.Assert(x.Target.Access.IsRead && x.Target.Access.IsWrite);

            Visit(x.Target, BoundAccess.Read);
            Visit(x.Value, BoundAccess.Read);

            Debug.Assert(IsNumberOnly(x.Value));    // 1L

            TypeRefMask resulttype;
            TypeRefMask sourcetype = x.Target.TypeRefMask;  // type of target before operation

            if (IsDoubleOnly(x.Target))
            {
                // double++ => double
                resulttype = TypeCtx.GetDoubleTypeMask();
            }
            else if (IsLTInt64Max(x.Target))    // we'd like to keep long if we are sure we don't overflow to double
            {
                // long++ [< long.MaxValue] => long
                resulttype = TypeCtx.GetLongTypeMask();
            }
            else
            {
                // long|double|anything++ => number
                resulttype = TypeCtx.GetNumberTypeMask();
            }

            Visit(x.Target, BoundAccess.Write.WithWrite(resulttype));

            //
            x.Target.Access = x.Target.Access.WithRead();   // put read access back to the target
            x.TypeRefMask = (x.IncrementKind == UnaryOperationKind.OperatorPrefixIncrement ||
                             x.IncrementKind == UnaryOperationKind.OperatorPrefixDecrement)
                            ? resulttype : sourcetype;
        }

        #endregion

        #region Visit BinaryEx

        private void VisitShortCircuitOp(BoundExpression lExpr, BoundExpression rExpr, bool isAndOp, ConditionBranch branch)
        {
            // Each operand has to be evaluated in various states and then the state merged.
            // Simulates short-circuit evaluation in runtime:

            var state = this.State; // original state

            if (branch == ConditionBranch.AnyResult)
            {
                if (isAndOp)
                {
                    // A == True && B == Any
                    // A == False

                    State = state.Clone();
                    VisitCondition(lExpr, ConditionBranch.ToTrue);
                    VisitCondition(rExpr, ConditionBranch.AnyResult);
                    var tmp = State;
                    State = state.Clone();
                    VisitCondition(lExpr, ConditionBranch.ToFalse);
                    State = State.Merge(tmp);
                }
                else
                {
                    // A == False && B == Any
                    // A == True

                    State = state.Clone();
                    VisitCondition(lExpr, ConditionBranch.ToFalse);
                    VisitCondition(rExpr, ConditionBranch.AnyResult);
                    var tmp = State;
                    State = state.Clone();
                    VisitCondition(lExpr, ConditionBranch.ToTrue);
                    State = State.Merge(tmp);
                }
            }
            else if (branch == ConditionBranch.ToTrue)
            {
                if (isAndOp)
                {
                    // A == True && B == True

                    VisitCondition(lExpr, ConditionBranch.ToTrue);
                    VisitCondition(rExpr, ConditionBranch.ToTrue);
                }
                else
                {
                    // A == False && B == True
                    // A == True

                    State = state.Clone();
                    VisitCondition(lExpr, ConditionBranch.ToFalse);
                    VisitCondition(rExpr, ConditionBranch.ToTrue);
                    var tmp = State;
                    State = state.Clone();
                    VisitCondition(lExpr, ConditionBranch.ToTrue);
                    State = State.Merge(tmp);
                }
            }
            else if (branch == ConditionBranch.ToFalse)
            {
                if (isAndOp)
                {
                    // A == True && B == False
                    // A == False

                    State = state.Clone();
                    VisitCondition(lExpr, ConditionBranch.ToTrue);
                    VisitCondition(rExpr, ConditionBranch.ToFalse);
                    var tmp = State;
                    State = state.Clone();
                    VisitCondition(lExpr, ConditionBranch.ToFalse);
                    State = State.Merge(tmp);
                }
                else
                {
                    // A == False && B == False

                    VisitCondition(lExpr, ConditionBranch.ToFalse);
                    VisitCondition(rExpr, ConditionBranch.ToFalse);
                }
            }
        }

        /// <summary>
        /// Gets resulting type of bit operation (bit or, and, xor).
        /// </summary>
        TypeRefMask GetBitOperationType(TypeRefMask lValType, TypeRefMask rValType)
        {
            TypeRefMask type;

            // type is string if both operands are string
            if ((lValType.IsAnyType && rValType.IsAnyType) ||
                (TypeCtx.IsAString(lValType) && TypeCtx.IsAString(rValType)))
            {
                type = TypeCtx.GetStringTypeMask();
            }
            else
            {
                type = default(TypeRefMask);
            }

            // type can be always long
            type |= TypeCtx.GetLongTypeMask();

            //
            return type;
        }

        /// <summary>
        /// Gets resulting type of <c>+</c> operation.
        /// </summary>
        TypeRefMask GetPlusOperationType(BoundExpression left, BoundExpression right)
        {
            var lValType = left.TypeRefMask;
            var rValType = right.TypeRefMask;

            // array + array => array
            // array + number => 0 (ERROR)
            // number + number => number
            // anytype + array => array
            // anytype + number => number

            var or = lValType | rValType;

            // double + number => double
            if (IsNumberOnly(or))
            {
                if (IsDoubleOnly(lValType) || IsDoubleOnly(rValType))
                    return TypeCtx.GetDoubleTypeMask();

                if (IsLTInt64Max(left as BoundReferenceExpression) && IsLongConstant(right, 1)) // LONG + 1, where LONG < long.MaxValue
                    return TypeCtx.GetLongTypeMask();

                return TypeCtx.GetNumberTypeMask();
            }

            //
            var type = TypeCtx.GetArraysFromMask(or);

            //
            if (or.IsAnyType || TypeCtx.IsNumber(or) || type == 0) // !this.TypeRefContext.IsArray(lValType & rValType))
                type |= TypeCtx.GetNumberTypeMask();    // anytype or an operand is number or operands are not a number nor both are not array

            if (or.IsAnyType)
                type |= TypeCtx.GetArrayTypeMask();

            //
            return type;
        }

        public sealed override void VisitBinaryOperatorExpression(IBinaryOperatorExpression operation)
            => ((BoundExpression)operation).TypeRefMask = VisitBinaryOperatorExpression((BoundBinaryEx)operation, ConditionBranch.AnyResult);

        protected virtual TypeRefMask VisitBinaryOperatorExpression(BoundBinaryEx x, ConditionBranch branch)
        {
            if (x.Operation == Operations.And || x.Operation == Operations.Or)
            {
                this.VisitShortCircuitOp(x.Left, x.Right, x.Operation == Operations.And, branch);
            }
            else
            {
                Visit(x.Left);
                Visit(x.Right);
            }

            switch (x.Operation)
            {
                #region Arithmetic Operations

                case Operations.Add:
                    return GetPlusOperationType(x.Left, x.Right);

                case Operations.Sub:
                case Operations.Div:
                case Operations.Mul:
                case Operations.Pow:
                    if (IsDoubleOnly(x.Left.TypeRefMask) || IsDoubleOnly(x.Right.TypeRefMask)) // some operand is double and nothing else
                        return TypeCtx.GetDoubleTypeMask(); // double if we are sure about operands
                    return TypeCtx.GetNumberTypeMask();

                case Operations.Mod:
                    return TypeCtx.GetLongTypeMask();

                case Operations.ShiftLeft:
                case Operations.ShiftRight:
                    return TypeCtx.GetLongTypeMask();

                #endregion

                #region Boolean and Bitwise Operations

                case Operations.And:
                case Operations.Or:
                case Operations.Xor:
                    return TypeCtx.GetBooleanTypeMask();

                case Operations.BitAnd:
                case Operations.BitOr:
                case Operations.BitXor:
                    return GetBitOperationType(x.Left.TypeRefMask, x.Right.TypeRefMask);    // int or string

                #endregion

                #region Comparing Operations

                case Operations.Equal:
                case Operations.NotEqual:
                case Operations.GreaterThan:
                case Operations.LessThan:
                case Operations.GreaterThanOrEqual:
                case Operations.LessThanOrEqual:
                case Operations.Identical:
                case Operations.NotIdentical:

                    if (branch == ConditionBranch.ToTrue)
                    {
                        if (x.Operation == Operations.LessThan && IsLongOnly(x.Right))
                            LTInt64Max(x.Left as BoundReferenceExpression, true);   // $x < LONG
                    }

                    return TypeCtx.GetBooleanTypeMask();

                #endregion

                case Operations.Concat:
                    return TypeCtx.GetWritableStringTypeMask();

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        #endregion

        #region Visit UnaryEx

        public sealed override void VisitUnaryOperatorExpression(IUnaryOperatorExpression operation)
            => ((BoundExpression)operation).TypeRefMask = ResolveUnaryOperatorExpression((BoundUnaryEx)operation);

        protected virtual TypeRefMask ResolveUnaryOperatorExpression(BoundUnaryEx x)
        {
            //
            Visit(x.Operand);

            //
            switch (x.Operation)
            {
                case Operations.AtSign:
                    return x.Operand.TypeRefMask;

                case Operations.BitNegation:
                    throw new NotImplementedException();

                case Operations.Clone:
                    throw new NotImplementedException();

                case Operations.LogicNegation:
                    return TypeCtx.GetBooleanTypeMask();

                case Operations.Minus:
                    if (IsDoubleOnly(x.Operand))
                        return TypeCtx.GetDoubleTypeMask(); // double in case operand is double
                    return TypeCtx.GetNumberTypeMask();     // TODO: long in case operand is not a number

                case Operations.ObjectCast:
                    if (IsClassOnly(x.Operand.TypeRefMask))
                        return x.Operand.TypeRefMask;   // (object)<object>

                    return TypeCtx.GetSystemObjectTypeMask();   // TODO: return the exact type in case we know, return stdClass in case of a scalar

                case Operations.Plus:
                    if (IsNumberOnly(x.Operand.TypeRefMask))
                        return x.Operand.TypeRefMask;
                    return TypeCtx.GetNumberTypeMask();

                case Operations.Print:
                    return TypeCtx.GetLongTypeMask();

                case Operations.BoolCast:
                    return TypeCtx.GetBooleanTypeMask();

                case Operations.Int8Cast:
                case Operations.Int16Cast:
                case Operations.Int32Cast:
                case Operations.UInt8Cast:
                case Operations.UInt16Cast:
                // -->
                case Operations.UInt64Cast:
                case Operations.UInt32Cast:
                case Operations.Int64Cast:
                    return TypeCtx.GetLongTypeMask();

                case Operations.DecimalCast:
                case Operations.DoubleCast:
                case Operations.FloatCast:
                    return TypeCtx.GetDoubleTypeMask();

                case Operations.UnicodeCast: // TODO
                case Operations.StringCast:
                    return TypeCtx.GetStringTypeMask(); // binary | unicode | both

                case Operations.BinaryCast:
                    return TypeCtx.GetWritableStringTypeMask(); // binary string builder

                case Operations.ArrayCast:
                    return TypeCtx.GetArrayTypeMask();  // TODO: can we be more specific?

                case Operations.UnsetCast:
                    return TypeCtx.GetNullTypeMask();   // null

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        #endregion

        #region Visit InstanceOf

        public sealed override void VisitIsExpression(IIsExpression operation)
            => VisitInstanceOf((BoundInstanceOfEx)operation, ConditionBranch.AnyResult);

        protected virtual void VisitInstanceOf(BoundInstanceOfEx x, ConditionBranch branch)
        {
            Visit(x.Operand);

            //
            if (!x.IsTypeDirect.IsEmpty())
            {
                x.IsTypeResolved = (NamedTypeSymbol)_model.GetType(x.IsTypeDirect);

                // TOOD: x.ConstantValue // in case we know and the operand is a local variable (we can ignore the expression and emit result immediatelly)

                if (branch == ConditionBranch.ToTrue && x.Operand is BoundVariableRef)
                {
                    // if (Variable is T) => variable is T in True branch state
                    State.SetVar(((BoundVariableRef)x.Operand).Name, TypeCtx.GetTypeMask(x.IsTypeDirect));
                }
            }

            //
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();
        }

        #endregion

        #region Visit Function Call

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            var x = (BoundRoutineCall)operation;

            // TODO: write arguments Access
            // TODO: visit invocation member of
            // TODO: 2 pass, analyze arguments -> resolve method -> assign argument to parameter -> write arguments access -> analyze arguments again

            // analyze arguments
            operation.ArgumentsInSourceOrder.ForEach(VisitArgument);

            // resolve invocation
            if (operation is BoundEcho)
            {
                VisitEcho((BoundEcho)operation);
            }
            else if (operation is BoundConcatEx)
            {
                VisitConcat((BoundConcatEx)operation);
            }
            else if (operation is BoundFunctionCall)
            {
                VisitFunctionCall((BoundFunctionCall)operation);
            }
            else if (operation is BoundInstanceMethodCall)
            {
                VisitInstanceMethodCall((BoundInstanceMethodCall)operation);
            }
            else if (operation is BoundStMethodCall)
            {
                VisitStMethodCall((BoundStMethodCall)operation);
            }
            else if (operation is BoundNewEx)
            {
                VisitNewEx((BoundNewEx)operation);
            }
            else if (operation is BoundIncludeEx)
            {
                VisitIncludeEx((BoundIncludeEx)operation);
            }
            else if (operation is BoundExitEx)
            {
                VisitExit((BoundExitEx)operation);
            }
            else
            {
                throw new NotImplementedException();
            }

            if (x.Access.IsReadRef)
            {
                x.TypeRefMask = x.TypeRefMask.WithRefFlag;
            }
        }

        protected virtual void VisitExit(BoundExitEx x)
        {
            //
        }

        protected virtual void VisitEcho(BoundEcho x)
        {
            x.TypeRefMask = 0;
        }

        protected virtual void VisitConcat(BoundConcatEx x)
        {
            x.TypeRefMask = TypeCtx.GetWritableStringTypeMask();
        }

        protected virtual void VisitFunctionCall(BoundFunctionCall x)
        {
            string name;
            // resolve candidates
            var candidates = _model.ResolveFunction(x.Name).ToList();
            if (candidates.Count == 0 && x.AlternativeName.HasValue)
            {
                name = x.AlternativeName.Value.ClrName();
                candidates.AddRange(_model.ResolveFunction(x.AlternativeName.Value));
            }
            else
            {
                name = x.Name.ClrName();
            }

            //
            var args = x.ArgumentsInSourceOrder.Select(a => a.Value).ToImmutableArray();

            var overloads = new OverloadsList(name, candidates.Cast<MethodSymbol>())
            {
                IsFinal = true,
            };
            overloads.WithParametersType(TypeCtx, args.Select(a => a.TypeRefMask).ToArray());

            //
            TypeRefMask result_type = 0;

            // reanalyse candidates
            foreach (var c in overloads.Candidates)
            {
                // analyze TargetMethod with x.Arguments
                // require method result type if access != none
                var enqueued = this.Worklist.EnqueueRoutine(c, _analysis.CurrentBlock, args);
                if (enqueued)   // => target has to be reanalysed
                {
                    // note: continuing current block may be waste of time
                }

                result_type |= c.GetResultType(TypeCtx);
            }

            if (overloads.Candidates.Length == 0)
            {
                result_type = TypeRefMask.AnyType;
            }

            x.Overloads = overloads;
            x.TypeRefMask = result_type;
        }

        protected virtual void VisitInstanceMethodCall(BoundInstanceMethodCall x)
        {
            Debug.Assert(x.Instance != null);

            x.TypeRefMask = TypeRefMask.AnyType;    // temporarily until we won't resolve the method call

            // resolve instance type
            Visit(x.Instance);

            var typeref = TypeCtx.GetTypes(x.Instance.TypeRefMask);
            if (typeref.Count != 1)
            {
                // TODO: resolve common base

                // otherwise dynamic call
                return;
            }

            var classtypes = typeref.Where(t => t.IsObject).AsImmutable();
            if (classtypes.Length != 1)
            {
                // dynamic call
                return;
            }

            var qname = classtypes[0].QualifiedName;
            var type = _model.GetType(qname);
            if (type == null)
            {
                return;
            }

            // TODO: LookupMember: lookup in base classes, accessibility resolution
            var candidates = type.GetMembers(x.Name.Value).OfType<MethodSymbol>();  // TODO: GetPhpMember: case insensitive

            // TODO: merge following with VisitFunctionCall

            //
            var args = x.ArgumentsInSourceOrder.Select(a => a.Value).ToImmutableArray();

            var overloads = new OverloadsList(x.Name.Value, candidates);
            overloads.WithInstanceCall((TypeSymbol)type);
            overloads.WithInstanceCall(TypeCtx, x.Instance.TypeRefMask);
            overloads.WithParametersType(TypeCtx, args.Select(a => a.TypeRefMask).ToArray());

            //
            TypeRefMask result_type = 0;

            if (overloads.IsFinal)
            {
                // reanalyse candidates
                foreach (var c in overloads.Candidates)
                {
                    // analyze TargetMethod with x.Arguments
                    // require method result type if access != none
                    var enqueued = this.Worklist.EnqueueRoutine(c, _analysis.CurrentBlock, args);
                    if (enqueued)   // => target has to be reanalysed
                    {
                        // note: continuing current block may be waste of time
                    }

                    result_type |= c.GetResultType(TypeCtx);
                }
            }
            else
            {
                result_type = TypeRefMask.AnyType;
            }

            x.Overloads = overloads;
            x.TypeRefMask = result_type;
        }

        protected virtual void VisitStMethodCall(BoundStMethodCall x)
        {
            // resolve candidates
            Debug.Assert(!x.ContainingType.IsGeneric, "Not Implemented");

            var type = _model.GetType(x.ContainingType.QualifiedName);
            if (type == null)
            {
                x.TypeRefMask = TypeRefMask.AnyType;
                return;
            }

            // TODO: LookupMember: lookup in base classes, accessibility resolution
            var candidates = type.GetMembers(x.Name.Value).OfType<MethodSymbol>();  // TODO: GetPhpMember: case insensitive

            // TODO: merge following with VisitFunctionCall

            //
            var args = x.ArgumentsInSourceOrder.Select(a => a.Value).ToImmutableArray();

            var overloads = new OverloadsList(x.Name.Value, candidates)
            {
                IsFinal = true,
            };
            overloads.WithParametersType(TypeCtx, args.Select(a => a.TypeRefMask).ToArray());

            //
            TypeRefMask result_type = 0;

            // reanalyse candidates
            foreach (var c in overloads.Candidates)
            {
                // analyze TargetMethod with x.Arguments
                // require method result type if access != none
                var enqueued = this.Worklist.EnqueueRoutine(c, _analysis.CurrentBlock, args);
                if (enqueued)   // => target has to be reanalysed
                {
                    // note: continuing current block may be waste of time
                }

                result_type |= c.GetResultType(TypeCtx);
            }

            x.Overloads = overloads;
            x.TypeRefMask = result_type;
        }

        protected virtual void VisitNewEx(BoundNewEx x)
        {
            // resolve target type
            var type = (NamedTypeSymbol)_model.GetType(x.TypeName);
            if (type != null)
            {
                var candidates = type.InstanceConstructors;

                //
                var args = x.ArgumentsInSourceOrder.Select(a => a.Value).ToImmutableArray();

                var overloads = new OverloadsList(WellKnownMemberNames.InstanceConstructorName, candidates.Cast<MethodSymbol>())
                {
                    IsFinal = true,
                };
                overloads.WithParametersType(TypeCtx, args.Select(a => a.TypeRefMask).ToArray());

                // reanalyse candidates
                foreach (var c in overloads.Candidates)
                {
                    // analyze TargetMethod with x.Arguments
                    this.Worklist.EnqueueRoutine(c, _analysis.CurrentBlock, args);
                }

                x.Overloads = overloads;
                x.ResultType = type;
            }
            else
            {
                x.ResultType = new MissingMetadataTypeSymbol(x.TypeName.ClrName(), 0, false);
            }

            x.TypeRefMask = TypeCtx.GetTypeMask(x.TypeName, false);
        }

        protected virtual void VisitIncludeEx(BoundIncludeEx x)
        {
            this.Routine.Flags |= SourceRoutineSymbol.RoutineFlags.HasInclude;

            // resolve target script
            Debug.Assert(x.ArgumentsInSourceOrder.Length == 1);
            var targetExpr = x.ArgumentsInSourceOrder[0].Value;

            //
            x.Target = null;

            if (targetExpr.ConstantValue.HasValue)
            {
                var value = targetExpr.ConstantValue.Value as string;
                if (value != null)
                {
                    var targetFile = _model.GetFile(value);
                    if (targetFile != null)
                    {
                        x.Target = targetFile.MainMethod;
                    }
                }
            }

            // resolve result type
            if (x.Access.IsRead)
            {
                var target = x.Target;
                if (target != null)
                {
                    x.ResultType = target.ReturnType;
                    x.TypeRefMask = target.GetResultType(TypeCtx);

                    if (x.IsOnceSemantic)
                    {
                        // include_once, require_once returns TRUE in case the script was already included
                        x.TypeRefMask |= TypeCtx.GetBooleanTypeMask();
                    }
                }
                else
                {
                    x.TypeRefMask = TypeRefMask.AnyType;
                }
            }
            else
            {
                x.TypeRefMask = 0;
            }
        }

        public override void VisitArgument(IArgument operation)
        {
            if (operation.Parameter != null)
            {
                // TODO: write arguments access
                // TODO: conversion by simplifier visitor
            }

            VisitArgument((BoundArgument)operation);
        }

        protected virtual void VisitArgument(BoundArgument x)
        {
            Visit(x.Value);
        }

        #endregion

        #region Visit FieldRef

        public sealed override void VisitFieldReferenceExpression(IFieldReferenceExpression operation)
        {
            VisitFieldRef((BoundFieldRef)operation);
        }

        protected void VisitFieldRef(BoundFieldRef x)
        {
            Debug.Assert(x.Instance != null);
            Debug.Assert(x.Instance.Access.IsRead);

            Visit(x.Instance);

            x.TypeRefMask = TypeRefMask.AnyType;

            var typerefs = TypeCtx.GetTypes(x.Instance.TypeRefMask);
            if (typerefs.Count == 1 && typerefs[0].IsObject)
            {
                // TODO: x.Instance.ResultType instead of following
                var t = _model.GetType(typerefs[0].QualifiedName);
                if (t != null)
                {
                    // TODO: visibility and resolution (model)
                    x.Field = t.GetMembers(x.Name.Value).OfType<FieldSymbol>().SingleOrDefault();

                    if (x.Field != null)
                    {
                        x.TypeRefMask = x.Field.GetResultType(TypeCtx);
                    }
                }
            }
            else
            {
                // TODO: ErrCode
            }
        }

        #endregion

        #region Visit ArrayEx, ArrayItemEx

        public sealed override void VisitArrayCreationExpression(IArrayCreationExpression operation)
            => VisitArrayEx((BoundArrayEx)operation);

        protected void VisitArrayEx(BoundArrayEx x)
        {
            var items = x.Items;
            TypeRefMask elementType = 0;

            // analyse elements
            foreach (var i in items)
            {
                Visit(i.Key);
                Visit(i.Value);

                elementType |= i.Value.TypeRefMask;
            }

            // writeup result type
            x.TypeRefMask = elementType.IsVoid
                ? TypeCtx.GetArrayTypeMask()
                : TypeCtx.GetArrayTypeMask(elementType);
        }

        public sealed override void VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation)
            => VisitArrayItemEx((BoundArrayItemEx)operation);

        protected void VisitArrayItemEx(BoundArrayItemEx x)
        {
            Visit(x.Array);
            Visit(x.Index);

            //
            x.TypeRefMask = x.Access.IsReadRef
                ? TypeRefMask.AnyType.WithRefFlag
                : TypeRefMask.AnyType;
        }

        #endregion

        #region Visit

        public override void DefaultVisit(IOperation operation)
        {
            if (operation is BoundPseudoConst)
            {
                VisitPseudoConst((BoundPseudoConst)operation);
            }
            else if (operation is BoundGlobalConst)
            {
                VisitGlobalConst((BoundGlobalConst)operation);
            }
            else if (operation is BoundIsSetEx)
            {
                VisitsSet((BoundIsSetEx)operation);
            }
            else if (operation is BoundUnset)
            {
                VisitUnset((BoundUnset)operation);
            }
            else if (operation is BoundEmptyStatement)
            {
                // nop
            }
            else
            {
                throw new NotImplementedException(operation.GetType().Name);
            }
        }

        public virtual void VisitsSet(BoundIsSetEx x)
        {
            x.VarReferences.ForEach(Visit);
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();
        }

        public virtual void VisitUnset(BoundUnset x)
        {
            x.VarReferences.ForEach(Visit);
        }

        public void VisitPseudoConst(BoundPseudoConst x)
        {
            switch (x.Type)
            {
                case PseudoConstUse.Types.Line:
                    x.TypeRefMask = TypeCtx.GetLongTypeMask();
                    break;

                case PseudoConstUse.Types.Class:
                case PseudoConstUse.Types.Trait:
                case PseudoConstUse.Types.Method:
                case PseudoConstUse.Types.Function:
                case PseudoConstUse.Types.Namespace:
                case PseudoConstUse.Types.Dir:
                case PseudoConstUse.Types.File:
                    x.TypeRefMask = TypeCtx.GetStringTypeMask();
                    break;

                default:
                    throw new NotImplementedException(x.Type.ToString());
            }
        }

        public void VisitGlobalConst(BoundGlobalConst x)
        {
            // TODO: check constant name

            // TODO: bind to app-wide constant if possible
            //x.SetConstantValue(...)

            x.TypeRefMask = TypeRefMask.AnyType;    // only scalars ?
        }

        public override void VisitConditionalChoiceExpression(IConditionalChoiceExpression operation)
            => VisitConditionalChoiceExpression((BoundConditionalEx)operation);

        protected virtual void VisitConditionalChoiceExpression(BoundConditionalEx x)
        {
            var state = State;
            var trueExpr = x.IfTrue ?? x.Condition;

            // true branch
            var trueState = State = state.Clone();
            VisitCondition(x.Condition, ConditionBranch.ToTrue);
            Visit(trueExpr);

            // false branch
            var falseState = State = state.Clone();
            VisitCondition(x.Condition, ConditionBranch.ToFalse);
            Visit(x.IfFalse);

            // merge both states
            State = trueState.Merge(falseState);

            //
            x.TypeRefMask = trueExpr.TypeRefMask | x.IfFalse.TypeRefMask;
        }

        public override void VisitExpressionStatement(IExpressionStatement operation)
            => Visit(operation.Expression);

        public sealed override void VisitReturnStatement(IReturnStatement operation)
            => VisitReturnStatement((BoundReturnStatement)operation);

        protected virtual void VisitReturnStatement(BoundReturnStatement x)
        {
            if (x.Returned != null)
            {
                Visit(x.Returned);
                State.FlowThroughReturn(x.Returned.TypeRefMask);
            }
        }

        #endregion
    }
}
