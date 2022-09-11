using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Text;
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

        const int MaxCharsSegment = 2048;

        static int GetEncodingMaxByteSize(Encoding encoding)
        {
            if (encoding == Encoding.UTF8)
            {
                return UTF8MaxByteLength;
            }

            return encoding.GetMaxByteCount(1);
        }

        public override void Write(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Write(value.AsSpan());
            }
        }

        public override void Write(char[] chars, int index, int count)
        {
            Debug.Assert(chars != null);
            Debug.Assert(index <= chars.Length && index >= 0);
            Debug.Assert(count >= 0 && count <= chars.Length - index);

            Write(chars.AsSpan(index, count));
        }

        public void Write(ReadOnlySpan<byte> bytes)
        {
            HttpResponse.BodyWriter.Write(bytes);

            // CONSIDER: Flush
        }

        public override void Write(ReadOnlySpan<char> chars)
        {
            var pipe = HttpResponse.BodyWriter;

            while (chars.Length > 0)
            {
                var segment = chars.Length > MaxCharsSegment ? chars.Slice(0, MaxCharsSegment) : chars;
                var bytesCount = Encoding.GetByteCount(segment);
                var span = pipe.GetSpan(bytesCount);
                pipe.Advance(Encoding.GetBytes(segment, span));

                //
                chars = chars.Slice(segment.Length);
            }

            // CONSIDER: Flush
        }

        public override void Write(char value)
        {
            Span<char> chars = stackalloc char[1] { value };
            var buffer = HttpResponse.BodyWriter.GetSpan(GetEncodingMaxByteSize(Encoding));
            HttpResponse.BodyWriter.Advance(Encoding.GetBytes(chars, buffer));

            // CONSIDER: Flush
        }

        public override void Flush() => FlushAsync().GetAwaiter().GetResult();

        public override Task FlushAsync() => HttpResponse.Body.FlushAsync();
    }
}
