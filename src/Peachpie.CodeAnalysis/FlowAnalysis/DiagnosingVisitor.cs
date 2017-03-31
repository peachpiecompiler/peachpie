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
            if (x.MaybeUninitialized)
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
    }
}
