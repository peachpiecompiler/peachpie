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

namespace Pchp.CodeAnalysis.Semantics
{

    /// <summary>
    /// Holds currently bound item and optionally statements that are supposed to be go before it. 
    /// </summary>
    /// <typeparam name="T">Either <c>BoundExpression</c> or <c>BoundStatement</c>.</typeparam>
    internal struct BoundItemsBag<T> where T : IPhpOperation
    {
        public ImmutableArray<BoundStatement> PreBoundStatements { get; private set; }
        public T BoundElement { get; private set; }

        public BoundItemsBag(ImmutableArray<BoundStatement> preBound, T bound)
        {
            PreBoundStatements = preBound;
            BoundElement = bound;
        }

        public BoundItemsBag(T bound) : this(ImmutableArray<BoundStatement>.Empty, bound) { }

        /// <summary>
        /// Returns bound elemenent and asserts that there are no <c>PreBoundStatements</c>.
        /// </summary>
        public T GetOnlyBoundElement()
        {
            Debug.Assert(PreBoundStatements.IsEmpty);
            return BoundElement;
        }

        public static implicit operator BoundItemsBag<T>(T item) => new BoundItemsBag<T>(item);
        
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
        readonly LocalsTable _locals;

        /// <summary>
        /// Gets corresponding routine.
        /// </summary>
        public SourceRoutineSymbol Routine => _locals?.Routine;

        /// <summary>
        /// Found yields (needed for ControlFlowGraph)
        /// </summary>
        public BoundYieldEx[] Yields { get => _yields.ToArray(); }
        readonly List<BoundYieldEx> _yields;

        readonly DiagnosticBag _diagnostics;

        #region Construction

        public SemanticsBinder(LocalsTable locals = null, DiagnosticBag diagnostics = null)
        {
            _locals = locals;
            _diagnostics = diagnostics ?? DiagnosticBag.GetInstance();
            _yields = new List<BoundYieldEx>();
        }

        #endregion

        #region Helpers

        IEnumerable<BoundStatement> BindStatements(IEnumerable<AST.Statement> statements)
        {
            return statements.Select(BindStatement);
        }

        ImmutableArray<BoundExpression> BindExpressions(IEnumerable<AST.Expression> expressions)
        {
            return expressions.Select(BindExpression).ToImmutableArray();
        }

        BoundExpression BindExpression(AST.Expression expr) => BindExpression(expr, BoundAccess.Read);

        ImmutableArray<BoundArgument> BindArguments(IEnumerable<AST.Expression> expressions)
        {
            return BindExpressions(expressions)
                .Select(x => new BoundArgument(x))
                .ToImmutableArray();
        }

        ImmutableArray<BoundArgument> BindArguments(IEnumerable<AST.ActualParam> parameters)
        {
            var unsupported = parameters.FirstOrDefault(p => p.IsUnpack || p.Ampersand);
            if (unsupported != null)
            {
                _diagnostics.Add(_locals.Routine, unsupported, Errors.ErrorCode.ERR_NotYetImplemented, "Passing parameter by ref or parameter unpacking.");
            }

            return BindArguments(parameters.Select(p => p.Expression));
        }

        ImmutableArray<BoundArgument> BindLambdaUseArguments(IEnumerable<AST.FormalParam> usevars)
        {
            return usevars.Select(v =>
            {
                var varuse = new AST.DirectVarUse(v.Name.Span, v.Name.Name);
                return BindExpression(varuse, v.PassedByRef ? BoundAccess.ReadRef : BoundAccess.Read);
            })
            .Select(expr => new BoundArgument(expr))
            .ToImmutableArray();
        }

        #endregion

        public BoundItemsBag<BoundStatement> HandleStatement(AST.Statement stmt) 
            => new BoundItemsBag<BoundStatement>(BindStatement(stmt));       

        BoundStatement BindStatement(AST.Statement stmt)
        {
            Debug.Assert(stmt != null);

            if (stmt is AST.EchoStmt echoStm) return new BoundExpressionStatement(new BoundEcho(BindArguments((echoStm).Parameters))) { PhpSyntax = stmt };
            if (stmt is AST.ExpressionStmt exprStm) return new BoundExpressionStatement(BindExpression((exprStm).Expression, BoundAccess.None)) { PhpSyntax = stmt };
            if (stmt is AST.JumpStmt jmpStm) return BindJumpStmt(jmpStm);
            if (stmt is AST.FunctionDecl) return new BoundFunctionDeclStatement(stmt.GetProperty<SourceFunctionSymbol>());
            if (stmt is AST.TypeDecl) return new BoundTypeDeclStatement(stmt.GetProperty<SourceTypeSymbol>());
            if (stmt is AST.GlobalStmt) return new BoundGlobalVariableStatement(
                ((AST.GlobalStmt)stmt).VarList.Cast<AST.DirectVarUse>()
                    .Select(s => _locals.BindVariable(s.VarName, VariableKind.GlobalVariable, s.Span.ToTextSpan(), null))
                    .ToImmutableArray());
            if (stmt is AST.StaticStmt staticStm) return new BoundStaticVariableStatement(
                staticStm.StVarList
                    .Select(s => (BoundStaticLocal)_locals.BindVariable(s.Variable, VariableKind.StaticVariable, s.Span.ToTextSpan(),
                        () => (s.Initializer != null ? BindExpression(s.Initializer) : null)))
                    .ToImmutableArray())
            { PhpSyntax = stmt };
            if (stmt is AST.UnsetStmt unsetStm) return new BoundUnset(
                unsetStm.VarList
                    .Select(v => (BoundReferenceExpression)BindExpression(v, BoundAccess.Unset))
                    .ToImmutableArray())
            { PhpSyntax = stmt };
            if (stmt is AST.ThrowStmt throwStm) return new BoundThrowStatement(BindExpression(throwStm.Expression, BoundAccess.Read)) { PhpSyntax = stmt };
            if (stmt is AST.PHPDocStmt) return new BoundEmptyStatement() { PhpSyntax = stmt };

            //
            _diagnostics.Add(_locals.Routine, stmt, Errors.ErrorCode.ERR_NotYetImplemented, $"Statement of type '{stmt.GetType().Name}'");
            return new BoundEmptyStatement(stmt.Span.ToTextSpan());
        }

        BoundStatement BindJumpStmt(AST.JumpStmt stmt)
        {
            if (stmt.Type == AST.JumpStmt.Types.Return)
            {
                Debug.Assert(_locals != null);
                var access = _locals.Routine.SyntaxSignature.AliasReturn
                    ? BoundAccess.ReadRef
                    : BoundAccess.Read;

                return new BoundReturnStatement(stmt.Expression != null ? BindExpression(stmt.Expression, access) : null)
                {
                    PhpSyntax = stmt
                };
            }

            throw ExceptionUtilities.Unreachable;
        }

        public BoundVariableRef BindCatchVariable(AST.CatchItem x)
        {
            return new BoundVariableRef(new BoundVariableName(x.Variable.VarName)) { PhpSyntax = x.Variable }
                .WithAccess(BoundAccess.Write);
        }

        public BoundItemsBag<BoundExpression> HandleExpression(AST.Expression expr, BoundAccess access) 
            => new BoundItemsBag<BoundExpression>(BindExpression(expr, access));

        BoundExpression BindExpression(AST.Expression expr, BoundAccess access)
        {
            var bound = BindExpressionCore(expr, access);
            bound.PhpSyntax = expr;

            return bound;
        }

        BoundExpression BindExpressionCore(AST.Expression expr, BoundAccess access)
        {
            Debug.Assert(expr != null);

            if (expr is AST.Literal literal) return BindLiteral(literal).WithAccess(access);
            if (expr is AST.ConstantUse constUse) return BindConstUse(constUse).WithAccess(access);
            if (expr is AST.VarLikeConstructUse varLikeConstructUse) return BindVarLikeConstructUse(varLikeConstructUse, access);
            if (expr is AST.BinaryEx binEx) return BindBinaryEx(binEx).WithAccess(access);
            if (expr is AST.AssignEx assignEx) return BindAssignEx(assignEx, access);
            if (expr is AST.UnaryEx unaryEx) return BindUnaryEx(unaryEx, access);
            if (expr is AST.IncDecEx incDecEx) return BindIncDec(incDecEx).WithAccess(access);
            if (expr is AST.ConditionalEx condEx) return BindConditionalEx(condEx).WithAccess(access);
            if (expr is AST.ConcatEx concatEx) return BindConcatEx(concatEx).WithAccess(access);
            if (expr is AST.IncludingEx includingEx) return BindIncludeEx(includingEx).WithAccess(access);
            if (expr is AST.InstanceOfEx instanceOfEx) return BindInstanceOfEx(instanceOfEx).WithAccess(access);
            if (expr is AST.PseudoConstUse pseudoConstUse) return BindPseudoConst(pseudoConstUse).WithAccess(access);
            if (expr is AST.IssetEx issetEx) return BindIsSet(issetEx).WithAccess(access);
            if (expr is AST.ExitEx exitEx) return BindExitEx(exitEx).WithAccess(access);
            if (expr is AST.EmptyEx emptyEx) return BindIsEmptyEx(emptyEx).WithAccess(access);
            if (expr is AST.LambdaFunctionExpr lambFuncEx) return BindLambda(lambFuncEx).WithAccess(access);
            if (expr is AST.EvalEx evalEx) return BindEval(evalEx).WithAccess(access);
            if (expr is AST.YieldEx yieldEx) return BindYieldEx(yieldEx).WithAccess(access);

            //
            _diagnostics.Add(_locals.Routine, expr, Errors.ErrorCode.ERR_NotYetImplemented, $"Expression of type '{expr.GetType().Name}'");
            return new BoundLiteral(null);
        }

        BoundYieldEx BindYieldEx(AST.YieldEx expr)
        {
            // Reference: https://github.com/dotnet/roslyn/blob/05d923831e1bc2a88918a2073fba25ab060dda0c/src/Compilers/CSharp/Portable/Binder/Binder_Statements.cs#L194

            // TODO: Throw error when trying to iterate a non-reference generator by reference 
            var access = _locals.Routine.SyntaxSignature.AliasReturn
                    ? BoundAccess.ReadRef
                    : BoundAccess.Read;

            var boundValueExpr = (expr.ValueExpr != null) ? BindExpression(expr.ValueExpr, access) : null;
            var boundKeyExpr = (expr.KeyExpr != null) ? BindExpression(expr.KeyExpr) : null;

            var boundYieldEx = new BoundYieldEx(boundValueExpr, boundKeyExpr);
            _yields.Add(boundYieldEx);
            return boundYieldEx;
        }

        BoundLambda BindLambda(AST.LambdaFunctionExpr expr)
        {
            // Syntax is bound by caller, needed to resolve lambda symbol in analysis
            return new BoundLambda(BindLambdaUseArguments(expr.UseParams));
        }

        BoundExpression BindEval(AST.EvalEx expr)
        {
            return new BoundEvalEx(BindExpression(expr.Code));
        }

        BoundExpression BindConstUse(AST.ConstantUse x)
        {
            if (x is AST.GlobalConstUse)
            {
                return BindGlobalConstUse((AST.GlobalConstUse)x);
            }

            if (x is AST.ClassConstUse)
            {
                var cx = (AST.ClassConstUse)x;
                var typeref = BindTypeRef(cx.TargetType);

                if (cx.Name.Equals("class"))   // pseudo class constant
                {
                    return new BoundPseudoClassConst(typeref, AST.PseudoClassConstUse.Types.Class);
                }

                return BoundFieldRef.CreateClassConst(typeref, new BoundVariableName(cx.Name));
            }

            throw ExceptionUtilities.UnexpectedValue(x);
        }

        BoundExpression BindExitEx(AST.ExitEx x)
        {
            return (x.ResulExpr != null)
                ? new BoundExitEx(BindExpression(x.ResulExpr))
                : new BoundExitEx();
        }

        BoundExpression BindIsEmptyEx(AST.EmptyEx x)
        {
            return new BoundIsEmptyEx(BindExpression(x.Expression, BoundAccess.Read.WithQuiet()));
        }

        BoundExpression BindIsSet(AST.IssetEx x)
        {
            return new BoundIsSetEx(x.VarList.Select(v => (BoundReferenceExpression)BindExpression(v, BoundAccess.Read.WithQuiet())).ToImmutableArray());
        }

        BoundExpression BindPseudoConst(AST.PseudoConstUse x) => new BoundPseudoConst(x.Type);

        BoundExpression BindInstanceOfEx(AST.InstanceOfEx x)
        {
            return new BoundInstanceOfEx(BindExpression(x.Expression, BoundAccess.Read), BindTypeRef(x.ClassNameRef));
        }

        BoundExpression BindIncludeEx(AST.IncludingEx x)
        {
            return new BoundIncludeEx(BindExpression(x.Target, BoundAccess.Read), x.InclusionType);
        }

        BoundExpression BindConcatEx(AST.ConcatEx x) => BindConcatEx(x.Expressions);

        BoundExpression BindConcatEx(AST.Expression[] args)
        {
            // bind expressions to bound arguments
            var boundargs = new List<BoundArgument>(BindArguments(args));

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

        BoundRoutineCall BindFunctionCall(AST.FunctionCall x, BoundAccess access)
        {
            if (!access.IsRead && !access.IsNone)
            {
                throw new NotSupportedException();
            }

            var boundinstance = (x.IsMemberOf != null) ? BindExpression(x.IsMemberOf, BoundAccess.Read/*Object?*/) : null;
            var boundargs = BindArguments(x.CallSignature.Parameters);

            if (x is AST.DirectFcnCall dirFcnCall)
            {
                var f = dirFcnCall;
                var fname = f.FullName;

                if (f.IsMemberOf == null)
                {
                    return new BoundGlobalFunctionCall(fname.Name, fname.FallbackName, boundargs)
                        .WithAccess(access);
                }
                else
                {
                    Debug.Assert(fname.FallbackName.HasValue == false);
                    Debug.Assert(fname.Name.QualifiedName.IsSimpleName);
                    return new BoundInstanceFunctionCall(boundinstance, fname.Name, boundargs)
                        .WithAccess(access);
                }
            }
            else if (x is AST.IndirectFcnCall indFcnCall)
            {
                var f = indFcnCall;
                var nameExpr = BindExpression(f.NameExpr);
                return ((f.IsMemberOf == null)
                    ? (BoundRoutineCall)new BoundGlobalFunctionCall(nameExpr, boundargs)
                    : new BoundInstanceFunctionCall(boundinstance, new BoundUnaryEx(nameExpr, AST.Operations.StringCast), boundargs))
                        .WithAccess(access);
            }
            else if (x is AST.StaticMtdCall staticMtdCall)
            {
                var f = staticMtdCall;
                Debug.Assert(f.IsMemberOf == null);

                var boundname = (f is AST.DirectStMtdCall)
                    ? new BoundRoutineName(new QualifiedName(((AST.DirectStMtdCall)f).MethodName))
                    : new BoundRoutineName(new BoundUnaryEx(BindExpression(((AST.IndirectStMtdCall)f).MethodNameVar), AST.Operations.StringCast));

                return new BoundStaticFunctionCall(BindTypeRef(f.TargetType), boundname, boundargs)
                    .WithAccess(access);
            }

            //
            throw new NotImplementedException(x.GetType().FullName);
        }

        public BoundTypeRef BindTypeRef(AST.TypeRef tref)
        {
            var bound = new BoundTypeRef(tref);

            if (tref is AST.IndirectTypeRef)
            {
                bound.TypeExpression = BindExpression(((AST.IndirectTypeRef)tref).ClassNameVar);
            }

            return bound;
        }

        BoundExpression BindConditionalEx(AST.ConditionalEx expr)
        {
            return new BoundConditionalEx(
                BindExpression(expr.CondExpr),
                (expr.TrueExpr != null) ? BindExpression(expr.TrueExpr) : null,
                BindExpression(expr.FalseExpr));
        }

        BoundExpression BindIncDec(AST.IncDecEx expr)
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

        BoundExpression BindVarLikeConstructUse(AST.VarLikeConstructUse expr, BoundAccess access)
        {
            if (expr is AST.SimpleVarUse simpleVarUse) return BindSimpleVarUse(simpleVarUse, access);
            if (expr is AST.FunctionCall funcCall) return BindFunctionCall(funcCall, access);
            if (expr is AST.NewEx newEx) return BindNew(newEx, access);
            if (expr is AST.ArrayEx arrayEx) return BindArrayEx(arrayEx, access);
            if (expr is AST.ItemUse itemUse) return BindItemUse(itemUse, access);
            if (expr is AST.StaticFieldUse staticFieldUse) return BindFieldUse(staticFieldUse, access);
            if (expr is AST.ListEx listEx) return BindListEx(listEx).WithAccess(access);

            throw new NotImplementedException(expr.GetType().FullName);
        }

        BoundExpression BindNew(AST.NewEx x, BoundAccess access)
        {
            Debug.Assert(access.IsRead || access.IsReadRef || access.IsNone);

            return new BoundNewEx(BindTypeRef(x.ClassNameRef), BindArguments(x.CallSignature.Parameters))
                .WithAccess(access);
        }

        BoundExpression BindArrayEx(AST.ArrayEx x, BoundAccess access)
        {
            Debug.Assert(access.IsRead);

            if (access.IsReadRef)
            {
                // TODO: warninng deprecated
            }

            return new BoundArrayEx(BindArrayItems(x.Items)) { PhpSyntax = x }.WithAccess(access);
        }

        IEnumerable<KeyValuePair<BoundExpression, BoundExpression>> BindArrayItems(AST.Item[] items)
        {
            foreach (var x in items)
            {
                Debug.Assert(x is AST.RefItem || x is AST.ValueItem);

                var boundIndex = (x.Index != null) ? BindExpression(x.Index, BoundAccess.Read) : null;
                var boundValue = (x is AST.RefItem refItem)
                    ? BindExpression(refItem.RefToGet, BoundAccess.ReadRef)
                    : BindExpression(((AST.ValueItem)x).ValueExpr, BoundAccess.Read);

                yield return new KeyValuePair<BoundExpression, BoundExpression>(boundIndex, boundValue);
            }
        }

        BoundExpression BindItemUse(AST.ItemUse x, BoundAccess access)
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

        BoundExpression BindSimpleVarUse(AST.SimpleVarUse expr, BoundAccess access)
        {
            var dexpr = expr as AST.DirectVarUse;
            var iexpr = expr as AST.IndirectVarUse;

            Debug.Assert(dexpr != null || iexpr != null);

            var varname = (dexpr != null)
                ? new BoundVariableName(dexpr.VarName)
                : new BoundVariableName(BindExpression(iexpr.VarNameEx));

            if (expr.IsMemberOf == null)
            {
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

        BoundExpression BindFieldUse(AST.StaticFieldUse x, BoundAccess access)
        {
            var typeref = BindTypeRef(x.TargetType);
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

        BoundExpression BindGlobalConstUse(AST.GlobalConstUse expr)
        {
            // translate built-in constants directly
            if (expr.Name == QualifiedName.True) return new BoundLiteral(true);
            if (expr.Name == QualifiedName.False) return new BoundLiteral(false);
            if (expr.Name == QualifiedName.Null) return new BoundLiteral(null);

            // bind constant
            return new BoundGlobalConst(expr.Name.ToString());
        }

        BoundExpression BindBinaryEx(AST.BinaryEx expr)
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

        BoundExpression BindUnaryEx(AST.UnaryEx expr, BoundAccess access)
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

        BoundExpression BindAssignEx(AST.AssignEx expr, BoundAccess access)
        {
            var target = (BoundReferenceExpression)BindExpression(expr.LValue, BoundAccess.Write);
            BoundExpression value;

            // bind value (read as value or as ref)
            if (expr is AST.ValueAssignEx assignEx)
            {
                value = BindExpression(assignEx.RValue, BoundAccess.Read.WithReadCopy());
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
                target.Access = target.Access.WithRead();   // Read & Write on target

                return new BoundCompoundAssignEx(target, value, expr.Operation).WithAccess(access);
            }
        }

        BoundExpression BindListEx(AST.ListEx expr)
        {
            var vars = expr.Items
                .Select(lval => (lval != null) ? (BoundReferenceExpression)BindExpression(((AST.ValueItem)lval).ValueExpr, BoundAccess.Write) : null)
                .ToArray();

            return new BoundListEx(vars).WithAccess(BoundAccess.Write);
        }

        public BoundStatement BindEmptyStmt(Span span)
        {
            return new BoundEmptyStatement(span.ToTextSpan());
        }

        static BoundExpression BindLiteral(AST.Literal expr)
        {
            if (expr is AST.LongIntLiteral longIntLit) return new BoundLiteral(longIntLit.Value);
            if (expr is AST.StringLiteral stringLit) return new BoundLiteral(stringLit.Value);
            if (expr is AST.DoubleLiteral doubleLit) return new BoundLiteral(doubleLit.Value);
            if (expr is AST.BoolLiteral boolLit) return new BoundLiteral(boolLit.Value);
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

        static void BindEnsureAccess(BoundExpression expr)
        {
            if (expr is BoundArrayItemEx)
            {
                var arritem = (BoundArrayItemEx)expr;
                arritem.Array.Access = arritem.Array.Access.WithEnsureArray();
                BindEnsureAccess(arritem.Array);
            }
        }
    }
}
