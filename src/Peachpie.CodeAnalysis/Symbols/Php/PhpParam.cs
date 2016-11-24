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
        public int Index;
        public TypeRefMask Type;
        public bool IsVariadic;

        /// <summary>
        /// Whether the parameter is passed as a PHP alias (<see cref="Core.PhpAlias"/>)..
        /// </summary>
        public bool IsAlias => Type.IsRef;
        public BoundExpression DefaultValue;

        /// <summary>
        /// Whether the parameter is passed as CLR <c>ref</c> or <c>out</c>.
        /// </summary>
        public bool IsByRef;

        public PhpParam(int index, TypeRefMask tmask, bool isByRef, bool isVariadic, BoundExpression defaultValue)
        {
            this.Index = index;
            this.Type = tmask;
            this.IsVariadic = isVariadic;
            this.DefaultValue = defaultValue;
            this.IsByRef = isByRef;
        }
    }
}
