using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// Lightweight abstraction over a native storage supported by storage places with address.
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

        public override string ToString() => $"${_def.Name}";

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

        public override string ToString() => $"${_p.Name}";

        public ParamPlace(ParameterSymbol p)
        {
            Contract.ThrowIfNull(p);
            Debug.Assert(p.Ordinal >= 0, "(p.Ordinal < 0)");
            Debug.Assert(p is SourceParameterSymbol sp ? !sp.IsFake : true);
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

        public TypeSymbol Type => _type;

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

        public TypeSymbol Type => _place.Type;

        public TypeSymbol EmitLoad(ILBuilder il) => _place.EmitLoad(il);

        public void EmitLoadAddress(ILBuilder il) => _place.EmitLoadAddress(il);

        public void EmitStore(ILBuilder il)
        {
            throw new InvalidOperationException($"{_place} is readonly!");  // TODO: ErrCode
        }

        public void EmitStorePrepare(ILBuilder il) { }
    }

    /// <summary>
    /// When we handle emitting receiver by ourselves.
    /// Emits op codes for using <see cref="FieldSymbol"/> but does not emit load of receiver.
    /// Use <see cref="FieldPlace"/> instead.
    /// </summary>
    internal class FieldPlace_Raw : IPlace
    {
        readonly protected FieldSymbol _field;
        readonly protected Cci.IFieldReference _fieldref;

        internal static IFieldSymbol GetRealDefinition(IFieldSymbol field)
        {
            // field redeclares its parent member, use the original def
            return (field is SourceFieldSymbol srcf)
                ? srcf.OverridenDefinition ?? field
                : field;
        }

        public FieldPlace_Raw(IFieldSymbol field, Emit.PEModuleBuilder module = null)
        {
            Contract.ThrowIfNull(field);

            field = GetRealDefinition(field);

            _field = (FieldSymbol)field;
            _fieldref = (module != null) ? module.Translate((FieldSymbol)field, null, DiagnosticBag.GetInstance()) : (Cci.IFieldReference)field;
        }

        virtual protected void EmitHolder(ILBuilder il)
        {
            // nope
        }

        void EmitOpCode(ILBuilder il, ILOpCode code)
        {
            il.EmitOpCode(code);
            il.EmitToken(_fieldref, null, DiagnosticBag.GetInstance());    // .{field}
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

        public void EmitLoadAddress(ILBuilder il)
        {
            EmitHolder(il);
            EmitOpCode(il, _field.IsStatic ? ILOpCode.Ldsflda : ILOpCode.Ldflda);
        }
    }

    internal class FieldPlace : FieldPlace_Raw
    {
        readonly IPlace _holder;

        public FieldPlace(IPlace holder, IFieldSymbol field, Emit.PEModuleBuilder module = null)
            : base(field, module)
        {
            _holder = holder;

            Debug.Assert(holder != null || field.IsStatic, "receiver not set");
            Debug.Assert(holder == null || holder.Type.IsOfType(_field.ContainingType) || _field.ContainingType.IsValueType, $"receiver of type {holder?.Type} mismatches the field's containing type {_field?.ContainingType}");
        }

        override protected void EmitHolder(ILBuilder il)
        {
            Debug.Assert(_field.IsStatic == (_holder == null));

            if (!_field.IsStatic)
            {
                VariableReferenceExtensions.EmitReceiver(il, _holder);
            }
        }
    }

    internal class PropertyPlace : IPlace
    {
        readonly IPlace _holder;
        readonly PropertySymbol _property;
        readonly Emit.PEModuleBuilder _module;

        public PropertyPlace(IPlace holder, Cci.IPropertyDefinition property, Emit.PEModuleBuilder module = null)
        {
            Contract.ThrowIfNull(property);

            _holder = holder;
            _property = (PropertySymbol)property;
            _module = module;
        }

        public TypeSymbol Type => _property.Type;

        public bool HasAddress => false;

        TypeSymbol EmitReceiver(ILBuilder il)
        {
            var lhs = VariableReferenceExtensions.EmitReceiver(il, _holder);

            if (_property.IsStatic)
            {
                if (lhs.Stack != null && !lhs.Stack.IsVoid())
                {
                    il.EmitOpCode(ILOpCode.Pop);
                }
                return null;
            }

            return lhs.Stack;
        }

        public TypeSymbol EmitLoad(ILBuilder il)
        {
            //if (_property.Getter == null)
            //    throw new InvalidOperationException();

            var stack = +1;
            var getter = _property.GetMethod;

            var receiver = EmitReceiver(il);
            if (receiver != null)
            {
                stack -= 1;
            }

            il.EmitOpCode((getter.IsVirtual || getter.IsAbstract) ? ILOpCode.Callvirt : ILOpCode.Call, stack);

            var getterref = (_module != null)
                ? _module.Translate(getter, DiagnosticBag.GetInstance(), false)
                : getter;

            il.EmitToken(getterref, null, DiagnosticBag.GetInstance());    // TODO: Translate

            //
            return getter.ReturnType;
        }

        public void EmitLoadAddress(ILBuilder il)
        {
            throw new NotSupportedException();
        }

        public void EmitStorePrepare(ILBuilder il)
        {
            EmitReceiver(il);
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

            var setterref = (_module != null)
                ? _module.Translate(setter, DiagnosticBag.GetInstance(), false)
                : setter;

            il.EmitToken(setterref, null, DiagnosticBag.GetInstance());    // TODO: Translate

            //
            Debug.Assert(setter.ReturnType.SpecialType == SpecialType.System_Void);
        }
    }

    internal class OperatorPlace : IPlace
    {
        readonly MethodSymbol _operator;
        readonly IPlace _operand;

        public OperatorPlace(MethodSymbol @operator, IPlace operand)
        {
            Debug.Assert(@operator != null);
            Debug.Assert(@operator.HasThis == false);
            Debug.Assert(operand != null);

            _operator = @operator;
            _operand = operand;
        }

        public TypeSymbol Type => _operator.ReturnType;

        public bool HasAddress => false;

        public TypeSymbol EmitLoad(ILBuilder il)
        {
            _operand.EmitLoad(il);

            il.EmitOpCode(ILOpCode.Call, _operator.GetCallStackBehavior());
            il.EmitToken(_operator, null, DiagnosticBag.GetInstance());

            return Type;
        }

        public void EmitLoadAddress(ILBuilder il)
        {
            throw new NotSupportedException();
        }

        public void EmitStore(ILBuilder il)
        {
            throw new NotSupportedException();
        }

        public void EmitStorePrepare(ILBuilder il)
        {
            throw new NotSupportedException();
        }
    }

    ///// <summary>
    ///// Represents access to <c>IndirectLocal.Value</c>.
    ///// </summary>
    //internal sealed class IndirectLocalPlace : IPlace
    //{
    //    /// <summary>
    //    /// Refers to variable of type <c>IndirectLocal</c>.
    //    /// </summary>
    //    readonly IPlace _localPlace;

    //    readonly Emit.PEModuleBuilder _module;

    //    public IndirectLocalPlace(IPlace localPlace, Emit.PEModuleBuilder module)
    //    {
    //        Debug.Assert(localPlace != null);
    //        Debug.Assert(localPlace.TypeOpt != null);
    //        Debug.Assert(localPlace.TypeOpt.Name == "IndirectLocal");
    //        Debug.Assert(localPlace.HasAddress);

    //        _localPlace = localPlace;
    //        _module = module;
    //    }

    //    public TypeSymbol Type => ValueProperty.Type; // PhpValue

    //    public bool HasAddress => true;

    //    PropertySymbol/*!*/ValueProperty => _localPlace.TypeOpt.LookupMember<PropertySymbol>("Value");

    //    PropertySymbol/*!*/ValueRefProperty => _localPlace.TypeOpt.LookupMember<PropertySymbol>("ValueRef");

    //    public TypeSymbol EmitLoad(ILBuilder il)
    //    {
    //        // CALL {place}.get_Value

    //        _localPlace.EmitLoadAddress(il);    // ref IndirectLocal
    //        return il.EmitCall(_module, DiagnosticBag.GetInstance(), ILOpCode.Call, ValueProperty.GetMethod); // .get_Value()
    //    }

    //    public void EmitLoadAddress(ILBuilder il)
    //    {
    //        // CALL {place}.get_ValueRef

    //        _localPlace.EmitLoadAddress(il);    // ref IndirectLocal
    //        il.EmitCall(_module, DiagnosticBag.GetInstance(), ILOpCode.Call, ValueRefProperty.GetMethod); // .get_ValueRef()
    //    }

    //    public void EmitStorePrepare(ILBuilder il)
    //    {
    //        // LOAD ADDR {place}

    //        _localPlace.EmitLoadAddress(il);    // ref IndirectLocal
    //    }

    //    public void EmitStore(ILBuilder il)
    //    {
    //        // CALL .set_Value()

    //        il.EmitCall(_module, DiagnosticBag.GetInstance(), ILOpCode.Call, ValueProperty.SetMethod); // .set_Value()
    //    }
    //}

    #endregion
}
