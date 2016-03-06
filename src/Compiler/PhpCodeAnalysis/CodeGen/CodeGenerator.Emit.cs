using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
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
            if (from.SpecialType != SpecialType.System_Boolean)
            {
                throw new NotImplementedException();
            }
        }

        public void EmitBranch(ILOpCode code, BoundBlock label)
        {
            IL.EmitBranch(code, label);
        }

        public void EmitOpCode(ILOpCode code) => _il.EmitOpCode(code);

        public static int GetCallStackBehavior(BoundFunctionCall call)
        {
            int stack = 0;

            if (!call.TargetMethod.ReturnsVoid)
            {
                // The call puts the return value on the stack.
                stack += 1;
            }

            if (!call.TargetMethod.IsStatic)
            {
                // The call pops the receiver off the stack.
                stack -= 1;
            }

            if (call.TargetMethod.IsVararg)
            {
                // The call pops all the arguments, fixed and variadic.
                int fixedArgCount = call.ArgumentsInParameterOrder.Length - 1;
                int varArgCount = 0; //  ((BoundArgListOperator)call.Arguments[fixedArgCount]).Arguments.Length;
                stack -= fixedArgCount;
                stack -= varArgCount;
            }
            else
            {
                // The call pops all the arguments.
                stack -= call.ArgumentsInParameterOrder.Length;
            }

            return stack;
        }

        //public void EmitCall(ILOpCode code, MethodSymbol method)
        //{
        //    Debug.Assert(code == ILOpCode.Call || code == ILOpCode.Calli || code == ILOpCode.Callvirt);
        //    EmitOpCode(code);
        //    IL.EmitToken(method, /*method.Syntax*/ null, _diagnostics);
        //}

        internal void EmitSymbolToken(TypeSymbol symbol, SyntaxNode syntaxNode)
        {
            _il.EmitToken(_moduleBuilder.Translate(symbol, syntaxNode, _diagnostics), syntaxNode, _diagnostics);
        }

        //private void EmitSymbolToken(MethodSymbol method, SyntaxNode syntaxNode)
        //{
        //    _il.EmitToken(_moduleBuilder.Translate(method, syntaxNode, _diagnostics, null), syntaxNode, _diagnostics);
        //}

        //private void EmitSymbolToken(FieldSymbol symbol, SyntaxNode syntaxNode)
        //{
        //    _il.EmitToken(_moduleBuilder.Translate(symbol, syntaxNode, _diagnostics), syntaxNode, _diagnostics);
        //}
    }
}
