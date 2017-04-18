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
    internal class DiagnosingVisitor : GraphVisitor
    {
        private readonly DiagnosticBag _diagnostics;
        private SourceRoutineSymbol _routine;

        int inTryLevel = 0;
        int inCatchLevel = 0;
        int inFinallyLevel = 0;

        Stack<BoundBlock> endOfTryBlocks = new Stack<BoundBlock>();

        private int _visitedColor;

        public DiagnosingVisitor(DiagnosticBag diagnostics, SourceRoutineSymbol routine)
        {
            _diagnostics = diagnostics;
            _routine = routine;
        }

        public override void VisitCFG(ControlFlowGraph x)
        {
            Debug.Assert(x == _routine.ControlFlowGraph);

            _visitedColor = x.NewColor();
            base.VisitCFG(x);
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

        public override void VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            CheckUndefinedFunctionCall(x);
            base.VisitGlobalFunctionCall(x);
        }

        public override void VisitInstanceFunctionCall(BoundInstanceFunctionCall call)
        {
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

        protected override void VisitCFGBlockInternal(BoundBlock x)
        {
            if (x.Tag != _visitedColor)
            {
                x.Tag = _visitedColor;
                base.VisitCFGBlockInternal(x); 
            }
        }

        private void CheckUndefinedFunctionCall(BoundGlobalFunctionCall x)
        {
            if (x.Name.IsDirect && x.TargetMethod.IsErrorMethod())
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
            if (name.IsDirect && x.TargetMethod.IsErrorMethod() && type != null && !type.IsErrorType())
            {
                _diagnostics.Add(_routine, ((FunctionCall)x.PhpSyntax).NameSpan.ToTextSpan(), ErrorCode.WRN_UndefinedMethodCall, name.NameValue.ToString(), type.Name);
            }
        }

        private void CheckUninitializedVariableUse(BoundVariableRef x)
        {
            if (x.MaybeUninitialized && !(x.PhpSyntax.ContainingElement is IssetEx))
            {
                _diagnostics.Add(_routine, x.PhpSyntax, ErrorCode.WRN_UninitializedVariableUse, x.Name.NameValue.ToString());
            }
        }

        private void CheckUndefinedType(BoundTypeRef typeRef)
        {
            // Ignore indirect types (e.g. $foo = new $className())
            if (typeRef.IsDirect && (typeRef.ResolvedType == null || typeRef.ResolvedType.IsErrorType()))
            {
                if (typeRef.TypeRef is ReservedTypeRef)
                {
                    // unresolved parent, self ?
                }
                else
                {
                    var name = typeRef.TypeRef.QualifiedName?.ToString();
                    _diagnostics.Add(this._routine, typeRef.TypeRef, ErrorCode.WRN_UndefinedType, name);
                }
            }
        }

        public override void VisitCFGBlock(BoundBlock x)
        {
            // is current block directly after the end of some try block?
            Debug.Assert(inTryLevel == 0 || endOfTryBlocks.Count > 0);
            if (inTryLevel > 0 && endOfTryBlocks.Peek() == x) { --inTryLevel; endOfTryBlocks.Pop(); }

            base.VisitCFGBlock(x);
        }

        public override void VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            // .Accept() on BodyBlocks traverses not only the try block but also the rest of the code
            ++inTryLevel;
            var hasEndBlock = (x.NextBlock != null);                // if there's a block directly after try-catch-finally
            if (hasEndBlock) { endOfTryBlocks.Push(x.NextBlock); }  // -> add it as ending block
            x.BodyBlock.Accept(this);
            if (!hasEndBlock) { --inTryLevel; }                     // if there isn't dicrease tryLevel after going trough the try & rest (nothing)

            foreach (var c in x.CatchBlocks)
            {
                ++inCatchLevel;
                c.Accept(this);
                --inCatchLevel;
            }


            if (x.FinallyBlock != null)
            {
                ++inFinallyLevel;
                x.FinallyBlock.Accept(this);
                --inFinallyLevel;
            }
        }

        public override void VisitStaticStatement(BoundStaticVariableStatement x)
        {
            // TODO: Remove once fix for static variables handling in methods with unoptimized locals is done.
            if (_routine.IsGeneratorMethod())
            {
                _diagnostics.Add(_routine, x.PhpSyntax, ErrorCode.ERR_NotYetImplemented, "Having static variables in generator methods");
            }
            base.VisitStaticStatement(x);
        }

        public override void VisitYield(BoundYieldEx boundYieldEx)
        {
            // TODO: Start supporting yielding from exception handling constructs.
            if (inTryLevel > 0 || inCatchLevel > 0 || inFinallyLevel > 0)
            {
                _diagnostics.Add(_routine, boundYieldEx.PhpSyntax, ErrorCode.ERR_NotYetImplemented, "Yielding from an exception handling construct (try, catch, finally)");
            }

            // TODO: Start supporting sending values & subsequently yield as an Expression
            if (boundYieldEx.Access.IsRead)
            {
                _diagnostics.Add(_routine, boundYieldEx.PhpSyntax, ErrorCode.ERR_NotYetImplemented, "Returning a value from yield");
            }
        }
    }
}
