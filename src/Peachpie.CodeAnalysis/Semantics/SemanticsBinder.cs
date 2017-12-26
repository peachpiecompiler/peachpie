using Devsense.PHP.Syntax;
using Devsense.PHP.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AST = Devsense.PHP.Syntax.Ast;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Holds currently bound item and optionally the first and the last BoundBlock containing all the statements that are supposed to go before the BoundElement. 
    /// </summary>
    /// <typeparam name="T">Either <c>BoundExpression</c> or <c>BoundStatement</c>.</typeparam>
    public struct BoundItemsBag<T> where T : class, IPhpOperation
    {
        public BoundBlock PreBoundBlockFirst { get; private set; }
        public BoundBlock PreBoundBlockLast { get; private set; }

        public T BoundElement { get; private set; }

        public BoundItemsBag(T bound, BoundBlock preBoundFirst = null, BoundBlock preBoundLast = null)
        {
            Debug.Assert(bound != null || (preBoundFirst == null && preBoundLast == null));
            Debug.Assert(preBoundFirst != null || preBoundLast == null);

            PreBoundBlockFirst = preBoundFirst;
            PreBoundBlockLast = preBoundLast ?? preBoundFirst;
            BoundElement = bound;
        }

        /// <summary>
        /// An empty bag with no item and no pre-bound blocks.
        /// </summary>
        public static BoundItemsBag<T> Empty => new BoundItemsBag<T>(null);

        /// <summary>
        /// Returns bound element and asserts that there are no <c>PreBoundStatements</c>.
        /// </summary>
        public T SingleBoundElement()
        {
            if (!IsOnlyBoundElement)
            {
                throw new InvalidOperationException();
            }

            return BoundElement;
        }

        public static implicit operator BoundItemsBag<T>(T item) => new BoundItemsBag<T>(item);

        public bool IsEmpty => IsOnlyBoundElement && BoundElement == null;
        public bool IsOnlyBoundElement => PreBoundBlockFirst == null;
    }

    /// <summary>
    /// Binds syntax nodes (<see cref="AST.LangElement"/>) to semantic nodes (<see cref="IOperation"/>).
    /// Creates unbound nodes.
    /// </summary>
    internal class SemanticsBinder
    {
        /// <summary>
        /// Optional. Local variables table.
        /// Can be <c>null</c> for expressions without variable access (field initializers and parameters initializers).
        /// </summary>
        protected readonly LocalsTable _locals;

        /// <summary>
        /// Gets corresponding routine.
        /// Can be <c>null</c>.
        /// </summary>
        public SourceRoutineSymbol Routine => _locals?.Routine;

        /// <summary>
        /// Found yield statements (needed for ControlFlowGraph)
        /// </summary>
        public virtual BoundYieldStatement[] Yields { get => EmptyArray<BoundYieldStatement>.Instance; }
        protected readonly DiagnosticBag _diagnostics;

        /// <summary>
        /// Gets value determining whether to compile <c>assert</c>. otherwise the expression is ignored.
        /// </summary>
        bool EnableAssertExpression => Routine != null && Routine.DeclaringCompilation.Options.DebugPlusMode;

        #region Construction

        /// <summary>
        /// Creates <see cref="SemanticsBinder"/> for given routine (passed with <paramref name="locals"/>).
        /// </summary>
        /// <param name="locals">Table of local variables within routine.</param>
        /// <param name="diagnostics">Optional. Diagnostics.</param>
        public static SemanticsBinder Create(LocalsTable locals, DiagnosticBag diagnostics = null)
        {
            Debug.Assert(locals != null);

            var routine = locals.Routine;
            Debug.Assert(routine != null);

            // try to get yields from current routine
            routine.Syntax.Properties.TryGetProperty(out ImmutableArray<AST.IYieldLikeEx> yields);  // routine binder sets this property

            var isGeneratorMethod = !yields.IsDefaultOrEmpty;

            //
            return (isGeneratorMethod)
                ? new GeneratorSemanticsBinder(yields, locals, diagnostics)
                : new SemanticsBinder(locals, diagnostics);
        }

        public SemanticsBinder(LocalsTable locals = null, DiagnosticBag diagnostics = null)
        {
            _locals = locals;
            _diagnostics = diagnostics ?? DiagnosticBag.GetInstance();
        }

        /// <summary>
        /// Provides binder with CFG builder.
        /// </summary>
        public virtual void SetupBuilder(Func<BoundBlock> newBlockFunc)
        { }

        #endregion

        #region Helpers

        protected ImmutableArray<BoundStatement> BindStatements(IEnumerable<AST.Statement> statements)
        {
            return statements.Select(BindStatement).ToImmutableArray();
        }

        protected ImmutableArray<BoundExpression> BindExpressions(IEnumerable<AST.Expression> expressions)
        {
            return expressions.Select(BindExpression).ToImmutableArray();
        }

        protected BoundExpression BindExpression(AST.Expression expr) => BindExpression(expr, BoundAccess.Read);

        protected BoundArgument BindArgument(AST.Expression expr, bool isByRef = false, bool isUnpack = false)
        {
            //if (isUnpack)
            //{
            //    // remove:
            //    _diagnostics.Add(_locals.Routine, expr, Errors.ErrorCode.ERR_NotYetImplemented, "Parameter unpacking");
            //}

            var bound = BindExpression(expr, isByRef ? BoundAccess.ReadRef : BoundAccess.Read);
            Debug.Assert(!isUnpack || !isByRef);

            return isUnpack
                ? BoundArgument.CreateUnpacking(bound)
                : BoundArgument.Create(bound);
        }

        protected ImmutableArray<BoundArgument> BindArguments(params AST.Expression[] expressions)
        {
            Debug.Assert(expressions != null);

            if (expressions.Length == 0)
            {
                return ImmutableArray<BoundArgument>.Empty;
            }

            //
            var arguments = new BoundArgument[expressions.Length];
            for (int i = 0; i < expressions.Length; i++)
            {
                var expr = expressions[i];
                arguments[i] = BindArgument(expr);
            }

            //
            return ImmutableArray.Create(arguments);
        }

        protected ImmutableArray<BoundArgument> BindArguments(params AST.ActualParam[] parameters)
        {
            Debug.Assert(parameters != null);

            if (parameters.Length == 0)
            {
                return ImmutableArray<BoundArgument>.Empty;
            }

            //
            var unpacking = false;
            var arguments = new BoundArgument[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                var arg = BindArgument(p.Expression, p.Ampersand, p.IsUnpack);

                //
                arguments[i] = arg;

                // check the unpacking is not used before normal arguments:
                if (arg.IsUnpacking)
                {
                    unpacking = true;
                }
                else
                {
                    if (unpacking)
                    {
                        // https://wiki.php.net/rfc/argument_unpacking
                        _diagnostics.Add(this.Routine, p, Errors.ErrorCode.ERR_PositionalArgAfterUnpacking);
                    }
                }
            }

            //
            return ImmutableArray.Create(arguments);
        }

        protected ImmutableArray<BoundArgument> BindLambdaUseArguments(IEnumerable<AST.FormalParam> usevars)
        {
            return usevars.Select(v =>
            {
                var varuse = new AST.DirectVarUse(v.Name.Span, v.Name.Name);
                return BindExpression(varuse, v.PassedByRef ? BoundAccess.ReadRef : BoundAccess.Read);
            })
            .Select(BoundArgument.Create)
            .ToImmutableArray();
        }

        #endregion

        public virtual BoundItemsBag<BoundStatement> BindWholeStatement(AST.Statement stmt) => BindStatement(stmt);

        protected virtual BoundStatement BindStatement(AST.Statement stmt) => BindStatementCore(stmt).WithSyntax(stmt);

        BoundStatement BindStatementCore(AST.Statement stmt)
        {
            Debug.Assert(stmt != null);

            if (stmt is AST.EchoStmt echoStm) return BindEcho(echoStm, BindArguments(echoStm.Parameters));
            if (stmt is AST.ExpressionStmt exprStm) return new BoundExpressionStatement(BindExpression(exprStm.Expression, BoundAccess.None));
            if (stmt is AST.JumpStmt jmpStm) return BindJumpStmt(jmpStm);
            if (stmt is AST.FunctionDecl) return new BoundFunctionDeclStatement(stmt.GetProperty<SourceFunctionSymbol>());
            if (stmt is AST.TypeDecl) return new BoundTypeDeclStatement(stmt.GetProperty<SourceTypeSymbol>());
            if (stmt is AST.GlobalStmt glStmt) return BindGlobalStmt(glStmt);
            if (stmt is AST.StaticStmt staticStm) return BindStaticStmt(staticStm);
            if (stmt is AST.UnsetStmt unsetStm) return BindUnsetStmt(unsetStm);
            if (stmt is AST.ThrowStmt throwStm) return new BoundThrowStatement(BindExpression(throwStm.Expression, BoundAccess.Read));
            if (stmt is AST.PHPDocStmt) return new BoundEmptyStatement();
            if (stmt is AST.DeclareStmt declareStm) return new BoundDeclareStatement();
            if (stmt is AST.GlobalConstDeclList constDeclStm) return BindConstDecl(constDeclStm);

            //
            _diagnostics.Add(_locals.Routine, stmt, Errors.ErrorCode.ERR_NotYetImplemented, $"Statement of type '{stmt.GetType().Name}'");
            return new BoundEmptyStatement(stmt.Span.ToTextSpan());
        }

        BoundStatement BindEcho(AST.EchoStmt stmt, ImmutableArray<BoundArgument> args)
        {
            return new BoundExpressionStatement(new BoundEcho(args).WithSyntax(stmt));
        }

        BoundStatement BindUnsetStmt(AST.UnsetStmt stmt)
        {
            if (stmt.VarList.Count == 1)
            {
                return BindUnsetStmt(stmt.VarList[0]);
            }
            else
            {
                return new BoundBlock(
                    stmt.VarList
                        .Select(BindUnsetStmt)
                        .ToList()
                    );
            }
        }

        BoundStatement BindUnsetStmt(AST.VariableUse varuse)
        {
            Debug.Assert(varuse != null);
            return new BoundUnset((BoundReferenceExpression)BindExpression(varuse, BoundAccess.Unset));
        }

        BoundStatement BindGlobalStmt(AST.SimpleVarUse varuse)
        {
            return new BoundGlobalVariableStatement(
                new BoundVariableRef(BindVariableName(varuse))
                    .WithSyntax(varuse)
                    .WithAccess(BoundAccess.Write.WithWriteRef(TypeRefMask.AnyType)))
                .WithSyntax(varuse);
        }

        protected BoundStatement BindGlobalStmt(AST.GlobalStmt stmt)
        {
            if (stmt.VarList.Count == 1)
            {
                return BindGlobalStmt(stmt.VarList[0]);
            }
            else
            {
                return new BoundBlock(
                    stmt.VarList
                        .Select(BindGlobalStmt)
                        .ToList()
                    );
            }
        }

        protected BoundStatement BindStaticStmt(AST.StaticVarDecl decl)
        {
            return new BoundStaticVariableStatement(new BoundStaticVariableStatement.StaticVarDecl()
            {
                Variable = _locals.BindLocalVariable(decl.Variable, decl.Span.ToTextSpan()),
                InitialValue = (decl.Initializer != null) ? BindExpression(decl.Initializer) : null,
            });
        }

        protected BoundStatement BindStaticStmt(AST.StaticStmt stmt)
        {
            if (stmt.StVarList.Count == 1)
            {
                return BindStaticStmt(stmt.StVarList[0]);
            }
            else
            {
                return new BoundBlock(
                    stmt.StVarList
                        .Select(BindStaticStmt)
                        .ToList()
                    );
            }
        }

        protected BoundStatement BindJumpStmt(AST.JumpStmt stmt)
        {
            if (stmt.Type == AST.JumpStmt.Types.Return)
            {
                Debug.Assert(_locals != null);
                var access = _locals.Routine.SyntaxSignature.AliasReturn
                    ? BoundAccess.ReadRef
                    : BoundAccess.Read;

                return new BoundReturnStatement(stmt.Expression != null ? BindExpression(stmt.Expression, access) : null);
            }

            throw ExceptionUtilities.Unreachable;
        }

        public BoundVariableRef BindCatchVariable(AST.CatchItem x)
        {
            return new BoundVariableRef(new BoundVariableName(x.Variable.VarName))
                .WithSyntax(x.Variable)
                .WithAccess(BoundAccess.Write);
        }

        public virtual BoundItemsBag<BoundExpression> BindWholeExpression(AST.Expression expr, BoundAccess access) => BindExpression(expr, access);

        protected virtual BoundExpression BindExpression(AST.Expression expr, BoundAccess access) => BindExpressionCore(expr, access).WithSyntax(expr);

        protected BoundExpression BindExpressionCore(AST.Expression expr, BoundAccess access)
        {
            Debug.Assert(expr != null);

            if (expr is AST.Literal) return BindLiteral((AST.Literal)expr).WithAccess(access);
            if (expr is AST.ConstantUse) return BindConstUse((AST.ConstantUse)expr).WithAccess(access);
            if (expr is AST.VarLikeConstructUse)
            {
                if (expr is AST.SimpleVarUse) return BindSimpleVarUse((AST.SimpleVarUse)expr, access);
                if (expr is AST.FunctionCall) return BindFunctionCall((AST.FunctionCall)expr).WithAccess(access);
                if (expr is AST.NewEx) return BindNew((AST.NewEx)expr, access);
                if (expr is AST.ArrayEx) return BindArrayEx((AST.ArrayEx)expr, access);
                if (expr is AST.ItemUse) return BindItemUse((AST.ItemUse)expr, access);
                if (expr is AST.StaticFieldUse) return BindFieldUse((AST.StaticFieldUse)expr, access);
            }
            if (expr is AST.BinaryEx) return BindBinaryEx((AST.BinaryEx)expr).WithAccess(access);
            if (expr is AST.AssignEx) return BindAssignEx((AST.AssignEx)expr, access);
            if (expr is AST.UnaryEx) return BindUnaryEx((AST.UnaryEx)expr, access);
            if (expr is AST.IncDecEx) return BindIncDec((AST.IncDecEx)expr).WithAccess(access);
            if (expr is AST.ConditionalEx) return BindConditionalEx((AST.ConditionalEx)expr).WithAccess(access);
            if (expr is AST.ConcatEx) return BindConcatEx((AST.ConcatEx)expr).WithAccess(access);
            if (expr is AST.IncludingEx) return BindIncludeEx((AST.IncludingEx)expr).WithAccess(access);
            if (expr is AST.InstanceOfEx) return BindInstanceOfEx((AST.InstanceOfEx)expr).WithAccess(access);
            if (expr is AST.PseudoConstUse) return BindPseudoConst((AST.PseudoConstUse)expr).WithAccess(access);
            if (expr is AST.IssetEx) return BindIsSet((AST.IssetEx)expr).WithAccess(access);
            if (expr is AST.ExitEx) return BindExitEx((AST.ExitEx)expr).WithAccess(access);
            if (expr is AST.EmptyEx) return BindIsEmptyEx((AST.EmptyEx)expr).WithAccess(access);
            if (expr is AST.LambdaFunctionExpr) return BindLambda((AST.LambdaFunctionExpr)expr).WithAccess(access);
            if (expr is AST.EvalEx) return BindEval((AST.EvalEx)expr).WithAccess(access);
            if (expr is AST.YieldEx) return BindYieldEx((AST.YieldEx)expr, access).WithAccess(access);
            if (expr is AST.YieldFromEx) return BindYieldFromEx((AST.YieldFromEx)expr, access).WithAccess(access);
            if (expr is AST.ShellEx) return BindShellEx((AST.ShellEx)expr).WithAccess(access);

            //
            _diagnostics.Add(_locals.Routine, expr, Errors.ErrorCode.ERR_NotYetImplemented, $"Expression of type '{expr.GetType().Name}'");
            return new BoundLiteral(null);
        }

        protected virtual BoundYieldEx BindYieldEx(AST.YieldEx expr, BoundAccess access)
        {
            throw ExceptionUtilities.Unreachable;
        }

        protected virtual BoundExpression BindYieldFromEx(AST.YieldFromEx expr, BoundAccess access)
        {
            throw ExceptionUtilities.Unreachable;
        }

        protected BoundLambda BindLambda(AST.LambdaFunctionExpr expr)
        {
            // Syntax is bound by caller, needed to resolve lambda symbol in analysis
            return new BoundLambda(BindLambdaUseArguments(expr.UseParams));
        }

        protected BoundExpression BindEval(AST.EvalEx expr)
        {
            Routine.Flags |= RoutineFlags.HasEval;

            return new BoundEvalEx(BindExpression(expr.Code));
        }

        protected BoundExpression BindConstUse(AST.ConstantUse x)
        {
            if (x is AST.GlobalConstUse)
            {
                return BindGlobalConstUse((AST.GlobalConstUse)x);
            }

            if (x is AST.ClassConstUse)
            {
                var cx = (AST.ClassConstUse)x;
                var typeref = BindTypeRef(cx.TargetType, objectTypeInfoSemantic: true);

                if (cx.Name.Equals("class"))   // pseudo class constant
                {
                    return new BoundPseudoClassConst(typeref, AST.PseudoClassConstUse.Types.Class);
                }

                return BoundFieldRef.CreateClassConst(typeref, new BoundVariableName(cx.Name));
            }

            throw ExceptionUtilities.UnexpectedValue(x);
        }

        protected BoundExpression BindExitEx(AST.ExitEx x)
        {
            return (x.ResulExpr != null)
                ? new BoundExitEx(BindExpression(x.ResulExpr))
                : new BoundExitEx();
        }

        protected BoundExpression BindIsEmptyEx(AST.EmptyEx x)
        {
            return new BoundIsEmptyEx(BindExpression(x.Expression, BoundAccess.Read.WithQuiet()));
        }

        protected BoundExpression BindIsSet(AST.IssetEx x)
        {
            var varlist = x.VarList;

            BoundExpression result = null;

            for (int i = varlist.Count - 1; i >= 0; i--)
            {
                var expr = new BoundIsSetEx((BoundReferenceExpression)BindExpression(varlist[i], BoundAccess.Isset));

                if (result == null)
                {
                    result = expr;
                }
                else
                {
                    // isset(i1) && ... && isset(iN)
                    result = new BoundBinaryEx(expr, result, AST.Operations.And);
                }
            }

            //
            return result ?? throw new ArgumentException("isset() without arguments!");
        }

        protected BoundExpression BindPseudoConst(AST.PseudoConstUse x) => new BoundPseudoConst(x.Type);

        protected BoundExpression BindInstanceOfEx(AST.InstanceOfEx x)
        {
            return new BoundInstanceOfEx(BindExpression(x.Expression, BoundAccess.Read), BindTypeRef(x.ClassNameRef, objectTypeInfoSemantic: true));
        }

        protected BoundExpression BindIncludeEx(AST.IncludingEx x)
        {
            Routine.Flags |= RoutineFlags.HasInclude;

            return new BoundIncludeEx(BindExpression(x.Target, BoundAccess.Read), x.InclusionType);
        }

        protected BoundExpression BindConcatEx(AST.ConcatEx x) => BindConcatEx(x.Expressions);

        protected BoundExpression BindConcatEx(AST.Expression[] args) => BindConcatEx(new List<BoundArgument>(BindArguments(args)));

        protected BoundExpression BindConcatEx(List<BoundArgument> boundargs)
        {
            // flattern concat arguments
            for (int i = 0; i < boundargs.Count; i++)
            {
                if (boundargs[i].Value is BoundConcatEx c)
                {
                    var subargs = c.ArgumentsInSourceOrder;
                    boundargs.RemoveAt(i);
                    boundargs.InsertRange(i, subargs);
                }
            }

            //
            return new BoundConcatEx(boundargs.AsImmutable());
        }

        protected BoundExpression BindFunctionCall(AST.FunctionCall x)
        {
            return BindFunctionCall(x,
                boundTarget: x.IsMemberOf != null ? BindExpression(x.IsMemberOf, BoundAccess.Read/*Object?*/) : null,
                boundArguments: BindArguments(x.CallSignature.Parameters));
        }

        BoundExpression BindAssertExpression(ImmutableArray<BoundArgument> boundArguments)
        {
            return EnableAssertExpression
                ? (BoundExpression)new BoundAssertEx(boundArguments)
                : (BoundExpression)new BoundLiteral(true.AsObject());
        }

        BoundExpression BindFunctionCall(AST.FunctionCall x, BoundExpression boundTarget, ImmutableArray<BoundArgument> boundArguments)
        {
            if (x is AST.DirectFcnCall dirFcnCall)
            {
                var f = dirFcnCall;
                var fname = f.FullName;

                if (boundTarget == null)
                {
                    if (fname.IsAssertFunctionName())
                    {
                        // Template: assert(...)
                        return BindAssertExpression(boundArguments);    // TODO: do not even bind arguments if aseert is not enabled
                    }
                    else
                    {
                        return new BoundGlobalFunctionCall(fname.Name, fname.FallbackName, boundArguments);
                    }
                }
                else
                {
                    Debug.Assert(fname.FallbackName.HasValue == false);
                    Debug.Assert(fname.Name.QualifiedName.IsSimpleName);
                    return new BoundInstanceFunctionCall(boundTarget, fname.Name, boundArguments);
                }
            }
            else if (x is AST.IndirectFcnCall indFcnCall)
            {
                var f = indFcnCall;
                var nameExpr = BindExpression(f.NameExpr);
                if (boundTarget == null)
                {
                    return new BoundGlobalFunctionCall(nameExpr, boundArguments);
                }
                else
                {
                    return new BoundInstanceFunctionCall(boundTarget, new BoundUnaryEx(nameExpr, AST.Operations.StringCast), boundArguments);
                }
            }
            else if (x is AST.StaticMtdCall staticMtdCall)
            {
                var f = staticMtdCall;
                Debug.Assert(boundTarget == null);

                var boundname = (f is AST.DirectStMtdCall stmtd)
                    ? new BoundRoutineName(new QualifiedName(stmtd.MethodName))
                    : new BoundRoutineName(new BoundUnaryEx(BindExpression(((AST.IndirectStMtdCall)f).MethodNameExpression), AST.Operations.StringCast));

                return new BoundStaticFunctionCall(BindTypeRef(f.TargetType, objectTypeInfoSemantic: true), boundname, boundArguments);
            }

            //
            throw new NotImplementedException(x.GetType().FullName);
        }

        public BoundTypeRef BindTypeRef(AST.TypeRef tref, bool objectTypeInfoSemantic = false)
        {
            if (tref is AST.MultipleTypeRef mref)
            {
                return new BoundMultipleTypeRef(
                    mref.MultipleTypes.Select(r => BindTypeRef(r, objectTypeInfoSemantic)).AsImmutable(),
                    tref,
                    objectTypeInfoSemantic);
            }
            else
            {
                var bound = new BoundTypeRef(tref, objectTypeInfoSemantic);

                if (tref is AST.IndirectTypeRef)
                {
                    bound.TypeExpression = BindExpression(((AST.IndirectTypeRef)tref).ClassNameVar);
                }

                if (tref is AST.ReservedTypeRef rt && rt.Type == AST.ReservedTypeRef.ReservedType.@static && Routine != null)
                {
                    Routine.Flags |= RoutineFlags.UsesLateStatic;
                }

                return bound;
            }
        }

        protected virtual BoundExpression BindConditionalEx(AST.ConditionalEx expr)
        {
            return new BoundConditionalEx(
                BindExpression(expr.CondExpr),
                (expr.TrueExpr != null) ? BindExpression(expr.TrueExpr) : null,
                BindExpression(expr.FalseExpr));
        }

        protected BoundExpression BindIncDec(AST.IncDecEx expr)
        {
            // bind variable reference
            var varref = (BoundReferenceExpression)BindExpression(expr.Variable, BoundAccess.ReadAndWrite);

            // resolve kind
            UnaryOperationKind kind;
            if (expr.Inc)
                kind = (expr.Post) ? UnaryOperationKind.OperatorPostfixIncrement : UnaryOperationKind.OperatorPrefixIncrement;
            else
                kind = (expr.Post) ? UnaryOperationKind.OperatorPostfixDecrement : UnaryOperationKind.OperatorPrefixDecrement;

            //
            return new BoundIncDecEx(varref, kind);
        }

        protected BoundExpression BindShellEx(AST.ShellEx expr)
        {
            return new BoundGlobalFunctionCall(NameUtils.SpecialNames.shell_exec, null, BindArguments(expr.Command));
        }

        protected BoundExpression BindNew(AST.NewEx x, BoundAccess access)
        {
            Debug.Assert(access.IsRead || access.IsReadRef || access.IsNone);

            return new BoundNewEx(BindTypeRef(x.ClassNameRef), BindArguments(x.CallSignature.Parameters))
                .WithAccess(access);
        }

        protected BoundExpression BindArrayEx(AST.ArrayEx x, BoundAccess access)
        {
            if (x.Operation == AST.Operations.Array)
            {
                Debug.Assert(access.IsRead);

                if (access.IsReadRef)
                {
                    // TODO: warninng deprecated
                    // _diagnostics. ...
                }

                return new BoundArrayEx(BindArrayItems(x.Items, BoundAccess.Read.WithReadCopy()))
                    .WithAccess(access);
            }
            else if (x.Operation == AST.Operations.List)
            {
                Debug.Assert(access.IsWrite);

                var items = BindArrayItems(x.Items, BoundAccess.Write)
                    .Select(pair => new KeyValuePair<BoundExpression, BoundReferenceExpression>(pair.Key, (BoundReferenceExpression)pair.Value));

                return new BoundListEx(items.ToArray())
                    .WithAccess(BoundAccess.Write);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(x.Operation);
            }
        }

        protected IEnumerable<KeyValuePair<BoundExpression, BoundExpression>> BindArrayItems(AST.Item[] items, BoundAccess valueaccess)
        {
            // trim trailing empty items
            int count = items.Length;
            while (count > 0 && items[count - 1] == null)
            {
                count--;
            }

            for (int i = 0; i < count; i++)
            {
                var x = items[i];
                if (x == null)
                {
                    // list() may contain empty items
                    yield return new KeyValuePair<BoundExpression, BoundExpression>();
                }
                else
                {
                    Debug.Assert(x is AST.RefItem || x is AST.ValueItem);

                    var boundIndex = (x.Index != null) ? BindExpression(x.Index, BoundAccess.Read) : null;
                    var boundValue = (x is AST.RefItem refItem)
                        ? BindExpression(refItem.RefToGet, BoundAccess.ReadRef)
                        : BindExpression(((AST.ValueItem)x).ValueExpr, valueaccess);

                    yield return new KeyValuePair<BoundExpression, BoundExpression>(boundIndex, boundValue);
                }
            }
        }

        protected BoundExpression BindItemUse(AST.ItemUse x, BoundAccess access)
        {
            AstUtils.PatchItemUse(x);

            var arrayAccess = BoundAccess.Read;

            if (x.Index == null && (!access.IsEnsure && !access.IsWrite))   // READ variable[]
                access = access.WithReadRef();                              // -> READREF variable[] // the only way new item will be ensured

            if (access.IsWrite || access.IsEnsure)
                arrayAccess = arrayAccess.WithEnsureArray();
            if (access.IsQuiet)
                arrayAccess = arrayAccess.WithQuiet();

            var boundArray = BindExpression(x.Array, arrayAccess);

            // boundArray.Access = boundArray.Access.WithRead(typeof(PhpArray))

            return new BoundArrayItemEx(
                boundArray, (x.Index != null) ? BindExpression(x.Index, BoundAccess.Read) : null)
                .WithAccess(access);
        }

        protected BoundVariableName BindVariableName(AST.SimpleVarUse varuse)
        {
            var dexpr = varuse as AST.DirectVarUse;
            var iexpr = varuse as AST.IndirectVarUse;

            Debug.Assert(dexpr != null || iexpr != null);

            return (dexpr != null)
                ? new BoundVariableName(dexpr.VarName)
                : new BoundVariableName(BindExpression(iexpr.VarNameEx));
        }

        protected BoundExpression BindSimpleVarUse(AST.SimpleVarUse expr, BoundAccess access)
        {
            var varname = BindVariableName(expr);

            if (expr.IsMemberOf == null)
            {
                if (!varname.IsDirect)
                {
                    Routine.Flags |= RoutineFlags.HasIndirectVar;
                }

                return new BoundVariableRef(varname).WithAccess(access);
            }
            else
            {
                var instanceAccess = BoundAccess.Read;

                if (access.IsWrite || access.EnsureObject || access.EnsureArray)
                    instanceAccess = instanceAccess.WithEnsureObject();

                if (access.IsQuiet)
                    instanceAccess = instanceAccess.WithQuiet();

                return BoundFieldRef.CreateInstanceField(BindExpression(expr.IsMemberOf, instanceAccess), varname).WithAccess(access);
            }
        }

        protected BoundExpression BindFieldUse(AST.StaticFieldUse x, BoundAccess access)
        {
            var typeref = BindTypeRef(x.TargetType, objectTypeInfoSemantic: true);
            BoundVariableName varname;

            if (x is AST.DirectStFldUse)
            {
                var dx = (AST.DirectStFldUse)x;
                varname = new BoundVariableName(dx.PropertyName);
            }
            else if (x is AST.IndirectStFldUse)
            {
                var ix = (AST.IndirectStFldUse)x;
                var fieldNameExpr = BindExpression(ix.FieldNameExpr, BoundAccess.Read);

                varname = new BoundVariableName(fieldNameExpr);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(x);
            }

            return BoundFieldRef.CreateStaticField(typeref, varname).WithAccess(access);
        }

        protected BoundExpression BindGlobalConstUse(AST.GlobalConstUse expr)
        {
            // translate built-in constants directly
            if (expr.Name == QualifiedName.True) return new BoundLiteral(true);
            if (expr.Name == QualifiedName.False) return new BoundLiteral(false);
            if (expr.Name == QualifiedName.Null) return new BoundLiteral(null);

            // bind constant
            return new BoundGlobalConst(expr.FullName.Name, expr.FullName.FallbackName);
        }

        protected BoundStatement BindConstDecl(AST.GlobalConstDeclList decl)
        {
            if (decl.Constants.Count == 1)
            {
                return BindConstDecl(decl.Constants[0]);
            }
            else
            {
                return new BoundBlock(decl.Constants.Select(BindConstDecl).ToList());
            }
        }

        BoundStatement BindConstDecl(AST.GlobalConstantDecl decl)
        {
            var qname = NameUtils.MakeQualifiedName(new Name(decl.Name.Name.Value), decl.ContainingNamespace);
            return new BoundGlobalConstDeclStatement(qname, BindExpression(decl.Initializer, BoundAccess.Read));
        }

        protected virtual BoundExpression BindBinaryEx(AST.BinaryEx expr)
        {
            switch (expr.Operation)
            {
                case AST.Operations.Concat:     // Left . Right
                    return BindConcatEx(new[] { expr.LeftExpr, expr.RightExpr });

                default:
                    return new BoundBinaryEx(
                        BindExpression(expr.LeftExpr, BoundAccess.Read),
                        BindExpression(expr.RightExpr, BoundAccess.Read),
                        expr.Operation);
            }
        }

        protected BoundExpression BindUnaryEx(AST.UnaryEx expr, BoundAccess access)
        {
            var operandAccess = BoundAccess.Read;

            switch (expr.Operation)
            {
                case AST.Operations.AtSign:
                    operandAccess = access;
                    break;
                case AST.Operations.UnsetCast:
                    operandAccess = BoundAccess.None;
                    break;
            }

            return new BoundUnaryEx(BindExpression(expr.Expr, operandAccess), expr.Operation)
                .WithAccess(access);
        }

        protected BoundExpression BindAssignEx(AST.AssignEx expr, BoundAccess access)
        {
            var target = (BoundReferenceExpression)BindExpression(expr.LValue, BoundAccess.Write);
            BoundExpression value;

            // bind value (read as value or as ref)
            if (expr is AST.ValueAssignEx assignEx)
            {
                var readaccess = BoundAccess.Read;
                if (!(target is BoundListEx))   // we don't need copy of RValue if assigning to list()
                {
                    readaccess = readaccess.WithReadCopy();
                }

                value = BindExpression(assignEx.RValue, readaccess);
            }
            else if (expr is AST.RefAssignEx refAssignEx)
            {
                Debug.Assert(expr.Operation == AST.Operations.AssignRef);
                target.Access = target.Access.WithWriteRef(0); // note: analysis will write the write type
                value = BindExpression(refAssignEx.RValue, BoundAccess.ReadRef);
            }
            else
            {
                ExceptionUtilities.UnexpectedValue(expr);
                return null;
            }

            //
            if (expr.Operation == AST.Operations.AssignValue || expr.Operation == AST.Operations.AssignRef)
            {
                return new BoundAssignEx(target, value).WithAccess(access);
            }
            else
            {
                if (target is BoundArrayItemEx itemex && itemex.Index == null)
                {
                    // Special case:
                    switch (expr.Operation)
                    {
                        // "ARRAY[] .= VALUE" => "ARRAY[] = (string)VALUE"
                        case AST.Operations.AssignPrepend:
                        case AST.Operations.AssignAppend:   // .=
                            value = BindConcatEx(new List<BoundArgument>() { BoundArgument.Create(new BoundLiteral(null).WithAccess(BoundAccess.Read)), BoundArgument.Create(value) });
                            break;

                        default:
                            value = new BoundBinaryEx(new BoundLiteral(null).WithAccess(BoundAccess.Read), value, AstUtils.CompoundOpToBinaryOp(expr.Operation));
                            break;
                    }

                    return new BoundAssignEx(target, value.WithAccess(BoundAccess.Read)).WithAccess(access);
                }
                else
                {
                    target.Access = target.Access.WithRead();   // Read & Write on target

                    return new BoundCompoundAssignEx(target, value, expr.Operation).WithAccess(access);
                }
            }
        }

        public BoundStatement BindEmptyStmt(Span span)
        {
            return new BoundEmptyStatement(span.ToTextSpan());
        }

        protected static BoundExpression BindLiteral(AST.Literal expr)
        {
            if (expr is AST.LongIntLiteral longIntLit) return new BoundLiteral(longIntLit.Value);
            if (expr is AST.StringLiteral stringLit) return new BoundLiteral(stringLit.Value);
            if (expr is AST.DoubleLiteral doubleLit) return new BoundLiteral(doubleLit.Value);
            if (expr is AST.BoolLiteral boolLit) return new BoundLiteral(boolLit.Value.AsObject());
            if (expr is AST.NullLiteral nullLit) return new BoundLiteral(null);
            if (expr is AST.BinaryStringLiteral binStringLit) return new BoundLiteral(binStringLit.Value);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates <paramref name="expr"/>'s <see cref="BoundAccess"/> to <see cref="BoundAccess.ReadRef"/>.
        /// </summary>
        /// <param name="expr">Expression which access has to be updated.</param>
        public static void BindReadRefAccess(BoundReferenceExpression expr)
        {
            if (expr == null || expr.Access.IsReadRef) return;

            expr.Access = expr.Access.WithReadRef();
            BindEnsureAccess(expr); // parent expression chain has to be updated as well
        }

        /// <summary>
        /// Updates <paramref name="expr"/>'s <see cref="BoundAccess"/> to <see cref="BoundAccess.Write"/>.
        /// </summary>
        /// <param name="expr">Expression which access has to be updated.</param>
        public static void BindWriteAccess(BoundReferenceExpression expr)
        {
            if (expr == null || expr.Access.IsWrite) return;

            expr.Access = expr.Access.WithWrite(0);
            BindEnsureAccess(expr); // parent expression chain has to be updated as well
        }

        protected static void BindEnsureAccess(BoundExpression expr)
        {
            if (expr is BoundArrayItemEx)
            {
                var arritem = (BoundArrayItemEx)expr;
                arritem.Array.Access = arritem.Array.Access.WithEnsureArray();
                BindEnsureAccess(arritem.Array);
            }
        }
    }

    internal sealed class GeneratorSemanticsBinder : SemanticsBinder
    {
        #region FieldsAndProperties

        /// <summary>
        /// Found yield statements (needed for ControlFlowGraph)
        /// </summary>
        public override BoundYieldStatement[] Yields { get => _yields.ToArray(); }
        readonly List<BoundYieldStatement> _yields = new List<BoundYieldStatement>();

        readonly HashSet<AST.LangElement> _yieldsToStatementRootPath = new HashSet<AST.LangElement>();
        int _rewriterVariableIndex = 0;
        int _underYieldLikeExLevel = -1;

        #endregion

        #region PreBoundBlocks

        private BoundBlock _preBoundBlockFirst;
        private BoundBlock _preBoundBlockLast;

        BoundBlock NewBlock() => _newBlockFunc();
        Func<BoundBlock> _newBlockFunc;

        BoundYieldStatement NewYieldStatement(BoundExpression valueExpression, BoundExpression keyExpression, AST.LangElement syntax = null)
        {
            var yieldStmt = new BoundYieldStatement(_yields.Count + 1, valueExpression, keyExpression).WithSyntax(syntax);

            _yields.Add(yieldStmt);

            return yieldStmt;
        }

        string GetTempVariableName()
        {
            return "<sm>" + _rewriterVariableIndex++;
        }

        void InitPreBoundBlocks()
        {
            _preBoundBlockFirst = _preBoundBlockFirst ?? NewBlock();
            _preBoundBlockLast = _preBoundBlockLast ?? _preBoundBlockFirst;
        }

        BoundBlock CurrentPreBoundBlock
        {
            get { InitPreBoundBlocks(); return _preBoundBlockLast; }
            set { _preBoundBlockLast = value; }
        }

        bool AnyPreBoundItems
        {
            get
            {
                Debug.Assert((_preBoundBlockFirst == null) == (_preBoundBlockLast == null));
                return _preBoundBlockFirst != null;
            }
        }

        #endregion

        #region Construction

        public GeneratorSemanticsBinder(ImmutableArray<AST.IYieldLikeEx> yields, LocalsTable locals, DiagnosticBag diagnostics)
            : base(locals, diagnostics)
        {
            Debug.Assert(Routine != null);

            // TODO: move this to SourceRoutineSymbol ctor?
            Routine.Flags |= RoutineFlags.IsGenerator;

            // save all parents of all yieldLikeEx in current routine -> will need to realocate all expressions on path and in its children
            //  - the ones to the left from yieldLikeEx<>root path need to get moved in statements before yieldLikeEx
            //  - the ones on the path could be left alone but if we prepend the ones on the right we must also move the ones on the path as they should get executed before the right ones
            //  - the ones to the right could be left alone but it's easier to move them as well; need to go after yieldLikeStatement
            foreach (var yieldLikeEx in yields)
            {
                Debug.Assert(yieldLikeEx is AST.LangElement);
                var parent = ((AST.LangElement)yieldLikeEx).ContainingElement;
                // add all parents until reaching the top of current statement tree
                while (!(parent is AST.MethodDecl || parent is AST.IStatement))
                {
                    _yieldsToStatementRootPath.Add(parent);
                    parent = parent.ContainingElement;
                }
            }
        }

        public override void SetupBuilder(Func<BoundBlock> newBlockFunc)
        {
            Debug.Assert(newBlockFunc != null);

            base.SetupBuilder(newBlockFunc);

            //
            _newBlockFunc = newBlockFunc;
        }

        #endregion

        #region GeneralOverrides

        public override BoundItemsBag<BoundExpression> BindWholeExpression(AST.Expression expr, BoundAccess access)
        {
            Debug.Assert(!AnyPreBoundItems);

            var boundItem = BindExpression(expr, access);
            var boundBag = new BoundItemsBag<BoundExpression>(boundItem, _preBoundBlockFirst, _preBoundBlockLast);

            ClearPreBoundBlocks();

            return boundBag;
        }

        public override BoundItemsBag<BoundStatement> BindWholeStatement(AST.Statement stmt)
        {
            Debug.Assert(!AnyPreBoundItems);

            var boundItem = BindStatement(stmt);
            var boundBag = new BoundItemsBag<BoundStatement>(boundItem, _preBoundBlockFirst, _preBoundBlockLast);

            ClearPreBoundBlocks();
            return boundBag;
        }

        protected override BoundExpression BindExpression(AST.Expression expr, BoundAccess access)
        {
            var _underYieldLikeExLevelOnEnter = _underYieldLikeExLevel;

            // can't use only AST to determine whether we're under yield<>root route 
            //  -> there're expressions (such as foreach variable) outside in terms of semantics tree
            //  -> for those we don't want to do any moving (can actually be a problem for those)

            // update _underYieldLikeExLevel
            if (_underYieldLikeExLevel >= 0) { _underYieldLikeExLevel++; }
            if (_yieldsToStatementRootPath.Contains(expr)) { _underYieldLikeExLevel = 0; }

            var boundExpr = base.BindExpression(expr, access);

            // move expressions on and directly under yieldLikeEx<>root path
            if (_underYieldLikeExLevel == 1 || _underYieldLikeExLevel == 0)
            {
                boundExpr = MakeTmpCopyAndPrependAssigment(boundExpr, access);
            }

            _underYieldLikeExLevel = _underYieldLikeExLevelOnEnter;
            return boundExpr;
        }

        protected override BoundStatement BindStatement(AST.Statement stmt)
        {
            var _underYieldLikeExLevelOnEnter = _underYieldLikeExLevel;

            // update _underYieldLikeExLevel
            if (_underYieldLikeExLevel >= 0) { _underYieldLikeExLevel++; }
            if (_yieldsToStatementRootPath.Contains(stmt)) { _underYieldLikeExLevel = 0; }

            var boundStatement = base.BindStatement(stmt);

            _underYieldLikeExLevel = _underYieldLikeExLevelOnEnter;
            return boundStatement;
        }
        #endregion

        #region SpecificExOverrides

        protected override BoundExpression BindBinaryEx(AST.BinaryEx expr)
        {
            // if not a short-circuit operator -> use normal logic for binding
            if (!(expr.Operation == AST.Operations.And || expr.Operation == AST.Operations.Or || expr.Operation == AST.Operations.Coalesce))
            { return base.BindBinaryEx(expr); }

            // create/get a source block defensively before potential rightExprBag.PreBoundBlocks (it needs to have a smaller Ordinal)
            var currBlock = CurrentPreBoundBlock;

            // left operand is always evaluated -> handle normally
            var leftExpr = BindExpression(expr.LeftExpr, BoundAccess.Read);

            // right operand & its pre-bound statements might not be evaluated due to short-circuit evaluation
            // get all of the pre-bound statements and convert them to statemenets conditioned by short-cirtuit-eval logic
            var rightExprBag = BindExpressionWithSeparatePreBoundStatements(expr.RightExpr, BoundAccess.Read);

            if (!rightExprBag.IsOnlyBoundElement)
            {
                // make a defensive copy if multiple evaluations could be a problem for left expr (which serves as the condition)
                // no need to worry about order of execution: right bag contains preBoundStatements  
                // .. -> we are on yield<>root path -> expression to the left are already prepended
                leftExpr = MakeTmpCopyAndPrependAssigment(leftExpr, BoundAccess.Read);

                // create a condition expr. that is true only when right operand would have to be evaluated
                BoundExpression condition = null;
                switch (expr.Operation)
                {
                    case AST.Operations.And:
                        condition = leftExpr; // left is true
                        break;
                    case AST.Operations.Or:
                        condition = new BoundUnaryEx(leftExpr, AST.Operations.LogicNegation); // left is false
                        break;
                    case AST.Operations.Coalesce:
                        if (leftExpr is BoundReferenceExpression leftRef) // left is not set or null
                        {
                            condition = new BoundUnaryEx(
                                new BoundIsSetEx(leftRef),
                                AST.Operations.LogicNegation
                                );
                        }
                        else
                        {
                            // Template: "is_null( LValue )"
                            condition = new BoundGlobalFunctionCall(
                                NameUtils.SpecialNames.is_null, null,
                                ImmutableArray.Create(BoundArgument.Create(leftExpr)));
                        }
                        break;
                    default:
                        throw ExceptionUtilities.Unreachable;
                }

                // create a conditional edge and set the last (current) pre-bound block to the conditional edge's end block
                CurrentPreBoundBlock = CreateConditionalEdge(currBlock, condition, rightExprBag.PreBoundBlockFirst, rightExprBag.PreBoundBlockLast, null, null);
            }

            return new BoundBinaryEx(
                leftExpr,
                rightExprBag.BoundElement,
                expr.Operation);
        }

        protected override BoundExpression BindConditionalEx(AST.ConditionalEx expr)
        {
            var condExpr = BindExpression(expr.CondExpr);

            // create/get a source block defensively before potential true/falseExprBag.PreBoundBlocks (it needs to have a smaller Ordinal)
            var currBlock = CurrentPreBoundBlock;

            // get expressions and their pre-bound elements for both branches
            var trueExprBag = BindExpressionWithSeparatePreBoundStatements(expr.TrueExpr, BoundAccess.Read);
            var falseExprBag = BindExpressionWithSeparatePreBoundStatements(expr.FalseExpr, BoundAccess.Read);

            // if at least branch has any pre-bound statements we need to condition them
            if (!trueExprBag.IsOnlyBoundElement || !falseExprBag.IsOnlyBoundElement)
            {
                // make a defensive copy of condition, would be evaluated twice otherwise (conditioned prebound blocks and original position)
                // no need to worry about order of execution: either true or false branch contains preBoundStatements  
                // .. -> we are on yield<>root path -> expression to the left are already prepended
                condExpr = MakeTmpCopyAndPrependAssigment(condExpr, BoundAccess.Read);

                // create a conditional edge and set the last (current) pre-bound block to the conditional edge's end block
                CurrentPreBoundBlock = CreateConditionalEdge(currBlock, condExpr,
                    trueExprBag.PreBoundBlockFirst, trueExprBag.PreBoundBlockLast,
                    falseExprBag.PreBoundBlockFirst, falseExprBag.PreBoundBlockLast);
            }

            return new BoundConditionalEx(
                condExpr,
                trueExprBag.BoundElement,
                falseExprBag.BoundElement);
        }

        protected override BoundYieldEx BindYieldEx(AST.YieldEx expr, BoundAccess access)
        {
            // Reference: https://github.com/dotnet/roslyn/blob/05d923831e1bc2a88918a2073fba25ab060dda0c/src/Compilers/CSharp/Portable/Binder/Binder_Statements.cs#L194

            // TODO: Throw error when trying to iterate a non-reference generator by reference 
            var valueaccess = _locals.Routine.SyntaxSignature.AliasReturn
                    ? BoundAccess.ReadRef
                    : BoundAccess.Read;

            // bind value and key expressions
            var boundValueExpr = (expr.ValueExpr != null) ? BindExpression(expr.ValueExpr, valueaccess) : null;
            var boundKeyExpr = (expr.KeyExpr != null) ? BindExpression(expr.KeyExpr) : null;

            // bind yield statement (represents return & continuation)
            CurrentPreBoundBlock.Add(NewYieldStatement(boundValueExpr, boundKeyExpr, syntax: expr));

            // return BoundYieldEx representing a reference to a value sent to the generator
            return new BoundYieldEx();
        }

        protected override BoundExpression BindYieldFromEx(AST.YieldFromEx expr, BoundAccess access)
        {
            var aliasedValues = _locals.Routine.SyntaxSignature.AliasReturn;
            var tmpVar = MakeTmpCopyAndPrependAssigment(BindExpression(expr.ValueExpr), BoundAccess.Read);

            /* Template:
             * foreach (<tmp> => <key> as <value>) {
             *     yield <key> => <value>;
             * }
             * return <tmp>.getReturn()
             */

            string valueVarName = GetTempVariableName();
            string keyVarName = valueVarName + "k";

            var move = NewBlock();
            var body = NewBlock();
            var end = NewBlock();

            var enumereeEdge = new ForeachEnumereeEdge(CurrentPreBoundBlock, move, tmpVar, aliasedValues);

            // MoveNext()
            var moveEdge = new ForeachMoveNextEdge(
                move, body, end, enumereeEdge,
                keyVar: new BoundTemporalVariableRef(keyVarName).WithAccess(BoundAccess.Write),
                valueVar: new BoundTemporalVariableRef(valueVarName).WithAccess(aliasedValues ? BoundAccess.Write.WithWriteRef(0) : BoundAccess.Write),
                moveSpan: default(Microsoft.CodeAnalysis.Text.TextSpan));

            // body:
            // Template: yield key => value
            body.Add(
                NewYieldStatement(
                    valueExpression: new BoundTemporalVariableRef(valueVarName).WithAccess(aliasedValues ? BoundAccess.ReadRef : BoundAccess.Read),
                    keyExpression: new BoundTemporalVariableRef(keyVarName).WithAccess(BoundAccess.Read)));

            // goto move
            new SimpleEdge(body, move);

            // end:
            CurrentPreBoundBlock = end;

            // GET_RETURN( tmp as Generator )
            return new BoundYieldFromEx(tmpVar);
        }

        #endregion

        #region Helpers

        public struct BoundSynthesizedVariableInfo
        {
            public BoundReferenceExpression BoundExpr;
            public BoundAssignEx Assignment;
        }

        private void ClearPreBoundBlocks()
        {
            _preBoundBlockFirst = null;
            _preBoundBlockLast = null;
        }

        /// <summary>
        /// Assigns an expression to a temp variable, puts the assigment to <c>_preCurrentlyBinded</c>, and returns reference to the temp variable.
        /// </summary>
        private BoundExpression MakeTmpCopyAndPrependAssigment(BoundExpression boundExpr, BoundAccess access)
        {
            // no need to do anything if the expression is constant because neither multiple evaluation nor different order of eval is a problem
            if (boundExpr.IsConstant()) { return boundExpr; }

            var assignVarTouple = CreateAndAssignSynthesizedVariable(boundExpr, access, GetTempVariableName());

            CurrentPreBoundBlock.Add(new BoundExpressionStatement(assignVarTouple.Assignment)); // assigment
            return assignVarTouple.BoundExpr; // temp variable ref
        }

        internal static BoundSynthesizedVariableInfo CreateAndAssignSynthesizedVariable(BoundExpression expr, BoundAccess access, string name)
        {
            // determine whether the synthesized variable should be by ref (for readRef and writes) or a normal PHP copy
            var refAccess = (access.IsReadRef || access.IsWrite);

            // bind assigment target variable with appropriate access
            var targetVariable = new BoundTemporalVariableRef(name)
                .WithAccess(refAccess ? BoundAccess.Write.WithWriteRef(0) : BoundAccess.Write);

            // set appropriate access of the original value expression
            var valueBeingMoved = (refAccess) ? expr.WithAccess(BoundAccess.ReadRef) : expr.WithAccess(BoundAccess.Read);

            // bind assigment and reference to the created synthesized variable
            var assigment = new BoundAssignEx(targetVariable, valueBeingMoved);
            var boundExpr = new BoundTemporalVariableRef(name).WithAccess(access);

            return new BoundSynthesizedVariableInfo() { BoundExpr = boundExpr, Assignment = assigment };
        }

        /// <summary>
        /// Creates a conditional edge and returns its endBlock.
        /// </summary>
        private BoundBlock CreateConditionalEdge(BoundBlock sourceBlock, BoundExpression condExpr,
            BoundBlock trueBlockStart, BoundBlock trueBlockEnd, BoundBlock falseBlockStart, BoundBlock falseBlockEnd)
        {
            Debug.Assert(trueBlockStart != null || falseBlockStart != null);
            Debug.Assert(trueBlockStart == null ^ trueBlockEnd != null);
            Debug.Assert(falseBlockStart == null ^ falseBlockEnd != null);

            // if only false branch is non-empty flip the condition and conditioned blocks so that true is non-empty
            if (trueBlockStart == null)
            {
                condExpr = new BoundUnaryEx(condExpr, AST.Operations.LogicNegation);
                trueBlockStart = falseBlockStart;
                trueBlockEnd = falseBlockEnd;
                falseBlockStart = null;
                falseBlockEnd = null;
            }

            var endBlock = NewBlock();
            falseBlockStart = falseBlockStart ?? endBlock;

            new ConditionalEdge(sourceBlock, trueBlockStart, falseBlockStart, condExpr);
            new SimpleEdge(trueBlockEnd, endBlock);
            if (falseBlockStart != endBlock) { new SimpleEdge(falseBlockEnd, endBlock); }

            return endBlock;
        }

        private BoundItemsBag<BoundExpression> BindExpressionWithSeparatePreBoundStatements(AST.Expression expr, BoundAccess access)
        {
            if (expr == null) { return BoundItemsBag<BoundExpression>.Empty; }

            // save original preBoundBlocks
            var originalPreBoundFirst = _preBoundBlockFirst;
            var originalPreBoundLast = _preBoundBlockLast;

            ClearPreBoundBlocks(); // clean state
            var currExprBag = BindWholeExpression(expr, access);

            // restore original preBoundBlocks
            _preBoundBlockFirst = originalPreBoundFirst;
            _preBoundBlockLast = originalPreBoundLast;

            return currExprBag;
        }

        #endregion
    }
}
