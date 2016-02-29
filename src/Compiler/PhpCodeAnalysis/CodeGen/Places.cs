using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
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
        void EmitLoad(ILBuilder il);

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

        public void EmitLoad(ILBuilder il) => il.EmitLocalLoad(_def);

        public void EmitLoadAddress(ILBuilder il) => il.EmitLocalAddress(_def);

        public void EmitStore(ILBuilder il) => il.EmitLocalStore(_def);
    }

    internal class ParamPlace : IPlace
    {
        public static readonly ParamPlace/*!*/ThisPlace = new ParamPlace(-1);

        readonly int _index;

        public ParamPlace(int index)
        {
            _index = index;
        }

        public bool HasAddress => true;

        public void EmitLoad(ILBuilder il) => il.EmitLoadArgumentOpcode(_index);

        public void EmitLoadAddress(ILBuilder il) => il.EmitLoadArgumentAddrOpcode(_index);

        public void EmitStore(ILBuilder il) => il.EmitStoreArgumentOpcode(_index);
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

        public void EmitLoad(ILBuilder il) => EmitOpCode(il, ILOpCode.Ldfld);

        public void EmitStore(ILBuilder il) => EmitOpCode(il, ILOpCode.Stfld);

        public void EmitLoadAddress(ILBuilder il) => EmitOpCode(il, ILOpCode.Ldflda);
    }
}
