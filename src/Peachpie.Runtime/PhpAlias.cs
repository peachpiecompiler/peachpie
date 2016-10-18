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
    [DebuggerDisplay("{Value.DisplayString,nq}, Refs#{_refcount}", Type= "&{Value.DebugTypeName,nq}")]
    public class PhpAlias : IPhpConvertible
    {
        #region Fields

        /// <summary>
        /// Gets or sets the underlaying value.
        /// </summary>
        /// <remarks>The field is not wrapped into a property, some internals need to access the raw field.</remarks>
        public PhpValue Value;

        /// <summary>
        /// References count.
        /// </summary>
        int _refcount;

        #endregion

        #region Properties

        /// <summary>
        /// Gets references count.
        /// </summary>
        public int ReferenceCount => _refcount;

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
            _refcount = refcount;
        }

        #endregion

        #region Methods

        public void AddRef()
        {
            _refcount++;
        }

        public void ReleaseRef()
        {
            if (--_refcount == 0)
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

        public PhpTypeCode TypeCode => Value.TypeCode;

        public double ToDouble() => Value.ToDouble();

        public long ToLong() => Value.ToLong();

        public bool ToBoolean() => Value.ToBoolean();

        public Convert.NumberInfo ToNumber(out PhpNumber number) => Value.ToNumber(out number);

        public string ToString(Context ctx) => Value.ToString(ctx);

        public string ToStringOrThrow(Context ctx) => Value.ToStringOrThrow(ctx);

        public object ToClass() => Value.ToClass();

        #endregion
    }
}
