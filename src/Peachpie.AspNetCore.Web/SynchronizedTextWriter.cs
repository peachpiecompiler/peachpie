using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Peachpie.AspNetCore.Web
{
    /// <summary>
    /// <see cref="TextWriter"/> implementation passing text to underlying response stream in given encoding.
    /// </summary>
    sealed class SynchronizedTextWriter : TextWriter
    {
        HttpResponse HttpResponse { get; }

        public override Encoding Encoding { get; }

        /// <summary>
        /// Invariant number format provider.
        /// </summary>
        public override IFormatProvider FormatProvider => Pchp.Core.Context.InvariantNumberFormatInfo;

        public SynchronizedTextWriter(HttpResponse response, Encoding encoding)
        {
            HttpResponse = response ?? throw new ArgumentNullException(nameof(response));
            Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        }

        const int UTF8MaxByteLength = 6;

        static int GetEncodingMaxByteSize(Encoding encoding)
        {
            if (encoding == Encoding.UTF8)
            {
                return UTF8MaxByteLength;
            }

            return encoding.GetMaxByteCount(1);
        }

        /// <summary>
        /// Writes a sequence of bytes into the underlying stream.
        /// </summary>
        public void Write(ReadOnlyMemory<byte> buffer)
        {
            HttpResponse.Body.WriteAsync(buffer).GetAwaiter().GetResult();
        }

        public override void Write(string value)
        {
            // TODO: NET50

            HttpResponse.WriteAsync(value, Encoding).GetAwaiter().GetResult();
        }

        public override void Write(char[] chars, int index, int count)
        {
            Debug.Assert(chars != null);
            Debug.Assert(index <= chars.Length && index >= 0);
            Debug.Assert(count >= 0 && count <= chars.Length - index);

            //
            // TODO: NET50 PERF - use HttpResponse.BodyWriter
            //

            //
            var encodedLength = Encoding.GetByteCount(chars, index, count);
            var bytes = ArrayPool<byte>.Shared.Rent(encodedLength);
            var nbytes = Encoding.GetBytes(chars, index, count, bytes, 0); // == encodedLength

            Write(bytes.AsMemory(0, nbytes));

            ArrayPool<byte>.Shared.Return(bytes);
        }


        public override void Write(char value)
        {
            Span<char> chars = stackalloc char[1] { value };
            var buffer = HttpResponse.BodyWriter.GetSpan(GetEncodingMaxByteSize(Encoding));
            HttpResponse.BodyWriter.Advance(Encoding.GetBytes(chars, buffer));
        }

        public override void Flush() => FlushAsync().GetAwaiter().GetResult();

        public override Task FlushAsync() => HttpResponse.Body.FlushAsync(CancellationToken.None);
    }
}
