using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// String builder providing fast concatenation and character replacements for both unicode and binary strings.
    /// </summary>
    /// <remarks>Optimized for concatenation and output.</remarks>
    public class PhpString : IPhpConvertible
    {
        #region Fields

        /// <summary>
        /// One string or concatenated string chunks of either <see cref="string"/>, <see cref="byte[]"/>, <see cref="char[]"/> or <see cref="PhpString"/>.
        /// </summary>
        object _chunks; // TODO: chunk as a struct { typetable, object }

        /// <summary>
        /// Count of objects in <see cref="_chunks"/>.
        /// </summary>
        int _chunksCount;

        ///// <summary>
        ///// Version of the chunks.
        ///// </summary>
        //int _version;

        // TODO: optimize
        // TODO: allow combination of binary string and unicode string
        // TODO: lazy ToString

        #endregion

        #region Construction

        private PhpString()
        {
            _chunks = null;
            _chunksCount = 0;
        }

        public PhpString(string x, string y)
        {
            _chunks = new object[2] { x, y };
            _chunksCount = 2;
        }

        public PhpString(string value)
        {
            _chunks = value;
        }

        public PhpString(byte[] value)
        {
            _chunks = value;
        }

        #endregion

        static void AssertChunkObject(object chunk)
        {
            Debug.Assert(chunk is byte[] || chunk is string || chunk is char[] || chunk is PhpString);
        }

        void AddChunk(object newchunk)
        {
            AssertChunkObject(newchunk);

            var chunks = _chunks;
            if (chunks != null)
            {
                // TODO: Compact byte[] chunks together
                // TODO: adding after PhpString adds to PhpString
                
                if (chunks.GetType() == typeof(object[]))
                {
                    AddChunkToArray((object[])chunks, newchunk);
                }
                else
                {
                    AssertChunkObject(chunks);
                    _chunks = new object[2] { chunks, newchunk };
                    _chunksCount = 2;
                }
            }
            else
            {
                _chunks = newchunk;
            }
        }

        void AddChunkToArray(object[] chunks, object newchunk)
        {
            Debug.Assert(chunks != null);
            
            if (_chunksCount >= chunks.Length)
            {
                Debug.Assert(chunks.Length != 0);
                var newarr = new object[chunks.Length * 2];
                Array.Copy(chunks, newarr, chunks.Length);
                _chunks = chunks = newarr;

                // TODO: when chunks.Length ~ N => compact
            }

            //
            chunks[_chunksCount++] = newchunk;
        }

        public int Length
        {
            get
            {
                var chunks = _chunks;
                if (chunks != null)
                {
                    if (chunks is object[])
                    {
                        // TODO: cache length for current chunks version
                        return ChunkLength((object[])chunks, _chunksCount);
                    }
                    else
                    {
                        return ChunkLength(chunks);
                    }
                }

                return 0;
            }
        }

        static int ChunkLength(object chunk)
        {
            AssertChunkObject(chunk);

            if (chunk.GetType() == typeof(string)) return ((string)chunk).Length;
            if (chunk.GetType() == typeof(byte[])) return ((byte[])chunk).Length;
            if (chunk.GetType() == typeof(PhpString)) return ((PhpString)chunk).Length;
            if (chunk.GetType() == typeof(char[])) return ((char[])chunk).Length;
            throw new ArgumentException();
        }

        static int ChunkLength(object[] chunks, int count)
        {
            int length = 0;

            for (int i = 0; i < count; i++)
            {
                length += ChunkLength(chunks[i]);
            }

            return length;
        }

        internal void Output(Context ctx)
        {
            var chunks = _chunks;
            if (chunks != null)
            {
                if (chunks.GetType() == typeof(object[]))
                {
                    OutputChunks(ctx, (object[])chunks, _chunksCount);
                }
                else
                {
                    OutputChunk(ctx, chunks);
                }
            }
        }
        
        static void OutputChunk(Context ctx, object chunk)
        {
            AssertChunkObject(chunk);

            if (chunk.GetType() == typeof(string)) ctx.Output.Write((string)chunk);
            if (chunk.GetType() == typeof(byte[])) ctx.OutputStream.Write((byte[])chunk);
            if (chunk.GetType() == typeof(PhpString)) ((PhpString)chunk).Output(ctx);
            if (chunk.GetType() == typeof(char[])) ctx.Output.Write((char[])chunk);
            throw new ArgumentException();
        }

        static void OutputChunks(Context ctx, object[] chunks, int count)
        {
            for (int i = 0; i < count; i++)
            {
                OutputChunk(ctx, chunks[i]);
            }
        }

        #region Append

        public void Append(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                AddChunk(value);
            }
        }

        public void Append(PhpString value)
        {
            if (value != null && value._chunks != null)
            {
                // TODO: if containing only one chunk, add it directly
                AddChunk(value);
            }
        }

        public void Append(byte[] value)
        {
            if (value != null && value.Length != 0)
            {
                AddChunk(value);
            }
        }

        #endregion

        // Prepend
        // this[] { get; set; }

        #region IPhpConvertible

        public PhpTypeCode TypeCode => PhpTypeCode.WritableString;

        public bool ToBoolean()
        {
            var chunks = _chunks;
            if (chunks != null)
            {
                if (chunks.GetType() == typeof(object[]))
                {
                    return _chunksCount == 1 && ChunkToBoolean(((object[])chunks)[0]);
                }
                else
                {
                    return ChunkToBoolean(chunks);
                }
            }

            return false;
        }

        static bool ChunkToBoolean(object chunk)
        {
            AssertChunkObject(chunk);

            if (chunk.GetType() == typeof(string)) return Convert.ToBoolean((string)chunk);
            if (chunk.GetType() == typeof(byte[])) Convert.ToBoolean((byte[])chunk);
            if (chunk.GetType() == typeof(PhpString)) ((PhpString)chunk).ToBoolean();
            if (chunk.GetType() == typeof(char[])) Convert.ToBoolean((char[])chunk);
            throw new ArgumentException();
        }

        public double ToDouble()
        {
            return Convert.StringToDouble(ToString());
        }

        public long ToLong()
        {
            return Convert.StringToLongInteger(ToString());
        }

        public Convert.NumberInfo ToNumber(out PhpNumber number)
        {
            double d;
            long l;
            var info = Convert.StringToNumber(ToString(), out l, out d);
            number = (info & Convert.NumberInfo.Double) != 0
                ? PhpNumber.Create(d)
                : PhpNumber.Create(l);

            return info;
        }

        public string ToString(Context ctx) => ToString(ctx.StringEncoding);

        public string ToStringOrThrow(Context ctx) => ToString(ctx.StringEncoding);

        public object ToClass()
        {
            return new stdClass(PhpValue.Create(ToString()));
        }

        #endregion

        public override string ToString() => ToString(Encoding.UTF8);

        string ToString(Encoding encoding)
        {
            // TODO: cache the result for current chunks version

            var chunks = _chunks;
            if (chunks != null)
            {
                return (chunks.GetType() == typeof(object[]))
                    ? ChunkToString(encoding, (object[])chunks, ref _chunksCount)
                    : ChunkToString(encoding, chunks);
            }
            else
            {
                return string.Empty;
            }
        }

        static string ChunkToString(Encoding encoding, object[] chunks, ref int count)
        {
            if (count == 1)
            {
                return ChunkToString(encoding, chunks[0]);
            }
            else
            {
                var builder = new StringBuilder(32);    // TODO: threadstatic cached instance

                for (int i = 0; i < count; i++)
                {
                    builder.Append(ChunkToString(encoding, chunks[i]));
                }

                return builder.ToString();
            }
        }

        static string ChunkToString(Encoding encoding, object chunk)
        {
            AssertChunkObject(chunk);

            if (chunk.GetType() == typeof(string)) return (string)chunk;
            if (chunk.GetType() == typeof(byte[])) encoding.GetString((byte[])chunk);
            if (chunk.GetType() == typeof(PhpString)) ((PhpString)chunk).ToString();
            if (chunk.GetType() == typeof(char[])) new string((char[])chunk);
            throw new ArgumentException();
        }
    }
}
