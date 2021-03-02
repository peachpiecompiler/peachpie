using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Base interface for <see cref="StructBox{TValue}"/>.
    /// </summary>
    public interface IStructBox
    {
        /// <summary>
        /// Gets the underlying boxed value boxed as an <see cref="object"/>.
        /// </summary>
        public abstract object BoxedValue { get; }
    }

    /// <summary>
    /// Boxed value type allowing to call member methods without unboxing and boxing back.
    /// </summary>
    [DebuggerDisplay("StructBox({Value,nq})")]
    public sealed class StructBox<TValue> :
        IStructBox, IPhpCloneable, IEquatable<TValue>
        where TValue : struct
    {
        /// <inheritdoc/>
        object IStructBox.BoxedValue => (object)Value;

        /// <summary>
        /// The boxed value.
        /// </summary>
        public TValue Value;

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        public StructBox(TValue value)
        {
            this.Value = value;
        }

        /// <inheritdoc/>
        object IPhpCloneable.Clone() => new StructBox<TValue>(Value);

        #region IEquatable, IEquatable<TValue>

        //public static bool operator ==(StructBox<TValue> left, StructBox<TValue> right) => ((IEquatable<TValue>)left).Equals(right.Value);

        //public static bool operator !=(StructBox<TValue> left, StructBox<TValue> right) => !(left == right);

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is TValue othervalue)
            {
                return ((IEquatable<TValue>)this).Equals(othervalue);
            }

            if (obj is StructBox<TValue> other)
            {
                return ((IEquatable<TValue>)this).Equals(other.Value);
            }

            return obj != null && Value.Equals(obj);
        }

        bool IEquatable<TValue>.Equals(TValue other)
        {
            if (Value is IEquatable<TValue> equatable)
            {
                return equatable.Equals(other);
            }

            return Value.Equals(other);
        }

        #endregion
    }
}
