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
        private Block/*!*/_current;
        private Dictionary<string, ControlFlowGraph.LabelBlockState> _labels;
        private List<BreakTargetScope> _breakTargets;
        private Stack<TryCatchEdge> _tryTargets;

        public Block/*!*/Start { get; private set; }
        public Block/*!*/Exit { get; private set; }
        public Block Exception { get; private set; }

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
        public List<Block>/*!*/DeadBlocks { get { return _deadBlocks; } }
        private readonly List<Block>/*!*/_deadBlocks = new List<Block>();

        #region BreakTargetScope

        /// <summary>
        /// Represents break scope.
        /// </summary>
        private struct BreakTargetScope
        {
            public readonly Block/*!*/BreakTarget;
            public readonly Block/*!*/ContinueTarget;

            public BreakTargetScope(Block breakBlock, Block continueBlock)
            {
                BreakTarget = breakBlock;
                ContinueTarget = continueBlock;
            }
        }

        private BreakTargetScope GetBreakTarget(int level)
        {
            if (level < 1) level = 1;
            if (_breakTargets == null || _breakTargets.Count < level)
                return default(BreakTargetScope);

            return _breakTargets[_breakTargets.Count - level];
        }

        private void EnterBreakTarget(Block breakBlock, Block continueBlock)
        {
            if (_breakTargets == null) _breakTargets = new List<BreakTargetScope>(1);
            _breakTargets.Add(new BreakTargetScope(breakBlock, continueBlock));
        }

        private void ExitBreakTarget()
        {
            Debug.Assert(_breakTargets != null && _breakTargets.Count != 0);
            _breakTargets.RemoveAt(_breakTargets.Count - 1);
        }

        #endregion

        #region TryTargetScope

        private TryCatchEdge GetTryTarget()
        {
            if (_tryTargets == null || _tryTargets.Count == 0)
                return null;

            return _tryTargets.Peek();
        }

        private void EnterTryTarget(TryCatchEdge edge)
        {
            if (_tryTargets == null) _tryTargets = new Stack<TryCatchEdge>();
            _tryTargets.Push(edge);
        }

        private void ExitTryTarget()
        {
            Debug.Assert(_tryTargets != null && _tryTargets.Count != 0);
            _tryTargets.Pop();
        }

        #endregion

        #region Construction

        private BuilderVisitor(IList<Statement>/*!*/statements)
        {
            Contract.ThrowIfNull(statements);

            this.Start = new StartBlock();
            this.Exit = new ExitBlock();

            _current = this.Start;

            statements.ForEach(this.VisitElement);
            _current = Connect(_current, this.Exit);
        }

        public static BuilderVisitor/*!*/Build(IList<Statement>/*!*/statements)
        {
            return new BuilderVisitor(statements);
        }

        #endregion

        #region Helper Methods

        private Block/*!*/GetExceptionBlock()
        {
            if (this.Exception == null)
                this.Exception = new ExitBlock();
            return this.Exception;
        }

        private void Add(Statement stmt)
        {
            _current.AddStatement(SemanticsBinder.BindStatement(stmt));
        }

        private Block/*!*/NewBlock()
        {
            return new Block();
        }

        /// <summary>
        /// Creates block we know nothing is pointing to.
        /// Such block will be analysed later whether it is empty or whether it contains some statements (which will be reported as unreachable).
        /// </summary>
        private Block/*!*/NewDeadBlock()
        {
            var block = NewBlock();
            _deadBlocks.Add(block);
            return block;
        }

        private CatchBlock/*!*/NewBlock(CatchItem item)
        {
            return new CatchBlock(item);
        }

        private CaseBlock/*!*/NewBlock(SwitchItem item)
        {
            return new CaseBlock(item);
        }

        private Block/*!*/Connect(Block/*!*/source, Block/*!*/ifTarget, Block/*!*/elseTarget, Expression/*!*/condition)
        {
            new ConditionalEdge(source, ifTarget, elseTarget, SemanticsBinder.BindExpression(condition));
            return ifTarget;
        }

        private Block/*!*/Connect(Block/*!*/source, Block/*!*/target)
        {
            new SimpleEdge(source, target);
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
                    Block = NewBlock(),
                    GotoSpan = Span.Invalid,
                    LabelSpan = Span.Invalid,
                    Label = label,
                    Flags = ControlFlowGraph.LabelBlockFlags.None,
                };
            }

            return result;
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
            var enumereeEdge = new ForeachEnumereeEdge(_current, move, SemanticsBinder.BindExpression(x.Enumeree));

            // ContinueTarget:
            EnterBreakTarget(end, move);

            // ForeachMoveNextEdge : ConditionalEdge
            var moveEdge = new ForeachMoveNextEdge(move, body, end, enumereeEdge, x.KeyVariable, x.ValueVariable);
            // while (enumerator.MoveNext()) {
            //   var key = enumerator.Current.Key
            //   var value = enumerator.Current.Value

            // Block
            //   { x.Body }
            _current = body;
            VisitElement(x.Body);
            //   goto ContinueTarget;
            Connect(_current, move);

            // BreakTarget:
            ExitBreakTarget();

            //
            _current = end;
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
            EnterBreakTarget(end, action);

            // while (x.Codition) {
            _current = Connect(_current, cond);
            if (hasConditions)
            {
                if (condExpr.Count > 1)
                    condExpr.Take(condExpr.Count - 1).ForEach(expr => this.Add(new ExpressionStmt(expr.Span, expr)));
                _current = Connect(_current, body, end, condExpr.LastOrDefault());
            }
            else
            {
                _deadBlocks.Add(end);
            }

            //   { x.Body }
            VisitElement(bodyStmt);
            //   { x.Action }
            if (hasActions)
            {
                _current = Connect(_current, action);
                actionExpr.ForEach(expr => this.Add(new ExpressionStmt(expr.Span, expr)));
            }
            // }
            Connect(_current, cond);

            //
            ExitBreakTarget();

            //
            _current = end;
        }

        public override void VisitForStmt(ForStmt x)
        {
            BuildForLoop(x.InitExList, x.CondExList, x.ActionExList, x.Body);
        }

        public override void VisitGotoStmt(GotoStmt x)
        {
            Add(x);

            var/*!*/label = GetLabelBlock(x.LabelName.Value);
            label.Flags |= ControlFlowGraph.LabelBlockFlags.Used;   // label is used
            label.GotoSpan = x.Span;

            Connect(_current, label.Block);

            _current = NewDeadBlock();  // any statement inside this block would be unreachable unless it is LabelStmt
        }

        public override void VisitJumpStmt(JumpStmt x)
        {
            Add(x);

            if (x.Type == JumpStmt.Types.Return)
            {
                Connect(_current, this.Exit);
            }
            else if (x.Type == JumpStmt.Types.Break || x.Type == JumpStmt.Types.Continue)
            {
                int level = (x.Expression is IntLiteral)
                    ? ((IntLiteral)x.Expression).Value
                    : 1;

                var brk = GetBreakTarget(level);
                var target = (x.Type == JumpStmt.Types.Break) ? brk.BreakTarget : brk.ContinueTarget;
                if (target != null)
                {
                    Connect(_current, target);
                }
                else
                {
                    Connect(_current, this.GetExceptionBlock());    // unreachable  // fatal error in PHP
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
            Block elseBlock = null;
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
                    _current = Connect(_current, body);
                }

                VisitElement(cond.Statement);
                Connect(_current, end);
                _current = elseBlock;
            }

            Debug.Assert(_current == end);
        }

        public override void VisitLabelStmt(LabelStmt x)
        {
            var/*!*/label = GetLabelBlock(x.Name.Value);
            if ((label.Flags & ControlFlowGraph.LabelBlockFlags.Defined) != 0)
                label.Flags |= ControlFlowGraph.LabelBlockFlags.Redefined;  // label was defined already
            label.Flags |= ControlFlowGraph.LabelBlockFlags.Defined;        // label is defined
            label.LabelSpan = x.Span;

            _current = Connect(_current, label.Block);

            Add(x);
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
            var edge = new SwitchEdge(_current, SemanticsBinder.BindExpression(x.SwitchValue), cases.ToArray());
            _current = cases[0];

            EnterBreakTarget(end, end); // NOTE: inside switch, Continue ~ Break

            for (int i = 0; i < cases.Count; i++)
            {
                if (i < items.Length)
                    items[i].Statements.ForEach(VisitElement);  // any break will connect block to end

                _current = Connect(_current, (i == cases.Count - 1) ? end : cases[i + 1]);
            }

            ExitBreakTarget();

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
            Block finallyBlock = null;

            for (int i = 0; i < catchBlocks.Length; i++)
                catchBlocks[i] = NewBlock(x.Catches[i]);

            if (x.FinallyItem != null)
                finallyBlock = NewBlock();

            // TryCatchEdge // Connects _current to body, catch blocks and finally
            var edge = new TryCatchEdge(_current, body, catchBlocks, finallyBlock);

            // build try body
            EnterTryTarget(edge);
            _current = body;
            x.Statements.ForEach(VisitElement);
            ExitTryTarget();
            _current = Connect(_current, finallyBlock ?? end);

            // built catches
            for (int i = 0; i < catchBlocks.Length; i++)
            {
                _current = catchBlocks[i];
                x.Catches[i].Statements.ForEach(VisitElement);
                _current = Connect(_current, finallyBlock ?? end);
            }

            // build finally
            if (finallyBlock != null)
            {
                _current = finallyBlock;
                x.FinallyItem.Statements.ForEach(VisitElement);
                _current = Connect(_current, end);
            }

            // _current == end
        }

        public override void VisitWhileStmt(WhileStmt x)
        {
            if (x.LoopType == WhileStmt.Type.Do)
            {
                var end = NewBlock();
                var body = NewBlock();
                EnterBreakTarget(end, body);
                _current = Connect(_current, body);
                // do {
                VisitElement(x.Body);
                Connect(_current, body, end, x.CondExpr);
                // } while (x.CondExpr)
                ExitBreakTarget();
                _current = end;
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
