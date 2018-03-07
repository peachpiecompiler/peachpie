using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
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
    internal class ExpressionAnalysis : AnalysisVisitor
    {
        #region Fields & Properties

        /// <summary>
        /// Gets model for symbols resolution.
        /// </summary>
        internal ISymbolProvider/*!*/Model => _model;
        readonly ISymbolProvider/*!*/_model;

        /// <summary>
        /// Reference to corresponding source routine.
        /// </summary>
        protected SourceRoutineSymbol Routine => State.Routine;

        #endregion

        #region Helpers

        /// <summary>
        /// In case given expression is a local or parameter reference,
        /// gets its variable handle within <see cref="State"/>.
        /// </summary>
        VariableHandle TryGetVariableHandle(BoundExpression expr)
        {
            var varname = AsVariableName(expr as BoundReferenceExpression);
            if (varname.IsValid())
            {
                return State.GetLocalHandle(varname);
            }
            else
            {
                return default(VariableHandle);
            }
        }

        /// <summary>
        /// In case of a local variable or parameter, gets its name.
        /// </summary>
        VariableName AsVariableName(BoundReferenceExpression r)
        {
            var vr = r as BoundVariableRef;
            if (vr != null && (vr.Variable is BoundLocal || vr.Variable is BoundParameter))
            {
                return new VariableName(vr.Variable.Name);
            }

            return default(VariableName);
        }

        bool IsLongConstant(BoundExpression expr, long value)
        {
            if (expr.ConstantValue.HasValue)
            {
                if (expr.ConstantValue.Value is long) return ((long)expr.ConstantValue.Value) == value;
                if (expr.ConstantValue.Value is int) return ((int)expr.ConstantValue.Value) == value;
            }
            return false;
        }

        bool BindConstantValue(BoundExpression target, FieldSymbol symbol)
        {
            if (symbol != null && symbol.IsConst)
            {
                var cvalue = symbol.GetConstantValue(false);
                target.ConstantValue = (cvalue != null) ? new Optional<object>(cvalue.Value) : null;
                target.TypeRefMask = TypeRefFactory.CreateMask(TypeCtx, symbol.Type);

                return true;
            }

            return false;
        }

        internal TypeSymbol ResolveType(INamedTypeRef dtype)
        {
            if (dtype.ClassName.IsReservedClassName)
            {
                throw ExceptionUtilities.UnexpectedValue(dtype.ClassName);
            }

            return (TypeSymbol)_model.ResolveType(dtype.ClassName);
        }

        /// <summary>
        /// Finds the root of given chain, i.e.:
        /// $a : $a
        /// $$a : $a
        /// $a->b : $a
        /// $a[..] : $a
        /// $a->foo() : $a
        /// etc.
        /// </summary>
        /// <remarks>If given expression 'isset', its root returned by this method must be set as well.</remarks>
        internal BoundExpression TryGetExpressionChainRoot(BoundExpression x)
        {
            if (x != null)
            {
                if (x is BoundVariableRef v) return v.Name.IsDirect ? v : TryGetExpressionChainRoot(v.Name.NameExpression);
                if (x is BoundFieldRef f) return TryGetExpressionChainRoot(f.Instance ?? f.ContainingType?.TypeExpression);
                if (x is BoundInstanceFunctionCall m) return TryGetExpressionChainRoot(m.Instance);
                if (x is BoundArrayItemEx a) return TryGetExpressionChainRoot(a.Array);
            }

            return null;
        }

        /// <summary>
        /// Gets current visibility scope.
        /// </summary>
        protected OverloadsList.VisibilityScope VisibilityScope => OverloadsList.VisibilityScope.Create(TypeCtx.SelfType, Routine);

        #endregion

        #region Construction

        public ExpressionAnalysis(Worklist<BoundBlock> worklist, ISymbolProvider model)
            : base(worklist)
        {
            Debug.Assert(model != null);
            _model = model;
        }

        #endregion

        #region Declaration Statements

        public override void VisitStaticStatement(BoundStaticVariableStatement x)
        {
            var v = x.Declaration;
            var local = State.GetLocalHandle(new VariableName(v.Variable.Name));

            State.SetVarKind(local, VariableKind.StaticVariable);

            var oldtype = State.GetLocalType(local).WithRefFlag;

            // set var
            if (v.InitialValue != null)
            {
                // analyse initializer
                Accept(v.InitialValue);

                State.SetLessThanLongMax(local, (v.InitialValue.ConstantValue.HasValue && v.InitialValue.ConstantValue.Value is long && (long)v.InitialValue.ConstantValue.Value < long.MaxValue));
                State.SetLocalType(local, ((IPhpExpression)v.InitialValue).TypeRefMask | oldtype);
            }
            else
            {
                State.SetLessThanLongMax(local, false);
                State.SetLocalType(local, TypeCtx.GetNullTypeMask() | oldtype);
                // TODO: explicitly State.SetLocalUninitialized() ?
            }
        }

        public override void VisitGlobalStatement(BoundGlobalVariableStatement x)
        {
            base.VisitGlobalStatement(x);   // Accept(x.Variable)
        }

        #endregion

        #region Visit Literals

        public override void VisitLiteral(BoundLiteral x)
        {
            x.TypeRefMask = x.ResolveTypeMask(TypeCtx);
        }

        #endregion

        #region Visit Assignments

        public override void VisitAssign(BoundAssignEx x)
        {
            Debug.Assert(x.Target.Access.IsWrite);
            Debug.Assert(x.Value.Access.IsRead);

            //
            Accept(x.Value);

            // keep WriteRef flag
            var targetaccess = BoundAccess.None.WithWrite(x.Value.TypeRefMask);
            if (x.Target.Access.IsWriteRef)
            {
                targetaccess = targetaccess.WithWriteRef(0);
            }

            // new target access with resolved target type
            Visit(x.Target, targetaccess);

            //
            x.TypeRefMask = x.Value.TypeRefMask;
        }

        public override void VisitCompoundAssign(BoundCompoundAssignEx x)
        {
            Debug.Assert(x.Target.Access.IsRead && x.Target.Access.IsWrite);
            Debug.Assert(x.Value.Access.IsRead);

            // Target X Value
            var tmp = new BoundBinaryEx(x.Target.WithAccess(BoundAccess.Read), x.Value, AstUtils.CompoundOpToBinaryOp(x.Operation));
            Visit(tmp, ConditionBranch.AnyResult);

            // Target =
            Visit(x.Target, BoundAccess.Write.WithWrite(tmp.TypeRefMask));

            // put read access back
            x.Target.Access = x.Target.Access.WithRead();

            //
            x.TypeRefMask = tmp.TypeRefMask;
        }

        protected virtual void VisitSuperglobalVariableRef(BoundVariableRef x)
        {
            Debug.Assert(x.Name.IsDirect);
            Debug.Assert(x.Name.NameValue.IsAutoGlobal);

            // remember the initial state of variable at this point
            x.BeforeTypeRef = TypeRefMask.AnyType;

            // bind variable place
            x.Variable = Routine.LocalsTable.BindAutoGlobalVariable(x.Name.NameValue);

            // update state
            if (x.Access.IsRead)
            {
                var vartype = TypeCtx.GetArrayTypeMask();

                if (x.Access.IsReadRef)
                {
                    vartype = vartype.WithRefFlag;
                }

                if (x.Access.EnsureObject)
                {
                    // TODO: report ERR
                }

                // resulting type of the expression
                x.TypeRefMask = vartype;
            }

            if (x.Access.IsWrite)
            {
                x.TypeRefMask = x.Access.WriteMask;
            }

            if (x.Access.IsUnset)
            {
                x.TypeRefMask = TypeCtx.GetNullTypeMask();
            }
        }

        protected virtual void VisitLocalVariableRef(BoundVariableRef x, VariableHandle local)
        {
            Debug.Assert(local.IsValid);

            var previoustype = State.GetLocalType(local);       // type of the variable in the previous state

            // remember the initial state of variable at this point
            x.BeforeTypeRef = previoustype;

            // bind variable place
            if (x.Variable == null)
            {
                x.Variable = (x is BoundTemporalVariableRef)     // synthesized variable constructed by semantic binder
                    ? Routine.LocalsTable.BindTemporalVariable(local.Name)
                    : Routine.LocalsTable.BindLocalVariable(local.Name, x.PhpSyntax.Span.ToTextSpan());
            }

            //
            State.VisitLocal(local);

            // update state
            if (x.Access.IsRead)
            {
                var vartype = previoustype;

                if (vartype.IsVoid || Routine.IsGlobalScope)
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
                        State.MarkLocalByRef(local);
                        vartype.IsRef = true;
                    }
                    if (x.Access.EnsureObject && !TypeCtx.IsObject(vartype))
                    {
                        vartype |= TypeCtx.GetSystemObjectTypeMask();
                    }
                    if (x.Access.EnsureArray)
                    {
                        if (!TypeHelpers.HasArrayAccess(vartype, TypeCtx, model: Model))
                        {
                            vartype |= TypeCtx.GetArrayTypeMask();
                        }
                        else if (TypeCtx.IsReadonlyString(vartype))
                        {
                            vartype |= TypeCtx.GetWritableStringTypeMask();
                        }
                    }

                    State.SetLocalType(local, vartype);
                }
                else
                {
                    // reset 'MaybeUninitialized' flag:
                    x.MaybeUninitialized = false;

                    if (!State.IsLocalSet(local))
                    {
                        // do not flag as uninitialized if variable:
                        // - may be a reference
                        // - is in a global scope
                        if (!vartype.IsRef && !Routine.IsGlobalScope)
                        {
                            x.MaybeUninitialized = true;
                        }

                        // variable maybe null if it can be uninitialized
                        vartype |= TypeCtx.GetNullTypeMask();
                    }
                }

                // resulting type of the expression
                x.TypeRefMask = vartype;
            }

            if (x.Access.IsWrite)
            {
                var vartype = x.Access.WriteMask;

                if (x.Access.IsWriteRef || previoustype.IsRef)    // keep the ref flag of local
                {
                    vartype.IsRef = true;
                    State.MarkLocalByRef(local);
                }
                else if (vartype.IsRef)
                {
                    // // we can't be sure about the type
                    vartype = TypeRefMask.AnyType; // anything, not ref
                                                   //vartype.IsRef = false;  // the variable won't be a reference from this point
                }

                //
                State.SetLocalType(local, vartype);
                State.SetLessThanLongMax(local, false);
                x.TypeRefMask = vartype;
            }

            if (x.Access.IsUnset)
            {
                x.TypeRefMask = TypeCtx.GetNullTypeMask();
                State.SetLocalType(local, x.TypeRefMask);
                State.SetLessThanLongMax(local, false);
                State.SetVarUninitialized(local);
            }
        }

        public override void VisitVariableRef(BoundVariableRef x)
        {
            if (x.Name.IsDirect)
            {
                // direct variable access:
                if (x.Name.NameValue.IsAutoGlobal)
                {
                    VisitSuperglobalVariableRef(x);
                }
                else
                {
                    VisitLocalVariableRef(x, State.GetLocalHandle(x.Name.NameValue));
                }
            }
            else
            {
                x.BeforeTypeRef = TypeRefMask.AnyType;

                Accept(x.Name.NameExpression);

                // bind variable place
                if (x.Variable == null)
                {
                    x.Variable = new BoundIndirectLocal(x.Name.NameExpression);
                }

                // update state
                if (x.Access.IsRead)
                {
                    State.FlowContext.SetAllUsed();
                }

                if (x.Access.IsWrite || x.Access.IsEnsure)
                {
                    State.SetAllUnknown(x.Access.IsWriteRef);
                }

                if (x.Access.IsUnset)
                {

                }

                return;
            }
        }

        public override void VisitIncDec(BoundIncDecEx x)
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
            else if (State.IsLessThanLongMax(TryGetVariableHandle(x.Target)))    // we'd like to keep long if we are sure we don't overflow to double
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
        Optional<object> ResolveBitOperation(Optional<object> xobj, Optional<object> yobj, Operations op)
        {
            var xconst = xobj.ToConstantValueOrNull();
            var yconst = yobj.ToConstantValueOrNull();

            if (xconst.TryConvertToLong(out long xval) && yconst.TryConvertToLong(out long yval))
            {
                long result;

                switch (op)
                {
                    case Operations.BitOr: result = xval | yval; break;
                    case Operations.BitAnd: result = xval & yval; break;
                    case Operations.BitXor: result = xval ^ yval; break;
                    default:
                        throw new ArgumentException(nameof(op));
                }

                //
                if (result >= int.MinValue && result <= int.MaxValue)
                {
                    return (int)result;
                }
                else
                {
                    return result;
                }

                //
            }

            return default(Optional<object>);
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

                if (State.IsLessThanLongMax(TryGetVariableHandle(left)) && IsLongConstant(right, 1)) // LONG + 1, where LONG < long.MaxValue
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

        protected override void Visit(BoundBinaryEx x, ConditionBranch branch)
        {
            x.TypeRefMask = ResolveBinaryEx(x, branch);
        }

        TypeRefMask ResolveBinaryEx(BoundBinaryEx x, ConditionBranch branch)
        {
            if (x.Operation == Operations.And || x.Operation == Operations.Or)
            {
                this.VisitShortCircuitOp(x.Left, x.Right, x.Operation == Operations.And, branch);
            }
            else
            {
                Accept(x.Left);
                Accept(x.Right);
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

                    x.ConstantValue = ResolveBitOperation(x.Left.ConstantValue, x.Right.ConstantValue, x.Operation);
                    return GetBitOperationType(x.Left.TypeRefMask, x.Right.TypeRefMask);    // int or string

                #endregion

                #region Comparing Operations

                case Operations.Equal:
                case Operations.NotEqual:
                case Operations.Identical:
                case Operations.NotIdentical:

                    if (x.Left.IsConstant() && x.Right.IsConstant())
                    {
                        x.ConstantValue = ResolveComparison(x.Operation, x.Left.ConstantValue.Value, x.Right.ConstantValue.Value);
                    }

                    if (branch != ConditionBranch.AnyResult)
                    {
                        if (x.Right.ConstantValue.HasValue && x.Left is BoundReferenceExpression boundLeft)
                        {
                            ResolveEqualityWithConstantValue(x, boundLeft, x.Right.ConstantValue, branch);
                        }
                        else if (x.Left.ConstantValue.HasValue && x.Right is BoundReferenceExpression boundRight)
                        {
                            ResolveEqualityWithConstantValue(x, boundRight, x.Left.ConstantValue, branch);
                        }
                    }

                    return TypeCtx.GetBooleanTypeMask();

                case Operations.GreaterThan:
                case Operations.LessThan:
                case Operations.GreaterThanOrEqual:
                case Operations.LessThanOrEqual:

                    if (x.Left.IsConstant() && x.Right.IsConstant())
                    {
                        x.ConstantValue = ResolveComparison(x.Operation, x.Left.ConstantValue.Value, x.Right.ConstantValue.Value);
                    }

                    // comparison with long value
                    if (branch == ConditionBranch.ToTrue && IsLongOnly(x.Right))
                    {
                        if (x.Operation == Operations.LessThan)
                        {
                            // $x < LONG
                            State.SetLessThanLongMax(TryGetVariableHandle(x.Left), true);
                        }
                    }

                    return TypeCtx.GetBooleanTypeMask();

                #endregion

                case Operations.Concat:
                    return TypeCtx.GetWritableStringTypeMask();

                case Operations.Coalesce:   // Left ?? Right
                    return x.Left.TypeRefMask | x.Right.TypeRefMask;

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// If possible, resolve the comparison operation in compile-time.
        /// </summary>
        static Optional<object> ResolveComparison(Operations op, object lvalue, object rvalue)
        {
            // TODO

            //
            return default(Optional<object>);
        }

        /// <summary>
        /// Resolves variable types and potentially assigns a constant boolean value to an expression of a comparison of
        /// a variable and a constant - operators ==, !=, === and !==.
        /// </summary>
        private void ResolveEqualityWithConstantValue(
            BoundBinaryEx cmpExpr,
            BoundReferenceExpression refExpr,
            Optional<object> value,
            ConditionBranch branch)
        {
            Debug.Assert(branch != ConditionBranch.AnyResult);

            if (value.IsNull() && refExpr is BoundVariableRef varRef)
            {
                bool isStrict = (cmpExpr.Operation == Operations.Identical || cmpExpr.Operation == Operations.NotIdentical);
                bool isPositive = (cmpExpr.Operation == Operations.Equal || cmpExpr.Operation == Operations.Identical);

                // We cannot say much about the type of $x in the true branch of ($x == null) and the false branch of ($x != null),
                // because it holds for false, 0, "", array() etc.
                if (isStrict || branch.TargetValue() != isPositive)
                {
                    AnalysisFacts.HandleTypeCheckingExpression(
                        varRef,
                        TypeCtx.GetNullTypeMask(),
                        branch,
                        State,
                        checkExpr: cmpExpr,
                        isPositiveCheck: isPositive);
                }
            }
        }

        #endregion

        #region Visit UnaryEx

        protected override void Visit(BoundUnaryEx x, ConditionBranch branch)
        {
            x.TypeRefMask = ResolveUnaryOperatorExpression(x, branch);
        }

        TypeRefMask ResolveUnaryOperatorExpression(BoundUnaryEx x, ConditionBranch branch)
        {
            if (branch != ConditionBranch.AnyResult && x.Operation == Operations.LogicNegation)
            {
                // Negation swaps the branches
                VisitCondition(x.Operand, branch.NegativeBranch());
            }
            else
            {
                Accept(x.Operand);
            }

            // clear any previous resolved constant 
            x.ConstantValue = default(Optional<object>);

            //
            switch (x.Operation)
            {
                case Operations.AtSign:
                    return x.Operand.TypeRefMask;

                case Operations.BitNegation:
                    if (x.Operand.ConstantValue.HasValue)
                    {
                        if (x.Operand.ConstantValue.Value is long l)
                        {
                            x.ConstantValue = new Optional<object>(~l);
                        }
                        else if (x.Operand.ConstantValue.Value is int i)
                        {
                            x.ConstantValue = new Optional<object>(~(long)i);
                        }
                    }

                    return TypeCtx.GetLongTypeMask();   // TODO: or byte[]

                case Operations.Clone:
                    // result is always object, not aliased
                    return TypeCtx.GetObjectsFromMask(x.Operand.TypeRefMask).IsVoid
                        ? TypeCtx.GetSystemObjectTypeMask()                     // "object"
                        : TypeCtx.GetObjectsFromMask(x.Operand.TypeRefMask);    // (object)T

                case Operations.LogicNegation:
                    {
                        if (x.Operand.ConstantValue.TryConvertToBool(out bool constBool))
                        {
                            x.ConstantValue = ConstantValueExtensions.AsOptional(!constBool);
                        }
                        return TypeCtx.GetBooleanTypeMask();
                    }

                case Operations.Minus:
                    var cvalue = ResolveUnaryMinus(x.Operand.ConstantValue.ToConstantValueOrNull());
                    if (cvalue != null)
                    {
                        x.ConstantValue = new Optional<object>(cvalue.Value);
                        return TypeCtx.GetTypeMask(TypeRefFactory.Create(cvalue), false);
                    }
                    else
                    {
                        if (IsDoubleOnly(x.Operand))
                        {
                            return TypeCtx.GetDoubleTypeMask(); // double in case operand is double
                        }
                        return TypeCtx.GetNumberTypeMask();     // TODO: long in case operand is not a number
                    }

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
                    {
                        if (x.Operand.ConstantValue.TryConvertToBool(out bool constBool))
                        {
                            x.ConstantValue = ConstantValueExtensions.AsOptional(constBool);
                        }
                        return TypeCtx.GetBooleanTypeMask();
                    }

                case Operations.Int8Cast:
                case Operations.Int16Cast:
                case Operations.Int32Cast:
                case Operations.UInt8Cast:
                case Operations.UInt16Cast:
                // -->
                case Operations.UInt64Cast:
                case Operations.UInt32Cast:
                case Operations.Int64Cast:
                    {
                        if (x.Operand.ConstantValue.TryConvertToLong(out long l))
                        {
                            x.ConstantValue = new Optional<object>(l);
                        }
                        return TypeCtx.GetLongTypeMask();
                    }

                case Operations.DecimalCast:
                case Operations.DoubleCast:
                case Operations.FloatCast:
                    return TypeCtx.GetDoubleTypeMask();

                case Operations.UnicodeCast: // TODO
                case Operations.StringCast:
                    if (x.Operand.ConstantValue.TryConvertToString(out string str))
                    {
                        x.ConstantValue = new Optional<object>(str);
                    }
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

        ConstantValue ResolveUnaryMinus(ConstantValue value)
        {
            if (value != null)
            {
                switch (value.SpecialType)
                {
                    case SpecialType.System_Double:
                        return ConstantValue.Create(-value.DoubleValue);

                    case SpecialType.System_Int64:
                        return (value.Int64Value != long.MinValue)  // (- Int64.MinValue) overflows to double
                            ? ConstantValue.Create(-value.Int64Value)
                            : ConstantValue.Create(-(double)value.Int64Value);
                    default:
                        break;
                }
            }

            return null;
        }

        #endregion

        #region Visit InstanceOf

        protected override void Visit(BoundInstanceOfEx x, ConditionBranch branch)
        {
            Accept(x.Operand);
            x.AsType.Accept(this);

            // TOOD: x.ConstantValue // in case we know and the operand is a local variable (we can ignore the expression and emit result immediatelly)

            if (branch == ConditionBranch.ToTrue && x.Operand is BoundVariableRef)
            {
                var vref = (BoundVariableRef)x.Operand;
                if (vref.Name.IsDirect)
                {
                    // if (Variable is T) => variable is T in True branch state
                    var vartype = TypeCtx.GetTypeMask(x.AsType.TypeRef, true);
                    if (x.Operand.TypeRefMask.IsRef) vartype = vartype.WithRefFlag; // keep IsRef flag

                    State.SetLocalType(State.GetLocalHandle(vref.Name.NameValue), vartype);
                }
            }

            //
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();
        }

        #endregion

        #region Visit IsSet

        protected override void Visit(BoundIsSetEx x, ConditionBranch branch)
        {
            Accept(x.VarReference);

            // try to get resulting value and type of the variable
            var localname = AsVariableName(x.VarReference);
            if (localname.IsValid())
            {
                var handle = State.GetLocalHandle(localname);
                Debug.Assert(handle.IsValid);

                // Remove any constant value of isset()
                x.ConstantValue = default(Optional<object>);

                //
                if (State.IsLocalSet(handle))
                {
                    // If the variable is always defined, isset() behaves like !is_null()
                    var currenttype = State.GetLocalType(handle);

                    // a type in the true branch:
                    var positivetype = TypeCtx.WithoutNull(currenttype);

                    // resolve the constant if possible,
                    // does not depend on the branch
                    if (!currenttype.IsRef)
                    {
                        if (positivetype.IsVoid)    // always false
                        {
                            x.ConstantValue = ConstantValueExtensions.AsOptional(false);
                        }
                        else if (positivetype == currenttype && !currenttype.IsAnyType)   // not void nor null
                        {
                            x.ConstantValue = ConstantValueExtensions.AsOptional(true);
                        }
                    }

                    // we can be more specific in true/false branches:
                    if (branch != ConditionBranch.AnyResult)
                    {
                        // update target type in true/false branch:
                        var newtype = (branch == ConditionBranch.ToTrue)
                            ? positivetype
                            : TypeCtx.GetNullTypeMask();

                        // keep the ref flag!
                        newtype.IsRef = currenttype.IsRef;

                        //
                        State.SetLocalType(handle, newtype);
                    }
                }
                else
                {
                    // variable is not set for sure
                    // isset : false
                    x.ConstantValue = ConstantValueExtensions.AsOptional(false);
                }

                // mark variable as either initialized or uninintialized in respective branches
                if (branch == ConditionBranch.ToTrue)
                {
                    State.SetVarInitialized(handle);
                }
            }

            // always returns a boolean
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();
        }

        #endregion

        #region Visit Function Call

        protected override void VisitRoutineCall(BoundRoutineCall x)
        {
            x.TypeRefMask = TypeRefMask.AnyType;

            // TODO: write arguments Access
            // TODO: visit invocation member of
            // TODO: 2 pass, analyze arguments -> resolve method -> assign argument to parameter -> write arguments access -> analyze arguments again

            // visit arguments:
            base.VisitRoutineCall(x);
        }

        bool BindParams(PhpParam[] expectedparams, ImmutableArray<BoundArgument> givenargs)
        {
            for (int i = 0; i < givenargs.Length; i++)
            {
                if (givenargs[i].IsUnpacking)
                {
                    break;
                }

                if (i < expectedparams.Length)
                {
                    if (expectedparams[i].IsVariadic)
                    {
                        break;
                    }

                    BindParam(expectedparams[i], givenargs[i]);
                }
                else
                {
                    // argument cannot be bound
                    return false;
                }
            }

            return true;
        }

        void BindParam(PhpParam expected, BoundArgument givenarg)
        {
            // bind ref parameters to variables:
            if (expected.IsAlias || expected.IsByRef)  // => args[i] must be a variable
            {
                var refexpr = givenarg.Value as BoundReferenceExpression;
                if (refexpr != null)
                {
                    if (expected.IsByRef && !refexpr.Access.IsWrite)
                    {
                        SemanticsBinder.BindWriteAccess(refexpr);
                        Worklist.Enqueue(CurrentBlock);
                    }

                    if (expected.IsAlias && !refexpr.Access.IsReadRef)
                    {
                        SemanticsBinder.BindReadRefAccess(refexpr);
                        Worklist.Enqueue(CurrentBlock);
                    }

                    var refvar = refexpr as BoundVariableRef;
                    if (refvar != null)
                    {
                        if (refvar.Name.IsDirect)
                        {
                            var local = State.GetLocalHandle(refvar.Name.NameValue);
                            State.SetLocalType(local, expected.Type);
                            refvar.MaybeUninitialized = false;
                            if (expected.IsAlias)
                            {
                                State.MarkLocalByRef(local);
                            }
                        }
                        else
                        {
                            // TODO: indirect variable -> all may be aliases of any type
                        }
                    }
                    else
                    {
                        // fields, array items, ...
                        // TODO: remember the field will be accessed as reference
                    }
                }
                else
                {
                    // TODO: Err, variable or field must be passed into byref argument. foo("hello") where function foo(&$x){}
                }
            }
        }

        /// <summary>
        /// Bind arguments to target method and resolve resulting <see cref="BoundExpression.TypeRefMask"/>.
        /// Expecting <see cref="BoundRoutineCall.TargetMethod"/> is resolved.
        /// If the target method cannot be bound at compile time, <see cref="BoundRoutineCall.TargetMethod"/> is nulled.
        /// </summary>
        void BindTargetMethod(BoundRoutineCall x, bool maybeOverload = false)
        {
            if (false == x.TargetMethod.IsErrorMethodOrNull())
            {
                // analyze TargetMethod with x.Arguments
                // require method result type if access != none
                if (x.Access.IsRead)
                {
                    if (Worklist.EnqueueRoutine(x.TargetMethod, CurrentBlock))
                    {
                        // target will be reanalysed
                        // note: continuing current block may be waste of time
                    }
                }

                //
                Routine.Flags |= x.TargetMethod.InvocationFlags();
                x.TypeRefMask = x.TargetMethod.GetResultType(TypeCtx);

                // process arguments
                if (!BindParams(x.TargetMethod.GetExpectedArguments(this.TypeCtx), x.ArgumentsInSourceOrder) && maybeOverload)
                {
                    x.TargetMethod = null; // nullify the target method -> call dynamically, arguments cannot be bound at compile time
                }
            }

            //

            if (x.Access.IsReadRef)
            {
                // reading by ref:
                x.TypeRefMask = x.TypeRefMask.WithRefFlag;
            }
        }

        public override void VisitExit(BoundExitEx x)
        {
            VisitRoutineCall(x);
            BindTargetMethod(x);
        }

        public override void VisitEcho(BoundEcho x)
        {
            VisitRoutineCall(x);
            x.TypeRefMask = 0;
            BindTargetMethod(x);
        }

        public override void VisitConcat(BoundConcatEx x)
        {
            VisitRoutineCall(x);
            x.TypeRefMask = TypeCtx.GetWritableStringTypeMask();
            BindTargetMethod(x);
        }

        public override void VisitAssert(BoundAssertEx x)
        {
            VisitRoutineCall(x);
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();
        }

        MethodSymbol[] AsMethodOverloads(MethodSymbol method)
        {
            if (method is AmbiguousMethodSymbol && ((AmbiguousMethodSymbol)method).IsOverloadable)
            {
                return ((AmbiguousMethodSymbol)method).Ambiguities.ToArray();
            }

            return new[] { method };
        }

        public override void VisitGlobalFunctionCall(BoundGlobalFunctionCall x, ConditionBranch branch)
        {
            Accept(x.Name.NameExpression);

            VisitRoutineCall(x);

            if (x.Name.IsDirect)
            {
                var symbol = (MethodSymbol)_model.ResolveFunction(x.Name.NameValue);
                if (symbol.IsMissingMethod() && x.NameOpt.HasValue)
                {
                    symbol = (MethodSymbol)_model.ResolveFunction(x.NameOpt.Value);
                }

                // symbol might be ErrorSymbol

                x.TargetMethod = new OverloadsList(AsMethodOverloads(symbol)).Resolve(this.TypeCtx, x.ArgumentsInSourceOrder, VisibilityScope);
            }

            BindTargetMethod(x);

            // if possible resolve ConstantValue and TypeRefMask:
            AnalysisFacts.HandleSpecialFunctionCall(x, this, branch);
        }

        public override void VisitInstanceFunctionCall(BoundInstanceFunctionCall x)
        {
            Accept(x.Instance);
            Accept(x.Name.NameExpression);

            VisitRoutineCall(x);

            if (x.Name.IsDirect)
            {
                var resolvedtype = x.Instance.ResultType;
                if (resolvedtype == null)
                {
                    var typeref = TypeCtx.GetTypes(TypeCtx.WithoutNull(x.Instance.TypeRefMask));    // ignore NULL, causes runtime exception anyway
                    if (typeref.Count > 1)
                    {
                        // TODO: some common base ?
                    }

                    if (typeref.Count == 1)
                    {
                        var classtype = typeref.Where(t => t.IsObject).AsImmutable().SingleOrDefault();
                        if (classtype != null)
                        {
                            resolvedtype = (NamedTypeSymbol)_model.ResolveType(classtype.QualifiedName);
                        }
                    }
                }

                if (resolvedtype != null)
                {
                    var candidates = resolvedtype.LookupMethods(x.Name.NameValue.Name.Value);
                    x.TargetMethod = new OverloadsList(candidates).Resolve(this.TypeCtx, x.ArgumentsInSourceOrder, VisibilityScope);
                }
                else
                {
                    x.TargetMethod = null;
                }
            }

            BindTargetMethod(x, maybeOverload: true);
        }

        public override void VisitStaticFunctionCall(BoundStaticFunctionCall x)
        {
            VisitTypeRef(x.TypeRef);

            VisitRoutineCall(x);

            Accept(x.Name.NameExpression);

            if (x.Name.IsDirect && x.TypeRef.ResolvedType != null)
            {
                // TODO: resolve all candidates, visibility, static methods or instance on self/parent/static
                var candidates = x.TypeRef.ResolvedType.LookupMethods(x.Name.NameValue.Name.Value);
                // if (candidates.Any(c => c.HasThis)) throw new NotImplementedException("instance method called statically");

                x.TargetMethod = new OverloadsList(candidates).Resolve(this.TypeCtx, x.ArgumentsInSourceOrder, VisibilityScope);
            }

            BindTargetMethod(x);
        }

        TypeSymbol ResolveTypeRef(TypeRef tref, BoundExpression expr = null, bool objectTypeInfoSemantic = false)
        {
            if (tref is INamedTypeRef namedref)
            {
                if (tref is TranslatedTypeRef translatedref && translatedref.OriginalType is ReservedTypeRef)
                {
                    // resolve self or parent directly
                    var resolved = ResolveTypeRef(translatedref.OriginalType);
                    if (resolved.IsValidType())
                    {
                        return resolved;
                    }
                }

                return ResolveType(namedref);
            }
            else if (tref is ReservedTypeRef)
            {
                if (TypeCtx.SelfType == null || TypeCtx.SelfType.IsTraitType())
                {
                    // no self, parent, static resolvable in compile-time:
                    return new MissingMetadataTypeSymbol(tref.QualifiedName.ToString(), 0, false);
                }

                // resolve types that parser skipped
                switch (((ReservedTypeRef)tref).Type)
                {
                    case ReservedTypeRef.ReservedType.self:
                        return TypeCtx.SelfType;

                    case ReservedTypeRef.ReservedType.parent:
                        var btype = TypeCtx.SelfType.BaseType;
                        return (btype == null || btype.IsObjectType()) // no "System.Object" in PHP, invalid parent
                            ? new MissingMetadataTypeSymbol(tref.QualifiedName.ToString(), 0, false)
                            : btype;

                    case ReservedTypeRef.ReservedType.@static:
                        if (TypeCtx.SelfType.IsSealed)
                        {
                            // `static` == `self` <=> self is sealed
                            return TypeCtx.SelfType;
                        }
                        break;
                }
            }
            else if (tref is AnonymousTypeRef anonymousref)
            {
                return ((TypeSymbol)_model
                    .ResolveType(anonymousref.TypeDeclaration.GetAnonymousTypeQualifiedName()))
                    .ExpectValid();
            }
            else if (tref is IndirectTypeRef)
            {
                Debug.Assert(expr != null);

                // string:
                if (expr.ConstantValue.HasValue && expr.ConstantValue.Value is string tname)
                {
                    return (TypeSymbol)_model.ResolveType(NameUtils.MakeQualifiedName(tname, true));
                }
                else if (objectTypeInfoSemantic)
                {
                    // $this:
                    if (expr is BoundVariableRef varref && varref.Name.NameValue.IsThisVariableName)
                    {
                        if (TypeCtx.ThisType != null && TypeCtx.ThisType.IsSealed)
                        {
                            return TypeCtx.ThisType; // $this, self
                        }
                    }
                    //else if (IsClassOnly(tref.TypeExpression.TypeRefMask))
                    //{
                    //    // ...
                    //}
                }
            }

            //
            return null;
        }

        public override void VisitTypeRef(BoundTypeRef tref)
        {
            if (tref == null)
            {
                return;
            }

            Debug.Assert(!(tref is BoundMultipleTypeRef));

            // visit indirect type
            Accept(tref.TypeExpression);

            // resolve type symbol
            tref.ResolvedType = ResolveTypeRef(tref.TypeRef,
                expr: tref.TypeExpression,
                objectTypeInfoSemantic: tref.ObjectTypeInfoSemantic);
        }

        public override void VisitNew(BoundNewEx x)
        {
            VisitTypeRef(x.TypeRef);

            VisitRoutineCall(x);    // analyse arguments

            // resolve target type
            var type = (NamedTypeSymbol)x.TypeRef.ResolvedType;
            if (type.IsValidType())
            {
                var candidates = type.InstanceConstructors.ToArray();

                //
                x.TargetMethod = new OverloadsList(candidates).Resolve(this.TypeCtx, x.ArgumentsInSourceOrder, VisibilityScope);
                x.ResultType = type;
            }

            x.TypeRefMask = TypeCtx.GetTypeMask(x.TypeRef.TypeRef, false);
        }

        public override void VisitInclude(BoundIncludeEx x)
        {
            VisitRoutineCall(x);

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
                    var targetFile = _model.ResolveFile(value);
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

            // reset type analysis (include may change local variables)
            State.SetAllUnknown(true);

            //
            BindTargetMethod(x);
        }

        public override void VisitArgument(BoundArgument x)
        {
            if (x.Parameter != null)
            {
                // TODO: write arguments access
                // TODO: conversion by simplifier visitor
            }

            Accept(x.Value);
        }

        #endregion

        #region Visit FieldRef

        public override void VisitFieldRef(BoundFieldRef x)
        {
            Accept(x.Instance);
            VisitTypeRef(x.ContainingType);
            Accept(x.FieldName.NameExpression);

            if (x.IsInstanceField)  // {Instance}->FieldName
            {
                Debug.Assert(x.Instance != null);
                Debug.Assert(x.Instance.Access.IsRead);

                // resolve field if possible
                var resolvedtype = x.Instance.ResultType as NamedTypeSymbol;
                if (resolvedtype == null)
                {
                    var typerefs = TypeCtx.GetTypes(TypeCtx.WithoutNull(x.Instance.TypeRefMask));   // ignore NULL, would cause runtime exception in read access, will be ensured to non-null in write access
                    if (typerefs.Count == 1 && typerefs[0].IsObject)
                    {
                        resolvedtype = (NamedTypeSymbol)_model.ResolveType(typerefs[0].QualifiedName);
                    }
                }

                if (resolvedtype != null)
                {
                    if (x.FieldName.IsDirect)
                    {
                        var fldname = x.FieldName.NameValue.Value;
                        var member = resolvedtype.ResolveInstanceProperty(fldname);
                        if (member != null && member.IsAccessible(this.TypeCtx.SelfType))
                        {
                            if (member is FieldSymbol)
                            {
                                var field = (FieldSymbol)member;
                                var srcf = field as SourceFieldSymbol;
                                var overridenf = srcf?.OverridenDefinition;

                                // field might be a redefinition with a different accessibility,
                                // such field is not declared actually and the base definition is used instead:

                                if (overridenf == null || overridenf.IsAccessible(this.TypeCtx.SelfType))
                                {
                                    x.BoundReference = new BoundFieldPlace(x.Instance, overridenf ?? field, x);
                                    x.TypeRefMask = field.GetResultType(TypeCtx);
                                    x.ResultType = field.Type;
                                    return;
                                }
                                else if (srcf != null && srcf.FieldAccessorProperty != null && srcf.FieldAccessorProperty.IsAccessible(TypeCtx.SelfType))
                                {
                                    member = srcf.FieldAccessorProperty; // use the wrapping property that is accessible from current context
                                    // -> continue
                                }
                                else
                                {
                                    member = null; // -> dynamic behavior
                                    // -> continue
                                }
                            }

                            if (member is PropertySymbol)
                            {
                                var prop = (PropertySymbol)member;
                                x.BoundReference = new BoundPropertyPlace(x.Instance, prop);
                                x.TypeRefMask = TypeRefFactory.CreateMask(TypeCtx, prop.Type);
                                x.ResultType = prop.Type;
                                return;
                            }

                            //
                            if (member != null)
                            {
                                throw ExceptionUtilities.UnexpectedValue(member);
                            }
                        }
                        else
                        {
                            // TODO: use runtime fields directly, __get, __set, etc.,
                            // do not fallback to BoundIndirectFieldPlace
                        }
                    }
                }

                // dynamic behavior
                // indirect field access ...

                x.BoundReference = new BoundIndirectFieldPlace(x);
                x.TypeRefMask = TypeRefMask.AnyType;
                return;
            }

            // static fields or constants
            if (x.IsStaticField || x.IsClassConstant)    // {ClassName}::${StaticFieldName}, {ClassName}::{ConstantName}
            {
                var containingType = (NamedTypeSymbol)x.ContainingType.ResolvedType;

                if (x.IsClassConstant)
                {
                    Debug.Assert(x.Access.IsRead);
                    Debug.Assert(!x.Access.IsEnsure && !x.Access.IsWrite && !x.Access.IsReadRef);
                }

                if (containingType != null && x.FieldName.IsDirect)
                {
                    var fldname = x.FieldName.NameValue.Value;
                    var field = x.IsStaticField ? containingType.ResolveStaticField(fldname) : containingType.ResolveClassConstant(fldname);
                    if (field != null)
                    {
                        // TODO: visibility -> ErrCode

                        if (BindConstantValue(x, field))
                        {
                            Debug.Assert(x.Access.IsRead && !x.Access.IsWrite && !x.Access.IsEnsure);
                            x.BoundReference = null; // not reachable
                        }
                        else
                        {
                            // real.NET static member (CLR static fields) or
                            // the field may be contained in special __statics container (fields & constants)
                            x.BoundReference = new BoundFieldPlace(null, field, x);
                        }

                        x.TypeRefMask = field.GetResultType(TypeCtx);
                        return;
                    }
                    else if (x.IsStaticField)
                    {
                        // TODO: visibility
                        var prop = containingType.LookupMember<PropertySymbol>(fldname);
                        if (prop != null && prop.IsStatic)
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
                x.BoundReference = new BoundIndirectStFieldPlace(x.ContainingType, x.FieldName, x);
                x.TypeRefMask = TypeRefMask.AnyType;
                return;
            }
        }

        #endregion

        #region Visit ArrayEx, ArrayItemEx

        public override void VisitArray(BoundArrayEx x)
        {
            var items = x.Items;
            TypeRefMask elementType = 0;

            // analyse elements
            foreach (var i in items)
            {
                Debug.Assert(i.Value != null);

                Accept(i.Key);
                Accept(i.Value);

                elementType |= i.Value.TypeRefMask;
            }

            // writeup result type
            x.TypeRefMask = elementType.IsVoid
                ? TypeCtx.GetArrayTypeMask()
                : TypeCtx.GetArrayTypeMask(elementType);
        }

        public override void VisitArrayItem(BoundArrayItemEx x)
        {
            Accept(x.Array);
            Accept(x.Index);

            // TODO: resulting type if possible:
            // var element_type = TypeCtx.GetElementType(x.Array.TypeRefMask); // + handle classes with ArrayAccess and TypeRefMask.Uninitialized

            //
            x.TypeRefMask = x.Access.IsReadRef
                ? TypeRefMask.AnyType.WithRefFlag
                : TypeRefMask.AnyType;
        }

        #endregion

        #region VisitLambda

        public override void VisitLambda(BoundLambda x)
        {
            var container = (ILambdaContainerSymbol)Routine.ContainingFile;
            var symbol = container.ResolveLambdaSymbol((LambdaFunctionExpr)x.PhpSyntax);
            if (symbol == null)
            {
                throw ExceptionUtilities.UnexpectedValue(symbol);
            }

            // bind arguments to parameters
            var ps = symbol.SourceParameters;

            // first {N} source parameters correspond to "use" parameters
            for (int pi = 0; pi < x.UseVars.Length; pi++)
            {
                x.UseVars[pi].Parameter = ps[pi];
            }

            x.UseVars.ForEach(VisitArgument);

            //
            x.BoundLambdaMethod = symbol;
            x.ResultType = Routine.DeclaringCompilation.CoreTypes.Closure;
            Debug.Assert(x.ResultType != null);
            x.TypeRefMask = TypeCtx.GetTypeMask(new LambdaTypeRef(TypeRefMask.AnyType, symbol.SyntaxSignature), false); // specific {Closure}, no null, no subclasses
        }

        #endregion

        #region VisitYield

        public override void VisitYieldStatement(BoundYieldStatement x)
        {
            base.VisitYieldStatement(x);
        }

        public override void VisitYieldEx(BoundYieldEx x)
        {
            base.VisitYieldEx(x);
            x.TypeRefMask = TypeRefMask.AnyType;
        }

        public override void VisitYieldFromEx(BoundYieldFromEx x)
        {
            base.VisitYieldFromEx(x);
            x.TypeRefMask = TypeRefMask.AnyType;
        }

        #endregion

        #region Visit

        public override void VisitIsEmpty(BoundIsEmptyEx x)
        {
            Accept(x.Operand);
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();
        }

        public override void VisitUnset(BoundUnset x)
        {
            base.VisitUnset(x);
        }

        public override void VisitList(BoundListEx x)
        {
            var elementtype = this.TypeCtx.GetElementType(x.Access.WriteMask);
            Debug.Assert(!elementtype.IsVoid);

            foreach (var v in x.Items)   // list() may contain NULL implying ignored variable
            {
                if (v.Value != null)
                {
                    Accept(v.Key);
                    Visit(v.Value, v.Value.Access.WithWrite(elementtype));
                }
            }
        }

        public override void VisitPseudoConstUse(BoundPseudoConst x)
        {
            object value = null;

            switch (x.Type)
            {
                case PseudoConstUse.Types.Line:
                    value = x.PhpSyntax.ContainingSourceUnit.GetLineFromPosition(x.PhpSyntax.Span.Start) + 1;
                    break;

                case PseudoConstUse.Types.Class:
                case PseudoConstUse.Types.Trait:
                    {
                        var containingtype = x.PhpSyntax.ContainingType;
                        if (containingtype != null)
                        {
                            var intrait = containingtype.MemberAttributes.IsTrait();

                            value = containingtype.QualifiedName.ToString();

                            if (intrait && x.Type == PseudoConstUse.Types.Class)
                            {
                                // __CLASS__ inside trait resolved in runtime
                                x.TypeRefMask = TypeCtx.GetStringTypeMask();
                                return;
                            }

                            if (!intrait && x.Type == PseudoConstUse.Types.Trait)
                            {
                                // __TRAIT__ inside class is empty string
                                value = string.Empty;
                            }
                        }
                        else
                        {
                            value = string.Empty;
                        }
                    }
                    break;

                case PseudoConstUse.Types.Method:
                    if (Routine == null)
                    {
                        value = string.Empty;
                    }
                    else if (Routine is SourceLambdaSymbol)
                    {
                        // value = __CLASS__::"{closure}"; // PHP 5
                        value = "{closure}";    // PHP 7+
                    }
                    else
                    {
                        var containingtype = x.PhpSyntax.ContainingType;
                        value = containingtype != null
                            ? containingtype.QualifiedName.ToString(new Name(Routine.RoutineName), false)
                            : Routine.RoutineName;
                    }
                    break;

                case PseudoConstUse.Types.Function:
                    if (Routine is SourceLambdaSymbol)
                    {
                        value = "{closure}";
                    }
                    else
                    {
                        value = Routine != null
                            ? Routine.RoutineName
                            : string.Empty;
                    }
                    break;

                case PseudoConstUse.Types.Namespace:
                    var ns = x.PhpSyntax.ContainingNamespace;
                    value = ns != null && ns.QualifiedName.HasValue
                        ? ns.QualifiedName.QualifiedName.NamespacePhpName
                        : string.Empty;
                    break;

                case PseudoConstUse.Types.Dir:
                case PseudoConstUse.Types.File:
                    x.TypeRefMask = TypeCtx.GetStringTypeMask();
                    return;

                default:
                    throw ExceptionUtilities.UnexpectedValue(x.Type);
            }

            Debug.Assert(value != null);    // pseudoconstant has been set

            x.ConstantValue = new Optional<object>(value);

            if (value is string) x.TypeRefMask = TypeCtx.GetStringTypeMask();
            else if (value is int || value is long) x.TypeRefMask = TypeCtx.GetLongTypeMask();
            else throw ExceptionUtilities.UnexpectedValue(value);
        }

        public override void VisitPseudoClassConstUse(BoundPseudoClassConst x)
        {
            base.VisitPseudoClassConstUse(x);

            //
            if (x.Type == PseudoClassConstUse.Types.Class)
            {
                x.TypeRefMask = TypeCtx.GetStringTypeMask();

                var qname = x.TargetType.TypeRef.QualifiedName;
                if (qname.HasValue)
                {
                    if (qname.Value.IsReservedClassName) // self, static, parent
                    {
                        if (x.TargetType.ResolvedType.IsValidType() && x.TargetType.ResolvedType is IPhpTypeSymbol phpt)
                        {
                            x.ConstantValue = new Optional<object>(phpt.FullName.ToString());
                        }
                    }
                    else
                    {
                        x.ConstantValue = new Optional<object>(qname.Value.ToString());
                    }
                }
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(x.Type);
            }
        }

        public override void VisitGlobalConstUse(BoundGlobalConst x)
        {
            // TODO: check constant name

            // bind to app-wide constant if possible
            var constant = (FieldSymbol)_model.ResolveConstant(x.Name.ToString());
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

        public override void VisitConditional(BoundConditionalEx x)
        {
            var state = State;
            var trueExpr = x.IfTrue ?? x.Condition;

            // true branch
            var trueState = State = state.Clone();
            VisitCondition(x.Condition, ConditionBranch.ToTrue);
            Accept(trueExpr);

            // false branch
            var falseState = State = state.Clone();
            VisitCondition(x.Condition, ConditionBranch.ToFalse);
            Accept(x.IfFalse);

            // merge both states
            State = trueState.Merge(falseState);

            // merge resulting types
            var trueTypeMask = trueExpr.TypeRefMask;
            if (x.Condition == trueExpr)
            {
                // condition != false => condition != null => trueExpr cannot be NULL:
                trueTypeMask = TypeCtx.WithoutNull(trueTypeMask);
            }

            x.TypeRefMask = trueTypeMask | x.IfFalse.TypeRefMask;
        }

        public override void VisitExpressionStatement(BoundExpressionStatement x)
        {
            base.VisitExpressionStatement(x);
        }

        public override void VisitReturn(BoundReturnStatement x)
        {
            if (x.Returned != null)
            {
                Accept(x.Returned);
                State.FlowThroughReturn(x.Returned.TypeRefMask);

                // reanalyse blocks depending on this routine return type
                EnqueueSubscribers((ExitBlock)this.Routine?.ControlFlowGraph.Exit);
            }
            else
            {
                // remember "void" type explicitly
                State.FlowThroughReturn(0);
            }
        }

        public override void VisitThrow(BoundThrowStatement x)
        {
            Accept(x.Thrown);
        }

        public override void VisitEval(BoundEvalEx x)
        {
            base.VisitEval(x);

            //
            State.SetAllUnknown(true);

            //
            x.TypeRefMask = TypeRefMask.AnyType;
        }

        #endregion
    }
}
