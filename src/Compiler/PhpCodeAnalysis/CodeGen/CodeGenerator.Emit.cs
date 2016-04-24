using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public TypeSymbol EmitLoadContext()
        {
            return _contextPlace.EmitLoad(_il);
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

        /// <summary>
        /// If possible, based on type analysis, unwraps most specific type from give variable without a runtime type check.
        /// </summary>
        internal TypeSymbol TryEmitVariableSpecialize(BoundExpression expr)
        {
            Debug.Assert(expr.Access.IsRead);

            if (!expr.Access.IsEnsure)
            {
                // avoiding of load of full value if not necessary
                return TryEmitVariableSpecialize(PlaceOrNull(expr), expr.TypeRefMask);
            }
            else
            {
                // we has to call expr.Emit() to generate ensureness correctly (Ensure Object, Ensure Array, Read Alias)
                return null;
            }
        }

        /// <summary>
        /// If possible, based on type analysis, unwraps most specific type from give variable without a runtime type check.
        /// </summary>
        internal TypeSymbol TryEmitVariableSpecialize(IPlace place, TypeRefMask tmask)
        {
            if (place != null && tmask.IsSingleType)
            {
                if (place.HasAddress)
                {
                    if (place.Type == CoreTypes.PhpNumber)
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
                    else if (place.Type == CoreTypes.PhpValue)
                    {
                        // access directly without type checking
                        if (IsDoubleOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Double)
                                .Expect(SpecialType.System_Double);
                        }
                        else if (IsLongOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Long)
                                .Expect(SpecialType.System_Int64);
                        }
                        else if (IsBooleanOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Boolean)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else if (IsReadonlyStringOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_String)
                                .Expect(SpecialType.System_String);
                        }
                        else if (IsArrayOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Array)
                                .Expect(CoreTypes.PhpArray);
                        }
                        else if (IsClassOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Object)
                                .Expect(SpecialType.System_Object);

                            // DEBUG:
                            //if (tmask.IsSingleType)
                            //{
                            //    var tref = this.Routine.TypeRefContext.GetTypes(tmask)[0];
                            //    var clrtype = (TypeSymbol)this.DeclaringCompilation.GlobalSemantics.GetType(tref.QualifiedName);
                            //    if (clrtype != null && !clrtype.IsErrorType())
                            //    {
                            //        this.EmitCastClass(clrtype);
                            //        return clrtype;
                            //    }
                            //}

                            return this.CoreTypes.Object;
                        }
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
            Debug.Assert(expr.Access.IsRead);

            return expr.ResultType = (TryEmitVariableSpecialize(expr) ?? EmitSpecialize(expr.Emit(this), expr.TypeRefMask));
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
                else if (stack == CoreTypes.PhpValue)
                {
                    // access directly without type checking
                    if (IsDoubleOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Double)
                            .Expect(SpecialType.System_Double);
                    }
                    else if (IsLongOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Long)
                            .Expect(SpecialType.System_Int64);
                    }
                    else if (IsBooleanOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Boolean)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (IsReadonlyStringOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_String)
                            .Expect(SpecialType.System_String);
                    }
                }
                else if (stack.IsReferenceType)
                {
                    var tref = this.Routine.TypeRefContext.GetTypes(tmask)[0];
                    if (tref.IsObject)
                    {
                        HashSet<DiagnosticInfo> useSiteDiagnostic = null;
                        var t = _routine.DeclaringCompilation.SourceAssembly.GetTypeByMetadataName(tref.QualifiedName.ClrName(), true, false);
                        if (t != null && !t.IsErrorType() && t.IsDerivedFrom(stack, false, ref useSiteDiagnostic)) // TODO: or interface
                        {
                            EmitCastClass(t);
                            return t;
                        }
                        else
                        {
                            // TODO: class aliasing
                            Debug.Assert(t != null);
                        }
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
                if (expr.ConstantValue.Value is bool)
                {
                    _il.EmitDoubleConstant((bool)expr.ConstantValue.Value ? 1.0 : 0.0);
                    return this.CoreTypes.Double;
                }
            }

            // emit fast ToDouble() in case of a PhpNumber variable
            var place = PlaceOrNull(expr);
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
        /// Loads <see cref="RuntimeTypeHandle"/> of given type.
        /// </summary>
        public TypeSymbol EmitLoadToken(TypeSymbol type, SyntaxNode syntaxNodeOpt)
        {
            _il.EmitLoadToken(_moduleBuilder, _diagnostics, type, syntaxNodeOpt);

            return this.CoreTypes.RuntimeTypeHandle;
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
            _il.EmitSymbolToken(_moduleBuilder, _diagnostics, symbol, syntaxNode);
        }

        internal void EmitSymbolToken(FieldSymbol symbol, SyntaxNode syntaxNode)
        {
            _il.EmitSymbolToken(_moduleBuilder, _diagnostics, symbol, syntaxNode);
        }

        //private void EmitSymbolToken(MethodSymbol method, SyntaxNode syntaxNode)
        //{
        //    _il.EmitToken(_moduleBuilder.Translate(method, syntaxNode, _diagnostics, null), syntaxNode, _diagnostics);
        //}

        internal void EmitSequencePoint(Syntax.AST.LangElement element)
        {
            if (_emitPdbSequencePoints && element != null && element.Span.IsValid)
            {
                if (_syntaxTree == null)
                    _syntaxTree = new SyntaxTreeAdapter(_routine.ContainingFile.Syntax.SourceUnit);

                _il.DefineSequencePoint(_syntaxTree, new Microsoft.CodeAnalysis.Text.TextSpan(element.Span.Start, element.Span.Length));
            }
        }
        SyntaxTree _syntaxTree;

        /// <summary>
        /// Emits load of <c>PhpAlias.Value</c>,
        /// expecting <c>PhpAlias</c> on top of evaluation stack,
        /// pushing <c>PhpValue</c> on top of the stack.
        /// </summary>
        public TypeSymbol Emit_PhpAlias_GetValue()
        {
            // <stack>.Value
            EmitOpCode(ILOpCode.Ldfld);
            EmitSymbolToken(CoreMethods.PhpAlias.Value, null);
            return this.CoreTypes.PhpValue;
        }

        /// <summary>
        /// Emits store to <c>PhpAlias.Value</c>,
        /// expecting <c>PhpAlias</c> and <c>PhpValue</c> on top of evaluation stack.
        /// </summary>
        public void Emit_PhpAlias_SetValue()
        {
            // <stack_1>.Value = <stack_2>
            EmitOpCode(ILOpCode.Stfld);
            EmitSymbolToken(CoreMethods.PhpAlias.Value, null);
        }

        /// <summary>
        /// Emits <c>new PhpAlias</c>, expecting <c>PhpValue</c> on top of the evaluation stack.
        /// </summary>
        public TypeSymbol Emit_PhpValue_MakeAlias()
        {
            // new PhpAlias(<STACK>, 1)
            _il.EmitIntConstant(1);
            return EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpAlias_PhpValue_int);
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
            return _il.EmitCall(_moduleBuilder, _diagnostics, code, method);
        }

        internal TypeSymbol EmitCall(ILOpCode code, MethodSymbol method, BoundExpression thisExpr, ImmutableArray<BoundExpression> arguments)
        {
            Contract.ThrowIfNull(method);

            TypeSymbol thisType;

            // <this>
            if (thisExpr != null)
            {
                thisType = Emit(thisExpr);
                Debug.Assert(thisType != null && thisType.SpecialType != SpecialType.System_Void);

                if (!method.HasThis)
                {
                    EmitPop(thisType);
                    thisType = null;
                }
            }
            else
            {
                if (method.HasThis)
                {
                    throw new ArgumentException();  // TODO: PHP would create temporary instance of class
                }

                thisType = null;
            }

            // TODO: varargs

            // arguments
            var parameters = method.Parameters;
            int arg_index = 0;      // next argument to be emitted from <arguments>
            var param_index = 0;    // loaded parameters

            for (;  param_index < parameters.Length; param_index++)
            {
                var p = parameters[param_index];

                // <ctx>
                if (p.Type == CoreTypes.Context)
                {
                    EmitLoadContext();
                    continue;
                }

                // all arguments loaded
                if (arg_index < arguments.Length)
                {
                    EmitConvert(arguments[arg_index++], p.Type); // load argument
                }
                else
                {
                    EmitLoadDefaultValue(p.Type, 0);
                }
            }

            // emit remaining not used arguments
            for (; arg_index < arguments.Length; arg_index++)
            {
                EmitPop(Emit(arguments[arg_index]));
            }

            // call the method
            return EmitCall(code, method);
        }

        internal TypeSymbol EmitCall(ILOpCode code, BoundExpression thisExpr, OverloadsList overloads, ImmutableArray<BoundExpression> arguments)
        {
            // at this point we do expect <this> is already emitted on top of the evaluation stack

            if (!overloads.IsFinal)
                throw new NotImplementedException();    // we have to fallback to indirect call, there might be more overloads in runtime

            if (!overloads.IsStaticIsConsistent())
                throw new NotImplementedException();    // TODO: fallback to indirect call; some overloads expect <this>, others don't

            if (overloads.Candidates.Length > 1)
            {
                if (overloads.Candidates.All(c => c.ParameterCount == 0))
                    throw new InvalidOperationException("Ambiguous call."); // TODO: ErrorCode // NOTE: overrides should be resolved already
            }

            // parameter types actually emitted on top of evaluation stack; used for eventual generic indirect call
            var actualParamTypes = new List<TypeSymbol>(arguments.Length + 1);

            //
            var thisType = (thisExpr != null) ? Emit(thisExpr) : null;

            //
            int arg_index = 0;      // next argument to be emitted from <arguments>
            
            while (true)
            {
                int param_index = actualParamTypes.Count;    // next candidates parameter index
                var t = overloads.ConsistentParameterType(param_index);

                if (overloads.IsImplicitlyDeclaredParameter(param_index))
                {
                    // value for implicit parameter is not passed from source code

                    if (t == CoreTypes.Context)
                    {
                        EmitLoadContext();
                        actualParamTypes.Add(t);
                        continue;
                    }
                    else
                    {
                        //throw new NotImplementedException();    // not handled implicit parameter (late static, global this, locals, ...)

                        // ---> load arguments
                    }
                }

                // all arguments and implicit parameters loaded
                if (arg_index >= arguments.Length)
                    break;

                // magic
                TypeSymbol arg_type;
                if (t != null)                  // all overloads expect the same argument or there is just one
                {
                    EmitConvert(arguments[arg_index++], t); // load argument
                    arg_type = t;
                }
                else
                {
                    arg_type = arguments[arg_index++].Emit(this);
                }

                actualParamTypes.Add(arg_type);
            }

            // find overloads matching emitted areguments
            var candidates = overloads.FindByLoadedArgs(actualParamTypes).ToImmutableArray();
            if (candidates.Length == 1 && overloads.IsFinal)
            {
                var target = candidates[0];

                // pop unnecessary args
                while (actualParamTypes.Count > target.ParameterCount)
                {
                    var last = actualParamTypes.Count - 1;
                    this.EmitPop(actualParamTypes[last]);
                    actualParamTypes.RemoveAt(last);
                }

                // emit default values for missing args
                for (int i = actualParamTypes.Count; i < target.ParameterCount; i++)
                {
                    var t = target.Parameters[i].Type;
                    EmitLoadDefaultValue(t, 0); // TODO: ErrorCode // Missing mandatory argument (if mandatory)
                    actualParamTypes.Add(t);
                }

                //
                return this.EmitCall(code, target);
            }
            else
            {
                // use call site and resolve the overload in runtime
                throw new NotImplementedException();
            }
        }

        internal void EmitCastClass(TypeSymbol type)
        {
            // (T)
            _il.EmitOpCode(ILOpCode.Castclass);
            EmitSymbolToken(type, null);
        }

        public void EmitEcho(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);

            // <ctx>.Echo(expr);

            this.EmitLoadContext();
            var type = EmitSpecialize(expr);

            //
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
                    // TODO: if (VALUE) echo("1");
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
                    else if (type == CoreTypes.PhpArray)
                    {
                        this.EmitLoadContext();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpArray.ToString_Context).Expect(SpecialType.System_String);
                        method = CoreMethods.Operators.Echo_String.Symbol;
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

        public void EmitIntStringKey(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);

            var constant = expr.ConstantValue;
            if (constant.HasValue)
            {
                if (constant.Value is string)
                {
                    _il.EmitStringConstant((string)constant.Value);
                    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_string);
                }
                else if (constant.Value is long)
                {
                    _il.EmitIntConstant((int)(long)constant.Value);
                    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_int);
                }
                else if (constant.Value is int)
                {
                    _il.EmitIntConstant((int)constant.Value);
                    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_int);
                }
                else
                {
                    throw new NotSupportedException();
                }

                return;
            }

            var t = Emit(expr); // TODO: ConvertToArrayKey
            switch (t.SpecialType)
            {
                case SpecialType.System_Int64:
                    _il.EmitOpCode(ILOpCode.Conv_i4);   // i8 -> i4
                    goto case SpecialType.System_Int32;
                case SpecialType.System_Int32:
                    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_int);
                    break;
                case SpecialType.System_String:
                    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_string);
                    break;
            }
            // .call Convert.ToArrayKey(<t>)
            throw new NotImplementedException();
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
                case SpecialType.System_Int32:
                    _il.EmitIntConstant(0);
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
                        if (type == CoreTypes.PhpArray)
                        {
                            EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpArray);
                        }
                        else if (type == CoreTypes.PhpAlias)
                        {
                            // new PhpAlias(void, 1);
                            EmitCall(ILOpCode.Call, CoreMethods.PhpValue.CreateVoid);
                            Emit_PhpValue_MakeAlias();
                        }
                        else
                        {
                            // TODO: PhpString
                            _il.EmitNullConstant();
                        }
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
                            else if (typectx.IsAString(typemask))
                            {
                                throw ExceptionUtilities.UnexpectedValue(typemask);
                            }
                            else if (typectx.IsArray(typemask))
                            {
                                throw ExceptionUtilities.UnexpectedValue(typemask);
                            }
                            else if (typectx.IsNullable(typemask))
                            {
                                //_il.EmitNullConstant();
                                //EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Object);
                                throw ExceptionUtilities.UnexpectedValue(typemask);
                            }
                            else
                            {
                                EmitCall(ILOpCode.Call, CoreMethods.PhpValue.CreateNull);
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

    internal static class ILBuilderExtension
    {
        public static void EmitLoadToken(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, TypeSymbol type, SyntaxNode syntaxNodeOpt)
        {
            il.EmitOpCode(ILOpCode.Ldtoken);
            EmitSymbolToken(il, module, diagnostics, type, syntaxNodeOpt);
        }

        public static void EmitSymbolToken(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics,  TypeSymbol symbol, SyntaxNode syntaxNode)
        {
            il.EmitToken(module.Translate(symbol, syntaxNode, diagnostics), syntaxNode, diagnostics);
        }

        public static void EmitSymbolToken(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, FieldSymbol symbol, SyntaxNode syntaxNode)
        {
            il.EmitToken(symbol, syntaxNode, diagnostics);
        }

        /// <summary>
        /// Emits call to given method.
        /// </summary>
        /// <param name="code">Call op code, Call, Callvirt, Calli.</param>
        /// <param name="method">Method reference.</param>
        /// <returns>Method return type.</returns>
        public static TypeSymbol EmitCall(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, ILOpCode code, MethodSymbol method)
        {
            Contract.ThrowIfNull(method);
            Debug.Assert(code == ILOpCode.Call || code == ILOpCode.Calli || code == ILOpCode.Callvirt || code == ILOpCode.Newobj);

            var stack = method.GetCallStackBehavior();

            if (code == ILOpCode.Newobj)
                stack += 1 + 1;    // there is no <this>, + it pushes <newinst> on stack

            il.EmitOpCode(code, stack);
            il.EmitToken(module.Translate(method, diagnostics, false), null, diagnostics);
            return (code == ILOpCode.Newobj) ? method.ContainingType : method.ReturnType;
        }
    }
}
