using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Roslyn.Utilities;
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

        /// <summary>
        /// Emits reference to <c>this</c>.
        /// </summary>
        /// <returns>Type of <c>this</c> in current context, pushed on top of the evaluation stack.</returns>
        public TypeSymbol EmitThis()
        {
            if (_thisPlace == null)
            {
                throw new InvalidOperationException();
            }

            return _thisPlace.EmitLoad(_il);
        }

        public void EmitConvertToBool(TypeSymbol from, TypeRefMask fromHint, bool negation = false)
        {
            // TODO: handle {negation} within the switch to avoid unnecessary conversions
            // TODO: use {fromHint} to emit casting in compile time

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            //
            if (from.SpecialType != SpecialType.System_Boolean)
            {
                switch (from.SpecialType)
                {
                    case SpecialType.System_Int32:
                        break; // nop

                    case SpecialType.System_Int64:
                        _il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                        _il.EmitOpCode(ILOpCode.Conv_i8, 0);
                        _il.EmitOpCode(ILOpCode.Cgt_un);
                        break;

                    case SpecialType.None:
                        if (from == CoreTypes.PhpValue)
                        {
                            EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToBoolean);
                            break;
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }

                    default:
                        throw new NotImplementedException();
                }
            }

            // !<I4>
            if (negation)
            {
                EmitLogicNegation();
            }
        }

        public void EmitConvertToBool(BoundExpression expr, bool negation = false)
        {
            Contract.ThrowIfNull(expr);
            EmitConvertToBool(expr.Emit(this), expr.TypeRefMask, negation);
        }

        public void EmitConvertToPhpValue(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            switch (from.SpecialType)
            {
                case SpecialType.System_Boolean:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Boolean);
                    break;
                case SpecialType.System_Int64:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Long);
                    break;
                case SpecialType.System_Double:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Double);
                    break;
                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_PhpNumber);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    break;
            }
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
                default:
                    if (to == CoreTypes.PhpValue)
                    {
                        EmitConvertToPhpValue(from, fromHint);
                    }
                    else if (to == CoreTypes.PhpAlias)
                    {
                        EmitConvertToPhpValue(from, fromHint);
                        Emit_PhpValue_MakeAlias();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    break;
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

        private static int GetCallStackBehavior(MethodSymbol method)
        {
            int stack = 0;

            if (!method.ReturnsVoid)
            {
                // The call puts the return value on the stack.
                stack += 1;
            }

            if (!method.IsStatic)
            {
                // The call pops the receiver off the stack.
                stack -= 1;
            }

            // The call pops all the arguments.
            stack -= method.ParameterCount;

            //
            return stack;
        }

        public void EmitBox(ITypeSymbol valuetype)
        {
            _il.EmitOpCode(ILOpCode.Box);
            EmitSymbolToken((TypeSymbol)valuetype, null);
        }

        /// <summary>
        /// Emits "!= 0" operation. This method expects I4 value on top of evaluation stack.
        /// </summary>
        public void EmitLogicNegation()
        {
            _il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
            _il.EmitOpCode(ILOpCode.Ceq);
        }

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

        /// <summary>
        /// Emits call to <c>PhpAlias.Value</c>,
        /// expecting <c>PhpAlias</c> on top of evaluation stack,
        /// pushing <c>PhpValue</c> on top of the stack.
        /// </summary>
        public void Emit_PhpAlias_GetValue()
        {
            // <stack>.get_Value
            EmitCall(ILOpCode.Call, CoreMethods.Operators.PhpAlias_GetValue);
        }

        /// <summary>
        /// Emits <c>new PhpAlias</c>, expecting <c>PhpValue</c> on top of the evaluation stack.
        /// </summary>
        public void Emit_PhpValue_MakeAlias()
        {
            // new PhpAlias(<STACK>, 1)
            _il.EmitIntConstant(1);
            _il.EmitOpCode(ILOpCode.Newobj, -1);    // - 2 out, + 1 in
            _il.EmitToken(CoreMethods.Ctors.PhpAlias_PhpValue_int.Symbol, null, _diagnostics);
        }

        /// <summary>
        /// Emits call to given method.
        /// </summary>
        /// <param name="code">Call op code, Call, Callvirt, Calli.</param>
        /// <param name="method">Method reference.</param>
        /// <returns>Method return type.</returns>
        internal TypeSymbol EmitCall(ILOpCode code, MethodSymbol method)
        {
            Contract.ThrowIfNull(method);
            Debug.Assert(code == ILOpCode.Call || code == ILOpCode.Calli || code == ILOpCode.Callvirt);

            var stack = GetCallStackBehavior(method);
            _il.EmitOpCode(code, stack);
            _il.EmitToken(_moduleBuilder.Translate(method, _diagnostics, false), null, _diagnostics);
            return method.ReturnType;
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
            EmitCall(ILOpCode.Call, method);
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
                        if (return_type == CoreTypes.PhpValue)
                        {
                            var return_mask = this.Routine.ControlFlowGraph.ReturnTypeMask;
                            var typectx = this.Routine.ControlFlowGraph.FlowContext.TypeRefContext;

                            if (typectx.IsBoolean(return_mask))
                            {
                                _il.EmitBoolConstant(false);
                                EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Boolean);
                            }
                            else if (typectx.IsInteger(return_mask))
                            {
                                _il.EmitLongConstant(0);
                                EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Long);
                            }
                            else if (typectx.IsDouble(return_mask))
                            {
                                _il.EmitDoubleConstant(0.0);
                                EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Double);
                            }
                            //else if (typectx.IsString(return_mask))
                            //{
                            //}
                            //else if (typectx.IsArray(return_mask))
                            //{
                            //}
                            //else if (typectx.IsNullable(return_mask))
                            //{
                            //    _il.EmitNullConstant();
                            //    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Object);
                            //}
                            else
                            {
                                throw ExceptionUtilities.UnexpectedValue(return_type);
                            }
                        }
                        else
                        {
                            throw new NotImplementedException();    // default(T)
                        }
                    }
                    break;
            }

            //
            _il.EmitRet(return_type.SpecialType == SpecialType.System_Void);
        }
    }
}
