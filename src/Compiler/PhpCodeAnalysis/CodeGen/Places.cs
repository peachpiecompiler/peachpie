using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
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
	/// Interface supported by storage places.
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
}
