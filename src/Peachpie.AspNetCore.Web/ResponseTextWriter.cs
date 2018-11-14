using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Peachpie.AspNetCore.Web
{
    /// <summary>
    /// <see cref="TextWriter"/> implementation passing text to underlaying response stream in given encoding.
    /// </summary>
    sealed class ResponseTextWriter : TextWriter
    {
        readonly HttpResponse _response;
        readonly Encoding _encoding;

        /// <summary>
        /// Byte buffer used for decoded unicode strings.
        /// Cannot be <c>null</c>.
        /// </summary>
        byte[] _buffer = new byte[128];

        /// <summary>
        /// Temporary char array for writing a single char.
        /// </summary>
        readonly char[] _tmpchar = new char[1];

        public ResponseTextWriter(HttpResponse response, Encoding encoding)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        }

        public override Encoding Encoding => _encoding;

        /// <summary>
        /// Writes a sequence of bytes into the underlaying stream.
        /// </summary>
        public void Write(byte[] buffer, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(count <= buffer.Length);

            _response.Body.Write(buffer, 0, count);
        }

        byte[] EnsureBuffer(int charscount)
        {
            var buffer = _buffer;

            var maxBytes = _encoding.GetMaxByteCount(charscount);
            if (maxBytes > _buffer.Length)
            {
                _buffer = buffer = new byte[maxBytes + 64];
            }

            return buffer;
        }

        public override void Write(string value)
        {
            Debug.Assert(value != null);

            var buffer = EnsureBuffer(value.Length);
            var bytes = _encoding.GetBytes(value, 0, value.Length, buffer, 0);

            //
            Write(buffer, bytes);
        }

        public override void Write(char[] chars)
        {
            Debug.Assert(chars != null);

            Write(chars, 0, chars.Length);
        }

        public override void Write(char[] chars, int index, int count)
        {
            Debug.Assert(chars != null);
            Debug.Assert(index <= chars.Length && index >= 0);
            Debug.Assert(count >= 0 && count <= chars.Length - index);

            //
            var buffer = EnsureBuffer(count);
            var bytes = _encoding.GetBytes(chars, index, count, buffer, 0);

            //
            Write(buffer, bytes);
        }

        public override void Write(char value)
        {
            _tmpchar[0] = value;
            Write(_tmpchar, 0, 1);
        }
    }
}
