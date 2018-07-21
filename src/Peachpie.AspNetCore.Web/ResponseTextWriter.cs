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

        public ResponseTextWriter(HttpResponse response, Encoding encoding)
        {
            Debug.Assert(response != null);
            Debug.Assert(encoding != null);

            _response = response;
            _encoding = encoding;
        }

        public override Encoding Encoding => _encoding;

        public override void Write(string value)
        {
            // TODO: optimize, do not allocate byte array over and over

            var bytes = _encoding.GetBytes(value);
            _response.Body.Write(bytes, 0, bytes.Length);
        }

        public override void Write(char value)
        {
            Write(value.ToString());
        }
    }
}
