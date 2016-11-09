using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    public struct PhpParam
    {
        public TypeRefMask Type;
        public bool IsVariadic;
        public bool IsByRef => Type.IsRef;
        public BoundExpression DefaultValue;

        public PhpParam(TypeRefMask tmask, bool isVariadic, BoundExpression defaultValue)
        {
            this.Type = tmask;
            this.IsVariadic = isVariadic;
            this.DefaultValue = defaultValue;
        }
    }
}
