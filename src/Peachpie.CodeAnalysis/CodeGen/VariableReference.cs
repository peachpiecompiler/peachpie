using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Semantics
{
    #region LhsStack

    /// <summary>
    /// A helper maintaining what is loaded on stack when storing a chained variable.
    /// </summary>
    struct LhsStack : IDisposable
    {
        public static LhsStack operator +(LhsStack lhsreceiver, LhsStack lhsoverride)
        {
            return new LhsStack
            {
                Stack = lhsoverride.Stack ?? lhsreceiver.Stack,
                StackByRef = lhsoverride.Stack != null ? lhsoverride.StackByRef : lhsreceiver.StackByRef,
                CodeGenerator = lhsoverride.CodeGenerator ?? lhsreceiver.CodeGenerator,
            };
        }

        /// <summary>
        /// Loaded value on stack.
        /// </summary>
        public TypeSymbol Stack { get; set; }

        /// <summary>
        /// Loaded value on stack is address.
        /// </summary>
        public bool StackByRef { get; set; }

        /// <summary>
        /// Gets value whether to store receiver in temporary variable.
        /// </summary>
        public bool IsEnabled { get; set; }

        public CodeGenerator CodeGenerator { get; set; }

        //bool _lhsUsesStack; // when true, we can safely `.dup` instead of emitting preamble again

        LocalDefinition _receiverTemp; // receiver value had to be stored into temp, we can load it from there

        LocalDefinition _indexTemp;  // name/index value had to be stored into temp, we can load it from there

        public TypeSymbol EmitReceiver(CodeGenerator cg, BoundExpression receiver)
        {
            Debug.Assert(receiver != null);

            if (_receiverTemp != null)
            {
                // <loc>
                cg.Builder.EmitLocalLoad(_receiverTemp);
            }
            else
            {
                receiver.ResultType = receiver.Emit(cg);

                if (IsEnabled) // store the result
                {
                    Debug.Assert(CodeGenerator != null);

                    // (<loc> = <instance>);
                    _receiverTemp = cg.GetTemporaryLocal(receiver.ResultType);
                    cg.EmitOpCode(ILOpCode.Dup);
                    cg.Builder.EmitLocalStore(_receiverTemp);
                }
            }

            //

            return receiver.ResultType;
        }

        public void Dispose()
        {
            if (_receiverTemp != null)
            {
                CodeGenerator.ReturnTemporaryLocal(_receiverTemp);
                _receiverTemp = null;
            }

            if (_indexTemp != null)
            {
                CodeGenerator.ReturnTemporaryLocal(_indexTemp);
                _indexTemp = null;
            }
        }
    }

    #endregion

    #region VariableReferenceExtensions

    internal static class VariableReferenceExtensions
    {
        public static TypeSymbol EmitLoadValue(this IPlace place, CodeGenerator cg, ref LhsStack lhs, BoundAccess access)
        {
            var type = place.Type;

            //

            if (access.IsReadRef) // : PhpAlias
            {
                if (type == cg.CoreTypes.PhpAlias)
                {
                    // value : PhpAlias
                    return place.EmitLoad(cg.Builder).Expect(cg.CoreTypes.PhpAlias);
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    // EnsureAlias(ref PhpValue)
                    place.EmitLoadAddress(cg.Builder);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureAlias_PhpValueRef).Expect(cg.CoreTypes.PhpAlias);
                }

                throw cg.NotImplementedException($"EnsureAlias for {type}.");
            }
            else if (access.EnsureObject) // : object
            {
                if (type == cg.CoreTypes.PhpAlias)
                {
                    // PhpAlias.EnsureObject() : object
                    place.EmitLoad(cg.Builder).Expect(cg.CoreTypes.PhpAlias);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpAlias.EnsureObject).Expect(SpecialType.System_Object);
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    if (!place.HasAddress)
                    {
                        throw cg.NotImplementedException("unreachable: variable does not have an address");
                    }

                    // EnsureObject(ref PhpValue) : object
                    place.EmitLoadAddress(cg.Builder);
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureObject_PhpValueRef).Expect(SpecialType.System_Object);

                    //if (_thint.IsSingleType && cg.IsClassOnly(_thint))
                    //{
                    //    var tref = cg.Routine.TypeRefContext.GetTypes(_thint)[0];
                    //    var clrtype = (TypeSymbol)tref.ResolveTypeSymbol(cg.DeclaringCompilation);
                    //    if (clrtype != null && !clrtype.IsErrorType() && clrtype != cg.CoreTypes.Object)
                    //    {
                    //        cg.EmitCastClass(clrtype);
                    //        return clrtype;
                    //    }
                    //}

                    return cg.CoreTypes.Object;
                }
                else if (type.IsOfType(cg.CoreTypes.IPhpArray))
                {
                    // PhpArray -> stdClass
                    // PhpString -> stdClass (?)
                    // otherwise keep the instance on stack
                    throw new NotImplementedException();
                }
                else
                {
                    if (type.IsReferenceType)
                    {
                        if (type == cg.CoreTypes.Object && /*cg.TypeRefContext.IsNull(_thint) &&*/ place.HasAddress) // TODO: only if place can be NULL
                        {
                            // Operators.EnsureObject(ref <place>)
                            place.EmitLoadAddress(cg.Builder);
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureObject_ObjectRef)
                                .Expect(SpecialType.System_Object);
                        }
                        else
                        {
                            // <place>
                            return place.EmitLoad(cg.Builder);
                        }
                    }
                    else
                    {
                        // return new stdClass(ctx)
                        throw new NotImplementedException();
                    }
                }
            }
            else if (access.EnsureArray) // : IPhpArray | PhpArray | ArrayAccess
            {
                if (type == cg.CoreTypes.PhpAlias)
                {
                    // <place>.EnsureArray() : IPhpArray
                    place.EmitLoad(cg.Builder).Expect(cg.CoreTypes.PhpAlias);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpAlias.EnsureArray).Expect(cg.CoreTypes.IPhpArray);
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    //if (cg.IsArrayOnly(_thint))
                    //{
                    //    // uses typehint and accesses .Array directly if possible
                    //    // <place>.Array
                    //    _place.EmitLoadAddress(cg.Builder);
                    //    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.get_Array)
                    //        .Expect(cg.CoreTypes.PhpArray);
                    //}
                    //else
                    {
                        // <place>.EnsureArray() : IPhpArray
                        place.EmitLoadAddress(cg.Builder);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureArray_PhpValueRef).Expect(cg.CoreTypes.IPhpArray);
                    }
                }
                else if (type == cg.CoreTypes.PhpString)
                {
                    Debug.Assert(type.IsValueType);
                    // <place>.EnsureWritable() : PhpString.Blob
                    place.EmitLoadAddress(cg.Builder); // LOAD ref PhpString
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpString.EnsureWritable);
                }
                else if (type.IsOfType(cg.CoreTypes.IPhpArray))
                {
                    if (place.HasAddress)
                    {
                        // Operators.EnsureArray(ref <place>)
                        place.EmitLoadAddress(cg.Builder);

                        if (type == cg.CoreTypes.PhpArray)
                        {
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureArray_PhpArrayRef)
                                .Expect(cg.CoreTypes.PhpArray);
                        }
                        else
                        {
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureArray_IPhpArrayRef)
                                .Expect(cg.CoreTypes.IPhpArray);
                        }
                    }
                    else
                    {
                        return place.EmitLoad(cg.Builder);
                    }
                }
                else if (type.IsOfType(cg.CoreTypes.ArrayAccess))
                {
                    // LOAD <place> : ArrayAccess
                    return place.EmitLoad(cg.Builder);
                }
                else if (type.IsReferenceType)
                {
                    // Operators.EnsureArray(<stack>)
                    place.EmitLoad(cg.Builder);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureArray_Object);
                }

                throw cg.NotImplementedException("EnsureArray(" + type.Name + ")");
            }
            else if (access.IsRead) // : {place.Type}
            {
                return place.EmitLoad(cg.Builder);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(access);
            }
        }

        /// <summary>
        /// Emits preamble to an assignment.
        /// </summary>
        /// <param name="place">Target place to be assigned to.</param>
        /// <param name="cg">Ref to <see cref="CodeGenerator"/>.</param>
        /// <param name="access">The place's access.</param>
        /// <returns></returns>
        public static LhsStack EmitStorePreamble(this IPlace place, CodeGenerator cg, BoundAccess access)
        {
            var type = place.Type;

            if (!access.IsNotRef && type == cg.CoreTypes.PhpValue && place.HasAddress && !access.IsWriteRef && !access.IsUnset)
            {
                // might be ref ? emit address and use SetValue() operator
                place.EmitLoadAddress(cg.Builder);

                return new LhsStack { Stack = type, StackByRef = true };
            }
            else if (type == cg.CoreTypes.PhpAlias && !access.IsWriteRef && !access.IsUnset)
            {
                place.EmitLoad(cg.Builder); // : PhpAlias

                return new LhsStack { Stack = type };
            }
            else
            {
                place.EmitStorePrepare(cg.Builder); // TODO: LhsStack

                return default;
            }
        }

        public static void EmitStore(this IPlace place, CodeGenerator cg, ref LhsStack lhs, TypeSymbol stack, BoundAccess access)
        {
            var type = place.Type;

            if (access.IsUnset)
            {
                Debug.Assert(stack == null);
                Debug.Assert(!lhs.StackByRef);

                stack = cg.EmitLoadDefault(type, 0);
                place.EmitStore(cg.Builder);
                return;
            }

            Debug.Assert(stack != null);

            if (access.IsWriteRef)
            {
                Debug.Assert(stack == cg.CoreTypes.PhpAlias);
                Debug.Assert(!lhs.StackByRef);

                cg.EmitConvert(stack, 0, type);
                place.EmitStore(cg.Builder);
            }
            else if (access.IsWrite)
            {
                if (lhs.StackByRef && lhs.Stack == cg.CoreTypes.PhpValue)
                {
                    cg.EmitConvert(stack, 0, cg.CoreTypes.PhpValue);

                    // STACK: ref PhpValue, PhpValue
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetValue_PhpValueRef_PhpValue);
                    return;
                }

                if (lhs.Stack == cg.CoreTypes.PhpAlias && !lhs.StackByRef)
                {
                    cg.EmitConvert(stack, 0, cg.CoreTypes.PhpValue);

                    // STACK: PhpAlias, PhpValue

                    // Template: <alias>.Value = STACK
                    cg.Builder.EmitOpCode(ILOpCode.Stfld);
                    cg.EmitSymbolToken(cg.CoreMethods.PhpAlias.Value.Symbol, null);
                    return;
                }

                cg.EmitConvert(stack, 0, type);

                if (lhs.StackByRef)
                {
                    cg.Builder.EmitOpCode(ILOpCode.Stobj);
                    cg.EmitSymbolToken(type, null);
                }
                else
                {
                    place.EmitStore(cg.Builder);
                }
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(access);
            }
        }

        /// <summary>
        /// NOTICE: temporary API, will be replaced with operators.
        /// </summary>
        public static TypeSymbol EmitLoadValue(this IVariableReference/*!*/reference, CodeGenerator/*!*/cg, BoundAccess access)
        {
            Debug.Assert(reference != null);
            Debug.Assert(cg != null);

            var lhs = default(LhsStack);

            return reference.EmitLoadValue(cg, ref lhs, access);
        }

        public static TypeSymbol EmitLoadValue(CodeGenerator cg, MethodSymbol method, IPlace receiverOpt)
        {
            using (var lhs = EmitReceiver(cg, receiverOpt))
            {
                // TOOD: PhpValue.FromClr
                return cg.EmitCall(ILOpCode.Callvirt/*changed to .call by EmitCall if possible*/, method);
            }
        }

        public static TypeSymbol EmitLoadValue(this PropertySymbol property, CodeGenerator cg, IPlace receiver)
        {
            return EmitLoadValue(cg, property.GetMethod, receiver);
        }

        public static void EmitStore(this IVariableReference target, CodeGenerator cg, LocalDefinition local, BoundAccess access)
        {
            var lhs = target.EmitStorePreamble(cg, access);
            cg.Builder.EmitLocalLoad(local);
            target.EmitStore(cg, ref lhs, (TypeSymbol)local.Type, access);

            lhs.Dispose();
        }

        public static void EmitStore(this IVariableReference target, CodeGenerator cg, IPlace place, BoundAccess access)
        {
            Debug.Assert(access.IsWrite || access.IsWriteRef); // Write or WriteRef

            var lhs = target.EmitStorePreamble(cg, access);
            var type = place.EmitLoad(cg.Builder);
            target.EmitStore(cg, ref lhs, type, access);

            lhs.Dispose();
        }

        public static void EmitStore(this IVariableReference target, CodeGenerator cg, Func<TypeSymbol> valueLoader, BoundAccess access)
        {
            Debug.Assert(access.IsWrite || access.IsWriteRef); // Write or WriteRef

            var lhs = target.EmitStorePreamble(cg, access);
            var type = valueLoader();
            target.EmitStore(cg, ref lhs, type, access);

            lhs.Dispose();
        }

        public static LhsStack EmitReceiver(CodeGenerator cg, Symbol symbol, TypeSymbol receiver)
        {
            //
            if (symbol.IsStatic)
            {
                if (receiver != null)
                {
                    cg.EmitPop(receiver);
                }

                return default;
            }
            else
            {
                var statics = symbol is FieldSymbol field ? field.ContainingStaticsHolder() : null;   // in case field is a PHP static field
                if (statics != null)
                {
                    // PHP static field contained in a holder class
                    if (receiver != null)
                    {
                        cg.EmitPop(receiver);
                    }

                    // Template: <ctx>.GetStatics<_statics>()
                    cg.EmitLoadContext();
                    var t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Context.GetStatic_T.Symbol.Construct(statics)).Expect(statics);
                    return new LhsStack { Stack = t };
                }
                else
                {
                    if (receiver == null)
                    {
                        throw cg.NotImplementedException($"Non-static field {symbol.ContainingType.Name}::${symbol.MetadataName} accessed statically!");
                    }
                    else
                    {
                        var lhs = new LhsStack();

                        cg.EmitConvert(receiver, 0, lhs.Stack = symbol.ContainingType);

                        if (symbol.ContainingType.IsValueType)
                        {
                            cg.EmitStructAddr(symbol.ContainingType);
                            lhs.StackByRef = true;
                        }

                        return lhs;
                    }
                }
            }
        }

        public static LhsStack EmitReceiver(CodeGenerator cg, ref LhsStack lhs, Symbol symbol, BoundExpression receiver)
        {
            // TODO: try load Receiver address directly if necessary, otherwise cg.EmitStructAddr
            // TODO: use LhsStack

            var receiverType = receiver != null ? lhs.EmitReceiver(cg, receiver) : null;

            return EmitReceiver(cg, symbol, receiverType);
        }

        public static LhsStack EmitReceiver(ILBuilder il, IPlace receiver)
        {
            if (receiver == null)
            {
                return default;
            }

            var type = receiver.Type;
            if (type.IsValueType)
            {
                if (receiver.HasAddress)
                {
                    receiver.EmitLoadAddress(il);
                }
                else
                {
                    receiver.EmitLoad(il);
                    il.EmitStructAddr(type);
                }

                return new LhsStack { Stack = type, StackByRef = true, };
            }
            else
            {
                receiver.EmitLoad(il);

                return new LhsStack { Stack = type, StackByRef = false, };
            }
        }

        public static LhsStack EmitReceiver(CodeGenerator cg, IPlace receiver) => EmitReceiver(cg.Builder, receiver);
    }

    #endregion

    #region IVariableReference

    /// <summary>
    /// An object specifying a reference to a variable, a field, a property, an array item (a value in general).
    /// Used by <see cref="BoundReferenceExpression"/>.
    /// </summary>
    interface IVariableReference
    {
        /// <summary>
        /// Optional.
        /// Gets the referenced symbol.
        /// </summary>
        Symbol Symbol { get; }

        /// <summary>
        /// Gets native type of the variable.
        /// </summary>
        TypeSymbol Type { get; }

        /// <summary>
        /// Gets value indicating the native value can be accessed by address (<c>ref</c>).
        /// </summary>
        bool HasAddress { get; }

        /// <summary>
        /// Optional. Gets <see cref="IPlace"/> referring to the variable.
        /// </summary>
        /// <remarks>May be initialized lazily before emit and not during the analysis phase yet.</remarks>
        IPlace Place { get; }

        /// <summary>
        /// Prepare store operation for given access.
        /// Returns information on what was loaded onto the stack (receiver).
        /// </summary>
        LhsStack EmitStorePreamble(CodeGenerator cg, BoundAccess access);

        /// <summary>
        /// Stores the value on stack into the variable.
        /// </summary>
        /// <param name="cg">Reference to <see cref="CodeGenerator"/>.</param>
        /// <param name="lhs">Receiver loaded on stack by previous call to <see cref="EmitStorePreamble"/>.</param>
        /// <param name="stack">Value loaded on stack to be stored into the variable.</param>
        /// <param name="access">Access information.</param>
        void EmitStore(CodeGenerator cg, ref LhsStack lhs, TypeSymbol stack, BoundAccess access);

        /// <summary>
        /// Loads value with given access.
        /// </summary>
        /// <param name="cg">Reference to <see cref="CodeGenerator"/>.</param>
        /// <param name="lhs">Receiver loaded on stack with previous call to <see cref="EmitStorePreamble"/>.</param>
        /// <param name="access">Access information.</param>
        /// <returns>Loaded value (by value).</returns>
        TypeSymbol EmitLoadValue(CodeGenerator cg, ref LhsStack lhs, BoundAccess access);

        /// <summary>
        /// Loads value with given access.
        /// </summary>
        /// <returns>Loaded value (by ref).</returns>
        TypeSymbol EmitLoadAddress(CodeGenerator cg, ref LhsStack lhs);
    }

    #endregion

    #region Locals (local variables)

    /// <summary>
    /// Base class for local variables, parameters, $this, static locals and temporary (synthesized) locals.
    /// </summary>
    [DebuggerDisplay("Variable: ${Name,nq} : {Type,nq}")]
    class LocalVariableReference : IVariableReference
    {
        /// <summary>Variable kind.</summary>
        public VariableKind VariableKind { get; }

        /// <summary>Name of the variable.</summary>
        public string Name => BoundName.NameValue.Value ?? this.Symbol?.Name;

        /// <summary>
        /// Whether the variable is regular local on stack.
        /// Otherwise the variable is loaded from special ".locals" array of variables.
        /// </summary>
        internal virtual bool IsOptimized =>
            Symbol != null &&
            Routine.IsGlobalScope == false &&
            (Routine.Flags & FlowAnalysis.RoutineFlags.RequiresLocalsArray) == 0;

        public BoundVariableName BoundName { get; } // TODO: move to IVariableReference?

        public Symbol Symbol { get; protected set; }

        /// <summary>Containing routine symbol. Cannot be <c>null</c>.</summary>
        internal SourceRoutineSymbol Routine { get; }

        public virtual TypeSymbol Type => IsOptimized
            ? Symbol.GetTypeOrReturnType()
            : Routine.DeclaringCompilation.CoreTypes.PhpValue;

        public virtual bool HasAddress => true;

        public virtual IPlace Place { get; protected set; }

        public LocalVariableReference(VariableKind kind, SourceRoutineSymbol routine, Symbol symbol, BoundVariableName name)
        {
            this.VariableKind = kind;
            this.Routine = routine ?? throw ExceptionUtilities.ArgumentNull(nameof(routine));
            this.Symbol = symbol;
            this.BoundName = name ?? throw ExceptionUtilities.ArgumentNull(nameof(name));
        }

        /// <summary>
        /// Emits initialization of the variable if needed.
        /// Called from within <see cref="Graph.StartBlock"/>.
        /// </summary>
        public virtual void EmitInit(CodeGenerator cg)
        {
            if (IsOptimized == false || cg.InitializedLocals)
            {
                // do nothing,
                // Place == null
                return;
            }

            Debug.Assert(Symbol != null);

            // declare variable in global scope
            var il = cg.Builder;
            var def = il.LocalSlotManager.DeclareLocal(
                    (Cci.ITypeReference)Symbol.GetTypeOrReturnType(), Symbol as ILocalSymbolInternal,
                    this.Name, SynthesizedLocalKind.UserDefined,
                    LocalDebugId.None, 0, LocalSlotConstraints.None, ImmutableArray<bool>.Empty, ImmutableArray<string>.Empty, false);
            il.AddLocalToScope(def);

            this.Place = new LocalPlace(def);

            //
            if (Symbol is SynthesizedLocalSymbol)
            {
                return;
            }

            // Initialize local variable with void.
            // This is mandatory since even assignments reads the target value to assign properly to PhpAlias.

            // TODO: Once analysis tells us, the target cannot be alias, this step won't be necessary.

            // TODO: only if the local will be used uninitialized

            cg.EmitInitializePlace(Place);
        }

        TypeSymbol LoadVariablesArray(CodeGenerator cg)
        {
            Debug.Assert(Place == null);

            if (VariableKind == VariableKind.LocalTemporalVariable)
            {
                Debug.Assert(cg.TemporalLocalsPlace != null, $"Method with temporal variables must have 'CodeGenerator.{nameof(cg.TemporalLocalsPlace)}' set.");

                // LOAD <temp> : PhpArray
                return cg.TemporalLocalsPlace.EmitLoad(cg.Builder).Expect(cg.CoreTypes.PhpArray);
            }
            else
            {
                Debug.Assert(cg.LocalsPlaceOpt != null);

                // LOAD <locals> : PhpArray
                return cg.LocalsPlaceOpt.EmitLoad(cg.Builder).Expect(cg.CoreTypes.PhpArray);
            }
        }

        public virtual LhsStack EmitStorePreamble(CodeGenerator cg, BoundAccess access)
        {
            if (Place != null)
            {
                return Place.EmitStorePreamble(cg, access);
            }
            else
            {
                LoadVariablesArray(cg);         // PhpArray
                BoundName.EmitIntStringKey(cg); // IntStringKey

                // TODO: return LhsStack { ... }
            }

            //
            return default;
        }

        public virtual void EmitStore(CodeGenerator cg, ref LhsStack lhs, TypeSymbol stack, BoundAccess access)
        {
            if (Place != null)
            {
                Place.EmitStore(cg, ref lhs, stack, access);
            }
            else
            {
                // STACK: <PhpArray> <IntStringKey>

                if (stack == null) // IsUnset
                {
                    // .RemoveKey(key)
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.RemoveKey_IntStringKey);
                }
                else if (stack == cg.CoreTypes.PhpAlias) // IsWriteRef
                {
                    // .SetItemAlias(key, alias)
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.SetItemAlias_IntStringKey_PhpAlias);
                }
                else // IsWrite
                {
                    cg.EmitConvertToPhpValue(stack, 0);

                    // .SetItemValue(key, value)
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.SetItemValue_IntStringKey_PhpValue);
                }
            }
        }

        public virtual TypeSymbol EmitLoadValue(CodeGenerator cg, ref LhsStack lhs, BoundAccess access)
        {
            var place = this.Place;
            if (place != null)
            {
                return place.EmitLoadValue(cg, ref lhs, access);
            }
            else
            {
                LoadVariablesArray(cg);         // PhpArray
                BoundName.EmitIntStringKey(cg); // IntStringKey

                if (access.IsReadRef)
                {
                    // CALL <locals>.EnsureItemAlias(<key>) : PhpAlias
                    return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.EnsureItemAlias_IntStringKey);
                }
                else if (access.EnsureArray)
                {
                    // CALL <locals>.EnsureItemArray(<key>) : IPhpArray
                    return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.EnsureItemArray_IntStringKey);
                }
                else if (access.EnsureObject)
                {
                    // CALL <locals>.EnsureItemObject(<key>) : object
                    return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.EnsureItemObject_IntStringKey);
                }
                else if (access.IsRead)
                {
                    // CALL <locals>.GetItemValue(<key>) : PhpValue
                    return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.GetItemValue_IntStringKey);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(access);
                }
            }
        }

        public virtual TypeSymbol EmitLoadAddress(CodeGenerator cg, ref LhsStack lhs)
        {
            if (Place != null)
            {
                Place.EmitLoadAddress(cg.Builder);
                return Place.Type;
            }
            else
            {
                LoadVariablesArray(cg);         // PhpArray
                BoundName.EmitIntStringKey(cg); // IntStringKey

                // Template: ref PhpArray.GetItemRef(key)
                Debug.Assert(cg.CoreMethods.PhpArray.GetItemRef_IntStringKey.Symbol.ReturnValueIsByRef);
                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.GetItemRef_IntStringKey);
            }
        }

        /// <summary>
        /// Template: new IndirectLocal( LOCALS, NAME )
        /// </summary>
        internal TypeSymbol LoadIndirectLocal(CodeGenerator cg)
        {
            LoadVariablesArray(cg);
            BoundName.EmitIntStringKey(cg);
            return cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.IndirectLocal_PhpArray_IntStringKey);
        }
    }

    class ParameterReference : LocalVariableReference
    {
        #region IParameterSource, IParameterTarget

        static void EmitTypeCheck(CodeGenerator cg, IPlace valueplace, SourceParameterSymbol srcparam)
        {
            // TODO: check iterable, type if not resolved in ct

            // check NotNull
            if (srcparam.HasNotNull)
            {
                if ((valueplace.Type.IsReferenceType /*|| valueplace.Type.Is_PhpValue()*/) && valueplace.Type != cg.CoreTypes.PhpAlias)
                {
                    cg.EmitSequencePoint(srcparam.Syntax);

                    // Template: PhpException.ArgumentNullError( value, arg )
                    if (valueplace.Type.IsReferenceType)
                    {
                        valueplace.EmitLoad(cg.Builder);
                    }
                    else if (valueplace.Type.Is_PhpValue())
                    {
                        cg.CoreMethods.PhpValue.Object.Symbol.EmitLoadValue(cg, valueplace);
                    }
                    else
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                    cg.Builder.EmitIntConstant(srcparam.ParameterIndex + 1);
                    cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.ThrowIfArgumentNull_object_int));
                }
            }

            // check callable
            if (srcparam.Syntax.TypeHint.IsCallable())
            {
                cg.EmitSequencePoint(srcparam.Syntax);

                // Template: PhpException.ThrowIfArgumentNotCallable(<ctx>, current RuntimeTypeHandle, value, arg)
                cg.EmitLoadContext();
                cg.EmitCallerTypeHandle();
                cg.EmitConvertToPhpValue(valueplace.EmitLoad(cg.Builder), default);     // To handle conversion from PhpAlias when the parameter is by ref
                cg.Builder.EmitBoolConstant(!srcparam.HasNotNull);
                cg.Builder.EmitIntConstant(srcparam.ParameterIndex + 1);
                cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.ThrowIfArgumentNotCallable_Context_RuntimeTypeHandle_PhpValue_Bool_int));
            }
        }

        /// <summary>
        /// Describes the parameter source place.
        /// </summary>
        interface IParameterSource
        {
            void EmitTypeCheck(CodeGenerator cg, SourceParameterSymbol srcp);

            /// <summary>Inplace copies the parameter.</summary>
            void EmitPass(CodeGenerator cg);

            /// <summary>Loads copied parameter value.</summary>
            TypeSymbol EmitLoad(CodeGenerator cg);
        }

        /// <summary>
        /// Describes the local variable target slot.
        /// </summary>
        interface IParameterTarget
        {
            void StorePrepare(CodeGenerator cg);
            void Store(CodeGenerator cg, TypeSymbol valuetype);
        }

        /// <summary>
        /// Parameter or local is real CLR value on stack.
        /// </summary>
        sealed class DirectParameter : IParameterSource, IParameterTarget
        {
            readonly IPlace _place;
            readonly SourceParameterSymbol _param;

            public DirectParameter(IPlace place, SourceParameterSymbol param)
            {
                Debug.Assert(place != null);
                _place = place;
                _param = param;
            }

            /// <summary>Loads copied parameter value.</summary>
            public TypeSymbol EmitLoad(CodeGenerator cg)
            {
                if (_param.IsParams)
                {
                    // converts params -> PhpArray
                    Debug.Assert(_place.Type.IsSZArray());
                    return cg.ArrayToPhpArray(_place, deepcopy: true);
                }
                else
                {
                    // load parameter & dereference PhpValue
                    TypeSymbol t;
                    if (_place.Type == cg.CoreTypes.PhpValue)
                    {
                        // p.GetValue() : PhpValue
                        _place.EmitLoadAddress(cg.Builder);
                        t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.GetValue);
                    }
                    else
                    {
                        // p
                        t = _place.EmitLoad(cg.Builder);
                    }

                    if (_param.CopyOnPass)
                    {
                        // make copy of given value
                        return cg.EmitDeepCopy(t, nullcheck: !_param.HasNotNull);
                    }
                    else
                    {
                        return t;
                    }
                }
            }

            public void EmitPass(CodeGenerator cg)
            {
                if (_param.CopyOnPass == false ||
                    cg.IsCopiable(_place.Type) == false)
                {
                    // copy is not necessary
                    return;
                }

                // inplace copies the parameter

                if (_place.Type == cg.CoreTypes.PhpValue)
                {
                    // dereference & copy
                    // PassValue( ref <param> )
                    _place.EmitLoadAddress(cg.Builder);
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.PassValue_PhpValueRef);
                }
                else
                {
                    _place.EmitStorePrepare(cg.Builder);

                    // copy
                    // <param> = DeepCopy(<param>)
                    cg.EmitDeepCopy(_place.EmitLoad(cg.Builder), nullcheck: !_param.HasNotNull);

                    _place.EmitStore(cg.Builder);
                }
            }

            public void EmitTypeCheck(CodeGenerator cg, SourceParameterSymbol srcp)
            {
                ParameterReference.EmitTypeCheck(cg, _place, srcp);
            }

            public void Store(CodeGenerator cg, TypeSymbol valuetype)
            {
                cg.EmitConvert(valuetype, 0, _place.Type);
                _place.EmitStore(cg.Builder);
            }

            public void StorePrepare(CodeGenerator cg)
            {
                _place.EmitStorePrepare(cg.Builder); // nop
            }
        }

        /// <summary>
        /// Parameter is fake and is stored in {varargs} array.
        /// </summary>
        sealed class IndirectParameterSource : IParameterSource
        {
            readonly IPlace _varargsplace;
            readonly int _index;
            bool _isparams => _p.IsParams;
            bool _byref => _p.Syntax.PassedByRef;

            readonly SourceParameterSymbol _p;

            public IndirectParameterSource(SourceParameterSymbol p, ParameterSymbol varargparam)
            {
                Debug.Assert(p.IsFake);
                Debug.Assert(varargparam.Type.IsSZArray());

                _p = p;
                _varargsplace = new ParamPlace(varargparam);
                _index = p.Ordinal - varargparam.Ordinal;
                Debug.Assert(_index >= 0);
            }

            public TypeSymbol EmitLoad(CodeGenerator cg)
            {
                if (_isparams)
                {
                    // PhpArray( {varargs[index..] )
                    return cg.ArrayToPhpArray(_varargsplace, startindex: _index, deepcopy: true);
                }
                else
                {
                    var il = cg.Builder;

                    var lbl_default = new object();
                    var lbl_end = new object();

                    var element_type = ((ArrayTypeSymbol)_varargsplace.Type).ElementType;

                    // Template: _index < {vargags}.Length ? {varargs[_index]} : DEFAULT

                    // _index < {varargs}.Length
                    il.EmitIntConstant(_index);
                    _varargsplace.EmitLoad(il);
                    cg.EmitArrayLength();
                    il.EmitBranch(ILOpCode.Bge, lbl_default);

                    // LOAD varargs[index]
                    _varargsplace.EmitLoad(il);
                    il.EmitIntConstant(_index);
                    il.EmitOpCode(ILOpCode.Ldelem);
                    cg.EmitSymbolToken(element_type, null);
                    cg.EmitConvertToPhpValue(cg.EmitDeepCopy(element_type, nullcheck: false), 0);
                    il.EmitBranch(ILOpCode.Br, lbl_end);

                    // DEFAULT
                    il.MarkLabel(lbl_default);

                    if (_p.Initializer != null)
                    {
                        using (var tmpcg = new CodeGenerator(cg, _p.Routine))
                        {
                            tmpcg.EmitConvertToPhpValue(_p.Initializer);
                        }
                    }
                    else
                    {
                        cg.Emit_PhpValue_Null();
                    }

                    //
                    il.MarkLabel(lbl_end);

                    //
                    return cg.CoreTypes.PhpValue;
                }
            }

            public void EmitPass(CodeGenerator cg) => throw ExceptionUtilities.Unreachable;

            public void EmitTypeCheck(CodeGenerator cg, SourceParameterSymbol srcp)
            {
                // throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Local variables are unoptimized, parameter must be stored in {locals} array.
        /// </summary>
        sealed class IndirectLocalTarget : IParameterTarget
        {
            readonly string _localname;

            public IndirectLocalTarget(string localname)
            {
                _localname = localname;
            }

            public void StorePrepare(CodeGenerator cg)
            {
                Debug.Assert(cg.LocalsPlaceOpt != null);

                // LOAD <locals>, <name>
                cg.LocalsPlaceOpt.EmitLoad(cg.Builder);             // <locals>
                cg.EmitIntStringKey(new BoundLiteral(_localname));  // [key]
            }

            public void Store(CodeGenerator cg, TypeSymbol valuetype)
            {
                // Template: {PhpArray}.Add({name}, {value})
                cg.EmitConvertToPhpValue(valuetype, 0);
                cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Add_IntStringKey_PhpValue));  // TODO: Append() without duplicity check
            }
        }

        #endregion

        public ParameterSymbol Parameter => (ParameterSymbol)Symbol;

        public override TypeSymbol Type => Place != null ? Place.Type : base.Type;

        public ParameterReference(ParameterSymbol symbol, SourceRoutineSymbol routine)
            : base(VariableKind.Parameter, routine, symbol, new BoundVariableName(symbol.Name))
        {

        }

        public override void EmitInit(CodeGenerator cg)
        {
            if (cg.InitializedLocals)
            {
                return;
            }

            var srcparam = Symbol as SourceParameterSymbol;
            if (srcparam == null)
            {
                Place = new ParamPlace(Parameter);

                // an implicit parameter,
                // nothing to initialize
                return;
            }

            IPlace lazyPlace = null;

            //
            // source: real parameter OR fake parameter: IParameterSource { TypeCheck, Pass, Load }
            // target: optimized locals OR unoptimized locals: IParameterTarget { StorePrepare, Store }
            //

            var source = srcparam.IsFake
                ? (IParameterSource)new IndirectParameterSource(srcparam, srcparam.Routine.GetParamsParameter())
                : (IParameterSource)new DirectParameter(new ParamPlace(srcparam), srcparam);

            if (cg.HasUnoptimizedLocals == false) // usual case - optimized locals
            {
                // TODO: cleanup
                var tmask = srcparam.Routine.ControlFlowGraph.GetLocalTypeMask(srcparam.Name);
                var clrtype = cg.DeclaringCompilation.GetTypeFromTypeRef(srcparam.Routine, tmask);

                // target local must differ from source parameter ?
                if (srcparam.IsFake || (srcparam.Type != cg.CoreTypes.PhpValue && srcparam.Type != cg.CoreTypes.PhpAlias && srcparam.Type != clrtype))
                {
                    var loc = cg.Builder.LocalSlotManager.DeclareLocal(
                        (Cci.ITypeReference)clrtype, new SynthesizedLocalSymbol(srcparam.Routine, srcparam.Name, clrtype), srcparam.Name,
                        SynthesizedLocalKind.UserDefined, LocalDebugId.None, 0, LocalSlotConstraints.None, ImmutableArray<bool>.Empty, ImmutableArray<string>.Empty, false);
                    lazyPlace = new LocalPlace(loc);
                    cg.Builder.AddLocalToScope(loc);
                }
            }

            var target = cg.HasUnoptimizedLocals
                ? (IParameterTarget)new IndirectLocalTarget(srcparam.Name)
                : (lazyPlace != null)
                    ? new DirectParameter(lazyPlace, srcparam)
                    : (DirectParameter)source;

            // 1. TypeCheck
            source.EmitTypeCheck(cg, srcparam);

            if (source == target)
            {
                // 2a. (source == target): Pass (inplace copy and dereference) if necessary
                source.EmitPass(cg);
            }
            else
            {
                // 2b. (source != target): StorePrepare -> Load&Copy -> Store
                target.StorePrepare(cg);
                var loaded = source.EmitLoad(cg);
                target.Store(cg, loaded);
            }

            if (lazyPlace != null)
            {
                // TODO: perf warning
            }

            Place = lazyPlace ?? (cg.HasUnoptimizedLocals ? null : new ParamPlace(Parameter));

            // TODO: ? if (cg.HasUnoptimizedLocals && $this) <locals>["this"] = ...
        }
    }

    class ThisVariableReference : LocalVariableReference
    {
        public ThisVariableReference(SourceRoutineSymbol routine)
            : base(VariableKind.ThisParameter, routine, null, new BoundVariableName(VariableName.ThisVariableName))
        {
        }

        internal override bool IsOptimized => true;

        public override IPlace Place
        {
            get => Routine.GetPhpThisVariablePlace();
            protected set => throw ExceptionUtilities.Unreachable;
        }

        public override bool HasAddress => false;

        public override TypeSymbol Type => Place.Type;

        public override void EmitInit(CodeGenerator cg)
        {
            if (cg.HasUnoptimizedLocals)
            {
                // TODO: <locals>["this"] = this;
            }

            // nada
        }

        public override TypeSymbol EmitLoadValue(CodeGenerator cg, ref LhsStack lhs, BoundAccess access)
        {
            if (cg.GeneratorStateMachineMethod != null)
            {
                return (new ParamPlace(cg.GeneratorStateMachineMethod.ThisParameter)).EmitLoadValue(cg, ref lhs, access);
                // TODO: access.IsReadRef
            }

            if (access.IsReadRef)
            {
                // just wrap this into PhpAlias
                var t = base.EmitLoadValue(cg, ref lhs, BoundAccess.Read);
                cg.EmitConvertToPhpValue(t, 0);
                return cg.Emit_PhpValue_MakeAlias(); // : PhpAlias
            }

            //

            return base.EmitLoadValue(cg, ref lhs, access);
        }

        public override LhsStack EmitStorePreamble(CodeGenerator cg, BoundAccess access)
        {
            // should be handled in DiagnosticWalker before this happens
            throw ExceptionUtilities.Unreachable;
        }

        public override void EmitStore(CodeGenerator cg, ref LhsStack lhs, TypeSymbol stack, BoundAccess access)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }

    class SuperglobalVariableReference : LocalVariableReference
    {
        new VariableName Name => BoundName.NameValue;

        PropertySymbol/*!*/ResolveSuperglobalProperty(PhpCompilation compilation)
        {
            PropertySymbol prop;

            var c = compilation.CoreMethods.Context;

            if (Name == VariableName.GlobalsName) prop = c.Globals;
            else if (Name == VariableName.ServerName) prop = c.Server;
            else if (Name == VariableName.RequestName) prop = c.Request;
            else if (Name == VariableName.GetName) prop = c.Get;
            else if (Name == VariableName.PostName) prop = c.Post;
            else if (Name == VariableName.CookieName) prop = c.Cookie;
            else if (Name == VariableName.EnvName) prop = c.Env;
            else if (Name == VariableName.FilesName) prop = c.Files;
            else if (Name == VariableName.SessionName) prop = c.Session;
            else if (Name == VariableName.HttpRawPostDataName) prop = c.HttpRawPostData;
            else throw ExceptionUtilities.UnexpectedValue(Name);

            return prop;
        }

        internal override bool IsOptimized => true;

        public override TypeSymbol Type => ResolveSuperglobalProperty(Routine.DeclaringCompilation).Type;

        public override bool HasAddress => false;

        public SuperglobalVariableReference(VariableName name, SourceRoutineSymbol routine)
            : base(VariableKind.GlobalVariable, routine, null, new BoundVariableName(name))
        {
            Debug.Assert(name.IsAutoGlobal);
        }

        public override void EmitInit(CodeGenerator cg)
        {
            // just initialize Place
            if (cg.ContextPlaceOpt != null)
            {
                Place = new PropertyPlace(cg.ContextPlaceOpt, ResolveSuperglobalProperty(cg.DeclaringCompilation), cg.Module);
            }
            else
            {
                // unexpected
                throw ExceptionUtilities.Unreachable;
            }

            // nada
        }

        public override TypeSymbol EmitLoadValue(CodeGenerator cg, ref LhsStack lhsStack, BoundAccess access)
        {
            if (access.IsReadRef)
            {
                // TODO: update Context
                // &$<_name>
                // Template: ctx.Globals.EnsureAlias(<_name>)
                cg.EmitLoadContext();
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Context.Globals.Getter);
                BoundName.EmitIntStringKey(cg);
                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.EnsureItemAlias_IntStringKey)
                    .Expect(cg.CoreTypes.PhpAlias);
            }
            else
            {
                return base.EmitLoadValue(cg, ref lhsStack, access);
            }
        }
    }

    #endregion

    #region Fields, Properties

    class FieldReference : IVariableReference
    {
        public BoundExpression Receiver { get; } // can be null

        public FieldSymbol Field => (FieldSymbol)Symbol;

        public Symbol Symbol { get; }

        public TypeSymbol Type => Field.Type;

        public bool HasAddress => true;

        public IPlace Place
        {
            get
            {
                if (Receiver == null)
                {
                    // _statics holder ?
                    if (!Field.IsStatic)
                    {
                        Debug.Assert(Field.ContainingStaticsHolder() != null, "Field is non-static and does not have the receiver instance!");
                        return null;    // new FieldPlace ( Receiver: Context.GetStatics<Holder>(), Field );
                    }

                    return new FieldPlace(null, Field);
                }

                if (Receiver is BoundReferenceExpression bref && bref.Place() is IPlace receiver_place && receiver_place.Type.IsOfType(Field.ContainingType))
                {
                    return new FieldPlace(receiver_place, Field);
                }

                return null;
            }
        }

        public FieldReference(BoundExpression receiver, FieldSymbol/*!*/field)
        {
            this.Receiver = receiver;
            this.Symbol = field ?? throw ExceptionUtilities.ArgumentNull(nameof(field));
        }

        public LhsStack EmitStorePreamble(CodeGenerator cg, BoundAccess access)
        {
            LhsStack lhs = default;

            var fieldplace = new FieldPlace_Raw(Field, cg.Module);

            return
                VariableReferenceExtensions.EmitReceiver(cg, ref lhs, Field, Receiver) +
                fieldplace.EmitStorePreamble(cg, access);
        }

        public void EmitStore(CodeGenerator cg, ref LhsStack lhs, TypeSymbol stack, BoundAccess access)
        {
            if (Field.IsConst)
            {
                throw ExceptionUtilities.Unreachable; // cannot assign to const and analysis should report it already
            }

            new FieldPlace_Raw(Field, cg.Module).EmitStore(cg, ref lhs, stack, access);
        }

        public TypeSymbol EmitLoadValue(CodeGenerator cg, ref LhsStack lhs, BoundAccess access)
        {
            if (Field.IsConst)
            {
                return cg.EmitLoadConstant(Field.ConstantValue);
            }

            VariableReferenceExtensions.EmitReceiver(cg, ref lhs, Field, Receiver);

            if (access.IsQuiet && Receiver != null && cg.CanBeNull(Receiver.TypeRefMask))
            {
                // handle nullref in "quiet" mode (e.g. within empty() expression),
                // emit something like C#'s "?." operator

                //  .dup ? .ldfld : default

                // cg.EmitNullCoalescing( , ) but we need the resulting type

                var _il = cg.Builder;
                var lbl_null = new NamedLabel("ReceiverNull");
                var lbl_end = new object();

                _il.EmitOpCode(ILOpCode.Dup);
                _il.EmitBranch(ILOpCode.Brfalse, lbl_null);
                var type = new FieldPlace_Raw(Field, cg.Module).EmitLoadValue(cg, ref lhs, access); // .field

                _il.EmitBranch(ILOpCode.Br, lbl_end);

                _il.MarkLabel(lbl_null);
                _il.EmitOpCode(ILOpCode.Pop);
                cg.EmitLoadDefault(type); // default

                _il.MarkLabel(lbl_end);

                //
                return type;
            }
            else
            {
                return new FieldPlace_Raw(Field, cg.Module).EmitLoadValue(cg, ref lhs, access);
            }
        }

        public TypeSymbol EmitLoadAddress(CodeGenerator cg, ref LhsStack lhs)
        {
            VariableReferenceExtensions.EmitReceiver(cg, ref lhs, Field, Receiver);

            new FieldPlace_Raw(Field, cg.Module).EmitLoadAddress(cg.Builder);

            return Field.Type;
        }
    }

    class PropertyReference : IVariableReference
    {
        public BoundExpression Receiver { get; } // can be null

        public PropertySymbol Property => (PropertySymbol)Symbol;

        public Symbol Symbol { get; }

        public TypeSymbol Type => Property.Type;

        public bool HasAddress => false;

        public IPlace Place
        {
            get
            {
                if (Receiver == null)
                    return new PropertyPlace(null, Property);

                if (Receiver is BoundReferenceExpression bref && bref.Place() is IPlace receiver_place && receiver_place.Type.IsOfType(Property.ContainingType))
                    return new PropertyPlace(receiver_place, Property);

                return null;
            }
        }

        public PropertyReference(BoundExpression receiver, PropertySymbol/*!*/prop)
        {
            this.Receiver = receiver;
            this.Symbol = prop ?? throw ExceptionUtilities.ArgumentNull(nameof(prop));
        }

        public LhsStack EmitStorePreamble(CodeGenerator cg, BoundAccess access)
        {
            LhsStack lhs = default;
            return VariableReferenceExtensions.EmitReceiver(cg, ref lhs, Symbol, Receiver);
        }

        public void EmitStore(CodeGenerator cg, ref LhsStack lhs, TypeSymbol stack, BoundAccess access)
        {
            var setter = Property.SetMethod;
            var type = setter.Parameters[0].Type;

            if (stack == null) // unset
            {
                stack = cg.EmitLoadDefault(type, 0);
            }

            cg.EmitConvert(stack, 0, type, conversion: ConversionKind.Strict);
            cg.EmitCall(setter.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call, setter);
        }

        public TypeSymbol EmitLoadValue(CodeGenerator cg, ref LhsStack lhs, BoundAccess access)
        {
            VariableReferenceExtensions.EmitReceiver(cg, ref lhs, Symbol, Receiver);

            var getter = Property.GetMethod;
            return cg.EmitCall(getter.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call, getter);

            // TODO: ACCESS
        }

        public TypeSymbol EmitLoadAddress(CodeGenerator cg, ref LhsStack lhsStack)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }

    class IndirectProperty : IVariableReference
    {
        readonly BoundFieldRef _boundfield;

        public BoundVariableName Name => _boundfield.FieldName;
        public BoundExpression Instance => _boundfield.Instance;
        public string NameValueOpt => _boundfield.FieldName.NameValue.Value;
        public BoundAccess Access => _boundfield.Access;

        DynamicOperationFactory.CallSiteData _lazyLoadCallSite = null;
        DynamicOperationFactory.CallSiteData _lazyStoreCallSite = null;

        public IndirectProperty(BoundFieldRef boundfield)
        {
            _boundfield = boundfield ?? throw ExceptionUtilities.ArgumentNull(nameof(boundfield));
        }

        public Symbol Symbol => null;

        public TypeSymbol Type => null;

        public bool HasAddress => false;

        public IPlace Place => null;

        void EmitLoadPreamble(CodeGenerator cg, ref LhsStack lhs)
        {
            if (_lazyLoadCallSite == null)
                _lazyLoadCallSite = cg.Factory.StartCallSite("get_" + this.NameValueOpt);

            _lazyLoadCallSite.Prepare(cg);

            // callsite.Target callsite
            _lazyLoadCallSite.EmitLoadTarget();
            _lazyLoadCallSite.EmitLoadCallsite();

            if (_boundfield.IsInstanceField)
            {
                // target instance
                _lazyLoadCallSite.EmitTargetInstance(lhs.EmitReceiver(cg, Instance));
            }
            else if (_boundfield.IsStaticField || _boundfield.IsClassConstant)
            {
                // LOAD PhpTypeInfo
                _lazyLoadCallSite.EmitTargetTypeParam(_boundfield.ContainingType);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(_boundfield);
            }
        }

        public TypeSymbol EmitLoadValue(CodeGenerator cg, ref LhsStack lhs, BoundAccess access)
        {
            EmitLoadPreamble(cg, ref lhs);

            Debug.Assert(_lazyLoadCallSite != null);

            // resolve actual return type
            TypeSymbol return_type;
            if (Access.EnsureObject) return_type = cg.CoreTypes.Object;
            else if (Access.EnsureArray) return_type = cg.CoreTypes.IPhpArray;
            else if (Access.IsReadRef) return_type = cg.CoreTypes.PhpAlias;
            else if (Access.IsIsSet) return_type = cg.CoreTypes.Boolean;
            else return_type = Access.TargetType ?? cg.CoreTypes.PhpValue;

            // Template: Invoke(TInstance, Context, [string name])

            _lazyLoadCallSite.EmitLoadContext();                    // ctx : Context
            _lazyLoadCallSite.EmitNameParam(Name.NameExpression);   // [name] : string
            _lazyLoadCallSite.EmitCallerTypeParam();                // [classctx] : RuntimeTypeHandle

            // Target()
            var functype = cg.Factory.GetCallSiteDelegateType(
                null, RefKind.None,
                _lazyLoadCallSite.Arguments,
                _lazyLoadCallSite.ArgumentsRefKinds,
                null,
                return_type);

            cg.EmitCall(ILOpCode.Callvirt, functype.DelegateInvokeMethod);

            //
            _lazyLoadCallSite.Construct(functype, cctor =>
            {
                // new GetFieldBinder(field_name, context, return, flags)
                cctor.Builder.EmitStringConstant(this.NameValueOpt);
                cctor.EmitLoadToken(cg.CallerType, null);           // class context
                cctor.EmitLoadToken(return_type, null);
                cctor.Builder.EmitIntConstant((int)Access.Flags);
                cctor.EmitCall(ILOpCode.Call,
                    _boundfield.IsClassConstant
                        ? cg.CoreMethods.Dynamic.GetClassConstBinder
                        : cg.CoreMethods.Dynamic.GetFieldBinder
                );
            });

            //
            return return_type;
        }

        public TypeSymbol EmitLoadAddress(CodeGenerator cg, ref LhsStack lhsStack)
        {
            throw new InvalidOperationException();
        }

        public LhsStack EmitStorePreamble(CodeGenerator cg, BoundAccess access)
        {
            var lhs = new LhsStack { CodeGenerator = cg, IsEnabled = access.IsRead, };

            //

            if (_lazyStoreCallSite == null)
                _lazyStoreCallSite = cg.Factory.StartCallSite("set_" + this.NameValueOpt);

            _lazyStoreCallSite.Prepare(cg);

            // callsite.Target callsite
            _lazyStoreCallSite.EmitLoadTarget();
            _lazyStoreCallSite.EmitLoadCallsite();

            if (_boundfield.IsInstanceField)
            {
                // target instance
                _lazyStoreCallSite.EmitTargetInstance(lhs.EmitReceiver(cg, Instance));
            }
            else if (_boundfield.IsStaticField || _boundfield.IsClassConstant)
            {
                // LOAD PhpTypeInfo
                _lazyStoreCallSite.EmitTargetTypeParam(_boundfield.ContainingType);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(_boundfield);
            }

            // ctx : Context
            _lazyStoreCallSite.EmitLoadContext();

            // [name] : string
            _lazyStoreCallSite.EmitNameParam(Name.NameExpression);

            // [classctx] : RuntimeTypeHandle
            _lazyStoreCallSite.EmitCallerTypeParam();                // [classctx] : RuntimeTypeHandle

            //
            return lhs;
        }

        public void EmitStore(CodeGenerator cg, ref LhsStack lhs, TypeSymbol stack, BoundAccess access)
        {
            Debug.Assert(_lazyStoreCallSite != null);

            // Template: Invoke(TInstance, Context, [string name], [value])

            if (stack != null)
            {
                _lazyStoreCallSite.AddArg(stack, byref: false);
            }

            // Target()
            var functype = cg.Factory.GetCallSiteDelegateType(
                null, RefKind.None,
                _lazyStoreCallSite.Arguments,
                _lazyStoreCallSite.ArgumentsRefKinds,
                null,
                cg.CoreTypes.Void);

            cg.EmitCall(ILOpCode.Callvirt, functype.DelegateInvokeMethod);

            _lazyStoreCallSite.Construct(functype, cctor =>
            {
                cctor.Builder.EmitStringConstant(this.NameValueOpt);
                cctor.EmitLoadToken(cg.CallerType, null);           // class context
                cctor.Builder.EmitIntConstant((int)Access.Flags);   // flags
                cctor.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.SetFieldBinder);
            });
        }
    }

    #endregion

    #region Misc

    sealed class PlaceReference : IVariableReference
    {
        public PlaceReference(IPlace place)
        {
            Place = place ?? throw ExceptionUtilities.ArgumentNull(nameof(place));
        }

        public static IVariableReference Create(IPlace place) => place != null ? new PlaceReference(place) : null;

        public IPlace Place { get; }

        public Symbol Symbol => throw new NotImplementedException();

        public TypeSymbol Type => Place.Type;

        public bool HasAddress => Place.HasAddress;

        public TypeSymbol EmitLoadAddress(CodeGenerator cg, ref LhsStack lhsStack)
        {
            Place.EmitLoadAddress(cg.Builder);
            return Place.Type;
        }

        public TypeSymbol EmitLoadValue(CodeGenerator cg, ref LhsStack lhs, BoundAccess access)
        {
            return Place.EmitLoadValue(cg, ref lhs, access);
        }

        public LhsStack EmitStorePreamble(CodeGenerator cg, BoundAccess access)
        {
            return Place.EmitStorePreamble(cg, access);
        }

        public void EmitStore(CodeGenerator cg, ref LhsStack lhs, TypeSymbol stack, BoundAccess access)
        {
            Place.EmitStore(cg, ref lhs, stack, access);
        }
    }

    #endregion
}
