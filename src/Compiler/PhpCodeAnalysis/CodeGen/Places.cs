using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
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
        TypeSymbol Type { get; }

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

        public LocalPlace(LocalDefinition def)
        {
            Contract.ThrowIfNull(def);
            _def = def;
        }

        public TypeSymbol Type => (TypeSymbol)_def.Type;

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

        public ParamPlace(ParameterSymbol p)
        {
            Contract.ThrowIfNull(p);
            _p = p;
        }

        public TypeSymbol Type => _p.Type;

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
            if (_holder != null)
            {
                Debug.Assert(!_field.IsStatic);
                _holder.EmitLoad(il);
            }
        }

        void EmitOpCode(ILBuilder il, ILOpCode code)
        {
            il.EmitOpCode(code);
            il.EmitToken(_field, null, DiagnosticBag.GetInstance());    // .{field}
        }

        public TypeSymbol Type => _field.Type;

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

        public void EmitLoadAddress(ILBuilder il) => EmitOpCode(il, _field.IsStatic ? ILOpCode.Ldsflda : ILOpCode.Ldflda);
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

        public TypeSymbol Type => _property.Type;

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
            il.EmitToken(getter, null, null);

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
            il.EmitToken(setter, null, null);

            //
            Debug.Assert(setter.ReturnType.SpecialType == SpecialType.System_Void);
        }
    }

    #endregion

    #region IBoundPlace

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
        TypeSymbol Type { get; }

        /// <summary>
        /// Emits the preamble to the load operation.
        /// Must be called before <see cref="EmitLoad(CodeGenerator)"/>.
        /// </summary>
        /// <param name="cg">Code generator, must not be <c>null</c>.</param>
        /// <param name="instanceOpt">Temporary variable holding value of instance expression.</param>
        /// <returns>Type of instance emitted onto the evaluation stack. May be <c>null</c> or <c>void</c> in case of a local variable or a static field or a static property.</returns>
        /// <remarks>
        /// The returned type corresponds to the instance expression emitted onto top of the evaluation stack. The value can be stored to a temporary variable and passed to the next call to Emit**Prepare to avoid evaluating of instance again.
        /// </remarks>
        TypeSymbol EmitLoadPrepare(CodeGenerator cg, LocalDefinition instanceOpt = null);

        /// <summary>
        /// Emits the preamble to the load operation.
        /// Must be called before <see cref="EmitStore(CodeGenerator, TypeSymbol)"/>.
        /// </summary>
        /// <param name="cg">Code generator, must not be <c>null</c>.</param>
        /// <param name="instanceOpt">Temporary variable holding value of instance expression.</param>
        /// <returns>Type of instance emitted onto the evaluation stack. May be <c>null</c> or <c>void</c> in case of a local variable or a static field or a static property.</returns>
        /// <remarks>
        /// The returned type corresponds to the instance expression emitted onto top of the evaluation stack. The value can be stored to a temporary variable and passed to the next call to Emit**Prepare to avoid evaluating of instance again.
        /// </remarks>
        TypeSymbol EmitStorePrepare(CodeGenerator cg, LocalDefinition instanceOpt = null);

        /// <summary>
        /// Emits load of value.
        /// Expects <see cref="EmitPreamble(CodeGenerator)"/> to be called first.
        /// </summary>
        /// <remarks>
        /// <paramref name="expected"/> is the target type. It can be <c>array</c> or <c>object</c> or <c>alias</c> in case the expression is ensured to be array or object or to be passed as a reference. 
        /// </remarks>
        TypeSymbol EmitLoad(CodeGenerator cg);

        /// <summary>
        /// Emits code to storee a value to this place.
        /// Expects <see cref="EmitPreamble(CodeGenerator)"/> and actual value to be loaded on stack.
        /// </summary>
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

    internal static class BoundPlaceHelpers
    {
        /// <summary>
        /// Emits <paramref name="instance"/>. Uses <paramref name="instanceCacheOpt"/> if provided.
        /// </summary>
        public static TypeSymbol EmitInstanceOrTmp(CodeGenerator cg, BoundExpression instance, LocalDefinition instanceCacheOpt)
        {
            Debug.Assert(cg != null);

            if (instance != null)
            {
                if (instanceCacheOpt != null)
                {
                    Debug.Assert(instance.ResultType == (TypeSymbol)instanceCacheOpt.Type);
                    cg.Builder.EmitLocalLoad(instanceCacheOpt);
                    return (TypeSymbol)instanceCacheOpt.Type;
                }
                else
                {
                    return cg.Emit(instance);
                }
            }
            else
            {
                Debug.Assert(instanceCacheOpt == null);
            }

            return null;
        }
    }

    internal class BoundLocalPlace : IBoundReference, IPlace
    {
        readonly IPlace _place;
        readonly BoundAccess _access;

        public BoundLocalPlace(IPlace place, BoundAccess access)
        {
            Contract.ThrowIfNull(place);
            Debug.Assert(place.HasAddress);
            Debug.Assert(place is LocalPlace || place is ParamPlace);

            _place = place;
            _access = access;
        }

        public TypeSymbol EmitLoad(CodeGenerator cg)
        {
            Debug.Assert(_access.IsRead);

            var type = _place.Type;

            // Ensure Object ($x->.. =)
            if (_access.EnsureObject)
            {
                if (type == cg.CoreTypes.PhpAlias)
                {
                    _place.EmitLoad(cg.Builder);
                    cg.EmitLoadContext();
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpAlias.EnsureObject_Context)
                        .Expect(SpecialType.System_Object);
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    _place.EmitLoadAddress(cg.Builder);
                    cg.EmitLoadContext();
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.EnsureObject_Context)
                        .Expect(SpecialType.System_Object);
                }
                else
                {
                    if (type.IsReferenceType)
                    {
                        // TODO: ensure it is not null
                        return _place.EmitLoad(cg.Builder);
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
                throw new NotImplementedException();
            }
            // Ensure Alias (&$x)
            else if (_access.IsReadRef)
            {
                throw new NotImplementedException();
            }
            // Read Value & Dereference eventually
            else
            {
                if (type == cg.CoreTypes.PhpAlias)
                {
                    _place.EmitLoad(cg.Builder);
                    return cg.Emit_PhpAlias_GetValue();
                }
                else if (type == cg.CoreTypes.PhpValue)
                {
                    // TODO: dereference if applicable (=> PhpValue.Alias.Value)
                    return _place.EmitLoad(cg.Builder);
                }
                else
                {
                    return _place.EmitLoad(cg.Builder);
                }
            }
        }

        public void EmitStore(CodeGenerator cg, TypeSymbol valueType)
        {
            Debug.Assert(_access.IsWrite);

            // Write Ref
            if (_access.IsWriteRef)
            {
                throw new NotImplementedException();
            }

            // Write Value
            _place.EmitStore(cg.Builder);
        }

        public TypeSymbol EmitLoadPrepare(CodeGenerator cg, LocalDefinition instanceOpt) => null;

        public TypeSymbol EmitStorePrepare(CodeGenerator cg, LocalDefinition instanceOpt) => null;

        #region IPlace

        public TypeSymbol Type => _place.Type;

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

        public TypeSymbol Type => _property.Type;

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
            cg.EmitCall(setter.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call, setter);
        }

        public TypeSymbol EmitLoadPrepare(CodeGenerator cg, LocalDefinition instanceOpt)
        {
            if (_property == null)
            {
                // TODO: callsite.Target callsite
                throw new NotImplementedException();
            }

            return BoundPlaceHelpers.EmitInstanceOrTmp(cg, _instance, instanceOpt);
        }

        public TypeSymbol EmitStorePrepare(CodeGenerator cg, LocalDefinition instanceOpt)
        {
            if (_property == null)
            {
                // TODO: callsite.Target callsite
                throw new NotImplementedException();
            }

            return BoundPlaceHelpers.EmitInstanceOrTmp(cg, _instance, instanceOpt);
        }
    }

    #endregion
}
