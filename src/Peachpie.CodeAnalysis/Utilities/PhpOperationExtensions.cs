namespace Pchp.CodeAnalysis.Semantics
{
    public static class PhpOperationExtensions
    {
        #region ExpressionExtensions
        /// <summary>
        /// Returns whether the expression is safe to be evaluated multiple times i.e it has no side effects.
        /// </summary>
        public static bool IsSafeToEvalMultipleTimes(this BoundExpression expr)
        {
            return expr is BoundReferenceExpression || expr.ConstantValue.HasValue;
        }
        #endregion    
    }
}
