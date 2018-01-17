using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis.Errors;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Symbols;
using Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    internal partial class DiagnosingVisitor : GraphVisitor
    {
        private readonly DiagnosticBag _diagnostics;
        private SourceRoutineSymbol _routine;

        PhpCompilation DeclaringCompilation => _routine.DeclaringCompilation;

        TypeRefContext TypeCtx => _routine.TypeRefContext;

        int _inTryLevel = 0;
        int _inCatchLevel = 0;
        int _inFinallyLevel = 0;

        Stack<BoundBlock> endOfTryBlocks = new Stack<BoundBlock>();

        void Add(Devsense.PHP.Text.Span span, Devsense.PHP.Errors.ErrorInfo err, params string[] args)
        {
            _diagnostics.Add(DiagnosticBagExtensions.ParserDiagnostic(_routine, span, err, args));
        }

        void CannotInstantiate(IPhpOperation op, string kind, BoundTypeRef t)
        {
            _diagnostics.Add(_routine, op.PhpSyntax, ErrorCode.ERR_CannotInstantiateType, kind, t.ResolvedType);
        }

        public static void Analyse(DiagnosticBag diagnostics, SourceRoutineSymbol routine)
        {
            if (routine.ControlFlowGraph != null)   // non-abstract method
            {
                new DiagnosingVisitor(diagnostics, routine)
                    .VisitCFG(routine.ControlFlowGraph);
            }
        }

        private DiagnosingVisitor(DiagnosticBag diagnostics, SourceRoutineSymbol routine)
        {
            _diagnostics = diagnostics;
            _routine = routine;
        }

        public override void VisitCFG(ControlFlowGraph x)
        {
            Debug.Assert(x == _routine.ControlFlowGraph);

            InitializeReachabilityInfo(x);

            base.VisitCFG(x);

            // analyse missing or redefined labels
            CheckLabels(x.Labels);

            // TODO: Report also unreachable code caused by situations like if (false) { ... }
            CheckUnreachableCode(x);
        }

        void CheckLabels(ControlFlowGraph.LabelBlockState[] labels)
        {
            if (labels == null || labels.Length == 0)
            {
                return;
            }

            for (int i = 0; i < labels.Length; i++)
            {
                var flags = labels[i].Flags;
                if ((flags & ControlFlowGraph.LabelBlockFlags.Defined) == 0)
                {
                    Add(labels[i].LabelSpan, Devsense.PHP.Errors.Errors.UndefinedLabel, labels[i].Label);
                }
                if ((flags & ControlFlowGraph.LabelBlockFlags.Used) == 0)
                {
                    // Warning: label not used
                }
                if ((flags & ControlFlowGraph.LabelBlockFlags.Redefined) != 0)
                {
                    Add(labels[i].LabelSpan, Devsense.PHP.Errors.Errors.LabelRedeclared, labels[i].Label);
                }
            }
        }

        public override void VisitEval(BoundEvalEx x)
        {
            _diagnostics.Add(_routine, new TextSpan(x.PhpSyntax.Span.Start, 4)/*'eval'*/, ErrorCode.WRN_EvalDiscouraged);

            base.VisitEval(x);
        }

        public override void VisitTypeRef(BoundTypeRef typeRef)
        {
            if (typeRef != null)
            {
                CheckUndefinedType(typeRef);
                base.VisitTypeRef(typeRef);
            }
        }

        public override void VisitNew(BoundNewEx x)
        {
            if (x.TypeRef.ResolvedType.IsValidType())
            {
                if (x.TypeRef.ResolvedType.IsInterfaceType())
                {
                    CannotInstantiate(x, "interface", x.TypeRef);
                }
                else if (x.TypeRef.ResolvedType.IsStatic)
                {
                    CannotInstantiate(x, "static", x.TypeRef);
                }
                else if (x.TypeRef.ResolvedType.IsTraitType())
                {
                    CannotInstantiate(x, "trait", x.TypeRef);
                }
                else // class:
                {
                    // cannot instantiate Closure
                    if (x.TypeRef.ResolvedType == DeclaringCompilation.CoreTypes.Closure)
                    {
                        // Instantiation of '{0}' is not allowed
                        Add(x.TypeRef.TypeRef.Span, Devsense.PHP.Errors.Errors.ClosureInstantiated, x.TypeRef.ResolvedType.Name);
                    }

                    //
                    else if (x.TypeRef.ResolvedType.IsAbstract)
                    {
                        // Cannot instantiate abstract class {0}
                        CannotInstantiate(x, "abstract class", x.TypeRef);
                    }
                }
            }

            base.VisitNew(x);
        }

        public override void VisitReturn(BoundReturnStatement x)
        {
            if (_routine.Syntax is MethodDecl m)
            {
                if (m.Name.Name.IsToStringName)
                {
                    // __tostring() allows only strings to be returned
                    if (x.Returned == null || !IsAllowedToStringReturnType(x.Returned.TypeRefMask))
                    {
                        _diagnostics.Add(_routine, x.PhpSyntax ?? m, ErrorCode.ERR_ToStringMustReturnString, ((IPhpTypeSymbol)_routine.ContainingType).FullName.ToString());
                    }
                }
            }

            // "void" return type hint ?
            if (_routine.SyntaxReturnType is Devsense.PHP.Syntax.Ast.PrimitiveTypeRef pt && pt.PrimitiveTypeName == Devsense.PHP.Syntax.Ast.PrimitiveTypeRef.PrimitiveType.@void)
            {
                if (x.Returned != null)
                {
                    // A void function must not return a value
                    _diagnostics.Add(_routine, x.PhpSyntax, ErrorCode.ERR_VoidFunctionCannotReturnValue);
                }
            }

            //
            base.VisitReturn(x);
        }

        bool IsAllowedToStringReturnType(TypeRefMask tmask)
        {
            return
                tmask.IsRef ||
                tmask.IsAnyType ||  // dunno
                TypeCtx.IsAString(tmask);

            // anything else (object (even convertible to string), array, number, boolean, ...) is not allowed
        }

        public override void VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            CheckUndefinedFunctionCall(x);

            // calling indirectly:
            if (x.Name.NameExpression != null)
            {
                // check whether expression can be used as a function callback (must be callable - string, array, object ...)
                if (!TypeHelpers.IsCallable(TypeCtx, x.Name.NameExpression.TypeRefMask))
                {
                    _diagnostics.Add(_routine, x.PhpSyntax, ErrorCode.ERR_InvalidFunctionName, TypeCtx.ToString(x.Name.NameExpression.TypeRefMask));
                }
            }

            base.VisitGlobalFunctionCall(x);
        }

        public override void VisitInstanceFunctionCall(BoundInstanceFunctionCall call)
        {
            // check target type
            CheckMethodCallTargetInstance(call.Instance, call.Name.NameValue.Name.Value);

            // TODO: Enable the diagnostic when several problems are solved (such as __call())
            //CheckUndefinedMethodCall(call, call.Instance?.ResultType, call.Name);
            base.VisitInstanceFunctionCall(call);
        }

        public override void VisitStaticFunctionCall(BoundStaticFunctionCall call)
        {
            // TODO: Enable the diagnostic when the __callStatic() method is properly processed during analysis
            //CheckUndefinedMethodCall(call, call.TypeRef?.ResolvedType, call.Name);
            base.VisitStaticFunctionCall(call);
        }

        public override void VisitVariableRef(BoundVariableRef x)
        {
            CheckUninitializedVariableUse(x);
            base.VisitVariableRef(x);
        }

        public override void VisitTemporalVariableRef(BoundTemporalVariableRef x)
        {
            // do not make diagnostics on syntesized variables
        }

        public override void VisitDeclareStatement(BoundDeclareStatement x)
        {
            _diagnostics.Add(
                _routine,
                ((DeclareStmt)x.PhpSyntax).GetDeclareClauseSpan(),
                ErrorCode.WRN_NotYetImplementedIgnored,
                "Declare construct");

            base.VisitDeclareStatement(x);
        }

        public override void VisitAssert(BoundAssertEx x)
        {
            base.VisitAssert(x);

            var args = x.ArgumentsInSourceOrder;

            // check number of parameters
            // check whether it is not always false or always true
            if (args.Length >= 1)
            {
                if (args[0].Value.ConstantValue.EqualsOptional(false.AsOptional()))
                {
                    // always failing
                    _diagnostics.Add(_routine, x.PhpSyntax, ErrorCode.WRN_AssertAlwaysFail);
                }

                if (TypeCtx.IsAString(args[0].Value.TypeRefMask))
                {
                    // deprecated and not supported
                    _diagnostics.Add(_routine, args[0].Value.PhpSyntax, ErrorCode.WRN_StringAssertionDeprecated);
                }

                if (args.Length > 2)
                {
                    // too many args
                    _diagnostics.Add(_routine, x.PhpSyntax, ErrorCode.WRN_TooManyArguments);
                }
            }
            else
            {
                // assert() expects at least 1 parameter, 0 given
                _diagnostics.Add(_routine, x.PhpSyntax, ErrorCode.WRN_MissingArguments, "assert", 1, 0);
            }
        }

        public override void VisitBinaryExpression(BoundBinaryEx x)
        {
            base.VisitBinaryExpression(x);

            //

            switch (x.Operation)
            {
                case Operations.Div:
                    if (x.Right.IsConstant())
                    {
                        if (x.Right.ConstantValue.IsZero())
                        {
                            Add(x.Right.PhpSyntax.Span, Devsense.PHP.Errors.Warnings.DivisionByZero);
                        }
                    }
                    break;
            }
        }

        void CheckMethodCallTargetInstance(BoundExpression target, string methodName)
        {
            if (target == null)
            {
                // syntax error (?)
                return;
            }

            string nonobjtype = null;

            if (target.ResultType != null)
            {
                switch (target.ResultType.SpecialType)
                {
                    case SpecialType.System_Void:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_String:
                    case SpecialType.System_Boolean:
                        nonobjtype = target.ResultType.GetPhpTypeNameOrNull();
                        break;
                    default:
                        if (target.ResultType == DeclaringCompilation.CoreTypes.PhpString ||
                            target.ResultType == DeclaringCompilation.CoreTypes.PhpArray ||
                            target.ResultType == DeclaringCompilation.CoreTypes.PhpNumber ||
                            target.ResultType == DeclaringCompilation.CoreTypes.PhpResource ||
                            target.ResultType == DeclaringCompilation.CoreTypes.IPhpArray ||
                            target.ResultType == DeclaringCompilation.CoreTypes.IPhpCallable)
                        {
                            nonobjtype = target.ResultType.GetPhpTypeNameOrNull();
                        }
                        break;
                }
            }
            else
            {
                var tmask = target.TypeRefMask;
                if (!tmask.IsAnyType && !tmask.IsRef && !TypeCtx.IsObject(tmask))
                {
                    nonobjtype = TypeCtx.ToString(tmask);
                }
            }

            //
            if (nonobjtype != null)
            {
                _diagnostics.Add(_routine, target.PhpSyntax, ErrorCode.ERR_MethodCalledOnNonObject, methodName ?? "{}", nonobjtype);
            }
        }

        private void CheckUndefinedFunctionCall(BoundGlobalFunctionCall x)
        {
            if (x.Name.IsDirect && x.TargetMethod.IsErrorMethodOrNull())
            {
                var errmethod = (ErrorMethodSymbol)x.TargetMethod;
                if (errmethod != null && errmethod.ErrorKind == ErrorMethodKind.Missing)
                {
                    _diagnostics.Add(_routine, ((FunctionCall)x.PhpSyntax).NameSpan.ToTextSpan(), ErrorCode.WRN_UndefinedFunctionCall, x.Name.NameValue.ToString());
                }
            }
        }

        private void CheckUndefinedMethodCall(BoundRoutineCall x, TypeSymbol type, BoundRoutineName name)
        {
            if (name.IsDirect && x.TargetMethod.IsErrorMethodOrNull() && type != null && !type.IsErrorType())
            {
                _diagnostics.Add(_routine, ((FunctionCall)x.PhpSyntax).NameSpan.ToTextSpan(), ErrorCode.WRN_UndefinedMethodCall, name.NameValue.ToString(), type.Name);
            }
        }

        private void CheckUninitializedVariableUse(BoundVariableRef x)
        {
            if (x.MaybeUninitialized && !x.Access.IsQuiet && x.PhpSyntax != null)
            {
                _diagnostics.Add(_routine, x.PhpSyntax, ErrorCode.WRN_UninitializedVariableUse, x.Name.NameValue.ToString());
            }
        }

        private void CheckUndefinedType(BoundTypeRef typeRef)
        {
            // Ignore indirect types (e.g. $foo = new $className())
            if (typeRef.IsDirect && (typeRef.ResolvedType == null || typeRef.ResolvedType.IsErrorType()))
            {
                var errtype = typeRef.ResolvedType as ErrorTypeSymbol;
                if (errtype != null && errtype.CandidateReason == CandidateReason.Ambiguous)
                {
                    // type is declared but ambiguously,
                    // warning with declaration ambiguity was already reported, we may skip following
                    return;
                }

                if (typeRef.TypeRef is ReservedTypeRef)
                {
                    // unresolved parent, self ?
                }
                else
                {
                    var name = typeRef.TypeRef.QualifiedName?.ToString();
                    _diagnostics.Add(_routine, typeRef.TypeRef, ErrorCode.WRN_UndefinedType, name);
                }
            }
        }

        public override void VisitCFGBlock(BoundBlock x)
        {
            // is current block directly after the end of some try block?
            Debug.Assert(_inTryLevel == 0 || endOfTryBlocks.Count > 0);
            if (_inTryLevel > 0 && endOfTryBlocks.Peek() == x) { --_inTryLevel; endOfTryBlocks.Pop(); }

            base.VisitCFGBlock(x);
        }

        public override void VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            // .Accept() on BodyBlocks traverses not only the try block but also the rest of the code
            ++_inTryLevel;
            var hasEndBlock = (x.NextBlock != null);                // if there's a block directly after try-catch-finally
            if (hasEndBlock) { endOfTryBlocks.Push(x.NextBlock); }  // -> add it as ending block
            x.BodyBlock.Accept(this);
            if (!hasEndBlock) { --_inTryLevel; }                     // if there isn't dicrease tryLevel after going trough the try & rest (nothing)

            foreach (var c in x.CatchBlocks)
            {
                ++_inCatchLevel;
                c.Accept(this);
                --_inCatchLevel;
            }


            if (x.FinallyBlock != null)
            {
                ++_inFinallyLevel;
                x.FinallyBlock.Accept(this);
                --_inFinallyLevel;
            }
        }

        public override void VisitStaticStatement(BoundStaticVariableStatement x)
        {
            base.VisitStaticStatement(x);
        }

        public override void VisitYieldStatement(BoundYieldStatement boundYieldStatement)
        {

            // TODO: Start supporting yielding from exception handling constructs.
            if (_inTryLevel > 0 || _inCatchLevel > 0 || _inFinallyLevel > 0)
            {
                _diagnostics.Add(_routine, boundYieldStatement.PhpSyntax, ErrorCode.ERR_NotYetImplemented, "Yielding from an exception handling construct (try, catch, finally)");
            }
        }
    }
}
