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

                return TypeCtx.GetTypes(tmask)
                    .All(t => t.TypeCode == Core.PhpTypeCode.Long || t.TypeCode == Core.PhpTypeCode.Double);
            }

            return false;
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
                //if (condition is InstanceOfEx)
                //{
                //    VisitInstanceOfEx((InstanceOfEx)condition, branch);
                //    return;
                //}
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

        #endregion

        #region AccessType

        /// <summary>
        /// Access type - describes context within which an expression is used.
        /// </summary>
        [Flags]
        protected enum AccessFlags : byte
        {
            /// <summary>
            /// Expression is being read from.
            /// </summary>
            Read = 0,

            /// <summary>
            /// Expression is LValue of an assignment. Do not report uninitialized var use.
            /// </summary>
            Write = 1,

            /// <summary>
            /// Expression is LValue accessed with array item operator.
            /// </summary>
            EnsureArray = 2,

            /// <summary>
            /// Expression is LValue accessed by object operator (<c>-></c>).
            /// </summary>
            EnsureProperty = 4,

            /// <summary>
            /// Expression is being checked within <c>isset</c> function call. Do not report uninitialized var use.
            /// </summary>
            IsCheck = 8,

            /// <summary>
            /// Access by reference, read or write.
            /// </summary>
            IsRef = 16,

            /// <summary>
            /// Expression is used as a statement, return value is not used.
            /// </summary>
            None = Read,
        }

        #endregion

        #region Access

        protected struct Access : IEquatable<Access>
        {
            public bool IsRead { get { return !IsWrite; } }
            public bool IsWrite { get { return (flags & AccessFlags.Write) != 0; } }
            public bool IsCheck { get { return (flags & AccessFlags.IsCheck) != 0; } }
            public bool IsEnsureArray { get { return (flags & AccessFlags.EnsureArray) != 0; } }
            public bool IsEnsureHasProperty { get { return (flags & AccessFlags.EnsureProperty) != 0; } }
            public bool IsRef
            {
                get { return (flags & AccessFlags.IsRef) != 0; }
                set { if (value) flags |= AccessFlags.IsRef; else flags &= ~AccessFlags.IsRef; }
            }

            /// <summary>
            /// Type of expression access.
            /// </summary>
            private AccessFlags flags;

            /// <summary>
            /// In case of Write* access, specifies the type being written.
            /// </summary>
            public TypeRefMask RValueType;

            /// <summary>
            /// Span of right value for error reporting.
            /// </summary>
            public Span RValueSpan;

            public static Access Read { get { return new Access() { flags = AccessFlags.Read }; } }
            public static Access Check { get { return new Access() { flags = AccessFlags.IsCheck | AccessFlags.Read }; } }
            public static Access Write(TypeRefMask rValueType, Span rValueSpan) { return new Access() { flags = AccessFlags.Write, RValueType = rValueType, RValueSpan = rValueSpan }; }
            public static Access EnsureArray(TypeRefMask elementType) { return new Access() { flags = AccessFlags.EnsureArray | AccessFlags.Read, RValueType = elementType }; }

            #region IEquatable<Access>

            public bool Equals(Access other)
            {
                return this.flags == other.flags && this.RValueType == other.RValueType;
            }

            public override bool Equals(object obj)
            {
                return obj is Access && Equals((Access)obj);
            }

            public override int GetHashCode() => (int)flags ^ (int)RValueType.Mask;

            public static bool operator ==(Access x, Access y) => x.Equals(y);
            public static bool operator !=(Access x, Access y) => !x.Equals(y);

            #endregion

            /// <summary>
            /// Creates new <see cref="Access"/> with given check state.
            /// </summary>
            public static Access operator &(Access a, Access check)
            {
                a.flags |= (check.flags & AccessFlags.IsCheck);
                return a;
            }


        }

        #endregion

        #region Visit with Access

        protected void Visit(IOperation x, Access access)
        {
            if (access == Access.Read)
            {
                Visit(x);
                return;
            }

            //
            if (x is BoundVariableRef)
            {
                VisitBoundVariableRef((BoundVariableRef)x, access);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Visit Specialized

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

        #region Visit Literals

        public sealed override void VisitLiteralExpression(ILiteralExpression operation)
            => ((BoundExpression)operation).TypeRefMask = ((BoundLiteral)operation).ResolveTypeMask(TypeCtx);

        #endregion

        #region Visit Assignments

        public sealed override void VisitAssignmentExpression(IAssignmentExpression operation)
        => ((BoundExpression)operation).TypeRefMask = VisitAssignmentExpression((BoundAssignEx)operation);

        protected virtual TypeRefMask VisitAssignmentExpression(BoundAssignEx op)
        {
            Debug.Assert(op.Target.Access == AccessType.Write || op.Target.Access == AccessType.WriteRef);
            Debug.Assert(op.Value.Access == AccessType.Read || op.Value.Access == AccessType.ReadRef);

            Visit(op.Value);

            var targetAccess = Access.Write(op.Value.TypeRefMask, Span.Invalid);
            targetAccess.IsRef = (op.Target.Access == AccessType.WriteRef);

            Visit(op.Target, targetAccess);

            //
            return op.Value.TypeRefMask;
        }

        public override void VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation)
        {
            throw new NotImplementedException();
        }

        public override void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        {
            var expr = (BoundExpression)operation;

            if (expr is BoundVariableRef)
            {
                VisitBoundVariableRef((BoundVariableRef)expr, Access.Read);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected virtual void VisitBoundVariableRef(BoundVariableRef v, Access access)
        {
            if (access.IsRead)
            {
                State.SetVarUsed(v.Name);
                v.TypeRefMask = State.GetVarType(v.Name);
            }
            else if (access.IsWrite)
            {
                Debug.Assert(!access.RValueType.IsUninitialized);
                State.SetVarInitialized(v.Name);
                State.SetVar(v.Name, access.RValueType);
                State.LTInt64Max(v.Name, false);

                if (access.IsRef)
                {
                    State.SetVarRef(v.Name);
                }

                v.TypeRefMask = access.RValueType;
            }
        }

        public sealed override void VisitIncrementExpression(IIncrementExpression operation)
            => VisitIncrementExpression((BoundIncDecEx)operation);

        protected virtual void VisitIncrementExpression(BoundIncDecEx x)
        {
            // <target> = <target> +/- 1L

            Debug.Assert(x.Access == AccessType.Read || x.Access == AccessType.None);
            Debug.Assert(x.Target.Access == AccessType.ReadAndWrite);

            Visit(x.Target, Access.Read);
            Visit(x.Value, Access.Read);

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

            Visit(x.Target, Access.Write(resulttype, Span.Invalid));

            //
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
                    throw new NotImplementedException();

                case Operations.Plus:
                    throw new NotImplementedException();

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

        #region Visit Function Call

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
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
            else if (operation is BoundNewEx)
            {
                VisitNewEx((BoundNewEx)operation);
            }
            else
            {
                throw new NotImplementedException();
            }
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
            // resolve function symbol
            var candidates = _model.ResolveFunction(x.Name).ToImmutableArray();
            if (candidates.IsEmpty && x.AlternativeName.HasValue)
                candidates = _model.ResolveFunction(x.AlternativeName.Value).ToImmutableArray();

            // TODO: overload resolution

            if (candidates.Length == 0)
            {
                //x.TargetMethod = new MissingMethod(x.Name)
                throw new NotImplementedException("unknown function");
            }
            else if (candidates.Length == 1)
            {
                x.TargetMethod = (MethodSymbol)candidates[0];

                // bind parameters to arguments
                var parameters = x.TargetMethod.Parameters;
                var args = x.ArgumentsInSourceOrder;

                int argindex = 0;
                foreach (var p in x.TargetMethod.Parameters)
                {
                    if (!p.IsImplicitlyDeclared && argindex < args.Length)
                    {
                        args[argindex++].Parameter = p;
                    }
                }

                // analyze TargetMethod with x.Arguments
                // require method result type if access != none
                var enqueued = this.Worklist.EnqueueRoutine(x.TargetMethod, _analysis.CurrentBlock, args);
                if (enqueued)   // => target has to be reanalysed
                {
                    if (x.Access != AccessType.None)    // => and we need the return type
                    {
                        // TODO:
                        // throw to cancel current block analysis ?
                        // continue with void ?
                    }
                }

                //
                x.TypeRefMask = x.TargetMethod.GetResultType(TypeCtx);
            }
            else
            {
                throw new NotImplementedException("ambiguous call");
            }
        }

        protected virtual void VisitNewEx(BoundNewEx x)
        {
            x.TypeRefMask = TypeCtx.GetTypeMask(x.TypeName, false);

            // resolve target type
            var type = _model.GetType(x.TypeName);
            if (type != null)
            {
                x.ResultType = type;

                ImmutableArray<IMethodSymbol> candidates;

                // resolve .ctor method (CtorMethod)
                candidates = type.InstanceConstructors;
                if (candidates.Length != 1) throw new NotImplementedException();
                x.CtorMethod = (MethodSymbol)candidates[0];

                // resolve __construct method (TargetMethod)
                candidates = type.GetMembers(Name.SpecialMethodNames.Construct.Value).OfType<IMethodSymbol>().Where(c => !c.IsStatic).AsImmutable();
                if (candidates.Length == 1)
                    x.TargetMethod = (MethodSymbol)candidates[0];
                else if (candidates.Length > 1)
                    throw new NotImplementedException();
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

        #region Visit

        public override void DefaultVisit(IOperation operation)
        {
            throw new NotImplementedException();
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
            Visit(x.Returned);
            State.FlowThroughReturn(x.Returned.TypeRefMask);
        }

        #endregion
    }
}
