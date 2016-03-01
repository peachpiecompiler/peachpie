using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    static class ExpressionsExtension
    {
        public static BoundExpression WithAccess(this BoundExpression expr, AccessType access)
        {
            expr.Access = access;
            return expr;
        }
    }
}
