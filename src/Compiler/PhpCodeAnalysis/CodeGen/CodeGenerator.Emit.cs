using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CodeGen
{
    partial class CodeGenerator
    {
        /// <summary>
        /// Emit cast from one type to another.
        /// </summary>
        public void EmitCast(INamedTypeSymbol from, INamedTypeSymbol to)
        {
            throw new NotImplementedException();
        }

        public void EmitCastToBool(TypeSymbol from)
        {
            throw new NotImplementedException();
        }

        public void EmitBranch(ILOpCode code, BoundBlock label)
        {
            IL.EmitBranch(code, label);
        }

        public void EmitOpCode(ILOpCode code) => _il.EmitOpCode(code);

        public void EmitCall(ILOpCode code, MethodSymbol method)
        {
            Debug.Assert(code == ILOpCode.Call || code == ILOpCode.Calli || code == ILOpCode.Callvirt);
            EmitOpCode(code);
            IL.EmitToken(method, /*method.Syntax*/ null, _diagnostics);
        }
    }
}
