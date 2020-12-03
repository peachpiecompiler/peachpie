using System;
using System.Collections.Generic;
using System.Text;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Used by symbol implementations (source and metadata) to represent the value
    /// that was mapped from, or will be mapped to a [NullableContext] attribute.
    /// </summary>
    internal enum NullableContextKind : byte
    {
        /// <summary>
        /// Uninitialized state
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// No [NullableContext] attribute
        /// </summary>
        None,

        /// <summary>
        /// [NullableContext(0)]
        /// </summary>
        Oblivious,

        /// <summary>
        /// [NullableContext(1)]
        /// </summary>
        NotAnnotated,

        /// <summary>
        /// [NullableContext(2)]
        /// </summary>
        Annotated,
    }

    internal static class NullableContextUtils
    {
        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableContextKind.Oblivious"/>.
        /// </summary>
        public const byte ObliviousAttributeValue = 0;

        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableContextKind.NotAnnotated"/>.
        /// </summary>
        public const byte NotAnnotatedAttributeValue = 1;

        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableContextKind.Annotated"/>.
        /// </summary>
        public const byte AnnotatedAttributeValue = 2;

        internal static bool TryGetByte(this NullableContextKind kind, out byte? value)
        {
            switch (kind)
            {
                case NullableContextKind.Unknown:
                    value = null;
                    return false;
                case NullableContextKind.None:
                    value = null;
                    return true;
                case NullableContextKind.Oblivious:
                    value = ObliviousAttributeValue;
                    return true;
                case NullableContextKind.NotAnnotated:
                    value = NotAnnotatedAttributeValue;
                    return true;
                case NullableContextKind.Annotated:
                    value = AnnotatedAttributeValue;
                    return true;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        internal static NullableContextKind ToNullableContextFlags(this byte? value)
        {
            switch (value)
            {
                case null:
                    return NullableContextKind.None;
                case ObliviousAttributeValue:
                    return NullableContextKind.Oblivious;
                case NotAnnotatedAttributeValue:
                    return NullableContextKind.NotAnnotated;
                case AnnotatedAttributeValue:
                    return NullableContextKind.Annotated;
                default:
                    throw ExceptionUtilities.UnexpectedValue(value);
            }
        }
    }
}
