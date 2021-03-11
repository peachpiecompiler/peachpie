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

        /// <summary>Temporary buffer for encoded single-character.</summary>
        byte[] _encodedCharBuffer;

#if NETSTANDARD2_0
        readonly char[] _charBuffer = new char[1];
#endif

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
        public void Write(byte[] buffer, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(count <= buffer.Length);

#if NETSTANDARD2_0
            HttpResponse.Body.WriteAsync(buffer, 0, count).GetAwaiter().GetResult();
#else
            HttpResponse.Body.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, count)).GetAwaiter().GetResult();
#endif
        }

        public override void Write(string value)
        {
            // TODO

            HttpResponse.WriteAsync(value, Encoding).GetAwaiter().GetResult();
        }

        public override void Write(char[] chars, int index, int count)
        {
            Debug.Assert(chars != null);
            Debug.Assert(index <= chars.Length && index >= 0);
            Debug.Assert(count >= 0 && count <= chars.Length - index);

            //
            // TODO: PERF - use HttpResponse.BodyWriter directly once we move to CORE 3.0
            //

            //
            var encodedLength = Encoding.GetByteCount(chars, index, count);
            var bytes = ArrayPool<byte>.Shared.Rent(encodedLength);
            var nbytes = Encoding.GetBytes(chars, index, count, bytes, 0); // == encodedLength

            Write(bytes, nbytes);

            ArrayPool<byte>.Shared.Return(bytes);
        }


        public override void Write(char value)
        {
            _encodedCharBuffer ??= new byte[GetEncodingMaxByteSize(Encoding)];

#if NETSTANDARD2_0
            // encode the char
            _charBuffer[0] = value;
            var nbytes = Encoding.GetBytes(_charBuffer, 0, 1, _encodedCharBuffer, 0);
#else
            // encode the char on stack
            Span<char> chars = stackalloc char[1] { value };
            var nbytes = Encoding.GetBytes(chars, _encodedCharBuffer);
#endif

            Write(_encodedCharBuffer, nbytes); // NOTE: _tmp is copied by the underlying pipe
        }

        public override void Flush() => FlushAsync().GetAwaiter().GetResult();

        public override Task FlushAsync() => HttpResponse.Body.FlushAsync(CancellationToken.None);
    }
}
