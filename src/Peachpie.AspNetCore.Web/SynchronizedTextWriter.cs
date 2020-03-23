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
    /// <see cref="TextWriter"/> implementation passing text to underlaying response stream in given encoding.
    /// </summary>
    sealed class SynchronizedTextWriter : TextWriter
    {
        HttpResponse HttpResponse { get; }

        public override Encoding Encoding { get; }

        /// <summary>Temporary buffer for encoded single-character.</summary>
        byte[] _encodedCharBuffer;

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
        /// Writes a sequence of bytes into the underlaying stream.
        /// </summary>
        public void Write(byte[] buffer, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(count <= buffer.Length);

            HttpResponse.Body.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, count)).GetAwaiter().GetResult();
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
            Span<char> chars = stackalloc char[1] { value };
            // Span<byte> bytes = stackalloc byte[GetEncodingMaxByteSize(Encoding)];

            _encodedCharBuffer ??= new byte[GetEncodingMaxByteSize(Encoding)];

            // encode the char on stack
            var nbytes = Encoding.GetBytes(chars, _encodedCharBuffer);

            //
            Write(_encodedCharBuffer, nbytes); // NOTE: _tmp is copied by the underlaying pipe
        }

        public override void Flush() => FlushAsync().GetAwaiter().GetResult();

        public override Task FlushAsync() => HttpResponse.Body.FlushAsync(CancellationToken.None);
    }
}
