namespace Pchp.CodeAnalysis.Semantics
{
    public static class PhpOperationExtensions
    {
        #region ExpressionExtensions

        /// <summary>
        /// Returns whether the expression has constant value.
        /// </summary>
        public static bool IsConstant(this BoundExpression expr) => expr.ConstantValue.HasValue;

        /// <summary>
        /// Copies context information (type mask, access, constant value) from another expression.
        /// </summary>
        /// <returns>The given expression, to enable chaining.</returns>
        public static BoundExpression CopyContextFrom(this BoundExpression expr, BoundExpression other)
        {
            expr.TypeRefMask = other.TypeRefMask;
            expr.Access = other.Access;
            expr.ConstantValue = other.ConstantValue;

            return expr;
        }

        #endregion
    }
}
