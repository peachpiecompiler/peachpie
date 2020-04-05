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
    public class PhpAlias : IPhpConvertible
    {
        #region Fields

        /// <summary>
        /// Gets or sets the underlying value.
        /// </summary>
        /// <remarks>The field is not wrapped into a property, some internals need to access the raw field.</remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public PhpValue Value;

        #endregion

        #region Properties

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
        public PhpAlias(PhpValue value, int refcount = 1)
        {
            Debug.Assert(refcount >= 1);
            Debug.Assert(value.TypeCode != PhpTypeCode.Alias);

            Value = value;
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

        /// <summary>
        /// Ensures the underlying value is an object and gets its instance.
        /// Cannot be <c>null</c>.
        /// </summary>
        public object EnsureObject() => PhpValue.EnsureObject(ref Value);

        /// <summary>
        /// Ensures the underlying value is an array and gets its instance.
        /// Cannot be <c>null</c>.
        /// </summary>
        public IPhpArray EnsureArray() => PhpValue.EnsureArray(ref Value);

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

        public string ToString(Context ctx) => Value.ToString(ctx);

        public object ToClass() => Value.ToClass();

        public PhpArray ToArray() => Value.ToArray();

        #endregion

        #region Operations

        public static explicit operator PhpArray(PhpAlias alias) => alias.ToArray();

        public static implicit operator PhpValue(PhpAlias alias) => PhpValue.Create(alias);

        public static implicit operator bool(PhpAlias value) => value.Value;

        public static implicit operator IntStringKey(PhpAlias value) => value.Value;

        /// <summary>
        /// Casts the value to object instance.
        /// Non-object values are wrapped to <see cref="stdClass"/>.
        /// </summary>
        public object ToObject() => ToClass();

        public object AsObject() => Value.AsObject();

        public PhpNumber ToNumber() => Convert.ToNumber(Value);

        public PhpString ToPhpString(Context ctx) => Value.ToPhpString(ctx);

        public bool IsEmpty() => Value.IsEmpty;

        #endregion
    }
}
