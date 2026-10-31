using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    public static class EncodingExtensions
    {
        const int MaxBytesAtOnce = 1 * 1024 * 1024; // 1M

        /// <summary>
        /// Encodes byte array using <paramref name="encoding"/> into given <paramref name="builder"/>.
        /// </summary>
        /// <returns>Number of characters encoded.</returns>
        public static int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, StringBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(encoding, nameof(encoding));
            ArgumentNullException.ThrowIfNull(builder, nameof(builder));

            if (bytes.IsEmpty)
            {
                return 0;
            }

            if (bytes.Length <= MaxBytesAtOnce)
            {

                var maxCharCount = encoding.GetCharCount(bytes);

                var span = ArrayPool<char>.Shared.Rent(maxCharCount);
                var chars = encoding.GetChars(bytes, span.AsSpan());

                //
                builder.Append(span.AsSpan(0, chars));

                //
                ArrayPool<char>.Shared.Return(span);

                return chars;
            }

            //

            var decoder = encoding.GetDecoder(); // we need to encode in chunks, preserve state between chunks
            int charsCount = 0;

            while (bytes.Length > 0)
            {
                var segment = bytes.Slice(0, Math.Min(MaxBytesAtOnce, bytes.Length));

                bytes = bytes.Slice(segment.Length);

                var maxCharCount = decoder.GetCharCount(segment, flush: bytes.IsEmpty);
                var span = ArrayPool<char>.Shared.Rent(maxCharCount);
                var chars = decoder.GetChars(segment, span.AsSpan(), flush: bytes.IsEmpty);

                //
                builder.Append(span.AsSpan(0, chars));
                charsCount += chars;

                //
                ArrayPool<char>.Shared.Return(span);
            }

            //
            return charsCount;
        }
    }
}
