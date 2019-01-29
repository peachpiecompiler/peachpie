using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.Semantics
{
    static class ExpressionsExtension
    {
        /// <summary>
        /// If expr is of type <typeparamref name="T"/> or it is a <see cref="BoundCopyValue" /> enclosing an expression
        /// of type <typeparamref name="T"/>, store the expression to <paramref name="valueExpr"/> and return true; otherwise,
        /// return false. Store to <paramref name="isCopied"/> whether <paramref name="valueExpr"/> was enclosed in
        /// <see cref="BoundCopyValue"/>.
        /// </summary>
        public static bool MatchTypeSkipCopy<T>(this BoundExpression expr, out T valueExpr, out bool isCopied) where T : BoundExpression
        {
            if (expr is T res)
            {
                valueExpr = res;
                isCopied = false;
                return true;
            }
            else if (expr is BoundCopyValue copyVal && copyVal.Expression is T copiedRes)
            {
                valueExpr = copiedRes;
                isCopied = true;
                return true;
            }

            valueExpr = default;
            isCopied = default;
            return false;
        }

        public static T WithAccess<T>(this T expr, BoundAccess access) where T : BoundExpression
        {
            expr.Access = access;
            return expr;
        }

        public static T WithAccess<T>(this T expr, BoundExpression other) where T : BoundExpression => WithAccess(expr, other.Access);

        public static T WithSyntax<T>(this T expr, LangElement syntax) where T : IPhpOperation
        {
            expr.PhpSyntax = syntax;
            return expr;
        }

        /// <summary>
        /// Copies semantic information (<see cref="BoundExpression.Access"/>, <see cref="BoundExpression.PhpSyntax"/>) from another expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static T WithContext<T>(this T expr, BoundExpression other) where T : BoundExpression
        {
            expr.Access = other.Access;
            expr.PhpSyntax = other.PhpSyntax;

            // expr.TypeRefMask has to be set by the type analysis
            // expr.ConstantValue the same

            return expr;
        }

        /// <summary>
        /// Gets value indicating the object will be an empty string after converting to string.
        /// </summary>
        public static bool IsEmptyStringValue(object value)
        {
            if (value == null)
                return true;

            if (value is string str && str.Length == 0)
                return true;

            if (value is bool b && b == false)
                return true;

            return false;
        }
    }
}
