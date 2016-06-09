using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.CodeGen
{
    #region IPlace

    /// <summary>
    /// Interface supported by storage places with address.
    /// </summary>
    internal interface IPlace
    {
        /// <summary>
        /// Gets the type of place.
        /// </summary>
        TypeSymbol TypeOpt { get; }

        /// <summary>
        /// Emits code that loads the value from this storage place.
        /// </summary>
        /// <param name="il">The <see cref="ILBuilder"/> to emit the code to.</param>
        TypeSymbol EmitLoad(ILBuilder il);

        /// <summary>
        /// Emits preparation code for storing a value into the place.
        /// Must be call before loading a value and calling <see cref="EmitStore(ILBuilder)"/>.
        /// </summary>
        /// <param name="il">The <see cref="ILBuilder"/> to emit the code to.</param>
        void EmitStorePrepare(ILBuilder il);

        /// <summary>
        /// Emits code that stores a value to this storage place.
        /// </summary>
        /// <param name="il">The <see cref="ILBuilder"/> to emit the code to.</param>
        void EmitStore(ILBuilder il);

        /// <summary>
        /// Emits code that loads address of this storage place.
        /// </summary>
        /// <param name="il">The <see cref="ILBuilder"/> to emit the code to.</param>
        void EmitLoadAddress(ILBuilder il);

        /// <summary>
        /// Gets whether the place has an address.
        /// </summary>
        bool HasAddress { get; }
    }

    #endregion

    #region Places

    internal class LocalPlace : IPlace
    {
        readonly LocalDefinition _def;

        public override string ToString() => $"${_def.Name}";

        public LocalPlace(LocalDefinition def)
        {
            Contract.ThrowIfNull(def);
            _def = def;
        }

        public TypeSymbol TypeOpt => (TypeSymbol)_def.Type;

        public bool HasAddress => true;

        public TypeSymbol EmitLoad(ILBuilder il)
        {
            il.EmitLocalLoad(_def);
            return (TypeSymbol)_def.Type;
        }

        public void EmitLoadAddress(ILBuilder il) => il.EmitLocalAddress(_def);

        public void EmitStorePrepare(ILBuilder il) { }

        public void EmitStore(ILBuilder il) => il.EmitLocalStore(_def);
    }

    internal class ParamPlace : IPlace
    {
        readonly ParameterSymbol _p;

        public int Index => ((MethodSymbol)_p.ContainingSymbol).HasThis ? _p.Ordinal + 1 : _p.Ordinal;

        public override string ToString() => $"${_p.Name}";

        public ParamPlace(ParameterSymbol p)
        {
            Contract.ThrowIfNull(p);
            _p = p;
        }

        public TypeSymbol TypeOpt => _p.Type;

        public bool HasAddress => true;

        public TypeSymbol EmitLoad(ILBuilder il)
        {
            il.EmitLoadArgumentOpcode(Index);
            return _p.Type;
        }

        public void EmitLoadAddress(ILBuilder il) => il.EmitLoadArgumentAddrOpcode(Index);

        public void EmitStorePrepare(ILBuilder il) { }

        public void EmitStore(ILBuilder il) => il.EmitStoreArgumentOpcode(Index);
    }

    internal class ArgPlace : IPlace
    {
        readonly int _index;
        readonly TypeSymbol _type;

        public int Index => _index;

        public override string ToString() => $"${_index}";

        public ArgPlace(TypeSymbol t, int index)
        {
            Contract.ThrowIfNull(t);
            _type = t;
            _index = index;
        }

        public TypeSymbol TypeOpt => _type;

        public bool HasAddress => true;

        public TypeSymbol EmitLoad(ILBuilder il)
        {
            il.EmitLoadArgumentOpcode(Index);
            return _type;
        }

        public void EmitLoadAddress(ILBuilder il) => il.EmitLoadArgumentAddrOpcode(Index);

        public void EmitStorePrepare(ILBuilder il) { }

        public void EmitStore(ILBuilder il) => il.EmitStoreArgumentOpcode(Index);
    }

    /// <summary>
    /// Place wrapper allowing only read operation.
    /// </summary>
    internal class ReadOnlyPlace : IPlace
    {
        readonly IPlace _place;

        public ReadOnlyPlace(IPlace place)
        {
            Contract.ThrowIfNull(place);
            _place = place;
        }

        public bool HasAddress => _place.HasAddress;

        public TypeSymbol TypeOpt => _place.TypeOpt;

        public TypeSymbol EmitLoad(ILBuilder il) => _place.EmitLoad(il);

        public void EmitLoadAddress(ILBuilder il) => _place.EmitLoadAddress(il);

        public void EmitStore(ILBuilder il)
        {
            throw new InvalidOperationException($"{_place} is readonly!");  // TODO: ErrCode
        }

        public void EmitStorePrepare(ILBuilder il) { }
    }

    internal class FieldPlace : IPlace
    {
        readonly IPlace _holder;
        readonly FieldSymbol _field;

        public FieldPlace(IPlace holder, IFieldSymbol field)
        {
            Contract.ThrowIfNull(field);
            Debug.Assert(holder != null || field.IsStatic);

            _holder = holder;
            _field = (FieldSymbol)field;
        }

        void EmitHolder(ILBuilder il)
        {
            Debug.Assert(_field.IsStatic == (_holder == null));

            if (_holder != null)
            {
                _holder.EmitLoad(il);
            }
        }

        void EmitOpCode(ILBuilder il, ILOpCode code)
        {
            il.EmitOpCode(code);
            il.EmitToken(_field, null, DiagnosticBag.GetInstance());    // .{field}
        }

        public TypeSymbol TypeOpt => _field.Type;

        public bool HasAddress => true;

        public TypeSymbol EmitLoad(ILBuilder il)
        {
            EmitHolder(il);
            EmitOpCode(il, _field.IsStatic ? ILOpCode.Ldsfld : ILOpCode.Ldfld);
            return _field.Type;
        }

        public void EmitStorePrepare(ILBuilder il)
        {
            EmitHolder(il);
        }

        public void EmitStore(ILBuilder il)
        {
            EmitOpCode(il, _field.IsStatic ? ILOpCode.Stsfld : ILOpCode.Stfld);
        }

        public void EmitLoadAddress(ILBuilder il)
        {
            EmitHolder(il);
            EmitOpCode(il, _field.IsStatic ? ILOpCode.Ldsflda : ILOpCode.Ldflda);
        }
    }

    internal class PropertyPlace : IPlace
    {
        readonly IPlace _holder;
        readonly PropertySymbol _property;

        public PropertyPlace(IPlace holder, Cci.IPropertyDefinition property)
        {
            Contract.ThrowIfNull(property);

            _holder = holder;
            _property = (PropertySymbol)property;
        }

        public TypeSymbol TypeOpt => _property.Type;

        public bool HasAddress => false;

        public TypeSymbol EmitLoad(ILBuilder il)
        {
            //if (_property.Getter == null)
            //    throw new InvalidOperationException();

            var stack = +1;
            var getter = _property.GetMethod;

            if (_holder != null)
            {
                Debug.Assert(!getter.IsStatic);
                _holder.EmitLoad(il);   // {holder}
                stack -= 1;
            }

            il.EmitOpCode(getter.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call, stack);
            il.EmitToken(getter, null, DiagnosticBag.GetInstance());

            //
            return getter.ReturnType;
        }

        public void EmitLoadAddress(ILBuilder il)
        {
            throw new NotSupportedException();
        }

        public void EmitStorePrepare(ILBuilder il)
        {
            if (_holder != null)
            {
                Debug.Assert(_property.SetMethod != null);
                Debug.Assert(!_property.SetMethod.IsStatic);
                _holder.EmitLoad(il);   // {holder}
            }
        }

        public void EmitStore(ILBuilder il)
        {
            //if (_property.Setter == null)
            //    throw new InvalidOperationException();

            var stack = 0;
            var setter = _property.SetMethod;

            if (_holder != null)
            {
                stack -= 1;
            }

            //
            il.EmitOpCode(setter.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call, stack);
            il.EmitToken(setter, null, DiagnosticBag.GetInstance());

            //
            Debug.Assert(setter.ReturnType.SpecialType == SpecialType.System_Void);
        }
    }

    #endregion

    #region IBoundReference

    /// <summary>
    /// Helper object emitting value of a member instance.
    /// Used to avoid repetitious evaluation of the instance in case of BoundCompoundAssignEx or Increment/Decrement.
    /// </summary>
    internal sealed class InstanceCacheHolder : IDisposable
    {
        LocalDefinition _instance_loc;
        LocalDefinition _name_loc;
        CodeGenerator _cg;

        public void Dispose()
        {
            if (_instance_loc != null)
            {
                _cg.ReturnTemporaryLocal(_instance_loc);
                _instance_loc = null;
            }

            if (_name_loc != null)
            {
                _cg.ReturnTemporaryLocal(_name_loc);
                _name_loc = null;
            }

            _cg = null;
        }

        /// <summary>
        /// Emits instance. Caches the result if holder is provided, or loads evaluated instance if holder was initialized already.
        /// </summary>
        public static TypeSymbol EmitInstance(InstanceCacheHolder holderOrNull, CodeGenerator cg, BoundExpression instance)
        {
            if (instance != null)
            {
                if (holderOrNull != null)
                {
                    return holderOrNull.EmitInstance(cg, instance);
                }
                else
                {
                    return cg.Emit(instance);
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Emits name as string. Caches the result if holder is provided, or loads evaluated name if holder was initialized already.
        /// </summary>
        public static void EmitName(InstanceCacheHolder holderOrNull, CodeGenerator cg, BoundExpression name)
        {
            Contract.ThrowIfNull(cg);
            Contract.ThrowIfNull(name);

            if (holderOrNull != null)
            {
                holderOrNull.EmitName(cg, name);
            }
            else
            {
                cg.EmitConvert(name, cg.CoreTypes.String);
            }
        }

        /// <summary>
        /// Emits <paramref name="instance"/>, uses cached value if initialized already.
        /// </summary>
        TypeSymbol EmitInstance(CodeGenerator cg, BoundExpression instance)
        {
            Debug.Assert(cg != null);

            if (instance != null)
            {
                if (_instance_loc != null)
                {
                    Debug.Assert(instance.ResultType == (TypeSymbol)_instance_loc.Type);
                    cg.Builder.EmitLocalLoad(_instance_loc);
                }
                else
                {
                    _cg = cg;

                    // return (<loc> = <instance>);
                    _instance_loc = cg.GetTemporaryLocal(cg.Emit(instance));
                    cg.EmitOpCode(ILOpCode.Dup);
                    cg.Builder.EmitLocalStore(_instance_loc);
                }

                return (TypeSymbol)_instance_loc.Type;
            }

            return null;
        }

        /// <summary>
        /// Emits name as string, uses cached variable.
        /// </summary>
        void EmitName(CodeGenerator cg, BoundExpression name)
        {
            Contract.ThrowIfNull(cg);
            Contract.ThrowIfNull(name);

            if (_name_loc != null)
            {
                cg.Builder.EmitLocalLoad(_name_loc);
            }
            else
            {
                _cg = cg;

                // return (<loc> = <name>)
                _name_loc = cg.GetTemporaryLocal(cg.CoreTypes.String);
                cg.EmitConvert(name, cg.CoreTypes.String);
                cg.Builder.EmitOpCode(ILOpCode.Dup);
                cg.Builder.EmitLocalStore(_name_loc);
            }
        }
    }

    /// <summary>
    /// Interface wrapping bound storage places.
    /// </summary>
    /// <remarks>
    /// Provides methods for load and store to the place.
    /// 1. EmitPreamble. Optionally emits first argument of load/store operation. Can be stored to a temporary variable to both load and store.
    /// 2. EmitLoad. Loads value to the top of stack. Expect 1. to be on stack first.
    /// 3. EmitStore. Stores value from top of stack to the place. Expects 1. to be on stack before.
    /// </remarks>
    internal interface IBoundReference
    {
        /// <summary>
        /// Type of place. Can be <c>null</c>.
        /// </summary>
        TypeSymbol TypeOpt { get; }

        /// <summary>
        /// Emits the preamble to the load operation.
        /// Must be called before <see cref="EmitLoad(CodeGenerator)"/>.
        /// </summary>
        /// <param name="cg">Code generator, must not be <c>null</c>.</param>
        /// <param name="instanceOpt">Temporary variable holding value of instance expression.</param>
        /// <remarks>
        /// The returned type corresponds to the instance expression emitted onto top of the evaluation stack. The value can be stored to a temporary variable and passed to the next call to Emit**Prepare to avoid evaluating of instance again.
        /// </remarks>
        void EmitLoadPrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt = null);

        /// <summary>
        /// Emits the preamble to the load operation.
        /// Must be called before <see cref="EmitStore(CodeGenerator, TypeSymbol)"/>.
        /// </summary>
        /// <param name="cg">Code generator, must not be <c>null</c>.</param>
        /// <param name="instanceOpt">Temporary variable holding value of instance expression.</param>
        /// <remarks>
        /// The returned type corresponds to the instance expression emitted onto top of the evaluation stack. The value can be stored to a temporary variable and passed to the next call to Emit**Prepare to avoid evaluating of instance again.
        /// </remarks>
        void EmitStorePrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt = null);

        /// <summary>
        /// Emits load of value.
        /// Expects <see cref="EmitPreamble(CodeGenerator)"/> to be called first.
        /// </summary>
        /// <remarks>
        /// <paramref name="expected"/> is the target type. It can be <c>array</c> or <c>object</c> or <c>alias</c> in case the expression is ensured to be array or object or to be passed as a reference. 
        /// </remarks>
        TypeSymbol EmitLoad(CodeGenerator cg);

        /// <summary>
        /// Emits code to stores a value to this place.
        /// Expects <see cref="EmitStorePrepare(CodeGenerator,InstanceCacheHolder)"/> and actual value to be loaded on stack.
        /// </summary>
        /// <param name="cg">Code generator.</param>
        /// <param name="valueType">Type of value on the stack to be stored. Can be <c>null</c> in case of <c>Unset</c> semantic.</param>
        void EmitStore(CodeGenerator cg, TypeSymbol valueType);

        ///// <summary>
        ///// Emits code that loads address of this storage place.
        ///// Expects <see cref="EmitPrepare(CodeGenerator)"/> to be called first. 
        ///// </summary>
        //void EmitLoadAddress(CodeGenerator cg);

        ///// <summary>
        ///// Gets whether the place has an address.
        ///// </summary>
        //bool HasAddress { get; }
    }

    #endregion

    #region Bound Places
    
    internal class BoundLocalPlace : IBoundReference, IPlace
    {
        readonly IPlace _place;
        readonly BoundAccess _access;
        readonly TypeRefMask _thint;

        public BoundLocalPlace(IPlace place, BoundAccess access, TypeRefMask thint)
        {
            Contract.ThrowIfNull(place);
            Debug.Assert(place.HasAddress);
            
            _place = place;
            _access = access;
            _thint = thint;
        }

        public void EmitLoadPrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt) { }

        public TypeSymbol EmitLoad(CodeGenerator cg)
        {
            Debug.Assert(_access.IsRead);

            var type = _place.TypeOpt;

            // Ensure Object ($x->.. =)
            if (_access.EnsureObject)
            {
                if (type == cg.CoreTypes.PhpAlias)
                {
                    _place.EmitLoad(cg.Builder);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpAlias.EnsureObject)
                        .Expect(SpecialType.System_Object);
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    _place.EmitLoadAddress(cg.Builder);
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.EnsureObject)
                        .Expect(SpecialType.System_Object);

                    if (_thint.IsSingleType && cg.IsClassOnly(_thint))
                    {
                        var tref = cg.Routine.TypeRefContext.GetTypes(_thint)[0];
                        var clrtype = (TypeSymbol)cg.DeclaringCompilation.GetTypeByMetadataName(tref.QualifiedName.ClrName());
                        if (clrtype != null && !clrtype.IsErrorType() && clrtype != cg.CoreTypes.Object)
                        {
                            cg.EmitCastClass(clrtype);
                            return clrtype;
                        }
                    }

                    return cg.CoreTypes.Object;
                }
                else if (type == cg.CoreTypes.PhpArray)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (type.IsReferenceType)
                    {
                        if (type == cg.CoreTypes.Object)
                        {
                            // Operators.EnsureObject(ref <place>)
                            _place.EmitLoadAddress(cg.Builder);
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureObject_ObjectRef)
                                .Expect(SpecialType.System_Object);
                        }
                        else
                        {
                            // <place>
                            return _place.EmitLoad(cg.Builder);
                        }
                    }
                    else
                    {
                        // return new stdClass(ctx)
                        throw new NotImplementedException();
                    }
                }
            }
            // Ensure Array ($x[] =)
            else if (_access.EnsureArray)
            {
                if (type == cg.CoreTypes.PhpAlias)
                {
                    // <place>.EnsureArray()
                    _place.EmitLoad(cg.Builder);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpAlias.EnsureArray)
                        .Expect(cg.CoreTypes.PhpArray);
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    if (cg.IsArrayOnly(_thint))
                    {
                        // uses typehint and accesses .Array directly if possible
                        // <place>.Array
                        _place.EmitLoadAddress(cg.Builder);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.get_Array)
                            .Expect(cg.CoreTypes.PhpArray);
                    }
                    else
                    {
                        // <place>.EnsureArray()
                        _place.EmitLoadAddress(cg.Builder);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.EnsureArray)
                            .Expect(cg.CoreTypes.PhpArray);
                    }
                }
                else if (type == cg.CoreTypes.PhpArray)
                {
                    // Operators.EnsureArray(ref <place>)
                    _place.EmitLoadAddress(cg.Builder);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureArray_PhpArrayRef)
                        .Expect(cg.CoreTypes.PhpArray);
                }

                throw new NotImplementedException("EnsureArray(" + type.Name + ")");
            }
            // Ensure Alias (&$x)
            else if (_access.IsReadRef)
            {
                if (type == cg.CoreTypes.PhpAlias)
                {
                    // TODO: <place>.AddRef()
                    return _place.EmitLoad(cg.Builder);
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    // return <place>.EnsureAlias()
                    _place.EmitLoadAddress(cg.Builder);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.EnsureAlias)
                        .Expect(cg.CoreTypes.PhpAlias);
                }
                else if (type == cg.CoreTypes.PhpNumber)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    Debug.Assert(false, "value cannot be aliased");

                    // new PhpAlias((PhpValue)<place>, 1)
                    cg.EmitConvertToPhpValue(_place.EmitLoad(cg.Builder), 0);
                    return cg.Emit_PhpValue_MakeAlias();
                }
            }
            // Read Value & Dereference eventually
            else
            {
                if (type == cg.CoreTypes.PhpAlias)
                {
                    _place.EmitLoad(cg.Builder);

                    if (_access.TargetType == cg.CoreTypes.PhpArray)
                    {
                        // <place>.Value.AsArray()
                        cg.Builder.EmitOpCode(ILOpCode.Ldflda);
                        cg.EmitSymbolToken(cg.CoreMethods.PhpAlias.Value, null);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.AsArray)
                            .Expect(cg.CoreTypes.PhpArray);
                    }

                    return cg.Emit_PhpAlias_GetValue();
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    if (_access.TargetType == cg.CoreTypes.PhpArray)
                    {
                        // <place>.AsArray()
                        _place.EmitLoadAddress(cg.Builder);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.AsArray)
                            .Expect(cg.CoreTypes.PhpArray);
                    }

                    // TODO: dereference if applicable (=> PhpValue.Alias.Value)
                    return _place.EmitLoad(cg.Builder);
                }
                else
                {
                    return _place.EmitLoad(cg.Builder);
                }
            }
        }

        public void EmitStorePrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            var type = _place.TypeOpt;

            if (_access.IsWriteRef)
            {
                // no need for preparation
                _place.EmitStorePrepare(cg.Builder);
            }
            else
            {
                Debug.Assert(_access.IsWrite || _access.IsUnset);

                //
                if (type == cg.CoreTypes.PhpAlias)
                {
                    // (PhpAlias)<place>
                    _place.EmitLoad(cg.Builder);
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    if (_thint.IsRef)
                    {
                        // Operators.SetValue(ref <place>, (PhpValue)<value>);
                        _place.EmitLoadAddress(cg.Builder);
                    }
                    else
                    {
                        _place.EmitStorePrepare(cg.Builder);
                    }
                }
                else
                {
                    // no need for preparation
                    _place.EmitStorePrepare(cg.Builder);
                }
            }
        }

        public void EmitStore(CodeGenerator cg, TypeSymbol valueType)
        {
            var type = _place.TypeOpt;

            // Write Ref
            if (_access.IsWriteRef)
            {
                if (valueType != cg.CoreTypes.PhpAlias)
                {
                    Debug.Assert(false, "caller should get aliased value");
                    cg.EmitConvertToPhpValue(valueType, 0);
                    valueType = cg.Emit_PhpValue_MakeAlias();
                }

                //
                if (type == cg.CoreTypes.PhpAlias)
                {
                    // <place> = <alias>
                    _place.EmitStore(cg.Builder);
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    // <place> = PhpValue.Create(<alias>)
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Create_PhpAlias);
                    _place.EmitStore(cg.Builder);
                }
                else
                {
                    Debug.Assert(false, "Assigning alias to non-aliasable variable.");
                    cg.EmitConvert(valueType, 0, type);
                    _place.EmitStore(cg.Builder);
                }
            }
            else if (_access.IsUnset)
            {
                Debug.Assert(valueType == null);

                // <place> =

                if (type == cg.CoreTypes.PhpAlias)
                {
                    // new PhpAlias(void)
                    cg.Emit_PhpValue_Void();
                    cg.Emit_PhpValue_MakeAlias();
                }
                else if (type.IsReferenceType)
                {
                    // null
                    cg.Builder.EmitNullConstant();
                }
                else
                {
                    // default(T)
                    cg.EmitLoadDefaultOfValueType(type);
                }

                _place.EmitStore(cg.Builder);
            }
            else
            {
                Debug.Assert(_access.IsWrite);

                //
                if (type == cg.CoreTypes.PhpAlias)
                {
                    // <place>.Value = <value>
                    cg.EmitConvertToPhpValue(valueType, 0);
                    cg.Emit_PhpAlias_SetValue();
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    if (_thint.IsRef)
                    {
                        // Operators.SetValue(ref <place>, (PhpValue)<value>);
                        cg.EmitConvertToPhpValue(valueType, 0);
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetValue_PhpValueRef_PhpValue);
                    }
                    else
                    {
                        // <place> = <value>
                        cg.EmitConvertToPhpValue(valueType, 0);
                        _place.EmitStore(cg.Builder);
                    }
                }
                else
                {
                    cg.EmitConvert(valueType, 0, type);
                    _place.EmitStore(cg.Builder);
                }
            }
        }

        #region IPlace

        public TypeSymbol TypeOpt => _place.TypeOpt;

        public bool HasAddress => _place.HasAddress;

        TypeSymbol IPlace.EmitLoad(ILBuilder il) => _place.EmitLoad(il);

        void IPlace.EmitStorePrepare(ILBuilder il) { }

        void IPlace.EmitStore(ILBuilder il) => _place.EmitStore(il);

        void IPlace.EmitLoadAddress(ILBuilder il) => _place.EmitLoadAddress(il);

        #endregion
    }

    internal class BoundPropertyPlace : IBoundReference
    {
        readonly BoundExpression _instance;
        readonly PropertySymbol _property;

        public BoundPropertyPlace(BoundExpression instance, Cci.IPropertyDefinition property)
        {
            Contract.ThrowIfNull(property);

            _instance = instance;
            _property = (PropertySymbol)property;
        }

        public TypeSymbol TypeOpt => _property.Type;

        public bool HasAddress => false;

        public TypeSymbol EmitLoad(CodeGenerator cg)
        {
            //if (_property.Getter == null)
            //    throw new InvalidOperationException();

            var getter = _property.GetMethod;
            return cg.EmitCall(getter.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call, getter);
        }

        public void EmitLoadAddress(CodeGenerator cg)
        {
            throw new NotSupportedException();
        }

        public void EmitStore(CodeGenerator cg, TypeSymbol valueType)
        {
            //if (_property.Setter == null)
            //    throw new InvalidOperationException();

            var setter = _property.SetMethod;

            cg.EmitConvert(valueType, 0, setter.Parameters[0].Type);
            cg.EmitCall(setter.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call, setter);

            // TODO: unset
        }

        public void EmitLoadPrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            if (_property == null)
            {
                // TODO: callsite.Target callsite
                throw new NotImplementedException();
            }

            InstanceCacheHolder.EmitInstance(instanceOpt, cg, _instance);
        }

        public void EmitStorePrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            if (_property == null)
            {
                // TODO: callsite.Target callsite
                throw new NotImplementedException();
            }

            InstanceCacheHolder.EmitInstance(instanceOpt, cg, _instance);
        }

        public void EmitUnset(CodeGenerator cg)
        {
            var bound = (IBoundReference)this;
            bound.EmitStorePrepare(cg);
            bound.EmitStore(cg, cg.Emit_PhpValue_Void());
        }
    }

    internal class BoundSuperglobalPlace : IBoundReference
    {
        readonly Syntax.VariableName _name;
        readonly BoundAccess _access;
        
        public BoundSuperglobalPlace(Syntax.VariableName name, BoundAccess access)
        {
            Debug.Assert(name.IsAutoGlobal);
            _name = name;
            _access = access;
        }

        #region IBoundReference

        public TypeSymbol TypeOpt => null;  // PhpArray

        public void EmitLoadPrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt = null)
        {
            // nothing
        }

        public void EmitStorePrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt = null)
        {
            // nothing
        }

        public TypeSymbol EmitLoad(CodeGenerator cg)
        {
            if (_name == Syntax.VariableName.GlobalsName)
            {
                return cg.EmitLoadGlobals();
            }

            throw new NotImplementedException($"Superglobal ${_name.Value}");
        }

        public void EmitStore(CodeGenerator cg, TypeSymbol valueType)
        {
            throw new NotImplementedException($"Superglobal ${_name.Value}");
        }

        #endregion
    }

    internal class BoundIndirectVariablePlace : IBoundReference
    {
        readonly BoundExpression _nameExpr;
        readonly BoundAccess _access;

        public BoundIndirectVariablePlace(BoundExpression nameExpr, BoundAccess access)
        {
            Contract.ThrowIfNull(nameExpr);
            _nameExpr = nameExpr;
            _access = access;
        }

        /// <summary>
        /// Loads reference to <c>PhpArray</c> containing variables.
        /// </summary>
        /// <returns><c>PhpArray</c> type symbol.</returns>
        protected virtual TypeSymbol LoadVariablesArray(CodeGenerator cg)
        {
            Debug.Assert(cg.LocalsPlaceOpt != null);
            
            // <locals>
            return cg.LocalsPlaceOpt.EmitLoad(cg.Builder)
                .Expect(cg.CoreTypes.PhpArray);
        }

        #region IBoundReference

        public TypeSymbol TypeOpt => null;

        private void EmitPrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt = null)
        {
            // Template: <variables> Key

            LoadVariablesArray(cg);

            // key
            cg.EmitIntStringKey(_nameExpr);
        }

        public void EmitLoadPrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt = null)
        {
            EmitPrepare(cg, instanceOpt);
        }

        public TypeSymbol EmitLoad(CodeGenerator cg)
        {
            // STACK: <PhpArray> <key>

            if (_access.EnsureObject)
            {
                // <array>.EnsureItemObject(<key>)
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.EnsureItemObject_IntStringKey);
            }
            else if (_access.EnsureArray)
            {
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.EnsureItemArray_IntStringKey);
            }
            else if (_access.IsReadRef)
            {
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.EnsureItemAlias_IntStringKey);
            }
            else
            {
                Debug.Assert(_access.IsRead);
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.GetItemValue_IntStringKey);
            }
        }

        public void EmitStorePrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt = null)
        {
            EmitPrepare(cg, instanceOpt);
        }

        public virtual void EmitStore(CodeGenerator cg, TypeSymbol valueType)
        {
            // STACK: <PhpArray> <key>

            if (_access.IsWriteRef)
            {
                // PhpAlias
                if (valueType != cg.CoreTypes.PhpAlias)
                {
                    cg.EmitConvertToPhpValue(valueType, 0);
                    cg.Emit_PhpValue_MakeAlias();
                }

                // .SetItemAlias(key, alias)
                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.SetItemAlias_IntStringKey_PhpAlias);
            }
            else if (_access.IsUnset)
            {
                Debug.Assert(valueType == null);

                // .RemoveKey(key)
                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.RemoveKey_IntStringKey);
            }
            else
            {
                Debug.Assert(_access.IsWrite);

                cg.EmitConvertToPhpValue(valueType, 0);

                // .SetItemValue(key, value)
                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.SetItemValue_IntStringKey_PhpValue);
            }
        }

        #endregion
    }

    internal class BoundGlobalPlace : BoundIndirectVariablePlace
    {
        public BoundGlobalPlace(BoundExpression nameExpr, BoundAccess access)
            :base(nameExpr, access)
        {
        }

        protected override TypeSymbol LoadVariablesArray(CodeGenerator cg)
        {
            if (cg.IsGlobalScope)
            {
                // <locals>
                Debug.Assert(cg.LocalsPlaceOpt != null);
                return cg.LocalsPlaceOpt.EmitLoad(cg.Builder)
                    .Expect(cg.CoreTypes.PhpArray);
            }
            else
            {
                // $GLOBALS
                return cg.EmitLoadGlobals();
            }
        }
    }

    #endregion
}
