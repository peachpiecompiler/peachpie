#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Represents an aliased value.
    /// </summary>
    [DebuggerDisplay("{Value.DisplayString,nq}, Refs#{ReferenceCount}", Type = "&{Value.DebugTypeName,nq}")]
    [DebuggerNonUserCode]
    public abstract class PhpAlias : IPhpConvertible
    {
        #region Properties

        /// <summary>
        /// Gets or sets the underlying value.
        /// </summary>
        /// <remarks>The field is not wrapped into a property, some internals need to access the raw field.</remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public abstract PhpValue Value { get; set; }

        /// <summary>
        /// Gets references count.
        /// If greater than zero, the <see cref="PhpAlias"/> acts like a reference.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int ReferenceCount { get; private set; }

        #endregion

        #region Construction

        /// <summary>
        /// Creates an aliased value.
        /// </summary>
        public PhpAlias(int refcount = 1)
        {
            Debug.Assert(refcount >= 1);            
            ReferenceCount = refcount;
        }

        #endregion

        #region Methods

        internal PhpAlias AddRef()
        {
            ReferenceCount++;
            return this;
        }

        public PhpAlias ReleaseRef()
        {
            if (--ReferenceCount == 0)
            {
                // TODO: dispose implicitly
            }

            return this;
        }

        public static PhpAlias Create(PhpValue value) => new PhpValueAlias(value, 1);

        /// <summary>
        /// Ensures the underlying value is an object and gets its instance.
        /// Cannot be <c>null</c>.
        /// </summary>
        public abstract object EnsureObject();

        /// <summary>
        /// Ensures the underlying value is an array and gets its instance.
        /// Cannot be <c>null</c>.
        /// </summary>
        public abstract IPhpArray EnsureArray();

        /// <summary>
        /// Ensures underlying value as a writable string and gets the containing <see cref="PhpString.Blob"/>.
        /// </summary>
        public abstract PhpString.Blob EnsureWritableString();

        /// <summary>
        /// Implements <c>&amp;[]</c> operator on <see cref="PhpValue"/>.
        /// Ensures the value is an array and item at given <paramref name="index"/> is an alias.
        /// </summary>
        public abstract PhpAlias EnsureItemAlias(PhpValue index, bool quiet = false);

        /// <summary>
        /// Performs deep copy of the value.
        /// If the value is referenced, the method returns this instance of <see cref="PhpAlias"/>, otherwise the value is copied.
        /// </summary>
        public PhpValue DeepCopy() => ReferenceCount > 0 ? this.AddRef() : Value.DeepCopy();

        #endregion

        #region IPhpConvertible

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public PhpTypeCode TypeCode => Value.TypeCode;

        public double ToDouble() => Value.ToDouble();

        public long ToLong() => Value.ToLong();

        public bool ToBoolean() => Value.ToBoolean();

        public Convert.NumberInfo ToNumber(out PhpNumber number) => Value.ToNumber(out number);

        public object ToClass() => Value.ToClass();

        public PhpArray ToArray() => Value.ToArray();

        #endregion

        #region Operations

        public static explicit operator PhpArray(PhpAlias alias) => alias.ToArray();

        public static implicit operator PhpValue(PhpAlias alias) => PhpValue.Create(alias);

        public static explicit operator bool(PhpAlias value) => value.ToBoolean();

        public static explicit operator IntStringKey(PhpAlias value) => value.Value;

        /// <summary>
        /// Casts the value to object instance.
        /// Non-object values are wrapped to <see cref="stdClass"/>.
        /// </summary>
        public object ToObject() => ToClass();

        public object AsObject() => Value.AsObject();

        public PhpNumber ToNumber() => Convert.ToNumber(Value);

        public PhpString ToPhpString(Context ctx) => Value.ToPhpString(ctx);

        public override string ToString() => Value.ToString();

        public string ToString(Context ctx) => Value.ToString(ctx);

        public bool IsEmpty() => Value.IsEmpty;

        #endregion
    }

    sealed class PhpValueAlias : PhpAlias
    {
        PhpValue _value;

        public PhpValueAlias(PhpValue value, int refcount = 1)
            : base(refcount)
        {
            Debug.Assert(!value.IsAlias);
            _value = value;
        }

        public override PhpValue Value
        {
            get => _value;
            set => _value = value;
        }

        public override PhpAlias EnsureItemAlias(PhpValue index, bool quiet = false) => Operators.EnsureItemAlias(ref _value, index, quiet);

        public override IPhpArray EnsureArray() => PhpValue.EnsureArray(ref _value);

        public override object EnsureObject() => PhpValue.EnsureObject(ref _value);

        public override PhpString.Blob EnsureWritableString() => Operators.EnsureWritableString(ref _value);
    }
}
