using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;
using Ast = Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    internal partial class TransformationRewriter : GraphRewriter
    {
        private readonly DelayedTransformations _delayedTransformations;
        private readonly SourceRoutineSymbol _routine;
        private readonly HashSet<BoundCopyValue> _unnecessaryCopies;    // Possibly null if all are necessary

        protected PhpCompilation DeclaringCompilation => _routine.DeclaringCompilation;
        protected BoundTypeRefFactory BoundTypeRefFactory => DeclaringCompilation.TypeRefFactory;

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

            rewriter.TryTransformParameters();
            routine.ControlFlowGraph = updatedCFG;

            Debug.Assert(updatedCFG == currentCFG || rewriter.TransformationCount != 0); // cfg updated => transformations (not <= due to parameter transformations)
            return updatedCFG != currentCFG;
        }

        /// <summary>
        /// Map of well-known routines and corresponding rewrite rule that can return a new expression as a replacement for the routine call.
        /// Return <c>null</c> if the routine was not rewritten.
        /// </summary>
        readonly Dictionary<QualifiedName, Func<BoundGlobalFunctionCall, BoundExpression>> _special_functions;

        private TransformationRewriter()
        {
            // initialize rewrite rules for specific well-known functions:
            _special_functions = new Dictionary<QualifiedName, Func<BoundGlobalFunctionCall, BoundExpression>>()
            {
                { NameUtils.SpecialNames.dirname, x =>
                {
                    // dirname( __FILE__ ) -> __DIR__
                    if (x.ArgumentsInSourceOrder.Length == 1 &&
                        x.ArgumentsInSourceOrder[0].Value is BoundPseudoConst pc &&
                        pc.ConstType == Ast.PseudoConstUse.Types.File)
                    {
                        return new BoundPseudoConst(Ast.PseudoConstUse.Types.Dir).WithAccess(x.Access);
                    }

                    return null;
                } },
                { NameUtils.SpecialNames.basename, x =>
                {
                    // basename( __FILE__ ) -> "filename"
                    if (x.ArgumentsInSourceOrder.Length == 1 &&
                        x.ArgumentsInSourceOrder[0].Value is BoundPseudoConst pc &&
                        pc.ConstType == Ast.PseudoConstUse.Types.File)
                    {
                        var fname = _routine.ContainingFile.FileName;
                        return new BoundLiteral(fname) { ConstantValue = new Optional<object>(fname) }.WithContext(x);
                    }

                    return null;
                } },
                { NameUtils.SpecialNames.get_parent_class, x =>
                {
                    bool TryResolveParentClassInCurrentClassContext(SourceRoutineSymbol routine, out BoundLiteral newExpression)
                    {
                        // in global function, always FALSE
                        if (routine is SourceFunctionSymbol)
                        {
                            // FALSE
                            newExpression = new BoundLiteral(false.AsObject());
                            return true;
                        }

                        // in a method, we can resolve in compile time:
                        if (routine is SourceMethodSymbol m && m.ContainingType is SourceTypeSymbol t && !t.IsTrait)
                        {
                            if (t.BaseType == null || t.BaseType.IsObjectType())
                            {
                                // FALSE
                                newExpression = new BoundLiteral(false.AsObject())
                                {
                                    ConstantValue = false.AsOptional()
                                };
                                return true;
                            }
                            else
                            {
                                // {class name}
                                var baseTypeName = t.BaseType.PhpQualifiedName().ToString();
                                newExpression = new BoundLiteral(baseTypeName)
                                {
                                    ConstantValue = baseTypeName
                                };
                                return true;
                            }
                        }

                        //
                        newExpression = default;
                        return false;
                    }

                    // get_parent_class() -> {class name} | FALSE
                    if (x.ArgumentsInSourceOrder.Length == 0)
                    {
                        if (TryResolveParentClassInCurrentClassContext(_routine, out var newExpression))
                        {
                            return newExpression.WithContext(x);
                        }
                    }

                    // get_parent_class( ??? ) -> parent::class | FALSE
                    if (x.ArgumentsInSourceOrder.Length == 1)
                    {
                        // get_parent_class($this), get_parent_class(__CLASS__) ->  {class name} | FALSE
                        if ((x.ArgumentsInSourceOrder[0].Value is BoundVariableRef varref && varref.Variable is ThisVariableReference) ||
                            (x.ArgumentsInSourceOrder[0].Value is BoundPseudoConst pc && pc.ConstType == Ast.PseudoConstUse.Types.Class))
                        {
                            if (TryResolveParentClassInCurrentClassContext(_routine, out var newExpression))
                            {
                                return newExpression.WithContext(x);
                            }
                        }
                    }

                    return null;
                } },
                { NameUtils.SpecialNames.method_exists, x =>
                {
                    // method_exists(class_name, method_name) -> FALSE
                    if (x.ArgumentsInSourceOrder.Length == 2)
                    {
                        // method_exists(FALSE, ...) -> FALSE
                        var value = x.ArgumentsInSourceOrder[0].Value.ConstantValue;
                        if (value.HasValue && value.TryConvertToBool(out var bvalue) && !bvalue)
                        {
                            return new BoundLiteral(false.AsObject())
                            {
                                ConstantValue = false.AsOptional()
                            }.WithContext(x);
                        }
                    }
                    return null;
                } },
                { NameUtils.SpecialNames.ini_get, x =>
                {
                    // ini_get( {svalue} ) : string|FALSE
                    if (x.ArgumentsInSourceOrder.Length == 1 &&
                        x.ArgumentsInSourceOrder[0].Value.ConstantValue.TryConvertToString(out var svalue))
                    {
                        // options we're not supporting for sure
                        // always FALSE
                        if (svalue.StartsWith("xdebug.") || svalue.StartsWith("xcache.") || svalue.StartsWith("opcache.") || svalue.StartsWith("apc."))
                        {
                            return new BoundLiteral(false.AsObject())
                            {
                                ConstantValue = false.AsOptional()
                            }.WithContext(x);

                            // TODO: well-known ini options can be translated to access the configuration property directly
                        }
                    }

                    return null;
                } },
                { NameUtils.SpecialNames.extension_loaded, x =>
                {
                    // extension_loaded( {ext_name} ) : true|false
                    if (x.ArgumentsInSourceOrder.Length == 1 &&
                        x.ArgumentsInSourceOrder[0].Value.ConstantValue.TryConvertToString(out var ext_name))
                    {
                        bool hasextension = DeclaringCompilation.HasPhpExtenion(ext_name);

                        // CONSIDER: only when hasextension == True ? Can we add extensions in runtime ?

                        Trace.WriteLine($"'extension_loaded({ext_name})' evaluated to {hasextension}.");

                        return new BoundLiteral(hasextension.AsObject())
                        {
                            ConstantValue = hasextension.AsOptional()
                        }.WithContext(x);
                    }
                    return null;
                } },
                { NameUtils.SpecialNames.ord, x =>
                {
                    var typeCtx = _routine.TypeRefContext;

                    // ord($s[$i]) -> (int)s[i]     (elimination of the unnecessary allocation of a 1-char string)
                    if (x.ArgumentsInSourceOrder.Length == 1 &&
                        x.ArgumentsInSourceOrder[0].Value is BoundArrayItemEx itemAccess &&
                        itemAccess.Index != null && itemAccess.Index.TypeRefMask.IsSingleType && typeCtx.IsLong(itemAccess.Index.TypeRefMask))
                    {
                        return new BoundArrayItemOrdEx(DeclaringCompilation, itemAccess.Array, itemAccess.Index).WithContext(x);
                    }

                    return null;
                } },
            };
        }

        private TransformationRewriter(DelayedTransformations delayedTransformations, SourceRoutineSymbol routine)
            : this()
        {
            _delayedTransformations = delayedTransformations;
            _routine = routine ?? throw ExceptionUtilities.ArgumentNull(nameof(routine));

            // Gather information about value copy operations which can be removed
            _unnecessaryCopies = CopyAnalysis.TryGetUnnecessaryCopies(_routine);
        }

        private void TryTransformParameters()
        {
            var needPassValueParams = ParameterAnalysis.GetNeedPassValueParams(_routine);

            foreach (var parameter in _routine.SourceParameters)
            {
                var varindex = _routine.ControlFlowGraph.FlowContext.GetVarIndex(parameter.Syntax.Name.Name);
                if (!needPassValueParams.Get(varindex) && parameter.CopyOnPass)
                {
                    // It is unnecessary to copy a parameter whose value is only passed to another routines and cannot change
                    parameter.CopyOnPass = false;
                    TransformationCount++;
                }
            }
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
                        return new BoundConversionEx(x.Condition, BoundTypeRefFactory.BoolTypeRef).WithAccess(x);
                    }
                    else if (!trueVal && falseVal)
                    {
                        // A ? false : true => !A
                        TransformationCount++;
                        return new BoundUnaryEx(x.Condition, Ast.Operations.LogicNegation).WithAccess(x);
                    }
                }

                // (isset($a[$x]) ? $a[$x] : fallback) or (isset($a['const']) ? $a['const'] : fallback)
                if (x.Condition is BoundOffsetExists issetArrItem &&
                    x.IfTrue is BoundArrayItemEx trueArrItem &&
                    issetArrItem.Receiver is BoundVariableRef issetArrayVar &&
                    trueArrItem.Array is BoundVariableRef trueArrayVar &&
                    issetArrayVar.Variable == trueArrayVar.Variable &&
                    ((issetArrItem.Index is BoundVariableRef issetIndexVar &&
                        trueArrItem.Index is BoundVariableRef trueIndexVar &&
                        issetIndexVar.Variable == trueIndexVar.Variable)
                    || (issetArrItem.Index.ConstantValue.HasValue &&
                        issetArrItem.Index.ConstantValue.EqualsOptional(trueArrItem.Index.ConstantValue))))
                {
                    ++TransformationCount;
                    return new BoundTryGetItem(issetArrayVar, issetArrItem.Index, x.IfFalse).WithAccess(x);
                }

                // handled in BoundConditionalEx.Emit:
                //// !COND ? A : B => COND ? B : A
                //if (x.Condition is BoundUnaryEx unary && unary.Operation == Ast.Operations.LogicNegation)
                //{
                //    TransformationCount++;
                //    return new BoundConditionalEx(unary.Operand, x.IfFalse, x.IfTrue).WithAccess(x);
                //}
            }

            return x;
        }

        public override object VisitBinaryExpression(BoundBinaryEx x)
        {
            if (x.Operation == Ast.Operations.And ||
                x.Operation == Ast.Operations.Or)
            {
                // AND, OR:
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
            else if (x.Operation == Ast.Operations.Mul)
            {
                if ((x.Left.ConstantValue.TryConvertToLong(out long leftCons) && leftCons == -1)
                    || (x.Right.ConstantValue.TryConvertToLong(out long rightCons) && rightCons == -1))
                {
                    // X * -1, -1 * X -> -X
                    TransformationCount++;
                    return new BoundUnaryEx(leftCons == -1 ? x.Right : x.Left, Ast.Operations.Minus).WithAccess(x.Access);
                }
            }

            //
            return base.VisitBinaryExpression(x);
        }

        public override object VisitUnaryExpression(BoundUnaryEx x)
        {
            if (x.Operation == Ast.Operations.LogicNegation &&
                x.Operand is BoundUnaryEx ux &&
                ux.Operation == Ast.Operations.LogicNegation)
            {
                // !!X -> (bool)X
                TransformationCount++;
                return new BoundConversionEx((BoundExpression)Accept(ux.Operand), BoundTypeRefFactory.BoolTypeRef).WithAccess(x.Access);
            }

            return base.VisitUnaryExpression(x);
        }

        public override object VisitCopyValue(BoundCopyValue x)
        {
            var valueEx = (BoundExpression)Accept(x.Expression);
            if (valueEx.IsDeeplyCopied && _unnecessaryCopies?.Contains(x) != true)
            {
                return x.Update(valueEx);
            }
            else
            {
                // deep copy is unnecessary:
                TransformationCount++;
                return valueEx;
            }
        }

        public override object VisitAssign(BoundAssignEx x)
        {
            // A = A <binOp> <right>
            if (x.Target is BoundVariableRef trg
                && MatchExprSkipCopy(x.Value, out BoundBinaryEx binOp, isCopied: out _)
                && binOp.Left is BoundVariableRef valLeft
                && trg.Variable == valLeft.Variable)
            {
                var newTrg =
                    new BoundVariableRef(trg.Name)
                    .WithAccess(trg.Access.WithRead())
                    .WithSyntax(trg.PhpSyntax);

                // A = A +/- 1; => ++A; / --A;
                if ((binOp.Operation == Ast.Operations.Add || binOp.Operation == Ast.Operations.Sub)
                    && binOp.Right.ConstantValue.IsInteger(out long rightVal) && rightVal == 1)
                {
                    TransformationCount++;
                    return new BoundIncDecEx(newTrg, binOp.Operation == Ast.Operations.Add, false).WithAccess(x);
                }

                // A = A & B => A &= B; // &, |, ^, <<, >>, +, -, *, /, %, **, .
                switch (binOp.Operation)
                {
                    case Ast.Operations.BitAnd:
                    case Ast.Operations.BitOr:
                    case Ast.Operations.BitXor:
                    case Ast.Operations.ShiftLeft:
                    case Ast.Operations.ShiftRight:
                    case Ast.Operations.Add:
                    case Ast.Operations.Sub:
                    case Ast.Operations.Mul:
                    case Ast.Operations.Div:
                    case Ast.Operations.Mod:
                    case Ast.Operations.Pow:
                    case Ast.Operations.Concat:
                        TransformationCount++;
                        var compoundOp = AstUtils.BinaryToCompoundOp(binOp.Operation);
                        return new BoundCompoundAssignEx(newTrg, binOp.Right, compoundOp).WithAccess(x);
                }
            }

            return base.VisitAssign(x);
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

            if (x.Condition is BoundBinaryEx bex)
            {
                // if (A && FALSE)
                if (bex.Operation == Ast.Operations.And && bex.Right.ConstantValue.TryConvertToBool(out var bright) && bright == false)
                {
                    // if (Left && FALSE) {Unreachable} else {F} -> if (Left) {F} else {F}
                    // result is always FALSE but we have to evaluate Left
                    TransformationCount++;
                    NotePossiblyUnreachable(x.TrueTarget);

                    var target = (BoundBlock)Accept(x.FalseTarget);
                    return new ConditionalEdge(target, target, bex.Left.WithAccess(BoundAccess.None));
                }

                // if (A || TRUE)
                if (bex.Operation == Ast.Operations.Or && bex.Right.ConstantValue.TryConvertToBool(out bright) && bright == true)
                {
                    // if (Left || TRUE) {T} else {Unreachable} -> if (Left) {T} else {T}
                    // result is always FALSE but we have to evaluate Left
                    TransformationCount++;
                    NotePossiblyUnreachable(x.FalseTarget);

                    var target = (BoundBlock)Accept(x.TrueTarget);
                    return new ConditionalEdge(target, target, bex.Left.WithAccess(BoundAccess.None));
                }
            }

            //
            return base.VisitCFGConditionalEdge(x);
        }

        public override object VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            // first rewrite function arguments if possible
            var result = base.VisitGlobalFunctionCall(x);

            // use rewrite rule for this routine:
            if ((x = result as BoundGlobalFunctionCall) != null && x.TargetMethod != null &&
                _special_functions.TryGetValue(new QualifiedName(new Name(x.TargetMethod.RoutineName)), out var rewrite_func))
            {
                var newnode = rewrite_func(x);
                if (newnode != null)
                {
                    TransformationCount++;
                    return newnode;
                }
            }

            //
            return result;
        }

        public override object VisitConcat(BoundConcatEx x)
        {
            // transform arguments first:
            x = (BoundConcatEx)base.VisitConcat(x);

            //
            var args = x.ArgumentsInSourceOrder;
            if (args.Length == 0 || args.All(IsEmptyString))
            {
                // empty string:
                TransformationCount++;
                return new BoundLiteral(string.Empty) { ConstantValue = new Optional<object>(string.Empty) }.WithContext(x);
            }

            // visit & concat in compile time if we can:
            var newargs = args;
            int i = 0;
            do
            {
                // accumulate evaluated string value if possible:
                if (newargs[i].Value.ConstantValue.TryConvertToString(out var value))
                {
                    string result = value;
                    int end = i + 1;
                    while (end < newargs.Length && newargs[end].Value.ConstantValue.TryConvertToString(out var tmp))
                    {
                        result += tmp;
                        end++;
                    }

                    if (end > i + 1) // we concat'ed something!
                    {
                        newargs = newargs.RemoveRange(i, end - i);

                        if (!string.IsNullOrEmpty(result))
                        {
                            newargs = newargs.Insert(i, BoundArgument.Create(new BoundLiteral(result)
                            {
                                ConstantValue = new Optional<object>(result),
                                TypeRefMask = _routine.TypeRefContext.GetStringTypeMask(),
                                ResultType = DeclaringCompilation.CoreTypes.String,
                            }.WithAccess(BoundAccess.Read)));
                        }
                    }
                }

                //
                i++;
            } while (i < newargs.Length);

            //
            if (newargs != args)
            {
                TransformationCount++;

                if (newargs.Length == 0)
                {
                    return new BoundLiteral(string.Empty) { ConstantValue = new Optional<object>(string.Empty) }.WithContext(x);
                }
                else if (newargs.Length == 1 && newargs[0].Value.ConstantValue.TryConvertToString(out var value))
                {
                    // "value"
                    return new BoundLiteral(value) { ConstantValue = new Optional<object>(value) }.WithContext(x);
                }

                //
                return x.Update(newargs);
            }

            //
            return x;
        }

        public override object VisitLiteral(BoundLiteral x)
        {
            // implicit conversion: string -> callable
            if (x.Access.TargetType == DeclaringCompilation.CoreTypes.IPhpCallable &&
                x.ConstantValue.TryConvertToString(out var fnName))
            {
                // Template: (callable)"fnName"
                // resolve the 'RoutineInfo' if possible

                MethodSymbol symbol = null;
                var dc = fnName.IndexOf(Name.ClassMemberSeparator, StringComparison.Ordinal);
                if (dc < 0)
                {
                    var fnQName = NameUtils.MakeQualifiedName(fnName, true);
                    symbol = (MethodSymbol)DeclaringCompilation.ResolveFunction(fnQName, _routine);
                }
                else
                {
                    //var tname = fnName.Remove(dc);
                    //var mname = fnName.Substring(dc + Name.ClassMemberSeparator.Length);

                    // type::method
                    // self::method
                    // parent::method
                }

                if (symbol.IsValidMethod() || (symbol is AmbiguousMethodSymbol a && a.IsOverloadable && a.Ambiguities.Length != 0)) // valid or ambiguous
                {
                    TransformationCount++;
                    x.Access = x.Access.WithRead(DeclaringCompilation.CoreTypes.String);    // read the literal as string, do not rewrite it to BoundCallableConvert again
                    return new BoundCallableConvert(x, DeclaringCompilation) { TargetCallable = symbol }.WithContext(x);
                }
            }

            //
            return base.VisitLiteral(x);
        }

        public override object VisitFunctionDeclaration(BoundFunctionDeclStatement x)
        {
            if (x.Function.IsConditional && !IsConditional && _routine.IsGlobalScope)
            {
                _delayedTransformations.FunctionsMarkedAsUnconditional.Add(x.Function);
            }

            // no transformation
            return base.VisitFunctionDeclaration(x);
        }

        public override object VisitIsEmpty(BoundIsEmptyEx x)
        {
            if (x.Operand is BoundVariableRef varRef && varRef.Name.IsDirect && !varRef.Name.NameValue.IsAutoGlobal)
            {
                var flowContext = _routine.ControlFlowGraph.FlowContext;

                var varType = flowContext.GetVarType(varRef.Name.NameValue);
                if (!varType.IsRef && (varType.IsVoid || flowContext.TypeRefContext.WithoutNull(varType).IsVoid)
                    && !_routine.IsGlobalScope && (_routine.Flags & RoutineFlags.RequiresLocalsArray) == 0)
                {
                    // empty($x) where $x is undefined or null -> TRUE
                    TransformationCount++;
                    return new BoundLiteral(true.AsObject()).WithAccess(x.Access);
                }
            }

            return base.VisitIsEmpty(x);
        }

        public override object VisitExpressionStatement(BoundExpressionStatement x)
        {
            // Transform the original expression first
            x = (BoundExpressionStatement)base.VisitExpressionStatement(x);

            // Transform functions which can be turned only to statements (i.e. not to expressions)
            if (_routine.IsGlobalScope && x.Expression is BoundGlobalFunctionCall call && call.Name.IsDirect)
            {
                var args = call.ArgumentsInSourceOrder;
                if (call.Name.NameValue == NameUtils.SpecialNames.define && args.Length == 2
                    && args[0].Value.ConstantValue.TryConvertToString(out string constName)
                    && args[1].Value.ConstantValue.HasValue)
                {
                    // define("CONST", "value") -> const \CONST = value
                    TransformationCount++;
                    return new BoundGlobalConstDeclStatement(NameUtils.MakeQualifiedName(constName, true), args[1].Value);
                }
            }

            return x;
        }

        public override object VisitArray(BoundArrayEx x)
        {
            bool TryGetMethod(TypeSymbol typeSymbol, string methodName, out MethodSymbol methodSymbol)
            {
                if (typeSymbol != null && typeSymbol.TypeKind != TypeKind.Error &&
                    typeSymbol.LookupMethods(methodName).SingleOrDefault() is MethodSymbol mSymbol && mSymbol.IsValidMethod() &&
                    mSymbol.IsAccessible(_routine.ContainingType))
                {
                    methodSymbol = mSymbol;
                    return true;
                }
                else
                {
                    methodSymbol = null;
                    return false;
                }
            }

            BoundCallableConvert Transform(BoundArrayEx origArray, IMethodSymbol targetCallable, BoundExpression receiver = null)
            {
                // read the literal as array, do not rewrite it to BoundCallableConvert again
                origArray.Access = origArray.Access.WithRead(DeclaringCompilation.CoreTypes.PhpArray);

                TransformationCount++;
                return new BoundCallableConvert(origArray, DeclaringCompilation)
                {
                    TargetCallable = targetCallable,
                    Receiver = receiver
                }.WithContext(origArray);
            }

            // implicit conversion: ["typeName" / $this, "methodName"] -> callable
            if (x.Access.TargetType == DeclaringCompilation.CoreTypes.IPhpCallable &&
                x.Items.Length == 2 && x.Items[1].Value.ConstantValue.TryConvertToString(out var methodName))
            {
                var item0 = x.Items[0].Value;
                if (item0.ConstantValue.TryConvertToString(out var typeName))
                {
                    var typeQName = NameUtils.MakeQualifiedName(typeName, true);
                    TypeSymbol typeSymbol = null;
                    if (typeQName.IsReservedClassName)
                    {
                        if (typeQName.IsSelfClassName)
                        {
                            // ["self", "methodName"]
                            typeSymbol = _routine.ContainingType.IsClassType() ? _routine.ContainingType : null;
                        }
                        else if (typeQName.IsParentClassName)
                        {
                            // ["parent", "methodName"]
                            typeSymbol = _routine.ContainingType.IsClassType() ? _routine.ContainingType.BaseType : null;
                        }
                    }
                    else
                    {
                        typeSymbol = DeclaringCompilation.GlobalSemantics.ResolveType(typeQName) as TypeSymbol;
                    }

                    if (TryGetMethod(typeSymbol, methodName, out var methodSymbol) && methodSymbol.IsStatic)
                    {
                        // ["typeName", "methodName"]
                        return Transform(x, methodSymbol);
                    }
                }
                else if (MatchExprSkipCopy(item0, out BoundVariableRef varRef, out _) &&
                         varRef.Variable is ThisVariableReference thisVar &&
                         TryGetMethod(thisVar.Type, methodName, out var methodSymbol) && !methodSymbol.IsStatic)
                {
                    // [$this, "methodName"]
                    return Transform(x, methodSymbol, varRef);
                }
            }

            return base.VisitArray(x);
        }

        /// <summary>
        /// If <paramref name="expr"/> is of type <typeparamref name="T"/> or it is a <see cref="BoundCopyValue" /> enclosing an
        /// expression of type <typeparamref name="T"/>, store the expression to <paramref name="typedExpr"/> and return true;
        /// otherwise, return false. Store to <paramref name="isCopied"/> whether <paramref name="typedExpr"/> was enclosed in
        /// <see cref="BoundCopyValue"/>.
        /// </summary>
        private static bool MatchExprSkipCopy<T>(BoundExpression expr, out T typedExpr, out bool isCopied) where T : BoundExpression
        {
            if (expr is T res)
            {
                typedExpr = res;
                isCopied = false;
                return true;
            }
            else if (expr is BoundCopyValue copyVal)
            {
                isCopied = true;
                return MatchExprSkipCopy<T>(copyVal.Expression, out typedExpr, out _);
            }
            else
            {
                typedExpr = default;
                isCopied = false;
                return false;
            }
        }

        private static bool IsEmptyString(BoundArgument a) => a.Value.ConstantValue.HasValue && ExpressionsExtension.IsEmptyStringValue(a.Value.ConstantValue.Value);
    }
}
