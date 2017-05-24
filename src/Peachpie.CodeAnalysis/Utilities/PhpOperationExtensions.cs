namespace Pchp.CodeAnalysis.Semantics
{
    public static class PhpOperationExtensions
    {
        #region ExpressionExtensions

        /// <summary>
        /// Returns whether the expression has constant value.
        /// </summary>
        public static bool IsConstant(this BoundExpression expr) => expr.ConstantValue.HasValue;
        #endregion    
    }
}
