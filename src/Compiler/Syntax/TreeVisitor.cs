using System;
using System.Collections.Generic;

namespace Pchp.Syntax.AST
{
    /// <summary>
    /// Class visits recursively each AstNode 
    /// </summary>
    public class TreeVisitor
    {
        /// <summary>
        /// Visit language element and all children recursively.
        /// Depth-first search.
        /// </summary>
        /// <param name="element">Any LanguageElement. Can be null.</param>
        public virtual void VisitElement(LangElement element)
        {
            if ( element != null )
                element.VisitMe(this);
        }

        /// <summary>
        /// Visit global scope element and all children.
        /// </summary>
        /// <param name="x">GlobalCode.</param>
        public virtual void VisitGlobalCode(GlobalCode x)
        {
            VisitStatements(x.Statements);
        }

        #region Statements

        /// <summary>
        /// Visit statements and catches.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitTryStmt(TryStmt x)
        {
            // visit statements
            VisitStatements(x.Statements);

            // visit catch blocks
            if (x.Catches != null)
                foreach (CatchItem c in x.Catches)
                    VisitElement(c);

            // visit finally block
            VisitElement(x.FinallyItem);
        }

        /// <summary>
        /// Visit throw expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitThrowStmt(ThrowStmt x)
        {
            VisitElement(x.Expression);
        }

        /// <summary>
        /// Visit namespace statements.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitNamespaceDecl(NamespaceDecl x)
        {
            VisitStatements(x.Statements);
        }

        /// <summary>
        /// Visit constant declarations.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitGlobalConstDeclList(GlobalConstDeclList x)
        {
            foreach (GlobalConstantDecl c in x.Constants)
                VisitElement(c);
        }

        virtual public void VisitGlobalConstantDecl(GlobalConstantDecl x)
        {
            // nothing
        }

        /// <summary>
        /// Visit statements in given Block Statement.
        /// </summary>
        /// <param name="x">Block statement.</param>
        virtual public void VisitBlockStmt(BlockStmt x)
        {
            VisitStatements(x.Statements);
        }

        /// <summary>
        /// Visit expression in given expression statement.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitExpressionStmt(ExpressionStmt x)
        {
            VisitElement(x.Expression);
        }

        virtual public void VisitEmptyStmt(EmptyStmt x)
        {
            // nothing
        }

        /// <summary>
        /// Visit each VariableUse in unset variable list.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitUnsetStmt(UnsetStmt x)
        {
            foreach (VariableUse v in x.VarList)
                VisitElement(v);
        }

        /// <summary>
        /// Visit each SimpleVarUse in global variable list. 
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitGlobalStmt(GlobalStmt x)
        {
            foreach (SimpleVarUse v in x.VarList)
                VisitElement(v);
        }

        /// <summary>
        /// Visit each StaticVarDecl in static variable list.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitStaticStmt(StaticStmt x)
        {
            foreach (StaticVarDecl v in x.StVarList)
                VisitElement(v);
        }

        /// <summary>
        /// Visits <c>declare</c> statement and its inner statement.
        /// </summary>
        virtual public void VisitDeclareStmt(DeclareStmt x)
        {
            VisitElement(x.Statement);
        }

        /// <summary>
        /// Visit all conditional statements.
        /// See VisitConditionalStmt(ConditionalStmt x).
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitIfStmt(IfStmt x)
        {
            foreach (ConditionalStmt c in x.Conditions)
                VisitConditionalStmt(c);
        }

        /// <summary>
        /// Visit condition (if ConditionalStmt does not represent else).
        /// Visit statements in ConditionalStmt.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitConditionalStmt(ConditionalStmt x)
        {
            if (x.Condition != null) VisitElement(x.Condition);

            VisitElement(x.Statement);
        }

        /// <summary>
        /// Visit type members.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitTypeDecl(TypeDecl x)
        {
            foreach (TypeMemberDecl t in x.Members)
                VisitElement(t);
        }

        /// <summary>
        /// Visit method parameters and method body.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitMethodDecl(MethodDecl x)
        {
            // method parameters
            foreach (FormalParam p in x.Signature.FormalParams)
                VisitElement(p);
            
            // method body
            VisitStatements(x.Body);
        }

        /// <summary>
        /// Visit each FieldDecl in the given FieldDeclList.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitFieldDeclList(FieldDeclList x)
        {
            foreach (FieldDecl f in x.Fields)
                VisitElement(f);
        }

        /// <summary>
        /// Visit FieldDecl initializer expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitFieldDecl(FieldDecl x)
        {
            VisitElement(x.Initializer);
        }

        /// <summary>
        /// Visit each ClassConstantDecl in ConstDeclList.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitConstDeclList(ConstDeclList x)
        {
            foreach (ClassConstantDecl c in x.Constants)
                VisitElement(c);
        }

        /// <summary>
        /// Visit given constant and constant initializer expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitClassConstantDecl(ClassConstantDecl x)
        {
            VisitConstantDecl(x);
        }

        /// <summary>
        /// Visit constant initializer expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitConstantDecl(ConstantDecl x)
        {
            VisitElement(x.Initializer);
        }

        /// <summary>
        /// Visit function parameters and function body.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitFunctionDecl(FunctionDecl x)
        {
            // function parameters
            foreach (FormalParam p in x.Signature.FormalParams)
                VisitElement(p);

            // function body
            VisitStatements(x.Body);
        }

        virtual public void VisitTraitsUse(TraitsUse x)
        {
            // visits adaptation list
            var list = x.TraitAdaptationList;
            if (list != null && list.Any())
                foreach(Pchp.Syntax.AST.TraitsUse.TraitAdaptation t in list)
                    VisitElement(t);
        }

        virtual public void VisitTraitAdaptationPrecedence(TraitsUse.TraitAdaptationPrecedence x)
        {

        }

        virtual public void VisitTraitAdaptationAlias(TraitsUse.TraitAdaptationAlias x)
        {

        }

        /// <summary>
        /// Visit expressions in echo statement.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitEchoStmt(EchoStmt x)
        {
            VisitExpressions(x.Parameters);
        }

        /// <summary>
        /// Visit all statements in the given list.
        /// </summary>
        /// <param name="statements">Collection of statements to visit.</param>
        private void VisitStatements(IList<Statement> statements)
        {
            if (statements != null)
                foreach (Statement s in statements)
                    VisitElement(s);
        }

        /// <summary>
        /// Visit all expressions in the given list.
        /// </summary>
        /// <param name="expressions"></param>
        private void VisitExpressions(ICollection<Expression> expressions)
        {
            if (expressions != null && expressions.Any())
                foreach (Expression e in expressions)
                    VisitElement(e);
        }

        #endregion

        #region Switch statement

        /// <summary>
        /// Visit switch value and switch items.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitSwitchStmt(SwitchStmt x)
        {
            VisitElement(x.SwitchValue);

            foreach (SwitchItem item in x.SwitchItems)
                VisitElement(item);
        }

        /// <summary>
        /// Visit switch-case item.
        /// Case expression and case body.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitCaseItem(CaseItem x)
        {
            VisitElement(x.CaseVal);

            VisitSwitchItem(x);
        }

        /// <summary>
        /// Visit switch-default item.
        /// Visit case body.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitDefaultItem(DefaultItem x)
        {
            VisitSwitchItem(x);
        }

        /// <summary>
        /// Called by derived objects visitor (CaseItem and DefaultItem).
        /// Visit all statements in SwitchItem.
        /// </summary>
        /// <param name="x">SwitchItem, CaseItem or DefaultItem.</param>
        virtual public void VisitSwitchItem(SwitchItem x)
        {
            VisitStatements(x.Statements);
        }

        #endregion

        #region Jumps statements

        virtual public void VisitJumpStmt(JumpStmt x)
        {
            VisitElement(x.Expression);
        }
        virtual public void VisitGotoStmt(GotoStmt x)
        {
            // x.LabelName
        }
        virtual public void VisitLabelStmt(LabelStmt x)
        {
            // x.Name
        }
        
        #endregion

        #region  Cycle statements

        /// <summary>
        /// Visit cycle condition expression and cycle body.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitWhileStmt(WhileStmt x)
        {
            VisitElement(x.CondExpr);
            VisitElement(x.Body);
        }

        /// <summary>
        /// Visit "for" initialization,condition and action expressions.
        /// Visit "for" body.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitForStmt(ForStmt x)
        {
            VisitExpressions(x.InitExList);
            VisitExpressions(x.CondExList);
            VisitExpressions(x.ActionExList);

            VisitElement(x.Body);
        }

        /// <summary>
        /// Visit enumeree and body.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitForeachStmt(ForeachStmt x)
        {
            VisitElement(x.Enumeree);
            VisitElement(x.Body);
        }

        #endregion

        #region Expressions

        /*/// <summary>
        /// Called when derived class visited.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitVarLikeConstructUse(VarLikeConstructUse x)
        {
            // base for variable use
        }*/

        /// <summary>
        /// Called when derived class visited.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitConstantUse(ConstantUse x)
        {
            // base for constant use
        }

        virtual public void VisitDirectVarUse(DirectVarUse x)
        {
            VisitVarLikeConstructUse(x);
        }
        virtual public void VisitGlobalConstUse(GlobalConstUse x)
        {
            VisitConstantUse(x);
        }
        virtual public void VisitClassConstUse(ClassConstUse x)
        {
            VisitElement(x.TypeRef);
            VisitConstantUse(x);
        }
        virtual public void VisitPseudoClassConstUse(PseudoClassConstUse x)
        {
            VisitClassConstUse(x);
        }
        virtual public void VisitPseudoConstUse(PseudoConstUse x)
        {
            // nothing
        }
        virtual public void VisitIndirectVarUse(IndirectVarUse x)
        {
            VisitVarLikeConstructUse(x);
            VisitElement(x.VarNameEx);
        }

        virtual public void VisitVarLikeConstructUse(VarLikeConstructUse x)
        {
            VisitElement(x.IsMemberOf);
        }

        /// <summary>
        /// Visit include target.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitIncludingEx(IncludingEx x)
        {
            VisitElement(x.Target);
        }

        /// <summary>
        /// Visit each VariableUse in isset variable list.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitIssetEx(IssetEx x)
        {
            foreach (VariableUse v in x.VarList)
                VisitElement(v);
        }

        /// <summary>
        /// Visit parameter of "empty".
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitEmptyEx(EmptyEx x)
        {
            VisitElement(x.Expression);
        }

        /// <summary>
        /// Visit parameter of "eval".
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitEvalEx(EvalEx x)
        {
            VisitElement(x.Code);
        }

        /// <summary>
        /// Visit exit expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitExitEx(ExitEx x)
        {
            VisitElement(x.ResulExpr);
        }

        /// <summary>
        /// Visit left and right expressions of binary expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitBinaryEx(BinaryEx x)
        {
            VisitElement(x.LeftExpr);
            VisitElement(x.RightExpr);
        }

        /// <summary>
        /// Visit shell command expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitShellEx(ShellEx x)
        {
            VisitElement(x.Command);
        }

        /// <summary>
        /// Visit item use index (if not null) and array.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitItemUse(ItemUse x) 
        {
            VisitElement(x.Index);
            VisitElement(x.Array);

            VisitVarLikeConstructUse(x);
        }

        /// <summary>
        /// Visits string literal dereferencing.
        /// </summary>
        virtual public void VisitStringLiteralDereferenceEx(StringLiteralDereferenceEx x)
        {
            VisitElement(x.StringExpr);
            VisitElement(x.KeyExpr);
        }

        /// <summary>
        /// Called when derived class visited.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitFunctionCall(FunctionCall x)
        {
            VisitVarLikeConstructUse(x);

            foreach (ActualParam p in x.CallSignature.Parameters)
                VisitElement(p);
        }

        /// <summary>
        /// Visit function call actual parameters.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitDirectFcnCall(DirectFcnCall x)
        {
            VisitFunctionCall(x);
        }

        /// <summary>
        /// Visit name expression and actual parameters.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitIndirectFcnCall(IndirectFcnCall x)
        {
            VisitElement(x.NameExpr);
            VisitFunctionCall(x);
        }

        /// <summary>
        /// Visit function call actual parameters.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitDirectStMtdCall(DirectStMtdCall x)
        {
            VisitElement(x.TypeRef);            
            VisitFunctionCall(x);
        }

        /// <summary>
        /// Visit name expression and method call actual parameters.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitIndirectStMtdCall(IndirectStMtdCall x)
        {
            VisitElement(x.TypeRef);            
            VisitElement(x.MethodNameVar);
            VisitFunctionCall(x);
        }
        virtual public void VisitDirectStFldUse(DirectStFldUse x)
        {
            VisitElement(x.TypeRef);
        }
        virtual public void VisitIndirectStFldUse(IndirectStFldUse x)
        {
            VisitElement(x.TypeRef);            
            VisitElement(x.FieldNameExpr);
        }

        /// <summary>
        /// Visit new array items initializers.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitArrayEx(ArrayEx x)
        {
            foreach (Item item in x.Items)
                VisitArrayItem(item);
        }

        virtual public void VisitArrayItem(Item item)
        {
            // key
            VisitElement(item.Index);

            // value
            if (item is ValueItem)
                VisitElement(((ValueItem)item).ValueExpr);
            else
                VisitElement(((RefItem)item).RefToGet);
        }

        /// <summary>
        /// Visit conditions.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitConditionalEx(ConditionalEx x)
        {
            VisitElement(x.CondExpr);
            VisitElement(x.TrueExpr);
            VisitElement(x.FalseExpr);
        }

        /// <summary>
        /// Visit variable that is incremented (or decremented).
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitIncDecEx(IncDecEx x)
        {
            VisitElement(x.Variable);
        }

        /// <summary>
        /// Visit l-value of assignment.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitAssignEx(AssignEx x)
        {
            VisitElement(x.LValue);
        }

        /// <summary>
        /// Visit left and right values in assignment.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitValueAssignEx(ValueAssignEx x)
        {
            VisitAssignEx(x);
            VisitElement(x.RValue);
        }

        /// <summary>
        /// Visit left and right values in ref assignment.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitRefAssignEx(RefAssignEx x)
        {
            VisitAssignEx(x);
            VisitElement(x.RValue);
        }

        /// <summary>
        /// Visit unary expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitUnaryEx(UnaryEx x)
        {
            VisitElement(x.Expr);
        }

        /// <summary>
        /// Visit "new" call parameters.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitNewEx(NewEx x)
        {
            VisitElement(x.ClassNameRef);

            foreach (ActualParam p in x.CallSignature.Parameters)
                VisitElement(p);
        }

        /// <summary>
        /// Visit instanceof expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitInstanceOfEx(InstanceOfEx x)
        {
            VisitElement(x.Expression);
            VisitElement(x.ClassNameRef);
        }

        /// <summary>
        /// Visit typeof ClassNameRef expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitTypeOfEx(TypeOfEx x)
        {
            VisitElement(x.ClassNameRef);
        }

        /// <summary>
        /// Visit expressions in PHP concat.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitConcatEx(ConcatEx x)
        {
            VisitExpressions(x.Expressions);
        }

        /// <summary>
        /// Visit list initializer expressions and r-value (if not null)
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitListEx(ListEx x)
        {
            VisitExpressions(x.LValues);
            VisitElement(x.RValue);
        }

        /// <summary>
        /// Visit <see cref="LambdaFunctionExpr"/> expression.
        /// </summary>
        virtual public void VisitLambdaFunctionExpr(LambdaFunctionExpr x)
        {
            // use parameters
            if (x.UseParams != null)
                foreach (var p in x.UseParams)
                    VisitElement(p);

            // function parameters
            foreach (var p in x.Signature.FormalParams)
                VisitElement(p);

            // function body
            VisitStatements(x.Body);
        }

        /// <summary>
        /// Visit <see cref="YieldEx"/> expression.
        /// </summary>
        virtual public void VisitYieldEx(YieldEx x)
        {
            VisitElement(x.KeyExpr);
            VisitElement(x.ValueExpr);
        }

        #endregion

        #region Literals

        virtual public void VisitIntLiteral(IntLiteral x)
        {
            // nothing
        }

        virtual public void VisitLongIntLiteral(LongIntLiteral x)
        {
            // nothing
        }

        virtual public void VisitDoubleLiteral(DoubleLiteral x)
        {
            // nothing
        }

        virtual public void VisitStringLiteral(StringLiteral x)
        {
            // nothing
        }

        virtual public void VisitBinaryStringLiteral(BinaryStringLiteral x)
        {
            // nothing
        }

        virtual public void VisitBoolLiteral(BoolLiteral x)
        {
            // nothing
        }

        virtual public void VisitNullLiteral(NullLiteral x)
        {
            // nothing
        }

        #endregion

        #region Others

        /// <summary>
        /// Visit catch. Variable first then body statements.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitCatchItem(CatchItem x)
        {
            VisitElement(x.TypeRef);
            VisitElement(x.Variable);
            VisitStatements(x.Statements);
        }

        /// <summary>
        /// Visit <see cref="FinallyItem"/>.
        /// </summary>
        virtual public void VisitFinallyItem(FinallyItem x)
        {
            VisitStatements(x.Statements);
        }

        /// <summary>
        /// Visit static variable declaration, variable name and initializer expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitStaticVarDecl(StaticVarDecl x)
        {
            VisitElement(x.Variable);
            VisitElement(x.Initializer);
        }

        virtual public void VisitFormalTypeParam(FormalTypeParam x)
        {
            // nothing
        }

        /// <summary>
        /// Visit custom attributes NamedParameters.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitCustomAttribute(CustomAttribute x)
        {
            foreach (NamedActualParam p in x.NamedParameters)
                VisitElement(p);
        }

        /// <summary>
        /// Visit formal parameter initializer expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitFormalParam(FormalParam x)
        {
            if (x.InitValue != null)
                VisitElement(x.InitValue);
        }

        /// <summary>
        /// Visit actual parameter expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitActualParam(ActualParam x)
        {
            VisitElement(x.Expression);
        }

        /// <summary>
        /// Visit named actual parameter expression.
        /// </summary>
        /// <param name="x"></param>
        virtual public void VisitNamedActualParam(NamedActualParam x)
        {
            VisitElement(x.Expression);
        }

        virtual public void VisitPrimitiveTypeRef(PrimitiveTypeRef x)
        {
            // nothing
        }
        virtual public void VisitDirectTypeRef(DirectTypeRef x)
        {
            // nothing
        }
        virtual public void VisitIndirectTypeRef(IndirectTypeRef x)
        {
            VisitElement(x.ClassNameVar);
        }

        virtual public void VisitPHPDocStmt(PHPDocStmt x) { }

        #endregion
    }
}