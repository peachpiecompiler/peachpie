using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics
{
    public static class PhpOperationExtensions
    {
        /// <summary>
        /// Returns whether the expression has constant value.
        /// </summary>
        public static bool IsConstant(this BoundExpression expr) => expr.ConstantValue.HasValue;

        /// <summary>
        /// Gets value indicating the expression is a logic negation.
        /// </summary>
        public static bool IsLogicNegation(this BoundExpression expr, out BoundExpression operand)
        {
            if (expr is BoundUnaryEx unary && unary.Operation == Operations.LogicNegation)
            {
                operand = unary.Operand;
                return true;
            }
            else
            {
                operand = null;
                return false;
            }
        }
    }
}
