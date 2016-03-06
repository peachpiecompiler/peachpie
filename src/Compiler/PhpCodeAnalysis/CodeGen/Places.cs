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
        /// Emits code that loads the value from this storage place.
        /// </summary>
        /// <param name="il">The <see cref="ILBuilder"/> to emit the code to.</param>
        TypeSymbol EmitLoad(ILBuilder il);

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

        public bool HasAddress => true;

        public TypeSymbol EmitLoad(ILBuilder il)
        {
            il.EmitLocalLoad(_def);
            return (TypeSymbol)_def.Type;
        }

        public void EmitLoadAddress(ILBuilder il) => il.EmitLocalAddress(_def);

        public void EmitStore(ILBuilder il) => il.EmitLocalStore(_def);
    }

    internal class ParamPlace : IPlace
    {
        readonly ParameterSymbol _p;

        public int Index => _p.Ordinal;

        public ParamPlace(ParameterSymbol p)
        {
            _p = p;
        }

        public bool HasAddress => true;

        public TypeSymbol EmitLoad(ILBuilder il)
        {
            il.EmitLoadArgumentOpcode(Index);
            return _p.Type;
        }

        public void EmitLoadAddress(ILBuilder il) => il.EmitLoadArgumentAddrOpcode(Index);

        public void EmitStore(ILBuilder il) => il.EmitStoreArgumentOpcode(Index);
    }

    internal class FieldPlace : IPlace
    {
        readonly IPlace _holder;
        readonly Cci.IFieldReference _field;

        public FieldPlace(IPlace holder, Cci.IFieldDefinition field)
        {
            Contract.ThrowIfNull(field);
            Debug.Assert(holder != null || field.IsStatic);

            _holder = holder;
            _field = field;
        }

        void EmitOpCode(ILBuilder il, ILOpCode code)
        {
            if (_holder != null)
            {
                _holder.EmitLoad(il);   // {holder}
            }

            il.EmitOpCode(code);
            il.EmitToken(_field, null, null /*DiagnosticBag.GetInstance()*/);    // .{field}
        }

        public bool HasAddress => _holder.HasAddress;

        public TypeSymbol EmitLoad(ILBuilder il)
        {
            EmitOpCode(il, ILOpCode.Ldfld);
            return ((FieldSymbol)_field).Type;
        }

        public void EmitStore(ILBuilder il) => EmitOpCode(il, ILOpCode.Stfld);

        public void EmitLoadAddress(ILBuilder il) => EmitOpCode(il, ILOpCode.Ldflda);
    }
}
