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
        internal ParameterSymbol ParameterSymbol { get; }

        public bool IsByValue => !IsPhpRw && !IsByRef && !IsAlias;

        /// <summary>
        /// Whether the parameter is passed as a PHP alias (<c>PhpAlias</c>).
        /// </summary>
        public bool IsAlias => Type.IsRef;

        public BoundExpression DefaultValue;

        /// <summary>
        /// Whether the parameter is passed as CLR <c>ref</c> or <c>out</c>.
        /// </summary>
        public bool IsByRef;

        /// <summary>
        /// Whether the parameter is annotated with <c>PhpRwAttribute</c>.
        /// </summary>
        public bool IsPhpRw;

        internal PhpParam(ParameterSymbol psymbol, int index, TypeRefMask tmask)
        {
            this.ParameterSymbol = psymbol;
            this.Index = index;
            this.Type = tmask;
            this.IsVariadic = psymbol.IsParams;
            this.DefaultValue = psymbol.Initializer;
            this.IsByRef = psymbol.RefKind != Microsoft.CodeAnalysis.RefKind.None;
            this.IsPhpRw = psymbol.IsPhpRw;
        }
    }
}
