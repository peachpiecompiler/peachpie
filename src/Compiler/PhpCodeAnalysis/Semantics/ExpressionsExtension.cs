using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    static class ExpressionsExtension
    {
        public static T WithAccess<T>(this T expr, BoundAccess access) where T : BoundExpression
        {
            expr.Access = access;
            return expr;
        }
    }
}
