using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;

namespace Pchp.Library.Streams
{
    internal class ZlibStreamWrapper : StreamWrapper
    {
        public const string scheme = "compress.zlib";

        public override bool IsUrl
        {
            get { return true; }
        }

        public override string Label
        {
            get { return "ZLIB"; }
        }

        public override string Scheme
        {
            get { return scheme; }
        }

        public override PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context)
        {
            int level = -1;
            var deflateMode = DeflateFilterMode.Normal;

            #region Parse mode options
            // PHP just looks whether there are mode flags in the mode string (skip the first character)
            // last flag is the valid one

            for (int i = 1; i < mode.Length; i++)
            {
                if (Char.IsDigit(mode[i]))
                {
                    level = mode[i] - '0';
                }
                else if (mode[i] == 'f')
                {
                    deflateMode = DeflateFilterMode.Filter;
                }
                else if (mode[i] == 'h')
                {
                    deflateMode = DeflateFilterMode.Huffman;
                }
            }

            #endregion

            #region Path correction

            if (path.StartsWith("compress.zlib://"))
            {
                path = path.Substring(16);
            }
            else if (path.StartsWith("zlib:"))
            {
                path = path.Substring(5);
            }

            #endregion

            var stream = PhpStream.Open(ctx, path, mode, options);

            if (stream != null && stream.CanRead)
            {
                stream.AddFilter(new GzipUncompressionFilter(), FilterChainOptions.Read);
            }

            if (stream != null && stream.CanWrite)
            {
                stream.AddFilter(new GzipCompresionFilter(level, deflateMode), FilterChainOptions.Write);
            }

            return stream;
        }
    }
}
