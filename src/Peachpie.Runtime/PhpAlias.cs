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
    [DebuggerDisplay("{Value.DisplayString,nq}, Refs#{_refcount}", Type = "&{Value.DebugTypeName,nq}")]
    [DebuggerNonUserCode]
    public class PhpAlias : IPhpConvertible
    {
        #region Fields

        /// <summary>
        /// Gets or sets the underlaying value.
        /// </summary>
        /// <remarks>The field is not wrapped into a property, some internals need to access the raw field.</remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public PhpValue Value;

        #endregion

        #region Properties

        /// <summary>
        /// Gets references count.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int ReferenceCount { get; private set; }

        internal bool IsAliasForIterator => _iteratorAliasCount > 0;

        internal void IncIteratorAliasCount() => _iteratorAliasCount++;

        internal int DecIteratorAliasCount() => --_iteratorAliasCount;

        // how many times the underlying value was converted to an alias in OrderedDictionary.Enumerator.CurrentValueAliased
        private int _iteratorAliasCount;

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

        public void AddRef()
        {
            ReferenceCount++;
        }

        public void ReleaseRef()
        {
            if (--ReferenceCount == 0)
            {
                // TODO: dispose implicitly
            }
        }

        /// <summary>
        /// Ensures the underlaying value is an object and gets its instance.
        /// Cannot be <c>null</c>.
        /// </summary>
        public object EnsureObject() => Value.EnsureObject();

        /// <summary>
        /// Ensures the underlaying value is an array and gets its instance.
        /// Cannot be <c>null</c>.
        /// </summary>
        public IPhpArray EnsureArray() => Value.EnsureArray();

        #endregion

        #region IPhpConvertible

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public PhpTypeCode TypeCode => Value.TypeCode;

        public double ToDouble() => Value.ToDouble();

        public long ToLong() => Value.ToLong();

        public bool ToBoolean() => Value.ToBoolean();

        public Convert.NumberInfo ToNumber(out PhpNumber number) => Value.ToNumber(out number);

        public string ToString(Context ctx) => Value.ToString(ctx);

        public string ToStringOrThrow(Context ctx) => Value.ToStringOrThrow(ctx);

        public object ToClass() => Value.ToClass();

        public PhpArray ToArray() => Value.ToArray();

        #endregion

        #region Operations

        public static explicit operator PhpArray(PhpAlias alias) => alias.ToArray();

        public static implicit operator PhpValue(PhpAlias alias) => PhpValue.Create(alias);

        public static implicit operator bool(PhpAlias value) => (bool)value.Value;

        public static implicit operator IntStringKey(PhpAlias value) => (IntStringKey)value.Value;

        /// <summary>
        /// Casts the value to object instance.
        /// Non-object values are wrapped to <see cref="stdClass"/>.
        /// </summary>
        public object ToObject() => this.ToClass();

        public object AsObject() => this.Value.AsObject();

        public PhpNumber ToNumber() => Convert.ToNumber(Value);

        public PhpString ToPhpString(Context ctx) => Convert.ToPhpString(Value, ctx);

        public bool IsEmpty() => Value.IsEmpty;

        #endregion
    }
}
