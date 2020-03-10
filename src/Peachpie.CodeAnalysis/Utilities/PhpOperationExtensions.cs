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

        /// <summary>
        /// Gets the <see cref="BoundAccess"/> for writing operation.
        /// </summary>
        public static BoundAccess TargetAccess(this BoundReferenceExpression target)
        {
            var access = target.Access;

            Debug.Assert(access.IsWrite | access.IsUnset);

            // IsNotRef:
            if (target is BoundVariableRef varref)
            {
                var mightBeRef = varref.BeforeTypeRef.IsRef || !varref.Name.IsDirect;
                access = access.WithRefFlag(mightBeRef);
            }
            
            //
            return access;
        }
    }
}
