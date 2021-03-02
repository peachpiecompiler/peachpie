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
    public sealed class StructBox<TValue> : IStructBox, IPhpCloneable where TValue : struct
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
    }
}
