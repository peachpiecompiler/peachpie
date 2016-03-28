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

        #region EmitConvert

        /// <summary>
        /// Copies <c>PhpNumber</c> into a temp variable and loads its address.
        /// </summary>
        private void EmitPhpNumberAddr() => EmitStructAddr(CoreTypes.PhpNumber);

        /// <summary>
        /// Copies <c>PhpValue</c> into a temp variable and loads its address.
        /// </summary>
        private void EmitPhpValueAddr() => EmitStructAddr(CoreTypes.PhpValue);

        /// <summary>
        /// Copies a value type from the top of evaluation stack into a temporary variable and loads its address.
        /// </summary>
        private void EmitStructAddr(NamedTypeSymbol t)
        {
            Debug.Assert(t.IsStructType());
            var tmp = GetTemporaryLocal(t, true);
            _il.EmitLocalStore(tmp);
            _il.EmitLocalAddress(tmp);
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
            from = EmitSpecialize(from, fromHint);

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
                        // or ?
                        // _il.EmitOpCode(ILOpCode.Conv_i4);
                        break;

                    case SpecialType.None:
                        if (from == CoreTypes.PhpValue)
                        {
                            EmitPhpValueAddr();
                            EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToBoolean);
                            break;
                        }
                        else if (from == CoreTypes.PhpString)
                        {
                            EmitCall(ILOpCode.Call, CoreMethods.PhpString.ToBoolean);
                            break;
                        }
                        else if (from == CoreTypes.PhpNumber)
                        {
                            EmitPhpNumberAddr();
                            EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToBoolean);
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

            var place = GetPlace(expr);
            var type = TryEmitVariableSpecialize(place, expr.TypeRefMask);
            if (type != null)
            {
                EmitConvertToBool(type, 0, negation);
            }
            else
            {
                // avoiding of load of full value
                if (place != null && place.HasAddress)
                {
                    if (place.Type == CoreTypes.PhpNumber)
                    {
                        // < place >.ToBoolean()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToBoolean);
                        return;
                    }
                    else if (place.Type == CoreTypes.PhpValue)
                    {
                        // < place >.ToBoolean()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToBoolean);
                        return;
                    }
                }

                //
                EmitConvertToBool(Emit(expr), expr.TypeRefMask, negation);
            }
        }

        public void EmitConvertToPhpValue(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            switch (from.SpecialType)
            {
                case SpecialType.System_Boolean:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Boolean);
                    break;
                case SpecialType.System_Int32:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // Int32 -> Int64
                    goto case SpecialType.System_Int64; // PhpValue.Create((long)<stack>)
                case SpecialType.System_Int64:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Long);
                    break;
                case SpecialType.System_Double:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Double);
                    break;
                default:
                    if (from == CoreTypes.PhpAlias)
                    {
                        Emit_PhpAlias_GetValue();
                        return;
                    }
                    else if (from == CoreTypes.PhpValue)
                    {
                        // nop
                        break;
                    }
                    else if (from == CoreTypes.PhpNumber)
                    {
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_PhpNumber);
                        break;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        public void EmitConvertToPhpNumber(TypeSymbol from, TypeRefMask fromHint)
        {
            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            switch (from.SpecialType)
            {
                case SpecialType.System_Int32:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // Int32 -> Int64
                    goto case SpecialType.System_Int64; // PhpValue.Create((long)<stack>)
                case SpecialType.System_Int64:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.Create_Long);
                    break;
                case SpecialType.System_Double:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.Create_Double);
                    break;
                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        // nop
                        return;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        public void EmitConvertToLong(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            //
            from = EmitSpecialize(from, fromHint);

            switch (from.SpecialType)
            {
                case SpecialType.System_Int32:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // Int32 -> Int64
                    return;

                case SpecialType.System_Int64:
                    // nop
                    return;

                case SpecialType.System_Double:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // double -> int64
                    break;

                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        EmitPhpNumberAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToLong);
                        return;
                    }
                    else if (from == CoreTypes.PhpValue)
                    {
                        EmitPhpValueAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToLong);
                        return;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        public NamedTypeSymbol EmitConvertToDouble(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            from = EmitSpecialize(from, fromHint);
            var dtype = CoreTypes.Double.Symbol;

            switch (from.SpecialType)
            {
                case SpecialType.System_Int32:
                    _il.EmitOpCode(ILOpCode.Conv_r8);   // Int32 -> Double
                    return dtype;
                case SpecialType.System_Int64:
                    _il.EmitOpCode(ILOpCode.Conv_r8);   // Int64 -> Double
                    return dtype;
                case SpecialType.System_Double:
                    // nop
                    return dtype;
                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        EmitPhpNumberAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToDouble);
                        return dtype;
                    }
                    else if (from == CoreTypes.PhpValue)
                    {
                        EmitPhpValueAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToDouble);
                        return dtype;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        /// <summary>
        /// Emits conversion to <c>System.String</c>.
        /// </summary>
        public void EmitConvertToString(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            from = EmitSpecialize(from, fromHint);
            
            //
            switch (from.SpecialType)
            {
                case SpecialType.System_String:
                    // nop
                    break;
                case SpecialType.System_Boolean:
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToString_Bool);
                    break;
                case SpecialType.System_Int32:
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToString_Int32);
                    break;
                case SpecialType.System_Int64:
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToString_Long);
                    break;
                case SpecialType.System_Double:
                    EmitLoadContext();
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToString_Double_Context);
                    break;
                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        EmitPhpNumberAddr(); // PhpNumber -> PhpNumber addr
                        EmitLoadContext();  // Context
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToString_Context)
                            .Expect(SpecialType.System_String);
                        break;
                    }
                    else if (from == CoreTypes.PhpString)
                    {
                        EmitLoadContext();  // Context
                        EmitCall(ILOpCode.Call, CoreMethods.PhpString.ToString_Context)
                            .Expect(SpecialType.System_String);
                        break;
                    }
                    else if (from == CoreTypes.PhpValue)
                    {
                        EmitPhpValueAddr(); // PhpValue -> PhpValue addr
                        EmitLoadContext();  // Context
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToString_Context)
                            .Expect(SpecialType.System_String);
                        break;
                    }
                    throw new NotImplementedException();
            }
        }

        public void EmitConvert(BoundExpression expr, TypeSymbol to)
        {
            // loads value from place most effectively without runtime type checking
            var place = GetPlace(expr);
            var type = TryEmitVariableSpecialize(place, expr.TypeRefMask);  
            if (type != null)
            {
                EmitConvert(type, 0, to);
                return;
            }

            // avoiding of load of full value
            if (place != null && place.HasAddress)
            {
                if (place.Type == CoreTypes.PhpNumber)
                {
                    if (to.SpecialType == SpecialType.System_Int64)
                    {
                        // <place>.ToLong()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToLong);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_Double)
                    {
                        // <place>.ToDouble()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToDouble);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_Boolean)
                    {
                        // <place>.ToBoolean()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToBoolean);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_String)
                    {
                        // <place>.ToString(<ctx>)
                        place.EmitLoadAddress(_il);
                        EmitLoadContext();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToString_Context);
                        return;
                    }
                }
                else if (place.Type == CoreTypes.PhpValue)
                {
                    if (to.SpecialType == SpecialType.System_Int64)
                    {
                        // <place>.ToLong()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToLong);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_Double)
                    {
                        // <place>.ToDouble()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToDouble);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_Boolean)
                    {
                        // <place>.ToBoolean()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToBoolean);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_String)
                    {
                        // <place>.ToString(<ctx>)
                        place.EmitLoadAddress(_il);
                        EmitLoadContext();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToString_Context);
                        return;
                    }
                }
                else if (place.Type == CoreTypes.Long)
                {
                    if (to.SpecialType == SpecialType.System_String)
                    {
                        // <place>.ToString()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.Operators.Long_ToString);
                        return;
                    }
                }
            }

            //
            EmitConvert(expr.Emit(this), expr.TypeRefMask, to);
        }

        public void EmitConvert(TypeSymbol from, TypeRefMask fromHint, TypeSymbol to)
        {
            Contract.ThrowIfNull(from);
            Contract.ThrowIfNull(to);

            // conversion is not needed:
            if (to == from ||
               (from.SpecialType == to.SpecialType && from.SpecialType != SpecialType.None))
                return;

            //
            from = EmitSpecialize(from, fromHint);

            // specialized conversions:
            switch (to.SpecialType)
            {
                case SpecialType.System_Void:
                    EmitPop(from);
                    return;
                case SpecialType.System_Boolean:
                    EmitConvertToBool(from, fromHint);
                    return;
                case SpecialType.System_Int64:
                    EmitConvertToLong(from, fromHint);
                    return;
                case SpecialType.System_Double:
                    EmitConvertToDouble(from, fromHint);
                    return;
                case SpecialType.System_String:
                    EmitConvertToString(from, fromHint);
                    return;
                case SpecialType.System_Object:
                    // TODO: find common parent
                    throw new NotImplementedException();    //box ?
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
                    else if (to == CoreTypes.PhpNumber)
                    {
                        EmitConvertToPhpNumber(from, fromHint);
                        return;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    return;
            }

            //
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// If possible, based on type analysis, unwraps most specific type from give variable without a runtime type check.
        /// </summary>
        internal TypeSymbol TryEmitVariableSpecialize(BoundExpression expr)
        {
            // avoiding of load of full value
            return TryEmitVariableSpecialize(GetPlace(expr), expr.TypeRefMask);
        }

        /// <summary>
        /// If possible, based on type analysis, unwraps most specific type from give variable without a runtime type check.
        /// </summary>
        internal TypeSymbol TryEmitVariableSpecialize(IPlace place, TypeRefMask tmask)
        {
            if (place != null && tmask.IsSingleType)
            {
                if (place.HasAddress && place.Type == CoreTypes.PhpNumber)
                {
                    // access directly without type checking
                    if (IsDoubleOnly(tmask))
                    {
                        place.EmitLoadAddress(_il);
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.get_Double)
                            .Expect(SpecialType.System_Double);
                    }
                    else if (IsLongOnly(tmask))
                    {
                        place.EmitLoadAddress(_il);
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.get_Long)
                            .Expect(SpecialType.System_Int64);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// If possible, based on type analysis, unwraps more specific type from a value currently on stack without a runtime type check.
        /// </summary>
        /// <param name="stack">Type of value currently on top of evaluationb stack.</param>
        /// <param name="tmask">Result of analysis what type will be there in runtime.</param>
        /// <returns>New type on top of evaluation stack.</returns>
        internal TypeSymbol EmitSpecialize(BoundExpression expr)
        {
            return TryEmitVariableSpecialize(expr) ?? EmitSpecialize(expr.Emit(this), expr.TypeRefMask);
        }

        /// <summary>
        /// If possible, based on type analysis, unwraps more specific type from a value currently on stack without a runtime type check.
        /// </summary>
        /// <param name="stack">Type of value currently on top of evaluationb stack.</param>
        /// <param name="tmask">Result of analysis what type will be there in runtime.</param>
        /// <returns>New type on top of evaluation stack.</returns>
        internal TypeSymbol EmitSpecialize(TypeSymbol stack, TypeRefMask tmask)
        {
            // specialize type if possible
            if (tmask.IsSingleType)
            {
                if (stack == this.CoreTypes.PhpNumber)
                {
                    if (IsDoubleOnly(tmask))
                    {
                        EmitPhpNumberAddr();
                        return EmitCall(ILOpCode.Call, this.CoreMethods.PhpNumber.get_Double)
                            .Expect(SpecialType.System_Double);
                    }
                    else if (IsLongOnly(tmask))
                    {
                        EmitPhpNumberAddr();
                        return EmitCall(ILOpCode.Call, this.CoreMethods.PhpNumber.get_Long)
                            .Expect(SpecialType.System_Int64);
                    }
                }
            }

            //
            return stack;
        }

        /// <summary>
        /// In case there is <c>Int32</c> or <c>bool</c> on the top of evaluation stack,
        /// converts it to <c>Int64</c>.
        /// </summary>
        /// <param name="stack">New type on top of stack.</param>
        /// <returns></returns>
        internal TypeSymbol EmitConvertIntToLong(TypeSymbol stack)
        {
            if (stack.SpecialType == SpecialType.System_Int32 ||
                stack.SpecialType == SpecialType.System_Boolean)
            {
                _il.EmitOpCode(ILOpCode.Conv_i8);    // int|bool -> long
                stack = this.CoreTypes.Long;
            }

            return stack;
        }

        /// <summary>
        /// In case there is <c>Int32</c> or <c>bool</c> or <c>PhpNumber</c> on the top of evaluation stack,
        /// converts it to <c>double</c>.
        /// </summary>
        /// <param name="stack">New type on top of stack.</param>
        /// <returns></returns>
        internal TypeSymbol EmitConvertNumberToDouble(BoundExpression expr)
        {
            // emit number literal directly as double
            if (expr is BoundLiteral && expr.ConstantValue.HasValue)
            {
                if (expr.ConstantValue.Value is long)
                {
                    _il.EmitDoubleConstant((long)expr.ConstantValue.Value);
                    return this.CoreTypes.Double;
                }
                if (expr.ConstantValue.Value is int)
                {
                    _il.EmitDoubleConstant((int)expr.ConstantValue.Value);
                    return this.CoreTypes.Double;
                }
            }

            // emit fast ToDouble() in case of a PhpNumber variable
            var place = GetPlace(expr);
            var type = TryEmitVariableSpecialize(place, expr.TypeRefMask);
            if (type == null)
            {
                if (place != null && place.HasAddress)
                {
                    if (place.Type == CoreTypes.PhpNumber)
                    {
                        place.EmitLoadAddress(_il);
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToDouble)
                            .Expect(SpecialType.System_Double);
                    }
                }

                type = EmitSpecialize(expr);
            }

            Debug.Assert(type != null);

            if (type.SpecialType == SpecialType.System_Int32 ||
                type.SpecialType == SpecialType.System_Int64 ||
                type.SpecialType == SpecialType.System_Boolean)
            {
                _il.EmitOpCode(ILOpCode.Conv_r8);    // int|bool -> long
                type = this.CoreTypes.Double;
            }
            else if (type == CoreTypes.PhpNumber)
            {
                EmitPhpNumberAddr();
                EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToDouble);    // number -> double
                type = this.CoreTypes.Double;
            }

            //
            return type;
        }

        public void EmitOpCode(ILOpCode code) => _il.EmitOpCode(code);

        public void EmitPop(TypeSymbol type)
        {
            Contract.ThrowIfNull(type);

            if (type.SpecialType != SpecialType.System_Void)
            {
                _il.EmitOpCode(ILOpCode.Pop, -1);
            }
        }

        public TypeSymbol Emit(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);
            return EmitSpecialize(expr);
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

        public void EmitBox(TypeSymbol valuetype)
        {
            Contract.ThrowIfNull(valuetype);

            if (valuetype.IsValueType)
            {
                _il.EmitOpCode(ILOpCode.Box);
                EmitSymbolToken(valuetype, null);
            }
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

        public void Emit_New_PhpString(int capacity)
        {
            // new PhpString(capacity)
            _il.EmitIntConstant(capacity);
            _il.EmitOpCode(ILOpCode.Newobj, -1 + 1);    // - 1 out, + 1 in
            _il.EmitToken(CoreMethods.Ctors.PhpString_int.Symbol, null, _diagnostics);
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
            Debug.Assert(code == ILOpCode.Call || code == ILOpCode.Calli || code == ILOpCode.Callvirt || code == ILOpCode.Newobj);

            var stack = GetCallStackBehavior(method);

            if (code == ILOpCode.Newobj)
                stack++;

            _il.EmitOpCode(code, stack);
            _il.EmitToken(_moduleBuilder.Translate(method, _diagnostics, false), null, _diagnostics);
            return method.ReturnType;
        }

        public void EmitEcho(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);

            // <ctx>.Echo(expr);

            this.EmitLoadContext();
            var type = EmitSpecialize(expr);

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
                case SpecialType.System_Int32:
                    method = CoreMethods.Operators.Echo_Int32.Symbol;
                    break;
                case SpecialType.System_Int64:
                    method = CoreMethods.Operators.Echo_Long.Symbol;
                    break;
                case SpecialType.System_Boolean:
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToString_Bool).Expect(SpecialType.System_String);
                    method = CoreMethods.Operators.Echo_String.Symbol;
                    break;
                default:
                    if (type == CoreTypes.PhpString)
                    {
                        method = CoreMethods.Operators.Echo_PhpString.Symbol;
                    }
                    else if (type == CoreTypes.PhpNumber)
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
            Debug.Assert(method != null);
            EmitCall(ILOpCode.Call, method);
        }

        public void EmitLoadDefaultValue(TypeSymbol type, TypeRefMask typemask)
        {
            switch (type.SpecialType)
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
                    if (type.IsReferenceType)
                    {
                        _il.EmitNullConstant();
                    }
                    else
                    {
                        if (type == CoreTypes.PhpNumber)
                        {
                            // PhpNumber.Create(0L)
                            _il.EmitLongConstant(0L);
                            EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.Create_Long);
                        }
                        else if (type == CoreTypes.PhpValue)
                        {
                            var typectx = this.Routine.ControlFlowGraph.FlowContext.TypeRefContext;

                            if (typectx.IsBoolean(typemask))
                            {
                                _il.EmitBoolConstant(false);
                                EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Boolean);
                            }
                            else if (typectx.IsLong(typemask))
                            {
                                _il.EmitLongConstant(0);
                                EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Long);
                            }
                            else if (typectx.IsDouble(typemask))
                            {
                                _il.EmitDoubleConstant(0.0);
                                EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Double);
                            }
                            //else if (typectx.IsString(typemask))
                            //{
                            //}
                            //else if (typectx.IsArray(typemask))
                            //{
                            //}
                            //else if (typectx.IsNullable(typemask))
                            //{
                            //    _il.EmitNullConstant();
                            //    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Object);
                            //}
                            else
                            {
                                throw ExceptionUtilities.UnexpectedValue(typemask);
                            }
                        }
                        else
                        {
                            throw new NotImplementedException();    // default(T)
                        }
                    }
                    break;
            }
        }

        public void EmitReturnDefault()
        {
            // return default(RETURN_TYPE);

            var return_type = this.Routine.ReturnType;

            EmitLoadDefaultValue(return_type, this.Routine.ControlFlowGraph.ReturnTypeMask);
            _il.EmitRet(return_type.SpecialType == SpecialType.System_Void);
        }
    }
}
