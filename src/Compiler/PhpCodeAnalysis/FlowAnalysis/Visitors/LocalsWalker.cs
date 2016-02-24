using Pchp.Syntax;
using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Symbols;
using System.Diagnostics;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.FlowAnalysis.Visitors
{
    /// <summary>
    /// Walks through local variables.
    /// </summary>
    internal class LocalsWalker : TreeVisitor
    {
        protected class VisitLocalArgs : EventArgs
        {
            public VariableName Name;
            public LocalKind Kind;

            public VisitLocalArgs(VariableName name, LocalKind kind)
            {
                this.Name = name;
                this.Kind = kind;
            }
        }

        protected event EventHandler<VisitLocalArgs> VisitLocal;

        protected void OnLocal(VisitLocalArgs args)
        {
            Debug.Assert(args != null);

            var e = this.VisitLocal;
            if (e != null)
                e(this, args);
        }

        LocalKind _statementContext;
        LangElement _routine;

        public LocalsWalker(LangElement routine)
        {
            _statementContext = LocalKind.LocalVariable;
            _routine = routine;
        }

        public void VisitRoutine()
        {
            VisitElement(_routine);
        }

        #region AddVar

        private void AddVar(VariableName name, Syntax.Text.Span span)
        {
            AddVar(name, span, _statementContext);
        }

        private void AddVar(VariableName name, Syntax.Text.Span span, LocalKind kind)
        {
            if (name.IsThisVariableName)
                kind = LocalKind.ThisVariable;

            if (kind != LocalKind.GlobalVariable && kind != LocalKind.ThisVariable && kind != LocalKind.Parameter && kind != LocalKind.ReturnVariable    // just avoid checking IsAutoGlobal if not necessary
                && name.IsAutoGlobal)
                kind = LocalKind.GlobalVariable;

            OnLocal(new VisitLocalArgs(name, kind));
        }

        #endregion

        #region Declarations

        public override void VisitFunctionDecl(FunctionDecl x)
        {
            if (x == _routine)
            {
                _statementContext = LocalKind.LocalVariable;
                base.VisitFunctionDecl(x);
            }
        }

        public override void VisitMethodDecl(MethodDecl x)
        {
            if (x == _routine)
            {
                _statementContext = LocalKind.LocalVariable;
                base.VisitMethodDecl(x);
            }
        }

        public override void VisitTypeDecl(TypeDecl x)
        {
            // nope
        }

        public override void VisitLambdaFunctionExpr(LambdaFunctionExpr x)
        {
            if (x == _routine)
            {
                _statementContext = LocalKind.LocalVariable;

                // use params
                if (x.UseParams != null)
                    foreach (var u in x.UseParams)
                        AddVar(u.Name, u.Span, LocalKind.UseParameter);

                // params
                x.Signature.FormalParams.ForEach(VisitFormalParam);

                // body
                x.Body.ForEach(VisitElement);
            }
        }

        public override void VisitNamespaceDecl(NamespaceDecl x)
        {
            _statementContext = LocalKind.GlobalVariable;
            base.VisitNamespaceDecl(x);
        }

        public override void VisitGlobalCode(GlobalCode x)
        {
            _statementContext = LocalKind.GlobalVariable;
            base.VisitGlobalCode(x);
        }

        #endregion

        public override void VisitDirectVarUse(DirectVarUse x)
        {
            if (x.IsMemberOf == null)
            {
                AddVar(x.VarName, x.Span);
            }

            base.VisitDirectVarUse(x);
        }

        public override void VisitForeachStmt(ForeachStmt x)
        {
            var valuevar = x.ValueVariable; // VariableUse or ListEx
            var keyvar = x.KeyVariable;     // VariableUse or null

            if (valuevar.Variable != null)
                valuevar.Variable.VisitMe(this);
            else if (valuevar.List != null)
                valuevar.List.VisitMe(this);

            if (keyvar != null && keyvar.Variable != null)
                keyvar.Variable.VisitMe(this);

            //
            base.VisitForeachStmt(x);
        }

        public override void VisitGlobalStmt(GlobalStmt x)
        {
            var prevCtx = _statementContext;
            _statementContext = LocalKind.GlobalVariable;
            base.VisitGlobalStmt(x);
            _statementContext = prevCtx;
        }

        public override void VisitStaticStmt(StaticStmt x)
        {
            foreach (var st in x.StVarList)
            {
                VisitElement(st.Initializer);
                Debug.Assert(st.Variable.IsMemberOf == null);
                AddVar(st.Variable.VarName, st.Span, LocalKind.StaticVariable);
            }
        }

        public override void VisitFormalParam(FormalParam x)
        {
            AddVar(x.Name, x.Span, LocalKind.Parameter);
        }

        public override void VisitJumpStmt(JumpStmt x)
        {
            if (x.Type == JumpStmt.Types.Return && x.Expression != null)
            {
                AddVar(new VariableName(SourceReturnSymbol.SpecialName), x.Span, LocalKind.ReturnVariable);
            }

            base.VisitJumpStmt(x);
        }
    }

    internal class LocalsCollector : LocalsWalker
    {
        readonly SourceRoutineSymbol _routine;
        readonly List<SourceLocalSymbol> _locals = new List<SourceLocalSymbol>();
        readonly HashSet<VariableName>/*!*/_visited = new HashSet<VariableName>();
        
        private LocalsCollector(SourceRoutineSymbol routine)
            :base(routine.Syntax)
        {
            _routine = routine;

            this.VisitLocal += this.HandleLocal;
        }

        public static ImmutableArray<SourceLocalSymbol> GetLocals(SourceRoutineSymbol routine)
        {
            Contract.ThrowIfNull(routine);

            var visitor = new LocalsCollector(routine);
            visitor.VisitRoutine();
            return visitor._locals.ToImmutableArray();
        }

        void HandleLocal(object sender, VisitLocalArgs e)
        {
            Debug.Assert(sender == this);

            if (_visited.Add(e.Name))
            {
                _locals.Add(new SourceLocalSymbol(_routine, e.Name.Value, e.Kind));
            }
            else
            {
                // TODO: check kind matches with previous declaration
            }
        }
    }
}
