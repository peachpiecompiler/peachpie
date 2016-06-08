using Pchp.Syntax;
using Pchp.Syntax.AST;
using Pchp.Syntax.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Visitor implementation that constructs the graph.
    /// </summary>
    internal sealed class BuilderVisitor : TreeVisitor
    {
        readonly SemanticsBinder/*!*/_binder;
        private BoundBlock/*!*/_current;
        private Dictionary<string, ControlFlowGraph.LabelBlockState> _labels;
        private List<BreakTargetScope> _breakTargets;
        private Stack<TryCatchEdge> _tryTargets;
        private Stack<LocalScopeInfo> _scopes = new Stack<LocalScopeInfo>(1);
        private int _index = 0;

        public BoundBlock/*!*/Start { get; private set; }
        public BoundBlock/*!*/Exit { get; private set; }
        //public BoundBlock Exception { get; private set; }

        /// <summary>
        /// Gets labels defined within the routine.
        /// </summary>
        public ControlFlowGraph.LabelBlockState[] Labels
        {
            get { return (_labels != null) ? _labels.Values.ToArray() : EmptyArray<ControlFlowGraph.LabelBlockState>.Instance; }
        }

        /// <summary>
        /// Blocks we know nothing is pointing to (right after jump, throw, etc.).
        /// </summary>
        public List<BoundBlock>/*!*/DeadBlocks { get { return _deadBlocks; } }
        private readonly List<BoundBlock>/*!*/_deadBlocks = new List<BoundBlock>();

        #region LocalScope

        private class LocalScopeInfo
        {
            public BoundBlock FirstBlock => _firstblock;
            private BoundBlock _firstblock;

            public LocalScopeInfo(BoundBlock firstBlock)
            {
                _firstblock = firstBlock;
            }
        }

        private void OpenScope(BoundBlock block)
        {
            _scopes.Push(new LocalScopeInfo(block));
        }

        private void CloseScope()
        {
            if (_scopes.Count == 0)
                throw new InvalidOperationException();

            _scopes.Pop(); // .FirstBlock.ScopeTo = _index;
        }

        #endregion

        #region BreakTargetScope

        /// <summary>
        /// Represents break scope.
        /// </summary>
        private struct BreakTargetScope
        {
            public readonly BoundBlock/*!*/BreakTarget;
            public readonly BoundBlock/*!*/ContinueTarget;

            public BreakTargetScope(BoundBlock breakBlock, BoundBlock continueBlock)
            {
                BreakTarget = breakBlock;
                ContinueTarget = continueBlock;
            }
        }

        private BreakTargetScope GetBreakScope(int level)
        {
            if (level < 1) level = 1;   // PHP behavior
            if (_breakTargets == null || _breakTargets.Count < level)
                return default(BreakTargetScope);

            return _breakTargets[_breakTargets.Count - level];
        }

        private void OpenBreakScope(BoundBlock breakBlock, BoundBlock continueBlock)
        {
            if (_breakTargets == null) _breakTargets = new List<BreakTargetScope>(1);
            _breakTargets.Add(new BreakTargetScope(breakBlock, continueBlock));
        }

        private void CloseBreakScope()
        {
            Debug.Assert(_breakTargets != null && _breakTargets.Count != 0);
            _breakTargets.RemoveAt(_breakTargets.Count - 1);
        }

        #endregion

        #region TryTargetScope

        private TryCatchEdge CurrentTryScope
        {
            get
            {
                return (_tryTargets != null && _tryTargets.Count != 0)
                    ? _tryTargets.Peek()
                    : null;
            }
        }

        private void OpenTryScope(TryCatchEdge edge)
        {
            if (_tryTargets == null) _tryTargets = new Stack<TryCatchEdge>();
            _tryTargets.Push(edge);
        }

        private void CloeTryScope()
        {
            Debug.Assert(_tryTargets != null && _tryTargets.Count != 0);
            _tryTargets.Pop();
        }

        #endregion

        #region Construction

        private BuilderVisitor(IList<Statement>/*!*/statements, SemanticsBinder/*!*/binder)
        {
            Contract.ThrowIfNull(statements);
            Contract.ThrowIfNull(binder);

            _binder = binder;

            this.Start = new StartBlock();
            this.Exit = new ExitBlock();

            _current = WithOpenScope(this.Start);

            statements.ForEach(this.VisitElement);
            _current = Connect(_current, this.Exit);

            //
            WithNewOrdinal(this.Exit);
            CloseScope();

            //
            Debug.Assert(_scopes.Count == 0);
            Debug.Assert(_tryTargets == null || _tryTargets.Count == 0);
            Debug.Assert(_breakTargets == null || _breakTargets.Count == 0);
        }

        public static BuilderVisitor/*!*/Build(IList<Statement>/*!*/statements, SemanticsBinder/*!*/binder)
        {
            return new BuilderVisitor(statements, binder);
        }

        #endregion

        #region Helper Methods

        private BoundBlock/*!*/GetExceptionBlock()
        {
            //if (this.Exception == null)
            //    this.Exception = new ExitBlock();
            //return this.Exception;
            return this.Exit;
        }

        private void Add(Statement stmt)
        {
            _current.AddStatement(_binder.BindStatement(stmt));
        }

        private BoundBlock/*!*/NewBlock()
        {
            return WithNewOrdinal(new BoundBlock());
        }

        /// <summary>
        /// Creates block we know nothing is pointing to.
        /// Such block will be analysed later whether it is empty or whether it contains some statements (which will be reported as unreachable).
        /// </summary>
        private BoundBlock/*!*/NewDeadBlock()
        {
            var block = new BoundBlock();
            block.Ordinal = -1; // unreachable
            _deadBlocks.Add(block);
            return block;
        }

        private CatchBlock/*!*/NewBlock(CatchItem item)
        {
            return WithNewOrdinal(new CatchBlock(item.TypeRef, _binder.BindCatchVariable(item)) { PhpSyntax = item });
        }

        private CaseBlock/*!*/NewBlock(SwitchItem item)
        {
            var caseitem = item as CaseItem;
            BoundExpression caseValue =  // null => DefaultItem
                (caseitem != null) ? _binder.BindExpression(caseitem.CaseVal, BoundAccess.Read) : null;
            return WithNewOrdinal(new CaseBlock(caseValue) { PhpSyntax = item });
        }

        private BoundBlock/*!*/Connect(BoundBlock/*!*/source, BoundBlock/*!*/ifTarget, BoundBlock/*!*/elseTarget, Expression condition, bool isloop = false)
        {
            if (condition != null)
            {
                new ConditionalEdge(source, ifTarget, elseTarget, _binder.BindExpression(condition, BoundAccess.Read))
                {
                    IsLoop = isloop,
                };

                return ifTarget;
            }
            else
            {
                // jump to ifTarget if there is no condition (always true) // e.g. for (;;) { [ifTarget] }
                return Connect(source, ifTarget);
            }
        }

        private BoundBlock/*!*/Connect(BoundBlock/*!*/source, BoundBlock/*!*/target)
        {
            new SimpleEdge(source, target);
            return target;
        }

        private BoundBlock/*!*/Leave(BoundBlock/*!*/source, BoundBlock/*!*/target)
        {
            new LeaveEdge(source, target);
            return target;
        }

        private ControlFlowGraph.LabelBlockState/*!*/GetLabelBlock(string label)
        {
            if (_labels == null)
                _labels = new Dictionary<string, ControlFlowGraph.LabelBlockState>(StringComparer.Ordinal);    // goto is case sensitive

            ControlFlowGraph.LabelBlockState result;
            if (!_labels.TryGetValue(label, out result))
            {
                _labels[label] = result = new ControlFlowGraph.LabelBlockState()
                {
                    TargetBlock = NewBlock(),
                    LabelSpan = Span.Invalid,
                    Label = label,
                    Flags = ControlFlowGraph.LabelBlockFlags.None,
                };
            }

            return result;
        }

        /// <summary>
        /// Gets new block index.
        /// </summary>
        private int NewOrdinal() => _index++;

        private T WithNewOrdinal<T>(T block) where T : BoundBlock
        {
            block.Ordinal = NewOrdinal();
            return block;
        }

        private T WithOpenScope<T>(T block) where T : BoundBlock
        {
            OpenScope(block);
            return block;
        }

        #endregion

        #region Declaration Statements

        public override void VisitTypeDecl(TypeDecl x)
        {
            if (x.IsConditional)
            {
                Add(x);
            }
            // ignored
        }

        public override void VisitMethodDecl(MethodDecl x)
        {
            // ignored
        }

        public override void VisitConstDeclList(ConstDeclList x)
        {
            // ignored
        }

        public override void VisitFunctionDecl(FunctionDecl x)
        {
            if (x.IsConditional)
            {
                Add(x);
            }
            // ignored
        }

        #endregion

        #region Flow-Thru Statements

        public override void VisitEmptyStmt(EmptyStmt x)
        {
            // ignored
        }

        public override void VisitEchoStmt(EchoStmt x)
        {
            Add(x);
        }

        public override void VisitBlockStmt(BlockStmt x)
        {
            base.VisitBlockStmt(x); // visit nested statements
        }

        public override void VisitDeclareStmt(DeclareStmt x)
        {
            Add(x);
        }

        public override void VisitGlobalCode(GlobalCode x)
        {
            throw new InvalidOperationException();
        }

        public override void VisitGlobalStmt(GlobalStmt x)
        {
            Add(x);
        }

        public override void VisitStaticStmt(StaticStmt x)
        {
            Add(x);
        }

        public override void VisitExpressionStmt(ExpressionStmt x)
        {
            Add(x);

            VisitElement(x.Expression as ExitEx);
        }

        public override void VisitUnsetStmt(UnsetStmt x)
        {
            Add(x);
        }

        public override void VisitPHPDocStmt(PHPDocStmt x)
        {
            if (x.GetProperty<LangElement>() == null)
            {
                // if PHPDoc is not associated with any declaration yet
                Add(x);
            }
        }

        #endregion

        #region Conditional Statements

        public override void VisitConditionalStmt(ConditionalStmt x)
        {
            throw new InvalidOperationException();  // should be handled by IfStmt
        }

        public override void VisitExitEx(ExitEx x)
        {
            // NOTE: Added by VisitExpressionStmt already
            // NOTE: similar to ThrowEx but unhandleable

            // connect to Exception block
            Connect(_current, this.GetExceptionBlock());    // unreachable
            _current = NewDeadBlock();
        }

        public override void VisitForeachStmt(ForeachStmt x)
        {
            var end = NewBlock();
            var move = NewBlock();
            var body = NewBlock();

            // _current -> move -> body -> move -> ...

            // ForeachEnumereeEdge : SimpleEdge
            // x.Enumeree.GetEnumerator();
            var enumereeEdge = new ForeachEnumereeEdge(_current, move, _binder.BindExpression(x.Enumeree, BoundAccess.Read), x.ValueVariable.Alias);

            // ContinueTarget:
            OpenBreakScope(end, move);

            // ForeachMoveNextEdge : ConditionalEdge
            var moveEdge = new ForeachMoveNextEdge(move, body, end, enumereeEdge,
                (x.KeyVariable != null) ? (BoundReferenceExpression)_binder.BindExpression(x.KeyVariable.Variable, BoundAccess.Write) : null,
                (BoundReferenceExpression)_binder.BindExpression(
                    (Expression)x.ValueVariable.Variable ?? x.ValueVariable.List,
                    x.ValueVariable.Alias ? BoundAccess.Write.WithWriteRef(FlowAnalysis.TypeRefMask.AnyType) : BoundAccess.Write));
            // while (enumerator.MoveNext()) {
            //   var key = enumerator.Current.Key
            //   var value = enumerator.Current.Value

            // Block
            //   { x.Body }
            _current = WithOpenScope(WithNewOrdinal(body));
            VisitElement(x.Body);
            CloseScope();
            //   goto ContinueTarget;
            Connect(_current, move);

            // BreakTarget:
            CloseBreakScope();

            //
            _current = WithNewOrdinal(end);
        }

        private void BuildForLoop(List<Expression> initExpr, List<Expression> condExpr, List<Expression> actionExpr, Statement/*!*/bodyStmt)
        {
            var end = NewBlock();

            bool hasActions = actionExpr != null && actionExpr.Count != 0;
            bool hasConditions = condExpr != null && condExpr.Count != 0;

            // { initializer }
            if (initExpr != null && initExpr.Count != 0)
                initExpr.ForEach(expr => this.Add(new ExpressionStmt(expr.Span, expr)));

            var body = NewBlock();
            var cond = hasConditions ? NewBlock() : body;
            var action = hasActions ? NewBlock() : cond;
            OpenBreakScope(end, action);

            // while (x.Codition) {
            _current = WithNewOrdinal(Connect(_current, cond));
            if (hasConditions)
            {
                if (condExpr.Count > 1)
                    condExpr.Take(condExpr.Count - 1).ForEach(expr => this.Add(new ExpressionStmt(expr.Span, expr)));
                _current = WithNewOrdinal(Connect(_current, body, end, condExpr.LastOrDefault(), true));
            }
            else
            {
                _deadBlocks.Add(end);
            }

            //   { x.Body }
            OpenScope(_current);
            VisitElement(bodyStmt);
            //   { x.Action }
            if (hasActions)
            {
                _current = WithNewOrdinal(Connect(_current, action));
                actionExpr.ForEach(expr => this.Add(new ExpressionStmt(expr.Span, expr)));
            }

            CloseScope();

            // }
            Connect(_current, cond);

            //
            CloseBreakScope();

            //
            _current = WithNewOrdinal(end);
        }

        public override void VisitForStmt(ForStmt x)
        {
            BuildForLoop(x.InitExList, x.CondExList, x.ActionExList, x.Body);
        }

        public override void VisitGotoStmt(GotoStmt x)
        {
            var/*!*/label = GetLabelBlock(x.LabelName.Value);
            label.Flags |= ControlFlowGraph.LabelBlockFlags.Used;   // label is used
            
            Connect(_current, label.TargetBlock);

            _current.NextEdge.PhpSyntax = x;

            _current = NewDeadBlock();  // any statement inside this block would be unreachable unless it is LabelStmt
        }

        public override void VisitJumpStmt(JumpStmt x)
        {

            if (x.Type == JumpStmt.Types.Return)
            {
                Add(x);
                Connect(_current, this.Exit);
            }
            else if (x.Type == JumpStmt.Types.Break || x.Type == JumpStmt.Types.Continue)
            {
                int level = (x.Expression is IntLiteral)
                    ? ((IntLiteral)x.Expression).Value
                    : 1;

                var brk = GetBreakScope(level);
                var target = (x.Type == JumpStmt.Types.Break) ? brk.BreakTarget : brk.ContinueTarget;
                if (target != null)
                {
                    Connect(_current, target);

                    _current.NextEdge.PhpSyntax = x;
                }
                else
                {
                    throw new InvalidOperationException();   // TODO: ErrCode
                    //Connect(_current, this.GetExceptionBlock());    // unreachable  // fatal error in PHP
                }
            }
            else
            {
                throw new InvalidOperationException();
            }

            _current = NewDeadBlock();  // anything after these statements is unreachable
        }

        public override void VisitIfStmt(IfStmt x)
        {
            var end = NewBlock();

            var conditions = x.Conditions;
            Debug.Assert(conditions.Count != 0);
            BoundBlock elseBlock = null;
            for (int i = 0; i < conditions.Count; i++)
            {
                var cond = conditions[i];
                if (cond.Condition != null)  // if (Condition) ...
                {
                    elseBlock = (i == conditions.Count - 1) ? end : NewBlock();
                    _current = Connect(_current, NewBlock(), elseBlock, cond.Condition);
                }
                else  // else ...
                {
                    Debug.Assert(i != 0 && elseBlock != null);
                    var body = elseBlock;
                    elseBlock = end;    // last ConditionalStmt
                    _current = WithNewOrdinal(body);
                }

                OpenScope(_current);
                VisitElement(cond.Statement);
                CloseScope();

                Connect(_current, end);
                _current = WithNewOrdinal(elseBlock);
            }

            Debug.Assert(_current == end);
            WithNewOrdinal(_current);
        }

        public override void VisitLabelStmt(LabelStmt x)
        {
            var/*!*/label = GetLabelBlock(x.Name.Value);
            if ((label.Flags & ControlFlowGraph.LabelBlockFlags.Defined) != 0)
            {
                label.Flags |= ControlFlowGraph.LabelBlockFlags.Redefined;  // label was defined already
                return; // ignore label redefinition                
            }

            label.Flags |= ControlFlowGraph.LabelBlockFlags.Defined;        // label is defined
            label.LabelSpan = x.Span;

            _current = WithNewOrdinal(Connect(_current, label.TargetBlock));
        }

        public override void VisitSwitchStmt(SwitchStmt x)
        {
            var items = x.SwitchItems;
            if (items == null || items.Length == 0)
                return;

            var end = NewBlock();

            bool hasDefault = false;
            var cases = new List<CaseBlock>(items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                cases.Add(NewBlock(items[i]));
                hasDefault |= (items[i] is DefaultItem);
            }
            if (!hasDefault)
            {
                // create implicit default:
                cases.Add(NewBlock(new DefaultItem(x.Span, EmptyArray<Statement>.Instance)));
            }

            // SwitchEdge // Connects _current to cases
            var edge = new SwitchEdge(_current, _binder.BindExpression(x.SwitchValue, BoundAccess.Read), cases.ToArray(), end);
            _current = WithNewOrdinal(cases[0]);

            OpenBreakScope(end, end); // NOTE: inside switch, Continue ~ Break

            for (int i = 0; i < cases.Count; i++)
            {
                OpenScope(_current);

                if (i < items.Length)
                    items[i].Statements.ForEach(VisitElement);  // any break will connect block to end

                CloseScope();

                _current = WithNewOrdinal(Connect(_current, (i == cases.Count - 1) ? end : cases[i + 1]));
            }

            CloseBreakScope();

            Debug.Assert(_current == end);
        }

        public override void VisitThrowStmt(ThrowStmt x)
        {
            Add(x);

            //var tryedge = GetTryTarget();
            //if (tryedge != null)
            //{
            //    // find handling catch block
            //    QualifiedName qname;
            //    var newex = x.Expression as NewEx;
            //    if (newex != null && newex.ClassNameRef is DirectTypeRef)
            //    {
            //        qname = ((DirectTypeRef)newex.ClassNameRef).ClassName;
            //    }
            //    else
            //    {
            //        qname = new QualifiedName(Name.EmptyBaseName);
            //    }

            //    CatchBlock handlingCatch = tryedge.HandlingCatch(qname);
            //    if (handlingCatch != null)
            //    {
            //        // throw jumps to a catch item in runtime
            //    }
            //}

            // connect to Exception block
            Connect(_current, this.GetExceptionBlock());
            _current = NewDeadBlock();  // unreachable
        }

        public override void VisitTryStmt(TryStmt x)
        {
            // try {
            //   x.Body
            // }
            // catch (E1) { body }
            // catch (E2) { body }
            // finally { body }
            // end

            var end = NewBlock();
            var body = NewBlock();

            // init catch blocks and finally block
            var catchBlocks = new CatchBlock[(x.Catches == null) ? 0 : x.Catches.Length];
            BoundBlock finallyBlock = null;

            for (int i = 0; i < catchBlocks.Length; i++)
                catchBlocks[i] = NewBlock(x.Catches[i]);

            if (x.FinallyItem != null)
                finallyBlock = NewBlock();

            // TryCatchEdge // Connects _current to body, catch blocks and finally
            var edge = new TryCatchEdge(_current, body, catchBlocks, finallyBlock, end);

            // build try body
            OpenTryScope(edge);
            OpenScope(body);
            _current = WithNewOrdinal(body);
            x.Statements.ForEach(VisitElement);
            CloseScope();
            CloeTryScope();
            _current = Leave(_current, finallyBlock ?? end);

            // built catches
            for (int i = 0; i < catchBlocks.Length; i++)
            {
                _current = WithOpenScope(WithNewOrdinal(catchBlocks[i]));
                x.Catches[i].Statements.ForEach(VisitElement);
                CloseScope();
                _current = Leave(_current, finallyBlock ?? end);
            }

            // build finally
            if (finallyBlock != null)
            {
                _current = WithOpenScope(WithNewOrdinal(finallyBlock));
                x.FinallyItem.Statements.ForEach(VisitElement);
                CloseScope();
                _current = Leave(_current, end);
            }

            // _current == end
            _current.Ordinal = NewOrdinal();
        }

        public override void VisitWhileStmt(WhileStmt x)
        {
            if (x.LoopType == WhileStmt.Type.Do)
            {
                var end = NewBlock();
                var body = NewBlock();
                OpenBreakScope(end, body);
                _current = WithOpenScope(Connect(_current, body));
                // do {
                VisitElement(x.Body);
                Connect(_current, body, end, x.CondExpr);
                // } while (x.CondExpr)
                CloseScope();
                CloseBreakScope();

                _current = WithNewOrdinal(end);
            }
            else if (x.LoopType == WhileStmt.Type.While)
            {
                Debug.Assert(x.CondExpr != null);
                BuildForLoop(null, new List<Expression>(1) { x.CondExpr }, null, x.Body);
            }
        }

        #endregion
    }
}
