using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.CodeGen;
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
            if (expr.ConstantObject.HasValue)
            {
                if (expr.ConstantObject.Value is long) return ((long)expr.ConstantObject.Value) == value;
                if (expr.ConstantObject.Value is int) return ((int)expr.ConstantObject.Value) == value;
            }
            return false;
        }

        bool BindConstantValue(BoundExpression target, FieldSymbol symbol)
        {
            if (symbol != null && symbol.IsConst)
            {
                target.ConstantValue = symbol.GetConstantValue(false);
                target.TypeRefMask = TypeRefFactory.CreateMask(TypeCtx, symbol.Type);

                return true;
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
            if (operation is BoundFunctionDeclStatement ||
                operation is BoundTypeDeclStatement)
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

            // keep WriteRef flag
            var targetaccess = BoundAccess.Write;
            if (x.Target.Access.IsWriteRef)
                targetaccess = targetaccess.WithWriteRef(0);

            // new target access with resolved target type
            x.Target.Access = targetaccess.WithWrite(x.Value.TypeRefMask);
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
                throw ExceptionUtilities.UnexpectedValue(expr);
            }
        }

        protected virtual void VisitBoundVariableRef(BoundVariableRef x)
        {
            // bind variable place
            if (x.Variable == null)
            {
                x.Variable = x.Name.IsDirect
                    ? _analysis.State.FlowContext.GetVar(x.Name.NameValue.Value)
                    : new BoundIndirectLocal(x.Name.NameExpression);
            }

            // indirect variable access:
            if (!x.Name.IsDirect)
            {
                Routine.Flags |= RoutineFlags.HasIndirectVar;

                Visit(x.Name.NameExpression);

                if (x.Access.IsRead)
                {
                    State.Common.SetAllUsed();
                }

                if (x.Access.IsWrite)
                {
                    State.SetAllInitialized();
                }

                if (x.Access.IsUnset)
                {

                }

                return;
            }

            // direct variable access:
            var name = x.Name.NameValue.Value;
            if (x.Access.IsRead)
            {
                State.SetVarUsed(name);
                var vartype = State.GetVarType(name);

                if (vartype.IsVoid || x.Variable is BoundGlobalVariable)
                {
                    // in global code or in case of undefined variable,
                    // assume the type is mixed (unspecified).
                    // In global code, the type of variable cannot be determined by type analysis, it can change between every two operations (this may be improved by better flow analysis).
                    vartype = TypeRefMask.AnyType;
                }

                if (x.Access.IsEnsure)
                {
                    if (x.Access.IsReadRef)
                    {
                        State.SetVarRef(name);
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

                    State.SetVarInitialized(name);
                    State.SetVar(name, vartype);
                }

                x.TypeRefMask = vartype;
            }

            if (x.Access.IsWrite)
            {
                State.SetVarInitialized(name);
                State.SetVar(name, x.Access.WriteMask);
                State.LTInt64Max(name, false);

                x.TypeRefMask = x.Access.WriteMask;

                if (x.Access.IsWriteRef)
                {
                    State.SetVarRef(name);
                    x.TypeRefMask = x.TypeRefMask.WithRefFlag;
                }

                //
                if (x.Variable is BoundStaticLocal)
                {
                    // analysis has to be started over // TODO: start from the block which declares the static local variable
                    var startBlock = Routine.ControlFlowGraph.Start;
                    var startState = startBlock.FlowState;

                    var oldVar = startState.GetVarType(name);
                    if (oldVar != x.TypeRefMask)
                    {
                        startState.SetVar(name, x.TypeRefMask);
                        this.Worklist.Enqueue(startBlock);
                    }
                }
            }

            if (x.Access.IsUnset)
            {
                State.SetVar(name, 0);
                State.LTInt64Max(name, false);
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
        /// Resolves value of bit operation.
        /// </summary>
        /// <remarks>TODO: move to **evaluation**.</remarks>
        object ResolveBitOperation(object xobj, object yobj, Operations op)
        {
            if (xobj is string && yobj is string)
            {
                throw new NotImplementedException();    // bit op of chars
            }

            var x = Core.PhpValue.FromClr(xobj);
            var y = Core.PhpValue.FromClr(yobj);
            long result;

            // TODO: use PhpValue overriden operators

            switch (op)
            {
                case Operations.BitOr: result = x.ToLong() | y.ToLong(); break;
                case Operations.BitAnd: result = x.ToLong() & y.ToLong(); break;
                case Operations.BitXor: result = x.ToLong() ^ y.ToLong(); break;
                default:
                    throw new ArgumentException(nameof(op));
            }

            if (result >= int.MinValue && result <= int.MaxValue)
            {
                return (int)result;
            }
            else
            {
                return result;
            }
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

                    if (x.Left.ConstantObject.HasValue && x.Right.ConstantObject.HasValue)
                    {
                        x.ConstantObject = ResolveBitOperation(x.Left.ConstantObject.Value, x.Right.ConstantObject.Value, x.Operation);
                    }

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
                    return TypeCtx.GetLongTypeMask();   // TODO: or byte[]

                case Operations.Clone:
                    return x.Operand.TypeRefMask;

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
                    var vref = (BoundVariableRef)x.Operand;
                    if (vref.Name.IsDirect)
                    {
                        // if (Variable is T) => variable is T in True branch state
                        State.SetVar(vref.Name.NameValue.Value, TypeCtx.GetTypeMask(x.IsTypeDirect));
                    }
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

            x.TypeRefMask = TypeRefMask.AnyType;

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
            else if (operation is BoundGlobalFunctionCall)
            {
                VisitFunctionCall((BoundGlobalFunctionCall)operation);
            }
            else if (operation is BoundInstanceFunctionCall)
            {
                VisitFunctionCall((BoundInstanceFunctionCall)operation);
            }
            else if (operation is BoundStaticFunctionCall)
            {
                VisitFunctionCall((BoundStaticFunctionCall)operation);
            }
            else if (operation is BoundNewEx)
            {
                VisitNewEx((BoundNewEx)operation);
                return;
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

            //
            if (x.TargetMethod != null)
            {
                TypeRefMask result_type = 0;

                var args = x.ArgumentsInSourceOrder.Select(a => a.Value).ToImmutableArray();

                // reanalyse candidates
                foreach (var m in new[] { x.TargetMethod }) // TODO: all candidates
                {
                    // analyze TargetMethod with x.Arguments
                    // require method result type if access != none
                    var enqueued = this.Worklist.EnqueueRoutine(m, _analysis.CurrentBlock, args);
                    if (enqueued)   // => target has to be reanalysed
                    {
                        // note: continuing current block may be waste of time
                    }

                    result_type |= m.GetResultType(TypeCtx);
                }

                x.TypeRefMask = result_type;
            }

            //
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

        protected virtual void VisitFunctionCall(BoundGlobalFunctionCall x)
        {
            Visit(x.Name.NameExpression);

            if (x.Name.IsDirect)
            {
                var candidates = _model.ResolveFunction(x.Name.NameValue).Cast<MethodSymbol>().ToArray();
                if (candidates.Length == 0 && x.NameOpt.HasValue)
                {
                    candidates = _model.ResolveFunction(x.NameOpt.Value).Cast<MethodSymbol>().ToArray();
                }

                var args = x.ArgumentsInSourceOrder.Select(a => a.Value.TypeRefMask).ToArray();
                x.TargetMethod = new OverloadsList(candidates).Resolve(this.TypeCtx, args, null);
            }
        }

        protected virtual void VisitFunctionCall(BoundInstanceFunctionCall x)
        {
            Visit(x.Instance);
            Visit(x.Name.NameExpression);

            if (x.Name.IsDirect)
            {
                var typeref = TypeCtx.GetTypes(x.Instance.TypeRefMask);
                if (typeref.Count > 1)
                {
                    // TODO: some common base ?
                }

                if (typeref.Count == 1)
                {
                    var classtype = typeref.Where(t => t.IsObject).AsImmutable().SingleOrDefault();
                    if (classtype != null)
                    {
                        var type = (NamedTypeSymbol)_model.GetType(classtype.QualifiedName);
                        if (type != null)
                        {
                            var candidates = type.LookupMethods(x.Name.NameValue.Name.Value);
                            var args = x.ArgumentsInSourceOrder.Select(a => a.Value.TypeRefMask).ToArray();
                            x.TargetMethod = new OverloadsList(candidates.ToArray()).Resolve(this.TypeCtx, args, this.TypeCtx.ContainingType);
                        }
                    }
                }
            }
        }

        protected virtual void VisitFunctionCall(BoundStaticFunctionCall x)
        {
            VisitTypeRef(x.TypeRef);
            Visit(x.Name.NameExpression);

            if (x.Name.IsDirect && x.TypeRef.ResolvedType != null)
            {
                // TODO: resolve all candidates, visibility, static methods or instance on self/parent/static
                var candidates = x.TypeRef.ResolvedType.LookupMethods(x.Name.NameValue.Name.Value);
                // if (candidates.Any(c => c.HasThis)) throw new NotImplementedException("instance method called statically");

                var args = x.ArgumentsInSourceOrder.Select(a => a.Value.TypeRefMask).ToArray();
                x.TargetMethod = new OverloadsList(candidates.ToArray()).Resolve(this.TypeCtx, args, this.TypeCtx.ContainingType);
            }
        }

        protected virtual void VisitTypeRef(BoundTypeRef tref)
        {
            if (tref == null)
                return;

            Debug.Assert(tref.TypeRef.GenericParams.Count == 0, "Generics not implemented.");

            if (tref.TypeRef is DirectTypeRef)
            {
                var qname = ((DirectTypeRef)tref.TypeRef).ClassName;
                if (qname.IsReservedClassName)
                {
                    if (qname.IsSelfClassName)
                    {
                        tref.ResolvedType = TypeCtx.ContainingType ?? new MissingMetadataTypeSymbol(qname.ToString(), 0, false);
                    }
                    else if (qname.IsParentClassName)
                    {
                        tref.ResolvedType = TypeCtx.ContainingType?.BaseType ?? new MissingMetadataTypeSymbol(qname.ToString(), 0, false);
                    }
                    else if (qname.IsStaticClassName)
                    {
                        this.Routine.Flags |= RoutineFlags.UsesLateStatic;

                        throw new NotImplementedException("Late static bound type.");
                    }
                }
                else
                {
                    tref.ResolvedType = (TypeSymbol)_model.GetType(qname);
                }
            }

            Visit(tref.TypeExpression);
        }

        protected virtual void VisitNewEx(BoundNewEx x)
        {
            VisitTypeRef(x.TypeRef);

            // resolve target type
            var type = (NamedTypeSymbol)x.TypeRef.ResolvedType;
            if (type != null)
            {
                var candidates = type.InstanceConstructors.ToArray();

                //
                var args = x.ArgumentsInSourceOrder.Select(a => a.Value).ToImmutableArray();
                var argsType = args.Select(a => a.TypeRefMask).ToArray();

                x.TargetMethod = new OverloadsList(candidates).Resolve(this.TypeCtx, argsType, null);

                // reanalyse candidates
                foreach (var c in candidates)
                {
                    // analyze TargetMethod with x.Arguments
                    this.Worklist.EnqueueRoutine(c, _analysis.CurrentBlock, args);
                }

                x.ResultType = type;
            }

            x.TypeRefMask = TypeCtx.GetTypeMask(x.TypeRef.TypeRef, false);
        }

        protected virtual void VisitIncludeEx(BoundIncludeEx x)
        {
            this.Routine.Flags |= RoutineFlags.HasInclude;

            // resolve target script
            Debug.Assert(x.ArgumentsInSourceOrder.Length == 1);
            var targetExpr = x.ArgumentsInSourceOrder[0].Value;

            //
            x.Target = null;

            if (targetExpr.ConstantObject.HasValue)
            {
                var value = targetExpr.ConstantObject.Value as string;
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
            Visit(x.Instance);
            VisitTypeRef(x.ParentType);
            Visit(x.FieldName.NameExpression);

            if (x.IsInstanceField)  // {Instance}->FieldName
            {
                Debug.Assert(x.Instance != null);
                Debug.Assert(x.Instance.Access.IsRead);

                // resolve field if possible
                var typerefs = TypeCtx.GetTypes(x.Instance.TypeRefMask);
                if (typerefs.Count == 1 && typerefs[0].IsObject)
                {
                    // TODO: x.Instance.ResultType instead of following

                    var t = (NamedTypeSymbol)_model.GetType(typerefs[0].QualifiedName);
                    if (t != null)
                    {
                        if (x.FieldName.IsDirect)
                        {
                            // TODO: visibility and resolution (model)

                            var field = t.LookupMember<FieldSymbol>(x.FieldName.NameValue.Value);
                            if (field != null)
                            {
                                x.BoundReference = new BoundFieldPlace(x.Instance, field, x);
                                x.TypeRefMask = field.GetResultType(TypeCtx);
                                return;
                            }
                            else
                            {
                                var prop = t.LookupMember<PropertySymbol>(x.FieldName.NameValue.Value);
                                if (prop != null)
                                {
                                    x.BoundReference = new BoundPropertyPlace(x.Instance, prop);
                                    x.TypeRefMask = TypeRefFactory.CreateMask(TypeCtx, prop.Type);
                                    return;
                                }
                                else
                                {
                                    // TODO: use runtime fields directly, __get, __set, etc.,
                                    // do not fallback to BoundIndirectFieldPlace
                                }
                            }
                        }
                    }

                }

                // dynamic behavior
                // indirect field access ...

                x.BoundReference = new BoundIndirectFieldPlace(x.Instance, x.FieldName, x.Access);
                x.TypeRefMask = TypeRefMask.AnyType;
                return;
            }

            // static fields or constants
            if (x.IsStaticField || x.IsClassConstant)    // {ClassName}::${StaticFieldName}, {ClassName}::{ConstantName}
            {
                var ParentType = (NamedTypeSymbol)x.ParentType.ResolvedType;

                if (x.IsClassConstant)
                {
                    Debug.Assert(x.Access.IsRead);
                    Debug.Assert(!x.Access.IsEnsure && !x.Access.IsWrite && !x.Access.IsReadRef);
                }

                if (ParentType != null && x.FieldName.IsDirect)
                {
                    var field = ParentType.ResolveStaticField(x.FieldName.NameValue.Value);
                    if (field != null)
                    {
                        // TODO: visibility -> ErrCode

                        Debug.Assert(
                            field.IsConst ||    // .NET constant
                            field.IsStatic ||   // .NET static
                            field.ContainingType.TryGetStatics().LookupMember<FieldSymbol>(x.FieldName.NameValue.Value) != null); // or PHP context static

                        if (BindConstantValue(x, field))
                        {
                            Debug.Assert(x.Access.IsRead && !x.Access.IsWrite && !x.Access.IsEnsure);
                            x.BoundReference = null; // not reachable
                        }
                        else
                        {
                            x.BoundReference = field.IsStatic
                                ? new BoundFieldPlace(null, field, x)        // the field is real .NET static member (CLR static fields)
                                : new BoundPhpStaticFieldPlace(field, x);    // the field is contained in special __statics container (fields & constants)
                        }

                        x.TypeRefMask = field.GetResultType(TypeCtx);
                        return;
                    }
                    else if (x.IsStaticField)
                    {
                        // TODO: visibility
                        var prop = ParentType.LookupMember<PropertySymbol>(x.FieldName.NameValue.Value);
                        if (prop != null)
                        {
                            x.BoundReference = new BoundPropertyPlace(null, prop);
                            x.TypeRefMask = TypeRefFactory.CreateMask(TypeCtx, prop.Type);
                            return;
                        }
                    }

                    // TODO: __getStatic, __setStatic
                }

                // indirect field access:
                // indirect field access with known class name:
                x.BoundReference = new BoundIndirectStFieldPlace(x.ParentType, x.FieldName, x);
                x.TypeRefMask = TypeRefMask.AnyType;
                return;
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
            else if (operation is BoundIsEmptyEx)
            {
                VisitIsEmpty((BoundIsEmptyEx)operation);
            }
            else if (operation is BoundIsSetEx)
            {
                VisitsSet((BoundIsSetEx)operation);
            }
            else if (operation is BoundUnset)
            {
                VisitUnset((BoundUnset)operation);
            }
            else if (operation is BoundListEx)
            {
                VisitList((BoundListEx)operation);
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

        private void VisitIsEmpty(BoundIsEmptyEx x)
        {
            Visit(x.Variable);
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();
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

        public virtual void VisitList(BoundListEx x)
        {
            var elementtype = this.TypeCtx.GetElementType(x.Access.WriteMask);
            Debug.Assert(!elementtype.IsVoid);

            foreach (var v in x.Variables.WhereNotNull())   // list() may contain NULL implying ignored variable
            {
                v.Access = v.Access.WithWrite(elementtype);
                Visit(v);
            }
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

            // bind to app-wide constant if possible
            var constant = (FieldSymbol)_model.ResolveConstant(x.Name);
            if (!BindConstantValue(x, constant))
            {
                if (constant != null && constant.IsStatic && constant.IsReadOnly)
                {
                    x._boundExpressionOpt = new BoundFieldPlace(null, constant, x);
                    x.TypeRefMask = constant.GetResultType(TypeCtx);
                }
                else
                {
                    x.TypeRefMask = TypeRefMask.AnyType;    // only scalars ?
                }
            }
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

        public sealed override void VisitThrowStatement(IThrowStatement operation)
            => VisitThrowStatement((BoundThrowStatement)operation);

        protected virtual void VisitThrowStatement(BoundThrowStatement x)
        {
            Visit(x.Thrown);
        }

        #endregion
    }
}
