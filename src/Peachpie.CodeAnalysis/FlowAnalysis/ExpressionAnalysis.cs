using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Semantics.TypeRef;
using Pchp.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ast = Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Visits single expressions and project transformations to flow state.
    /// </summary>
    internal class ExpressionAnalysis<T> : AnalysisWalker<FlowState, T>
    {
        #region Fields & Properties

        /// <summary>
        /// The worklist to be used to enqueue next blocks.
        /// </summary>
        internal Worklist<BoundBlock> Worklist { get; }

        /// <summary>
        /// Gets model for symbols resolution.
        /// </summary>
        internal ISymbolProvider/*!*/Model => _model;
        readonly ISymbolProvider/*!*/_model;

        /// <summary>
        /// Reference to corresponding source routine.
        /// </summary>
        protected SourceRoutineSymbol Routine => State.Routine;

        /// <summary>
        /// Gets current type context for type masks resolving.
        /// </summary>
        internal TypeRefContext TypeCtx => State.TypeRefContext;

        protected PhpCompilation DeclaringCompilation => _model.Compilation;

        protected BoundTypeRefFactory BoundTypeRefFactory => DeclaringCompilation.TypeRefFactory;

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
            if (r is BoundVariableRef vr)
            {
                return vr.Name.NameValue;
            }

            return default;
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

                if (cvalue != null && cvalue.IsNull)
                {
                    target.TypeRefMask = TypeCtx.GetNullTypeMask();
                    return true;
                }

                target.TypeRefMask = TypeRefFactory.CreateMask(TypeCtx, symbol.Type, notNull: true);

                return true;
            }

            return false;
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
                if (x is BoundFieldRef f) return TryGetExpressionChainRoot(f.Instance ?? (f.ContainingType as BoundIndirectTypeRef)?.TypeExpression);
                if (x is BoundInstanceFunctionCall m) return TryGetExpressionChainRoot(m.Instance);
                if (x is BoundArrayItemEx a) return TryGetExpressionChainRoot(a.Array);
            }

            return null;
        }

        /// <summary>
        /// Gets current visibility scope.
        /// </summary>
        protected OverloadsList.VisibilityScope VisibilityScope => new OverloadsList.VisibilityScope(TypeCtx.SelfType, Routine);

        protected void PingSubscribers(ExitBlock exit)
        {
            if (exit != null)
            {
                var wasNotAnalysed = false;

                if (Routine != null && !Routine.IsReturnAnalysed)
                {
                    Routine.IsReturnAnalysed = true;
                    wasNotAnalysed = true;
                }

                // Ping the subscribers either if the return type has changed or
                // it is the first time the analysis reached the routine exit
                var rtype = State.GetReturnType();
                if (rtype != exit._lastReturnTypeMask || wasNotAnalysed)
                {
                    exit._lastReturnTypeMask = rtype;
                    var subscribers = exit.Subscribers;
                    if (subscribers.Count != 0)
                    {
                        lock (subscribers)
                        {
                            foreach (var subscriber in subscribers)
                            {
                                Worklist.PingReturnUpdate(exit, subscriber);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets value indicating the given type represents a double and nothing else.
        /// </summary>
        protected bool IsDoubleOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && this.TypeCtx.IsDouble(tmask);
        }

        /// <summary>
        /// Gets value indicating the given type represents a double and nothing else.
        /// </summary>
        protected bool IsDoubleOnly(BoundExpression x) => IsDoubleOnly(x.TypeRefMask);

        /// <summary>
        /// Gets value indicating the given type represents a long and nothing else.
        /// </summary>
        protected bool IsLongOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && this.TypeCtx.IsLong(tmask);
        }

        /// <summary>
        /// Gets value indicating the given type represents a long and nothing else.
        /// </summary>
        protected bool IsLongOnly(BoundExpression x) => IsLongOnly(x.TypeRefMask);

        /// <summary>
        /// Gets value indicating the given type is long or double or both but nothing else.
        /// </summary>
        /// <param name="tmask"></param>
        /// <returns></returns>
        protected bool IsNumberOnly(TypeRefMask tmask)
        {
            if (TypeCtx.IsLong(tmask) || TypeCtx.IsDouble(tmask))
            {
                if (tmask.IsSingleType)
                {
                    return true;
                }

                return !tmask.IsAnyType && TypeCtx.GetTypes(tmask).AllIsNumber();
            }

            return false;
        }

        /// <summary>
        /// Gets value indicating the given type represents only class types.
        /// </summary>
        protected bool IsClassOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsAnyType && TypeCtx.GetTypes(tmask).AllIsObject();
        }

        /// <summary>
        /// Gets value indicating the given type represents only array types.
        /// </summary>
        protected bool IsArrayOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsAnyType && TypeCtx.GetTypes(tmask).AllIsArray();
        }

        /// <summary>
        /// Gets value indicating the given type is long or double or both but nothing else.
        /// </summary>
        protected bool IsNumberOnly(BoundExpression x) => IsNumberOnly(x.TypeRefMask);

        #endregion

        #region Construction

        /// <summary>
        /// Creates an instance of <see cref="ExpressionAnalysis{T}"/> that can analyse a block.
        /// </summary>
        /// <param name="worklist">The worklist to be used to enqueue next blocks.</param>
        /// <param name="model">The semantic context of the compilation.</param>
        public ExpressionAnalysis(Worklist<BoundBlock> worklist, ISymbolProvider model)
        {
            Debug.Assert(model != null);
            _model = model;
            Worklist = worklist;
        }

        #endregion

        #region State and worklist handling

        protected override bool IsStateInitialized(FlowState state) => state != null;

        protected override bool AreStatesEqual(FlowState a, FlowState b) => a.Equals(b);

        protected override FlowState GetState(BoundBlock block) => block.FlowState;

        protected override void SetState(BoundBlock block, FlowState state) => block.FlowState = state;

        protected override FlowState CloneState(FlowState state) => state.Clone();

        protected override FlowState MergeStates(FlowState a, FlowState b) => a.Merge(b);

        protected override void SetStateUnknown(ref FlowState state) => state.SetAllUnknown(true);

        protected override void EnqueueBlock(BoundBlock block) => Worklist.Enqueue(block);

        #endregion

        #region Visit blocks

        public override T VisitCFGExitBlock(ExitBlock x)
        {
            VisitCFGBlock(x);

            // TODO: EdgeToCallers:
            PingSubscribers(x);

            return default;
        }

        public override T VisitCFGCatchBlock(CatchBlock x)
        {
            VisitCFGBlockInit(x);

            // add catch control variable to the state
            x.TypeRef.Accept(this);
            x.Variable.Access = BoundAccess.Write.WithWrite(x.TypeRef.GetTypeRefMask(TypeCtx));
            State.SetLocalType(State.GetLocalHandle(x.Variable.Name.NameValue), x.Variable.Access.WriteMask);

            Accept(x.Variable);

            //
            x.Variable.ResultType = (Symbols.TypeSymbol)x.TypeRef.Type;

            //
            DefaultVisitBlock(x);

            return default;
        }

        #endregion

        #region Declaration Statements

        public override T VisitStaticStatement(BoundStaticVariableStatement x)
        {
            var v = x.Declaration;
            var local = State.GetLocalHandle(new VariableName(v.Name));

            State.SetVarKind(local, VariableKind.StaticVariable);

            var oldtype = State.GetLocalType(local).WithRefFlag;

            // set var
            if (v.InitialValue != null)
            {
                // analyse initializer
                Accept(v.InitialValue);

                bool isInt = v.InitialValue.ConstantValue.IsInteger(out long intVal);
                State.SetLessThanLongMax(local, isInt && intVal < long.MaxValue);
                State.SetGreaterThanLongMin(local, isInt && intVal > long.MinValue);

                State.SetLocalType(local, ((IPhpExpression)v.InitialValue).TypeRefMask | oldtype);
            }
            else
            {
                State.SetLessThanLongMax(local, false);
                State.SetGreaterThanLongMin(local, false);
                State.SetLocalType(local, TypeCtx.GetNullTypeMask() | oldtype);
                // TODO: explicitly State.SetLocalUninitialized() ?
            }

            return default;
        }

        public override T VisitGlobalStatement(BoundGlobalVariableStatement x)
        {
            base.VisitGlobalStatement(x);   // Accept(x.Variable)

            return default;
        }

        #endregion

        #region Visit Literals

        public override T VisitLiteral(BoundLiteral x)
        {
            x.TypeRefMask = x.ResolveTypeMask(TypeCtx);

            return default;
        }

        #endregion

        #region Visit CopyValue

        public override T VisitCopyValue(BoundCopyValue x)
        {
            Accept(x.Expression);

            var tmask = x.Expression.TypeRefMask;

            if (tmask.IsRef)
            {
                // copied value is possible a reference,
                // might be anything:
                tmask = TypeRefMask.AnyType;
            }

            // the result is not a reference for sure:
            Debug.Assert(!tmask.IsRef);

            x.TypeRefMask = tmask;

            return default;
        }

        #endregion

        #region Visit Assignments

        public override T VisitAssign(BoundAssignEx x)
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

            return default;
        }

        public override T VisitCompoundAssign(BoundCompoundAssignEx x)
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

            return default;
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
                TypeRefMask vartype;

                if (x.Name.NameValue == VariableName.HttpRawPostDataName)
                {
                    // $HTTP_RAW_POST_DATA : string // TODO: make it mixed or string | binary string
                    vartype = TypeCtx.GetStringTypeMask();
                }
                else
                {
                    // all the other autoglobals are arrays:
                    vartype = TypeCtx.GetArrayTypeMask();
                }

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

            if (Routine == null)
            {
                // invalid use of variable:
                return;
            }

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

                if (x.Variable is ThisVariableReference)
                {
                    // optimization; we know the exact type here or at least we know it is `Object` (instead of AnyType)
                    // if vartype is resolved to a single instance it was probably done by some operand like 'instanceof' already and better

                    if (vartype.IsSingleType == false || Routine.IsGlobalScope)
                    {
                        vartype = TypeCtx.GetThisTypeMask(); // : System.Object or exact type, with subclasses if applicable
                    }
                }
                else if (vartype.IsVoid || Routine.IsGlobalScope)
                {
                    // in global code or in case of undefined variable,
                    // assume the type is mixed (unspecified).
                    // In global code, the type of variable cannot be determined by type analysis, it can change between every two operations (this may be improved by better flow analysis).
                    vartype = TypeRefMask.AnyType;
                    vartype.IsRef = previoustype.IsRef;

                    if (Routine.IsGlobalScope)
                    {
                        // in global code, treat the variable as initialized always:
                        State.SetVarInitialized(local);
                        vartype.IsRef = true;   // variable might be a reference
                    }
                }
                else
                {
                    //// if there are multiple types possible
                    //// find the common base (this allows for better methods resolution)
                    //if (!vartype.IsAnyType && !vartype.IsRef && vartype.IncludesSubclasses && !vartype.IsSingleType && TypeCtx.IsObject(vartype))
                    //{
                    //    // ...
                    //}
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
                        if (!TypeHelpers.HasArrayAccess(vartype, TypeCtx, DeclaringCompilation))
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
                State.SetGreaterThanLongMin(local, false);
                x.TypeRefMask = vartype;
            }

            if (x.Access.IsUnset)
            {
                x.TypeRefMask = TypeCtx.GetNullTypeMask();
                State.SetLocalType(local, x.TypeRefMask);
                State.SetLessThanLongMax(local, false);
                State.SetGreaterThanLongMin(local, false);
                State.SetVarUninitialized(local);
            }
        }

        public override T VisitVariableRef(BoundVariableRef x)
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
                x.Variable ??= new LocalVariableReference(VariableKind.LocalVariable, Routine, null, x.Name);

                // update state
                if (x.Access.IsRead)
                {
                    State.FlowContext.SetAllUsed();
                    x.TypeRefMask = TypeRefMask.AnyType.WithRefFlag;
                }

                if (x.Access.IsWrite || x.Access.IsEnsure)
                {
                    State.SetAllUnknown(x.Access.IsWriteRef);
                    x.TypeRefMask = x.Access.WriteMask;
                }

                if (x.Access.IsUnset)
                {

                }
            }

            return default;
        }

        public override T VisitIncDec(BoundIncDecEx x)
        {
            // <target> = <target> +/- 1L

            Debug.Assert(x.Access.IsRead || x.Access.IsNone);
            Debug.Assert(x.Target.Access.IsRead && x.Target.Access.IsWrite);

            Visit(x.Target, BoundAccess.Read);
            Visit(x.Value, BoundAccess.Read);

            Debug.Assert(IsNumberOnly(x.Value));    // 1L

            TypeRefMask resulttype;
            TypeRefMask sourcetype = x.Target.TypeRefMask;  // type of target before operation

            VariableHandle lazyVarHandle = default;
            bool lessThanLongMax = false;               // whether the variable's value is less than the max long value
            bool greaterThanLongMin = false;            // or greater than the min long value

            if (IsDoubleOnly(x.Target))
            {
                // double++ => double
                resulttype = TypeCtx.GetDoubleTypeMask();
            }
            else
            {
                // we'd like to keep long if we are sure we don't overflow to double
                lazyVarHandle = TryGetVariableHandle(x.Target);
                if (lazyVarHandle.IsValid && x.IsIncrement && State.IsLessThanLongMax(lazyVarHandle))
                {
                    // long++ [< long.MaxValue] => long
                    resulttype = TypeCtx.GetLongTypeMask();
                    lessThanLongMax = true;
                }
                else if (lazyVarHandle.IsValid && !x.IsIncrement && State.IsGreaterThanLongMin(lazyVarHandle))
                {
                    // long-- [> long.MinValue] => long
                    resulttype = TypeCtx.GetLongTypeMask();
                    greaterThanLongMin = true;
                }
                else
                {
                    // long|double|anything++/-- => number
                    resulttype = TypeCtx.GetNumberTypeMask();
                }
            }

            Visit(x.Target, BoundAccess.Write.WithWrite(resulttype));

            //
            x.Target.Access = x.Target.Access.WithRead();   // put read access back to the target
            x.TypeRefMask = x.IsPostfix ? sourcetype : resulttype;

            // We expect that an incrementation doesn't change the property of being less than the max long value,
            // it needs to be restored due to the write access of the target variable
            if (lessThanLongMax)
            {
                Debug.Assert(lazyVarHandle.IsValid);
                State.SetLessThanLongMax(lazyVarHandle, true);
            }

            // The same for the min long value
            if (greaterThanLongMin)
            {
                Debug.Assert(lazyVarHandle.IsValid);
                State.SetGreaterThanLongMin(lazyVarHandle, true);
            }

            return default;
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

        Optional<object> ResolveBooleanOperation(Optional<object> xobj, Optional<object> yobj, Operations op)
        {
            if (xobj.TryConvertToBool(out var bx) && yobj.TryConvertToBool(out var by))
            {
                switch (op)
                {
                    case Operations.And: return (bx && by).AsOptional();
                    case Operations.Or: return (bx || by).AsOptional();
                    case Operations.Xor: return (bx ^ by).AsOptional();
                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

            return default;
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

            if (IsNumberOnly(or))
            {
                // double + number => double
                if (IsDoubleOnly(lValType) || IsDoubleOnly(rValType))
                    return TypeCtx.GetDoubleTypeMask();

                // long + long => long
                if (State.IsLessThanLongMax(TryGetVariableHandle(left)) && IsLongConstant(right, 1)) // LONG + 1, where LONG < long.MaxValue
                    return TypeCtx.GetLongTypeMask();

                return TypeCtx.GetNumberTypeMask();
            }

            if ((!lValType.IsRef && !lValType.IsAnyType && !TypeCtx.IsArray(lValType)) ||
                (!rValType.IsRef && !rValType.IsAnyType && !TypeCtx.IsArray(rValType)))
            {
                // not array for sure:
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

        /// <summary>
        /// Gets resulting type of <c>-</c> operation.
        /// </summary>
        TypeRefMask GetMinusOperationType(BoundExpression left, BoundExpression right)
        {
            if (State.IsGreaterThanLongMin(TryGetVariableHandle(left)) && IsLongConstant(right, 1)) // LONG -1, where LONG > long.MinValue
                return TypeCtx.GetLongTypeMask();
            else if (IsDoubleOnly(left.TypeRefMask) || IsDoubleOnly(right.TypeRefMask)) // some operand is double and nothing else
                return TypeCtx.GetDoubleTypeMask(); // double if we are sure about operands
            else
                return TypeCtx.GetNumberTypeMask();
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
                    return GetMinusOperationType(x.Left, x.Right);

                case Operations.Div:
                case Operations.Mul:
                case Operations.Pow:

                    if (IsDoubleOnly(x.Left.TypeRefMask) || IsDoubleOnly(x.Right.TypeRefMask)) // some operand is double and nothing else
                        return TypeCtx.GetDoubleTypeMask(); // double if we are sure about operands
                    else
                        return TypeCtx.GetNumberTypeMask();

                case Operations.Mod:
                    return TypeCtx.GetLongTypeMask();

                case Operations.ShiftLeft:
                case Operations.ShiftRight:

                    x.ConstantValue = ResolveShift(x.Operation, x.Left.ConstantValue, x.Right.ConstantValue);
                    return TypeCtx.GetLongTypeMask();

                #endregion

                #region Boolean and Bitwise Operations

                case Operations.And:
                case Operations.Or:
                case Operations.Xor:

                    x.ConstantValue = ResolveBooleanOperation(x.Left.ConstantValue, x.Right.ConstantValue, x.Operation);
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
                        // We must beware not to compute constant value more than once (losing results) -> mark handling of the given expression by this boolean
                        bool handled = false;

                        if (x.Right.ConstantValue.HasValue && x.Left is BoundReferenceExpression boundLeft)
                        {
                            handled = ResolveEqualityWithConstantValue(x, boundLeft, x.Right.ConstantValue, branch);
                        }
                        else if (x.Left.ConstantValue.HasValue && x.Right is BoundReferenceExpression boundRight)
                        {
                            handled = ResolveEqualityWithConstantValue(x, boundRight, x.Left.ConstantValue, branch);
                        }

                        if (!handled)
                        {
                            ResolveEquality(x);
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
                        if (x.Operation == Operations.LessThan ||
                            (x.Operation == Operations.LessThanOrEqual && x.Right.ConstantValue.IsInteger(out long rightVal) && rightVal < long.MaxValue))
                        {
                            // $x < Long.Max
                            State.SetLessThanLongMax(TryGetVariableHandle(x.Left), true);
                        }
                        else if (x.Operation == Operations.GreaterThan ||
                            (x.Operation == Operations.GreaterThanOrEqual && x.Right.ConstantValue.IsInteger(out long rightVal2) && rightVal2 > long.MinValue))
                        {
                            // $x > Long.Min
                            State.SetGreaterThanLongMin(TryGetVariableHandle(x.Left), true);
                        }
                    }

                    return TypeCtx.GetBooleanTypeMask();

                #endregion

                case Operations.Concat:
                    return TypeCtx.GetWritableStringTypeMask();

                case Operations.Coalesce:   // Left ?? Right
                    return x.Left.TypeRefMask | x.Right.TypeRefMask;

                case Operations.Spaceship:
                    return TypeCtx.GetLongTypeMask(); // -1, 0, +1

                default:
                    throw ExceptionUtilities.UnexpectedValue(x.Operation);
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

        static Optional<object> ResolveShift(Operations op, Optional<object> lvalue, Optional<object> rvalue)
        {
            if (lvalue.TryConvertToLong(out var left) && rvalue.TryConvertToLong(out var right))
            {
                switch (op)
                {
                    case Operations.ShiftLeft:
                        return (left << (int)right).AsOptional();

                    case Operations.ShiftRight:
                        return (left >> (int)right).AsOptional();

                    default:
                        Debug.Fail("unexpected");
                        break;

                }
            }

            return default;
        }

        /// <summary>
        /// Resolves variable types and potentially assigns a constant boolean value to an expression of a comparison of
        /// a variable and a constant - operators ==, !=, === and !==. Returns true iff this expression was handled and there
        /// is no need to analyse it any more (adding constant value etc.).
        /// </summary>
        private bool ResolveEqualityWithConstantValue(
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

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to infer the result of an equality comparison from the types of the operands.
        /// </summary>
        private void ResolveEquality(BoundBinaryEx cmpExpr)
        {
            Debug.Assert(cmpExpr.Operation >= Operations.Equal && cmpExpr.Operation <= Operations.NotIdentical);

            bool isStrict = (cmpExpr.Operation == Operations.Identical || cmpExpr.Operation == Operations.NotIdentical);

            if (isStrict && !cmpExpr.Left.CanHaveSideEffects() && !cmpExpr.Right.CanHaveSideEffects())
            {
                // Always returns false if checked for strict equality and the operands are of different types (and vice versa for strict non-eq)
                bool isPositive = (cmpExpr.Operation == Operations.Equal || cmpExpr.Operation == Operations.Identical);
                bool canBeSameType = Routine.TypeRefContext.CanBeSameType(cmpExpr.Left.TypeRefMask, cmpExpr.Right.TypeRefMask);
                cmpExpr.ConstantValue = !canBeSameType ? (!isPositive).AsOptional() : default;
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
                        return TypeCtx.GetTypeMask(BoundTypeRefFactory.Create(cvalue), false);
                    }
                    else
                    {
                        if (IsDoubleOnly(x.Operand))
                        {
                            return TypeCtx.GetDoubleTypeMask(); // double in case operand is double
                        }
                        return TypeCtx.GetNumberTypeMask();     // TODO: long in case operand is not a number
                    }

                case Operations.UnsetCast:
                    return TypeCtx.GetNullTypeMask();   // null

                case Operations.Plus:
                    if (IsNumberOnly(x.Operand.TypeRefMask))
                        return x.Operand.TypeRefMask;
                    return TypeCtx.GetNumberTypeMask();

                case Operations.Print:
                    return TypeCtx.GetLongTypeMask();

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

                    case SpecialType.System_Int32:
                        return value.Int32Value != int.MinValue
                            ? ConstantValue.Create(-value.Int32Value)   // (- Int32.MinValue) overflows to int64
                            : ConstantValue.Create(-(long)value.Int32Value);

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

        #region Visit Conversion

        public override T VisitConversion(BoundConversionEx x)
        {
            base.VisitConversion(x);

            // evaluate if possible

            if (x.TargetType is BoundPrimitiveTypeRef pt)
            {
                switch (pt.TypeCode)
                {
                    case PhpTypeCode.Boolean:
                        if (x.Operand.ConstantValue.TryConvertToBool(out bool constBool))
                        {
                            x.ConstantValue = ConstantValueExtensions.AsOptional(constBool);
                        }
                        break;

                    case PhpTypeCode.Long:
                        if (x.Operand.ConstantValue.TryConvertToLong(out long l))
                        {
                            x.ConstantValue = new Optional<object>(l);
                        }
                        break;

                    case PhpTypeCode.Double:
                        break;

                    case PhpTypeCode.String:
                    case PhpTypeCode.WritableString:
                        if (x.Operand.ConstantValue.TryConvertToString(out string str))
                        {
                            x.ConstantValue = new Optional<object>(str);
                        }
                        break;

                    case PhpTypeCode.Object:
                        if (IsClassOnly(x.Operand.TypeRefMask))
                        {
                            // it is object already, keep its specific type
                            x.TypeRefMask = x.Operand.TypeRefMask;   // (object)<object>
                            return default;
                        }
                        break;
                }
            }

            //

            x.TypeRefMask = x.TargetType.GetTypeRefMask(TypeCtx);

            return default;
        }

        #endregion

        #region Visit InstanceOf

        protected override void Visit(BoundInstanceOfEx x, ConditionBranch branch)
        {
            Accept(x.Operand);
            x.AsType.Accept(this);

            // TOOD: x.ConstantValue // in case we know and the operand is a local variable (we can ignore the expression and emit result immediatelly)

            var opTypeMask = x.Operand.TypeRefMask;
            if (x.Operand is BoundLiteral
                || (!opTypeMask.IsAnyType && !opTypeMask.IsRef && !Routine.TypeRefContext.IsObject(opTypeMask)))
            {
                x.ConstantValue = ConstantValueExtensions.AsOptional(false);
            }
            else if (x.Operand is BoundVariableRef vref && vref.Name.IsDirect)
            {
                if (branch == ConditionBranch.ToTrue)
                {
                    // if (Variable is T) => variable is T in True branch state
                    var vartype = x.AsType.GetTypeRefMask(TypeCtx);
                    if (opTypeMask.IsRef) vartype = vartype.WithRefFlag; // keep IsRef flag

                    State.SetLocalType(State.GetLocalHandle(vref.Name.NameValue), vartype);
                }
            }

            //
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();
        }

        #endregion

        #region Visit IsSet, OffsetExists

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
                x.ConstantValue = default;

                //
                if (State.IsLocalSet(handle))
                {
                    // If the variable is always defined, isset() behaves like !is_null()
                    var currenttype = State.GetLocalType(handle);

                    // a type in the true branch:
                    var positivetype = TypeCtx.WithoutNull(currenttype);

                    // resolve the constant if possible,
                    // does not depend on the branch
                    if (!currenttype.IsRef && !currenttype.IsAnyType)
                    {
                        if (positivetype.IsVoid)    // always false
                        {
                            x.ConstantValue = ConstantValueExtensions.AsOptional(false);
                        }
                        else if (positivetype == currenttype)   // not void nor null
                        {
                            x.ConstantValue = ConstantValueExtensions.AsOptional(true);
                        }
                    }

                    // we can be more specific in true/false branches:
                    if (branch != ConditionBranch.AnyResult && !x.ConstantValue.HasValue)
                    {
                        // update target type in true/false branch:
                        var newtype = (branch == ConditionBranch.ToTrue)
                            ? positivetype
                            : TypeCtx.GetNullTypeMask();

                        // keep the flags
                        newtype |= currenttype.Flags;

                        //
                        State.SetLocalType(handle, newtype);
                    }
                }
                else if (localname.IsAutoGlobal)
                {
                    // nothing
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

        public override T VisitOffsetExists(BoundOffsetExists x)
        {
            // receiver[index]
            base.VisitOffsetExists(x);

            // TODO: if receiver is undefined -> result is false

            // always bool
            x.ResultType = DeclaringCompilation.CoreTypes.Boolean;
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();
            return default;
        }

        public override T VisitTryGetItem(BoundTryGetItem x)
        {
            // array, index, fallback
            base.VisitTryGetItem(x);

            // TODO: resulting type if possible (see VisitArrayItem)

            // The result of array[index] might be a reference
            x.TypeRefMask = TypeRefMask.AnyType.WithRefFlag;

            return default;
        }

        #endregion

        #region TypeRef

        internal override T VisitIndirectTypeRef(BoundIndirectTypeRef tref)
        {
            // visit indirect type
            base.VisitIndirectTypeRef(tref);

            //
            return VisitTypeRef(tref);
        }

        internal override T VisitTypeRef(BoundTypeRef tref)
        {
            Debug.Assert(!(tref is BoundMultipleTypeRef));

            // resolve type symbol
            tref.ResolvedType = (TypeSymbol)tref.ResolveTypeSymbol(DeclaringCompilation);

            return default;
        }

        #endregion

        #region Visit Function Call

        protected override T VisitRoutineCall(BoundRoutineCall x)
        {
            x.TypeRefMask = TypeRefMask.AnyType.WithRefFlag; // unknown function might return a reference

            // TODO: write arguments Access
            // TODO: visit invocation member of
            // TODO: 2 pass, analyze arguments -> resolve method -> assign argument to parameter -> write arguments access -> analyze arguments again

            // visit arguments:
            base.VisitRoutineCall(x);

            return default;
        }

        bool BindParams(IList<PhpParam> expectedparams, ImmutableArray<BoundArgument> givenargs)
        {
            for (int i = 0; i < givenargs.Length; i++)
            {
                if (givenargs[i].IsUnpacking)
                {
                    break;
                }

                if (i < expectedparams.Count)
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
            // [PhpRwAttribute]
            if (expected.IsPhpRw)
            {
                if (givenarg.Value is BoundReferenceExpression refexpr)
                {
                    if (TypeCtx.IsArray(expected.Type) && !givenarg.Value.Access.EnsureArray)   // [PhpRw]PhpArray
                    {
                        SemanticsBinder.BindEnsureArrayAccess(givenarg.Value as BoundReferenceExpression);
                        Worklist.Enqueue(CurrentBlock);
                    }
                }
            }

            // bind ref parameters to variables:
            if (expected.IsAlias || expected.IsByRef)  // => args[i] must be a variable
            {
                if (givenarg.Value is BoundReferenceExpression refexpr)
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

                    if (refexpr is BoundVariableRef refvar)
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
            else if (!expected.Type.IsAnyType && givenarg.Value is BoundVariableRef refvar && refvar.Name.IsDirect)
            {
                // Even for variables passed by value we may gain information about their type (if we previously had none),
                // because not complying with the parameter type would have caused throwing a TypeError
                var local = State.GetLocalHandle(refvar.Name.NameValue);
                var localType = State.GetLocalType(local);
                var paramTypeNonNull = TypeCtx.WithoutNull(expected.Type);
                if (localType.IsAnyType && !localType.IsRef &&
                    (TypeCtx.IsObjectOnly(paramTypeNonNull) || TypeCtx.IsArrayOnly(paramTypeNonNull)))    // E.g. support ?MyClass but not callable
                {
                    Debug.Assert(!expected.Type.IsRef);
                    State.SetLocalType(local, expected.Type);
                }
            }
        }

        TypeRefMask BindValidRoutineCall(BoundRoutineCall call, MethodSymbol method, ImmutableArray<BoundArgument> args, bool maybeoverload)
        {
            // analyze TargetMethod with x.Arguments
            // require method result type if access != none
            if (call.Access.IsRead)
            {
                if (Worklist.EnqueueRoutine(method, CurrentBlock, call))
                {
                    // target will be reanalysed
                    // note: continuing current block may be waste of time, but it might gather other called targets

                    // The next blocks will be analysed after this routine is re-enqueued due to the dependency
                    _flags |= AnalysisFlags.IsCanceled;
                }
            }

            if (Routine != null)
            {
                var rflags = method.InvocationFlags();
                Routine.Flags |= rflags;

                if ((rflags & RoutineFlags.UsesLocals) != 0
                    //&& (x is BoundGlobalFunctionCall gf && gf.Name.NameValue.Name.Value == "extract") // "compact" does not change locals // CONSIDER // TODO
                    )
                {
                    // function may change/add local variables
                    State.SetAllUnknown(true);
                }
            }

            // process arguments
            if (!BindParams(method.GetExpectedArguments(this.TypeCtx), args) && maybeoverload)
            {
                call.TargetMethod = null; // nullify the target method -> call dynamically, arguments cannot be bound at compile time
            }

            //
            return method.GetResultType(TypeCtx);
        }

        /// <summary>
        /// Bind arguments to target method and resolve resulting <see cref="BoundExpression.TypeRefMask"/>.
        /// Expecting <see cref="BoundRoutineCall.TargetMethod"/> is resolved.
        /// If the target method cannot be bound at compile time, <see cref="BoundRoutineCall.TargetMethod"/> is nulled.
        /// </summary>
        void BindRoutineCall(BoundRoutineCall x, bool maybeOverload = false)
        {
            if (MethodSymbolExtensions.IsValidMethod(x.TargetMethod))
            {
                x.TypeRefMask = BindValidRoutineCall(x, x.TargetMethod, x.ArgumentsInSourceOrder, maybeOverload);
            }
            else if (x.TargetMethod is MissingMethodSymbol || x.TargetMethod == null)
            {
                // we don't know anything about the target callsite,
                // locals passed as arguments should be marked as possible refs:
                foreach (var arg in x.ArgumentsInSourceOrder)
                {
                    if (arg.Value is BoundVariableRef bvar && bvar.Name.IsDirect && !arg.IsUnpacking)
                    {
                        State.SetLocalRef(State.GetLocalHandle(bvar.Name.NameValue));
                    }
                }
            }
            else if (x.TargetMethod is AmbiguousMethodSymbol ambiguity)
            {
                // check if arguments are not passed by bref, mark locals eventually as refs:
                foreach (var m in ambiguity.Ambiguities)
                {
                    var expected = m.GetExpectedArguments(this.TypeCtx);
                    var given = x.ArgumentsInSourceOrder;

                    for (int i = 0; i < given.Length && i < expected.Count; i++)
                    {
                        if (expected[i].IsAlias && given[i].Value is BoundVariableRef bvar && bvar.Name.IsDirect)
                        {
                            State.SetLocalRef(State.GetLocalHandle(bvar.Name.NameValue));
                        }
                    }
                }

                // get the return type from all the ambiguities:
                if (!maybeOverload && x.Access.IsRead)
                {
                    var r = (TypeRefMask)0;
                    foreach (var m in ambiguity.Ambiguities)
                    {
                        if (Worklist.EnqueueRoutine(m, CurrentBlock, x))
                        {
                            // The next blocks will be analysed after this routine is re-enqueued due to the dependency
                            _flags |= AnalysisFlags.IsCanceled;
                        }

                        r |= m.GetResultType(TypeCtx);
                    }

                    x.TypeRefMask = r;
                }
            }

            //

            if (x.Access.IsReadRef)
            {
                // reading by ref:
                x.TypeRefMask = x.TypeRefMask.WithRefFlag;
            }
        }

        public override T VisitExit(BoundExitEx x)
        {
            VisitRoutineCall(x);

            // no parameters binding
            // TODO: handle unpacking
            Debug.Assert(x.ArgumentsInSourceOrder.Length == 0 || !x.ArgumentsInSourceOrder[0].IsUnpacking);

            x.TypeRefMask = 0;  // returns void
            x.ResultType = DeclaringCompilation.GetSpecialType(SpecialType.System_Void);

            return default;
        }

        public override T VisitEcho(BoundEcho x)
        {
            VisitRoutineCall(x);

            x.TypeRefMask = 0;  // returns void
            x.ResultType = DeclaringCompilation.GetSpecialType(SpecialType.System_Void);

            //
            return default;
        }

        public override T VisitConcat(BoundConcatEx x)
        {
            VisitRoutineCall(x);

            // if possible, mark the result type as "String",
            // otherwise we have to use "PhpString"
            var args = x.ArgumentsInSourceOrder;
            bool mustBePhpString = false;
            for (int i = 0; i < args.Length; i++)
            {
                var targ = args[i].Value.TypeRefMask;
                mustBePhpString |= targ.IsRef || targ.IsAnyType || this.TypeCtx.IsWritableString(targ) /*|| this.TypeCtx.IsObject(targ) //object are always converted to UTF16 String// */;
            }

            x.TypeRefMask = mustBePhpString ? TypeCtx.GetWritableStringTypeMask() : TypeCtx.GetStringTypeMask();

            return default;
        }

        public override T VisitAssert(BoundAssertEx x)
        {
            VisitRoutineCall(x);
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();

            return default;
        }

        public override void VisitGlobalFunctionCall(BoundGlobalFunctionCall x, ConditionBranch branch)
        {
            Accept(x.Name);

            VisitRoutineCall(x);

            if (x.Name.IsDirect)
            {
                var symbol = (MethodSymbol)DeclaringCompilation.ResolveFunction(x.Name.NameValue, Routine);
                if (symbol.IsMissingMethod() && x.NameOpt.HasValue)
                {
                    symbol = (MethodSymbol)DeclaringCompilation.ResolveFunction(x.NameOpt.Value, Routine);
                }

                var overloads = symbol is AmbiguousMethodSymbol ambiguous && ambiguous.IsOverloadable
                    ? new OverloadsList(ambiguous.Ambiguities.ToArray())
                    : new OverloadsList(symbol ?? new MissingMethodSymbol(x.Name.NameValue.ToString()));

                Debug.Assert(x.TypeArguments.IsDefaultOrEmpty);

                // symbol might be ErrorSymbol

                x.TargetMethod = overloads.Resolve(this.TypeCtx, x.ArgumentsInSourceOrder, VisibilityScope, OverloadsList.InvocationKindFlags.StaticCall);
            }

            BindRoutineCall(x);

            // if possible resolve ConstantValue and TypeRefMask:
            AnalysisFacts.HandleSpecialFunctionCall(x, this, branch);
        }

        public override T VisitInstanceFunctionCall(BoundInstanceFunctionCall x)
        {
            Accept(x.Instance);
            Accept(x.Name);

            VisitRoutineCall(x);

            if (x.Name.IsDirect)
            {
                var resolvedtype = x.Instance.ResultType;
                if (resolvedtype == null)
                {
                    var typeref = TypeCtx.GetObjectTypes(TypeCtx.WithoutNull(x.Instance.TypeRefMask));    // ignore NULL, causes runtime exception anyway
                    if (typeref.IsSingle)
                    {
                        resolvedtype = (TypeSymbol)typeref.FirstOrDefault().ResolveTypeSymbol(DeclaringCompilation);
                    }
                    // else: a common base?
                }

                if (resolvedtype.IsValidType())
                {
                    var candidates = resolvedtype.LookupMethods(x.Name.NameValue.Name.Value);

                    candidates = Construct(candidates, x);

                    x.TargetMethod = new OverloadsList(candidates).Resolve(this.TypeCtx, x.ArgumentsInSourceOrder, VisibilityScope, OverloadsList.InvocationKindFlags.InstanceCall);

                    //
                    if (x.TargetMethod.IsValidMethod() && x.TargetMethod.IsStatic && x.Instance.TypeRefMask.IncludesSubclasses)
                    {
                        // static method invoked on an instance object,
                        // must be postponed to runtime since the type may change
                        x.TargetMethod = new AmbiguousMethodSymbol(ImmutableArray.Create(x.TargetMethod), false);
                    }
                }
                else
                {
                    x.TargetMethod = null;
                }
            }

            BindRoutineCall(x, maybeOverload: true);

            return default;
        }

        public override T VisitStaticFunctionCall(BoundStaticFunctionCall x)
        {
            Accept(x.TypeRef);
            Accept(x.Name);

            VisitRoutineCall(x);

            var type = (TypeSymbol)x.TypeRef.Type;

            if (x.Name.NameExpression != null)
            {
                // indirect method call -> not resolvable
            }
            else if (type.IsValidType())
            {
                var candidates = type.LookupMethods(x.Name.ToStringOrThrow());
                // if (candidates.Any(c => c.HasThis)) throw new NotImplementedException("instance method called statically");

                candidates = Construct(candidates, x);

                var flags = OverloadsList.InvocationKindFlags.StaticCall;

                if (Routine != null && !Routine.IsStatic && (x.TypeRef.IsSelf() || x.TypeRef.IsParent()))
                {
                    // self:: or parent:: $this forwarding, prefer both
                    flags |= OverloadsList.InvocationKindFlags.InstanceCall;
                }

                var method = new OverloadsList(candidates).Resolve(this.TypeCtx, x.ArgumentsInSourceOrder, VisibilityScope, flags);

                // method is missing or inaccessible:
                if (method is ErrorMethodSymbol errmethod && (errmethod.ErrorKind == ErrorMethodKind.Inaccessible || errmethod.ErrorKind == ErrorMethodKind.Missing))
                {
                    // NOTE: magic methods __call or __callStatic are called in both cases - the target method is inaccessible or missing

                    var isviable = true; // can we safely resolve the method?
                    var call = Array.Empty<MethodSymbol>();

                    // __call() might be used, if we have a reference to $this:
                    if (Routine != null && !Routine.IsStatic)
                    {
                        // there is $this variable:
                        if (TypeCtx.ThisType == null || TypeCtx.ThisType.IsOfType(type) || type.IsOfType(TypeCtx.ThisType))
                        {
                            // try to use __call() first:
                            call = type.LookupMethods(Name.SpecialMethodNames.Call.Value);

                            //
                            if (TypeCtx.ThisType == null && call.Length != 0)
                            {
                                // $this is resolved dynamically in runtime and
                                // we don't know if we can use __call() here
                                isviable = false;
                            }
                        }
                    }

                    if (call.Length == 0)
                    {
                        // __callStatic()
                        call = type.LookupMethods(Name.SpecialMethodNames.CallStatic.Value);
                    }

                    if (call.Length != 0)
                    {
                        // NOTE: PHP ignores visibility of __callStatic
                        call = Construct(call, x);

                        method = call.Length == 1 && isviable
                            ? new MagicCallMethodSymbol(x.Name.ToStringOrThrow(), call[0])
                            : null; // nullify the symbol so it will be called dynamically and resolved in rutime
                    }
                }

                x.TargetMethod = method;
            }
            else if (x.TypeRef.IsSelf() && Routine != null && Routine.ContainingType.IsTraitType())
            {
                // self:: within trait type
                // resolve possible code path
                // we need this at least to determine possible late static type binding

                var candidates = Construct(Routine.ContainingType.LookupMethods(x.Name.ToStringOrThrow()), x);
                if (candidates.Length != 0)
                {
                    // accessibility not have to be checked here
                    x.TargetMethod = new AmbiguousMethodSymbol(candidates.AsImmutable(), overloadable: true);
                }
            }

            BindRoutineCall(x);

            return default;
        }

        // helper
        MethodSymbol[] Construct(MethodSymbol[] methods, BoundRoutineCall bound)
        {
            if (bound.TypeArguments.IsDefaultOrEmpty)
            {
                return methods;
            }
            else
            {
                var types = bound.TypeArguments.Select(t => (TypeSymbol)t.Type).AsImmutable();
                var result = new List<MethodSymbol>();

                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].Arity == types.Length) // TODO: check the type argument is assignable
                    {
                        result.Add(methods[i].Construct(types));
                    }
                }
                return result.ToArray();
            }
        }

        public override T VisitNew(BoundNewEx x)
        {
            Accept(x.TypeRef);      // resolve target type

            VisitRoutineCall(x);    // analyse arguments

            // resolve .ctor method:
            var type = (NamedTypeSymbol)x.TypeRef.Type;
            if (type.IsValidType())
            {
                var candidates = type.InstanceConstructors.ToArray();

                //
                x.TargetMethod = new OverloadsList(candidates).Resolve(this.TypeCtx, x.ArgumentsInSourceOrder, VisibilityScope, OverloadsList.InvocationKindFlags.New);
                x.ResultType = type;
            }

            // bind arguments:
            BindRoutineCall(x);

            // resulting type is always known,
            // not null,
            // not ref:
            x.TypeRefMask = x.TypeRef.GetTypeRefMask(TypeCtx).WithoutSubclasses;

            return default;
        }

        public override T VisitInclude(BoundIncludeEx x)
        {
            VisitRoutineCall(x);

            // resolve target script
            Debug.Assert(x.ArgumentsInSourceOrder.Length == 1);
            var targetExpr = x.ArgumentsInSourceOrder[0].Value;

            //
            x.Target = null;

            if (targetExpr.ConstantValue.TryConvertToString(out var path))
            {
                // include (path)
                x.Target = (MethodSymbol)_model.ResolveFile(path)?.MainMethod;
            }
            else if (targetExpr is BoundConcatEx concat) // common case
            {
                // include (dirname( __FILE__ ) . path) // changed to (__DIR__ . path) by graph rewriter
                // include (__DIR__ . path)
                if (concat.ArgumentsInSourceOrder.Length == 2 &&
                    concat.ArgumentsInSourceOrder[0].Value is BoundPseudoConst pc &&
                    pc.ConstType == PseudoConstUse.Types.Dir &&
                    concat.ArgumentsInSourceOrder[1].Value.ConstantValue.TryConvertToString(out path))
                {
                    // create project relative path
                    // not starting with a directory separator!
                    path = Routine.ContainingFile.DirectoryRelativePath + path;
                    if (path.Length != 0 && PathUtilities.IsAnyDirectorySeparator(path[0])) path = path.Substring(1);   // make nicer when we have a helper method for that
                    x.Target = (MethodSymbol)_model.ResolveFile(path)?.MainMethod;
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

            return default;
        }

        #endregion

        #region Visit FieldRef

        public override T VisitFieldRef(BoundFieldRef x)
        {
            Accept(x.Instance);
            Accept(x.ContainingType);
            Accept(x.FieldName);

            if (x.IsInstanceField)  // {Instance}->FieldName
            {
                Debug.Assert(x.Instance != null);
                Debug.Assert(x.Instance.Access.IsRead);

                // resolve field if possible
                var resolvedtype = x.Instance.ResultType as NamedTypeSymbol;
                if (resolvedtype == null)
                {
                    var typerefs = TypeCtx.GetObjectTypes(TypeCtx.WithoutNull(x.Instance.TypeRefMask));   // ignore NULL, would cause runtime exception in read access, will be ensured to non-null in write access
                    if (typerefs.IsSingle)
                    {
                        resolvedtype = (NamedTypeSymbol)typerefs.FirstOrDefault().ResolveTypeSymbol(DeclaringCompilation);
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
                                    x.BoundReference = new FieldReference(x.Instance, overridenf ?? field);
                                    x.TypeRefMask = TypeRefFactory.CreateMask(TypeCtx, field.Type).WithIsRef(field.Type.CanBePhpAlias());
                                    x.ResultType = field.Type;
                                    return default;
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
                                x.BoundReference = new PropertyReference(x.Instance, prop);
                                x.TypeRefMask = TypeRefFactory.CreateMask(TypeCtx, prop.Type);
                                x.ResultType = prop.Type;
                                return default;
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

                x.BoundReference = new IndirectProperty(x); // ~ dynamic // new BoundIndirectFieldPlace(x);
                x.TypeRefMask = TypeRefMask.AnyType.WithRefFlag;
                return default;
            }

            // static fields or constants
            if (x.IsStaticField || x.IsClassConstant)    // {ClassName}::${StaticFieldName}, {ClassName}::{ConstantName}
            {
                var containingType = (NamedTypeSymbol)x.ContainingType.Type;

                if (x.IsClassConstant)
                {
                    Debug.Assert(x.Access.IsRead || x.Access.IsNone);
                    Debug.Assert(!x.Access.IsEnsure && !x.Access.IsWrite && !x.Access.IsReadRef);
                }

                if (containingType.IsValidType() && x.FieldName.IsDirect)
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
                            x.BoundReference = new FieldReference(null, field);
                            x.TypeRefMask = TypeRefFactory.CreateMask(TypeCtx, field.Type).WithIsRef(field.Type.CanBePhpAlias());
                        }

                        x.ResultType = field.Type;
                        return default;
                    }
                    else if (x.IsStaticField)
                    {
                        // TODO: visibility
                        var prop = containingType.LookupMember<PropertySymbol>(fldname);
                        if (prop != null && prop.IsStatic)
                        {
                            x.BoundReference = new PropertyReference(null, prop);
                            x.TypeRefMask = TypeRefFactory.CreateMask(TypeCtx, prop.Type).WithIsRef(prop.Type.CanBePhpAlias());
                            return default;
                        }
                    }

                    // TODO: __getStatic, __setStatic
                }

                // indirect field access:
                // indirect field access with known class name:
                x.BoundReference = new IndirectProperty(x); // ~ dynamic // new BoundIndirectStFieldPlace((BoundTypeRef)x.ContainingType, x.FieldName, x);
                x.TypeRefMask = TypeRefMask.AnyType.WithRefFlag;
            }

            return default;
        }

        #endregion

        #region Visit ArrayEx, ArrayItemEx, ArrayItemOrdEx

        public override T VisitArray(BoundArrayEx x)
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

            return default;
        }

        public override T VisitArrayItem(BoundArrayItemEx x)
        {
            Accept(x.Array);
            Accept(x.Index);

            // TODO: resulting type if possible:
            // var element_type = TypeCtx.GetElementType(x.Array.TypeRefMask); // + handle classes with ArrayAccess and TypeRefMask.Uninitialized

            //

            x.TypeRefMask =
                x.Access.IsReadRef ? TypeRefMask.AnyType.WithRefFlag :
                x.Access.IsEnsure ? TypeRefMask.AnyType : // object|array ?
                TypeRefMask.AnyType.WithRefFlag; // result might be a anything (including a reference?)

            return default;
        }

        public override T VisitArrayItemOrd(BoundArrayItemOrdEx x)
        {
            Accept(x.Array);
            Accept(x.Index);

            // ord($s[$i]) cannot be used as an l-value
            Debug.Assert(!x.Access.MightChange);

            x.TypeRefMask = TypeCtx.GetLongTypeMask();

            return base.VisitArrayItemOrd(x);
        }

        #endregion

        #region VisitLambda

        public override T VisitLambda(BoundLambda x)
        {
            var container = (ILambdaContainerSymbol)Routine.ContainingFile;
            var symbol = container.ResolveLambdaSymbol((LambdaFunctionExpr)x.PhpSyntax);
            if (symbol == null)
            {
                throw ExceptionUtilities.UnexpectedValue(symbol);
            }

            // lambda uses `static` => we have to know where it is:
            Routine.Flags |= (symbol.Flags & RoutineFlags.UsesLateStatic);

            // bind arguments to parameters
            var ps = symbol.SourceParameters;

            // first {N} source parameters correspond to "use" parameters
            for (int pi = 0; pi < x.UseVars.Length; pi++)
            {
                x.UseVars[pi].Parameter = ps[pi];
                VisitArgument(x.UseVars[pi]);
            }

            //
            x.BoundLambdaMethod = symbol;
            x.ResultType = DeclaringCompilation.CoreTypes.Closure;
            Debug.Assert(x.ResultType != null);
            x.TypeRefMask = TypeCtx.GetTypeMask(new BoundLambdaTypeRef(TypeRefMask.AnyType), false); // specific {Closure}, no null, no subclasses

            return default;
        }

        #endregion

        #region VisitYield

        public override T VisitYieldStatement(BoundYieldStatement x)
        {
            base.VisitYieldStatement(x);

            return default;
        }

        public override T VisitYieldEx(BoundYieldEx x)
        {
            base.VisitYieldEx(x);
            x.TypeRefMask = TypeRefMask.AnyType;

            return default;
        }

        public override T VisitYieldFromEx(BoundYieldFromEx x)
        {
            base.VisitYieldFromEx(x);
            x.TypeRefMask = TypeRefMask.AnyType;

            return default;
        }

        #endregion

        #region Visit

        public override T VisitIsEmpty(BoundIsEmptyEx x)
        {
            Accept(x.Operand);
            x.TypeRefMask = TypeCtx.GetBooleanTypeMask();

            return default;
        }

        public override T VisitUnset(BoundUnset x)
        {
            base.VisitUnset(x);

            return default;
        }

        public override T VisitList(BoundListEx x)
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

            return default;
        }

        public override T VisitPseudoConstUse(BoundPseudoConst x)
        {
            object value = null;

            switch (x.ConstType)
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

                            if (intrait && x.ConstType == PseudoConstUse.Types.Class)
                            {
                                // __CLASS__ inside trait resolved in runtime
                                x.TypeRefMask = TypeCtx.GetStringTypeMask();
                                return default;
                            }

                            if (!intrait && x.ConstType == PseudoConstUse.Types.Trait)
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
                        value = Routine != null ? Routine.RoutineName : string.Empty;
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
                    return default;

                default:
                    throw ExceptionUtilities.UnexpectedValue(x.ConstType);
            }

            Debug.Assert(value != null);    // pseudoconstant has been set

            x.ConstantValue = new Optional<object>(value);

            if (value is string) x.TypeRefMask = TypeCtx.GetStringTypeMask();
            else if (value is int || value is long) x.TypeRefMask = TypeCtx.GetLongTypeMask();
            else throw ExceptionUtilities.UnexpectedValue(value);

            return default;
        }

        public override T VisitPseudoClassConstUse(BoundPseudoClassConst x)
        {
            base.VisitPseudoClassConstUse(x);

            //
            if (x.ConstType == PseudoClassConstUse.Types.Class)
            {
                x.TypeRefMask = TypeCtx.GetStringTypeMask();

                // resolve the value:

                var type = x.TargetType.Type as TypeSymbol;
                if (type.IsValidType() && type is IPhpTypeSymbol phpt)
                {
                    x.ConstantValue = new Optional<object>(phpt.FullName.ToString());
                }
                else
                {
                    var tref = x.TargetType.PhpSyntax as TypeRef;
                    var qname = tref?.QualifiedName;
                    if (qname.HasValue)
                    {
                        if (!qname.Value.IsReservedClassName) // self, static, parent
                        {
                            x.ConstantValue = new Optional<object>(qname.Value.ToString());
                        }
                    }
                }
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(x.ConstType);
            }

            return default;
        }

        public override T VisitGlobalConstUse(BoundGlobalConst x)
        {
            // TODO: check constant name

            // bind to app-wide constant if possible
            var symbol = (Symbol)_model.ResolveConstant(x.Name.ToString());
            var field = symbol as FieldSymbol;

            if (!BindConstantValue(x, field))
            {
                if (field != null && field.IsStatic)
                {
                    x._boundExpressionOpt = new FieldReference(null, field);
                    x.TypeRefMask = field.GetResultType(TypeCtx);
                }
                else if (symbol is PEPropertySymbol prop && prop.IsStatic)
                {
                    x._boundExpressionOpt = new PropertyReference(null, prop);
                    x.TypeRefMask = prop.GetResultType(TypeCtx);
                }
                else
                {
                    x.TypeRefMask = TypeRefMask.AnyType;    // only scalars ?
                }
            }

            return default;
        }

        public override T VisitConditional(BoundConditionalEx x)
        {
            BoundExpression positiveExpr;    // positive expression (if evaluated to true, FalseExpr is not evaluated)
            FlowState positiveState; // state after successful positive branch

            if (x.IfTrue != null && x.IfTrue != x.Condition)
            {
                // Template: Condition ? IfTrue : IfFalse

                var originalState = State.Clone();
                positiveExpr = x.IfTrue;

                // true branch:
                if (VisitCondition(x.Condition, ConditionBranch.ToTrue))
                {
                    Accept(x.IfTrue);
                    positiveState = State;

                    // false branch
                    State = originalState.Clone();
                    VisitCondition(x.Condition, ConditionBranch.ToFalse);
                }
                else
                {
                    // OPTIMIZATION: Condition does not have to be visited twice!

                    originalState = State.Clone(); // state after visiting Condition

                    Accept(x.IfTrue);
                    positiveState = State;

                    State = originalState.Clone();
                }
            }
            else
            {
                // Template: Condition ?: IfFalse
                positiveExpr = x.Condition;

                // in case ?: do not evaluate trueExpr twice:
                // Template: Condition ?: FalseExpr

                Accept(x.Condition);
                positiveState = State.Clone();

                // condition != false => condition != null =>
                // ignoring NULL type from Condition:
                x.Condition.TypeRefMask = TypeCtx.WithoutNull(x.Condition.TypeRefMask);
            }

            // and start over with false branch:
            Accept(x.IfFalse);

            // merge both states (after positive evaluation and the false branch)
            State = State.Merge(positiveState);
            x.TypeRefMask = positiveExpr.TypeRefMask | x.IfFalse.TypeRefMask;

            return default;
        }

        public override T VisitExpressionStatement(BoundExpressionStatement x)
        {
            return base.VisitExpressionStatement(x);
        }

        public override T VisitReturn(BoundReturnStatement x)
        {
            if (x.Returned != null)
            {
                Accept(x.Returned);
                State.FlowThroughReturn(x.Returned.TypeRefMask);
            }
            else
            {
                // remember "void" type explicitly
                var voidMask = State.TypeRefContext.GetTypeMask(BoundTypeRefFactory.VoidTypeRef, false); // NOTE: or remember the routine may return Void
                State.FlowThroughReturn(voidMask);
            }

            return default;
        }

        public override T VisitThrow(BoundThrowStatement x)
        {
            Accept(x.Thrown);

            return default;
        }

        public override T VisitEval(BoundEvalEx x)
        {
            base.VisitEval(x);

            //
            State.SetAllUnknown(true);

            //
            x.TypeRefMask = TypeRefMask.AnyType;

            return default;
        }

        #endregion
    }
}
