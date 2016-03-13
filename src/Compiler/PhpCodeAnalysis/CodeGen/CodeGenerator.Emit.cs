using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
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
        /// Emits <c>context</c> onto the evaluation stack.
        /// </summary>
        public void EmitLoadContext()
        {
            _contextPlace.EmitLoad(_il);
        }

        public void EmitConvertToBool(TypeSymbol from, TypeRefMask fromHint)
        {
            if (from.SpecialType != SpecialType.System_Boolean)
            {
                switch (from.SpecialType)
                {
                    case SpecialType.System_Int32: break; // nop
                    case SpecialType.System_Int64:
                        _il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                        _il.EmitOpCode(ILOpCode.Conv_i8, 0);
                        _il.EmitOpCode(ILOpCode.Cgt_un);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public void EmitConvertToBool(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);
            EmitConvertToBool(expr.Emit(this), expr.TypeRefMask);
        }

        public void EmitConvert(BoundExpression expr, TypeSymbol to)
        {
            EmitConvert(expr.Emit(this), expr.TypeRefMask, to);
        }

        public void EmitConvert(TypeSymbol from, TypeRefMask fromHint, TypeSymbol to)
        {
            Contract.ThrowIfNull(from);
            Contract.ThrowIfNull(to);

            // conversion is not needed:
            if (from.SpecialType == to.SpecialType && from.SpecialType != SpecialType.None)
                return;

            // specialized conversions:
            switch (to.SpecialType)
            {
                case SpecialType.System_Void:
                    EmitPop(from);
                    return;
                case SpecialType.System_Boolean:
                    EmitConvertToBool(from, fromHint);
                    return;
            }

            //
            throw new NotImplementedException();
        }

        public void EmitBranch(ILOpCode code, BoundBlock label) => _il.EmitBranch(code, label);

        public void EmitOpCode(ILOpCode code) => _il.EmitOpCode(code);

        public void EmitPop(TypeSymbol type)
        {
            if (type.SpecialType != SpecialType.System_Void)
            {
                _il.EmitOpCode(ILOpCode.Pop, -1);
            }
        }

        /// <summary>
        /// Emits load of a variable.
        /// </summary>
        public TypeSymbol EmitLoad(BoundVariable variable)
        {
            Contract.ThrowIfNull(variable);
            return variable.GetPlace(_il).EmitLoad(_il);
        }

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

        public void EmitBox(ITypeSymbol valuetype)
        {
            _il.EmitOpCode(ILOpCode.Box);
            EmitSymbolToken((TypeSymbol)valuetype, null);
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

        public void Emit_PhpAlias_GetValue()
        {
            // <stack>.get_Value
            _il.EmitOpCode(ILOpCode.Call, stackAdjustment: 0);
            _il.EmitToken(CoreMethods.Operators.PhpAlias_GetValue.Symbol, null, this.Diagnostics);
        }

        public void EmitEcho(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);

            // <ctx>.Echo(expr);

            this.EmitLoadContext();
            var type = expr.Emit(this);

            MethodSymbol method = null;

            switch (type.SpecialType)
            {
                case SpecialType.System_Void:
                    Debug.Assert(false);
                    EmitPop(type);
                    return;
                case SpecialType.System_String:
                    method = CoreMethods.Operators.Echo_String.Symbol;
                    break;
                case SpecialType.System_Double:
                    method = CoreMethods.Operators.Echo_Double.Symbol;
                    break;
                case SpecialType.System_Int64:
                    method = CoreMethods.Operators.Echo_Long.Symbol;
                    break;
                default:
                    if (type == CoreTypes.PhpNumber)
                    {
                        method = CoreMethods.Operators.Echo_PhpNumber.Symbol;
                    }
                    else if (type == CoreTypes.PhpValue)
                    {
                        method = CoreMethods.Operators.Echo_PhpValue.Symbol;
                    }
                    else if (type == CoreTypes.PhpAlias)
                    {
                        Emit_PhpAlias_GetValue();
                        method = CoreMethods.Operators.Echo_PhpValue.Symbol;
                    }
                    else
                    {
                        // TODO: check expr.TypeRefMask if it is only NULL
                        EmitBox(type);
                        method = CoreMethods.Operators.Echo_Object.Symbol;
                    }
                    break;
            }

            //
            _il.EmitOpCode(ILOpCode.Call, stackAdjustment: -2); // - <ctx> - <expr>
            _il.EmitToken(method, null, this.Diagnostics);
        }

        public void EmitReturnDefault()
        {
            // return default(RETURN_TYPE);

            var return_type = this.Routine.ReturnType;
            switch (return_type.SpecialType)
            {
                case SpecialType.System_Void:
                    break;
                case SpecialType.System_Double:
                    _il.EmitDoubleConstant(0.0);
                    break;
                case SpecialType.System_Int64:
                    _il.EmitLongConstant(0);
                    break;
                case SpecialType.System_Boolean:
                    _il.EmitBoolConstant(false);
                    break;
                case SpecialType.System_String:
                    _il.EmitStringConstant(string.Empty);
                    break;
                default:
                    if (return_type.IsReferenceType)
                    {
                        _il.EmitNullConstant();
                    }
                    else
                    {
                        throw new NotImplementedException();    // default(T)
                    }
                    break;
            }

            //
            _il.EmitRet(return_type.SpecialType == SpecialType.System_Void);
        }
    }
}
