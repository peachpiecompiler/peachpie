namespace Pchp.CodeAnalysis.Semantics
{
    public static class PhpOperationExtensions
    {
        #region ExpressionExtensions
        /// <summary>
        /// Returns whether the expression is safe to be evaluated multiple times i.e it has no side effects.
        /// </summary>
        public static bool IsSafeToEvalMultipleTimes(this BoundExpression expr)
            => expr == null || expr is BoundReferenceExpression || expr.IsConstant();


        /// <summary>
        /// Returns whether the expression has constant value.
        /// </summary>
        public static bool IsConstant(this BoundExpression expr) => expr.ConstantValue.HasValue;
        #endregion    
    }
}
