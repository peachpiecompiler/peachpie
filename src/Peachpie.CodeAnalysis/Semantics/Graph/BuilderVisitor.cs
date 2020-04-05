using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Text;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        /// <summary>
        /// Gets enumeration of unconditional declarations.
        /// </summary>
        public IEnumerable<BoundStatement> Declarations => _declarations ?? Enumerable.Empty<BoundStatement>();
        public List<BoundStatement> _declarations;

        public BoundBlock/*!*/Start { get; private set; }
        public BoundBlock/*!*/Exit { get; private set; }
        //public BoundBlock Exception { get; private set; }

        /// <summary>
        /// Gets labels defined within the routine.
        /// </summary>
        public ImmutableArray<ControlFlowGraph.LabelBlockState> Labels
        {
            get { return (_labels != null) ? _labels.Values.ToImmutableArray() : ImmutableArray<ControlFlowGraph.LabelBlockState>.Empty; }
        }

        /// <summary>
        /// Blocks we know nothing is pointing to (right after jump, throw, etc.).
        /// </summary>
        public ImmutableArray<BoundBlock>/*!*/DeadBlocks { get { return _deadBlocks.ToImmutableArray(); } }
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

        private void CloseTryScope()
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

            // setup the binder
            binder.SetupBuilder(this.NewBlock);

            _binder = binder;

            this.Start = WithNewOrdinal(new StartBlock());
            this.Exit = new ExitBlock();

            _current = WithOpenScope(this.Start);

            statements.ForEach(this.VisitElement);
            FinalizeRoutine();
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
            Add(_binder.BindWholeStatement(stmt));
        }

        private void Add(BoundItemsBag<BoundStatement> stmtBag)
        {
            ConnectBoundItemsBagBlocksToCurrentBlock(stmtBag);
            _current.Add(stmtBag.BoundElement);
        }

        private void ConnectBoundItemsBagBlocksToCurrentBlock<T>(BoundItemsBag<T> bag) where T : class, IPhpOperation
        {
            _current = ConnectBoundItemsBagBlocks(bag, _current);
        }

        private BoundBlock ConnectBoundItemsBagBlocks<T>(BoundItemsBag<T> bag, BoundBlock block) where T : class, IPhpOperation
        {
            if (bag.IsOnlyBoundElement) { return block; }

            Connect(block, bag.PreBoundBlockFirst);
            return bag.PreBoundBlockLast;
        }

        private void FinalizeRoutine()
        {
            if (!_deadBlocks.Contains(_current))
            {
                AddFinalReturn();
            }
        }

        private void AddFinalReturn()
        {
            BoundExpression expression;

            if (_binder.Routine.IsGlobalScope)
            {
                // global code returns 1 by default if no other value is specified
                expression = new BoundLiteral(1).WithAccess(BoundAccess.Read);
            }
            else
            {
                // function returns NULL by default (void?)
                expression = null; // void
            }

            // return <expression>;
            _current.Add(new BoundReturnStatement(expression));
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
            return WithNewOrdinal(new CatchBlock(_binder.BindTypeRef(item.TargetType), _binder.BindCatchVariable(item)) { PhpSyntax = item });
        }

        private CaseBlock/*!*/NewBlock(SwitchItem item)
        {
            BoundItemsBag<BoundExpression> caseValueBag = item is CaseItem caseItem
                ? _binder.BindWholeExpression(caseItem.CaseVal, BoundAccess.Read)
                : BoundItemsBag<BoundExpression>.Empty; // BoundItem -eq null => DefaultItem

            return WithNewOrdinal(new CaseBlock(caseValueBag) { PhpSyntax = item });
        }

        private BoundBlock/*!*/Connect(BoundBlock/*!*/source, BoundBlock/*!*/ifTarget, BoundBlock/*!*/elseTarget, Expression condition, bool isloop = false)
        {
            if (condition != null)
            {
                // bind condition expression & connect pre-condition blocks to source
                var boundConditionBag = _binder.BindWholeExpression(condition, BoundAccess.Read);
                source = ConnectBoundItemsBagBlocks(boundConditionBag, source);

                new ConditionalEdge(source, ifTarget, elseTarget, boundConditionBag.BoundElement)
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

        void AddUnconditionalDeclaration(BoundStatement decl)
        {
            if (_declarations == null)
            {
                _declarations = new List<BoundStatement>();
            }

            _declarations.Add(decl);
        }

        public override void VisitTypeDecl(TypeDecl x)
        {
            var bound = _binder.BindWholeStatement(x).SingleBoundElement();
            if (DeclareConditionally(x))
            {
                _current.Add(bound);
            }
            else
            {
                AddUnconditionalDeclaration(bound);
            }
        }

        bool DeclareConditionally(TypeDecl x)
        {
            if (x.IsConditional)
            {
                return true;
            }

            if (_current == Start && _current.Statements.Count == 0)
            {
                return false;
            }

            // Helper that lookups for the type if it is declared unconditionally.
            bool IsDeclared(QualifiedName qname)
            {
                if (_declarations != null &&
                    _declarations.OfType<TypeDecl>().FirstOrDefault(t => t.QualifiedName == qname) != default)
                {
                    return true;
                }

                if (_binder.Routine.DeclaringCompilation.GlobalSemantics.ResolveType(qname) is NamedTypeSymbol named &&
                    named.IsValidType() &&
                    !named.IsPhpUserType()) // user types are not declared in compile time // CONSIDER: more flow analysis
                {
                    return true;
                }

                //
                return false;
            }

            // the base class should be resolved first?
            if (x.BaseClass != null)
            {
                if (!IsDeclared(x.BaseClass.ClassName))
                    return true;
            }

            // the interface should be resolved first?
            if (x.ImplementsList.Length != 0)
            {
                if (x.ImplementsList.OfType<ClassTypeRef>().Any(t => !IsDeclared(t.ClassName)))
                    return true;
            }

            // the trait type should be resolved first?
            foreach (var t in x.Members.OfType<TraitsUse>())
            {
                if (t.TraitsList.OfType<ClassTypeRef>().Any(tu => !IsDeclared(tu.ClassName)))
                    return true;
            }

            // can be declared unconditionally
            return false;
        }

        public override void VisitFunctionDecl(FunctionDecl x)
        {
            var bound = _binder.BindWholeStatement(x).SingleBoundElement();
            if (x.IsConditional)
            {
                _current.Add(bound);
            }
            else
            {
                AddUnconditionalDeclaration(bound);
            }
        }

        public override void VisitMethodDecl(MethodDecl x)
        {
            // ignored
        }

        public override void VisitConstDeclList(ConstDeclList x)
        {
            // ignored
        }

        public override void VisitGlobalConstantDecl(GlobalConstantDecl x)
        {
            var bound = _binder.BindGlobalConstantDecl(x);
            _current.Add(bound);
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
            Add(_binder.BindEmptyStmt(new Span(x.Span.Start, 1))); // {

            base.VisitBlockStmt(x); // visit nested statements

            Add(_binder.BindEmptyStmt(new Span(x.Span.End - 1, 1))); // } // TODO: endif; etc.
        }

        public override void VisitDeclareStmt(DeclareStmt x)
        {
            Add(x);

            base.VisitDeclareStmt(x); // visit inner statement, if present
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
            // binds enumeree expression & connect pre-enumeree-expr blocks
            var boundEnumereeBag = _binder.BindWholeExpression(x.Enumeree, BoundAccess.Read);
            ConnectBoundItemsBagBlocksToCurrentBlock(boundEnumereeBag);

            var end = NewBlock();
            var move = NewBlock();
            var body = NewBlock();

            // _current -> move -> body -> move -> ...

            // ForeachEnumereeEdge : SimpleEdge
            // x.Enumeree.GetEnumerator();
            var enumereeEdge = new ForeachEnumereeEdge(_current, move, boundEnumereeBag.BoundElement, x.ValueVariable.Alias);

            // ContinueTarget:
            OpenBreakScope(end, move);

            // bind reference expression for foreach key variable
            var keyVar = (x.KeyVariable != null)
                ? (BoundReferenceExpression)_binder.BindWholeExpression(x.KeyVariable.Variable, BoundAccess.Write).SingleBoundElement()
                : null;

            // bind reference expression for foreach value variable 
            var valueVar = (BoundReferenceExpression)(_binder.BindWholeExpression(
                    (Expression)x.ValueVariable.Variable ?? x.ValueVariable.List,
                    x.ValueVariable.Alias ? BoundAccess.Write.WithWriteRef(FlowAnalysis.TypeRefMask.AnyType) : BoundAccess.Write)
                .SingleBoundElement());

            // ForeachMoveNextEdge : ConditionalEdge
            var moveEdge = new ForeachMoveNextEdge(
                move, body, end, enumereeEdge,
                keyVar, valueVar,
                x.GetMoveNextSpan());
            // while (enumerator.MoveNext()) {
            //   var value = enumerator.Current.Value
            //   var key = enumerator.Current.Key

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

        private void BuildForLoop(IList<Expression> initExpr, IList<Expression> condExpr, IList<Expression> actionExpr, Statement/*!*/bodyStmt)
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

        private void BuildDoLoop(Expression condExpr, Statement/*!*/bodyStmt)
        {
            var end = NewBlock();

            var body = NewBlock();
            var cond = NewBlock();

            OpenBreakScope(end, cond);

            // do { ...
            _current = WithNewOrdinal(Connect(_current, body));

            // x.Body
            OpenScope(_current);
            VisitElement(bodyStmt);
            CloseScope();

            // } while ( COND )
            _current = WithNewOrdinal(Connect(_current, cond));
            _current = WithNewOrdinal(Connect(_current, body, end, condExpr, true));

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
            var/*!*/label = GetLabelBlock(x.LabelName.Name.Value);
            label.Flags |= ControlFlowGraph.LabelBlockFlags.Used;   // label is used

            if (!label.LabelSpan.IsValid)
            {
                // remember label span if not declared
                label.LabelSpan = x.LabelName.Span;
            }

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
                int level = (x.Expression is LongIntLiteral)
                    ? (int)((LongIntLiteral)x.Expression).Value
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
                    // fatal error in PHP:
                    _binder.Diagnostics.Add(_binder.Routine, x, Errors.ErrorCode.ERR_NeedsLoopOrSwitch, x.Type.ToString().ToLowerInvariant());
                    Connect(_current, this.GetExceptionBlock());    // unreachable, wouldn't compile
                }
            }
            else
            {
                throw Peachpie.CodeAnalysis.Utilities.ExceptionUtilities.UnexpectedValue(x.Type);
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
            var/*!*/label = GetLabelBlock(x.Name.Name.Value);
            if ((label.Flags & ControlFlowGraph.LabelBlockFlags.Defined) != 0)
            {
                label.Flags |= ControlFlowGraph.LabelBlockFlags.Redefined;  // label was defined already
                return; // ignore label redefinition                
            }

            label.Flags |= ControlFlowGraph.LabelBlockFlags.Defined;        // label is defined
            label.LabelSpan = x.Name.Span;

            _current = WithNewOrdinal(Connect(_current, label.TargetBlock));
        }

        public override void VisitSwitchStmt(SwitchStmt x)
        {
            var items = x.SwitchItems;
            if (items == null || items.Length == 0)
                return;

            // get bound item for switch value & connect potential pre-switch-value blocks
            var boundBagForSwitchValue = _binder.BindWholeExpression(x.SwitchValue, BoundAccess.Read);
            ConnectBoundItemsBagBlocksToCurrentBlock(boundBagForSwitchValue);
            var switchValue = boundBagForSwitchValue.BoundElement;

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

            // if switch value isn't a constant & there're case values with preBoundStatements 
            // -> the switch value might get evaluated multiple times (see SwitchEdge.Generate) -> preemptively evaluate and cache it
            if (!switchValue.IsConstant() && !cases.All(c => c.CaseValue.IsOnlyBoundElement))
            {
                var result = GeneratorSemanticsBinder.CreateAndAssignSynthesizedVariable(switchValue, BoundAccess.Read, $"<switchValueCacher>{x.Span}");
                switchValue = result.BoundExpr;
                _current.Add(new BoundExpressionStatement(result.Assignment));
            }

            // SwitchEdge // Connects _current to cases
            var edge = new SwitchEdge(_current, switchValue, cases.ToImmutableArray(), end);
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
            var catchBlocks = ImmutableArray<CatchBlock>.Empty;
            if (x.Catches != null)
            {
                var catchBuilder = ImmutableArray.CreateBuilder<CatchBlock>(x.Catches.Length);
                for (int i = 0; i < x.Catches.Length; i++)
                {
                    catchBuilder.Add(NewBlock(x.Catches[i]));
                }

                catchBlocks = catchBuilder.MoveToImmutable();
            }

            BoundBlock finallyBlock = null;
            if (x.FinallyItem != null)
                finallyBlock = NewBlock();

            // TryCatchEdge // Connects _current to body, catch blocks and finally
            var edge = new TryCatchEdge(_current, body, catchBlocks, finallyBlock, end);

            // build try body
            OpenTryScope(edge);
            OpenScope(body);
            _current = WithNewOrdinal(body);
            VisitElement(x.Body);
            CloseScope();
            CloseTryScope();
            _current = Leave(_current, finallyBlock ?? end);

            // built catches
            for (int i = 0; i < catchBlocks.Length; i++)
            {
                _current = WithOpenScope(WithNewOrdinal(catchBlocks[i]));
                VisitElement(x.Catches[i].Body);
                CloseScope();
                _current = Leave(_current, finallyBlock ?? end);
            }

            // build finally
            if (finallyBlock != null)
            {
                _current = WithOpenScope(WithNewOrdinal(finallyBlock));
                VisitElement(x.FinallyItem.Body);
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
                Debug.Assert(x.CondExpr != null);

                // do { BODY } while (COND)
                BuildDoLoop(x.CondExpr, x.Body);
            }
            else if (x.LoopType == WhileStmt.Type.While)
            {
                Debug.Assert(x.CondExpr != null);

                // while (COND) { BODY }
                BuildForLoop(null, new List<Expression>(1) { x.CondExpr }, null, x.Body);
            }
        }

        #endregion
    }
}
