using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.DiagnosticTests
{
    class SymbolsSelector : GraphVisitor
    {
        public struct SymbolStat
        {
            public Span Span { get; set; }
            public IPhpExpression BoundExpression { get; set; }
            public ISymbol Symbol { get; set; }
            public TypeRefContext TypeCtx { get; }

            public SymbolStat(TypeRefContext tctx, Span span, IPhpExpression expr = null, ISymbol symbol = null)
            {
                this.TypeCtx = tctx;
                this.Span = span;
                this.BoundExpression = expr;
                this.Symbol = symbol;
            }
        }

        /// <summary>
        /// Gets the resulting list.
        /// </summary>
        readonly List<SymbolStat> _result = new List<SymbolStat>(16);

        TypeRefContext _tctx;

        private SymbolsSelector() { }

        int _color;

        public static IEnumerable<SymbolStat> Select(ControlFlowGraph cfg)
        {
            var visitor = new SymbolsSelector();
            visitor.VisitCFG(cfg);
            return visitor._result;
        }

        public static IEnumerable<SymbolStat> Select(IPhpRoutineSymbol routine)
        {
            TypeRefContext typeCtx = routine.ControlFlowGraph?.FlowContext?.TypeRefContext;

            foreach (var p in routine.Parameters)
            {
                if (p.IsImplicitlyDeclared || p.Locations.Length == 0) continue;    // ignore compiler generated parameters
                var textSpan = p.Locations[0].SourceSpan;
                yield return new SymbolStat(typeCtx, textSpan.ToSpan(), null, p);
            }

            yield break;
        }

        public static IEnumerable<SymbolStat> Select(IPhpTypeSymbol type)
        {
            yield break;
        }

        public override void VisitCFG(ControlFlowGraph x)
        {
            _tctx = x.FlowContext?.TypeRefContext;
            _color = x.NewColor();
            base.VisitCFG(x);

            foreach (var block in x.UnreachableBlocks)
            {
                block.Accept(this);
            }
        }

        protected override void VisitCFGBlockInternal(BoundBlock x)
        {
            if (x.Tag == _color) return;
            x.Tag = _color;

            base.VisitCFGBlockInternal(x);
        }

        public override void VisitVariableRef(BoundVariableRef x)
        {
            ISymbol symbolOpt = null;
            try
            {
                // may throw NotImplementedException
                symbolOpt = (ISymbol)
                    (x.Variable as IVariableDeclaratorOperation)?.Symbol ??
                    (x.Variable as IParameterInitializerOperation)?.Parameter;
            }
            catch (NotImplementedException)
            {
                // ignore
            }

            _result.Add(new SymbolStat(_tctx, x.PhpSyntax.Span, x, symbolOpt));

            //
            base.VisitVariableRef(x);
        }

        public override void VisitTypeRef(BoundTypeRef x)
        {
            if (x != null)
            {
                if (x.Symbol != null)
                {
                    if (x.TypeRef is AnonymousTypeRef)
                    {
                        // nada
                    }
                    else
                    {
                        _result.Add(new SymbolStat(_tctx, x.TypeRef.Span, null, x.Symbol));
                    }
                }

                base.VisitTypeRef(x);
            }
        }

        protected override void VisitRoutineCall(BoundRoutineCall x)
        {
            var invocation = (IInvocationOperation)x;
            if (invocation.TargetMethod != null)
            {
                if (!invocation.TargetMethod.IsImplicitlyDeclared || invocation.TargetMethod is IErrorMethodSymbol)
                {
                    Span span;
                    if (x.PhpSyntax is FunctionCall)
                    {
                        span = ((FunctionCall)x.PhpSyntax).NameSpan;
                        _result.Add(new SymbolStat(_tctx, span, x, invocation.TargetMethod));
                    }
                }
            }

            //
            base.VisitRoutineCall(x);
        }

        public override void VisitGlobalConstUse(BoundGlobalConst x)
        {
            _result.Add(new SymbolStat(_tctx, x.PhpSyntax.Span, x, null));

            //
            base.VisitGlobalConstUse(x);
        }

        public override void VisitPseudoConstUse(BoundPseudoConst x)
        {
            _result.Add(new SymbolStat(_tctx, x.PhpSyntax.Span, x, null));

            base.VisitPseudoConstUse(x);
        }

        public override void VisitFieldRef(BoundFieldRef x)
        {
            Span span = Span.Invalid;
            if (x.PhpSyntax is DirectVarUse)
            {
                span = ((DirectVarUse)x.PhpSyntax).Span;
            }
            else if (x.PhpSyntax is StaticFieldUse)
            {
                span = ((StaticFieldUse)x.PhpSyntax).NameSpan;
            }
            else if (x.PhpSyntax is ClassConstUse)
            {
                span = ((ClassConstUse)x.PhpSyntax).NamePosition;
            }

            if (span.IsValid)
            {
                _result.Add(new SymbolStat(_tctx, span, x, ((IFieldReferenceOperation)x).Member));
            }

            //
            base.VisitFieldRef(x);
        }
    }
}
