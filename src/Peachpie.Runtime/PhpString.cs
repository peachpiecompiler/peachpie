using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// String builder providing fast concatenation and character replacements for hybrid strings (both unicode and binary).
    /// </summary>
    /// <remarks>Optimized for concatenation and output.</remarks>
    [DebuggerDisplay("{ToString()}", Type = PhpVariable.TypeNameString)]
    public partial class PhpString : IPhpConvertible, IPhpArray
    {
        //[StructLayout(LayoutKind.Explicit)]
        //public struct StringChunk
        //{
        //    [FieldOffset(0)]
        //    string _string;

        //    [FieldOffset(0)]
        //    byte[] _bytes;

        //    [FieldOffset(0)]
        //    char[] _chars;

        //    [FieldOffset(0)]
        //    PhpString _phpstring;
        //}

        [Flags]
        enum Flags : byte
        {
            None = 0,

            ContainsBinary = 1,

            IsNonEmpty = 2,

            IsArrayOfChunks = 4,
        }

        #region Fields

        /// <summary>
        /// One string or concatenated string chunks of either <see cref="string"/>, <see cref="byte"/>, <see cref="char"/> or <see cref="PhpString"/>.
        /// </summary>
        object _chunks;

        /// <summary>
        /// Count of objects in <see cref="_chunks"/>.
        /// </summary>
        int _chunksCount;

        /// <summary>
        /// String information.
        /// </summary>
        Flags _flags;

        /// <summary>
        /// Cached length of the concatenated string.
        /// </summary>
        int _length = -1;

        /// <summary>
        /// Cached concatenated string.
        /// </summary>
        string _string;

        ///// <summary>
        ///// Version of the chunks.
        ///// </summary>
        //int _version;

        // TODO: optimize
        // TODO: allow combination of binary string and unicode string
        // TODO: lazy ToString

        #endregion

        /// <summary>
        /// Gets value indicating the string contains <see cref="byte"/> instead of unicode <see cref="string"/>.
        /// </summary>
        public bool ContainsBinaryData => (_flags & Flags.ContainsBinary) != 0;

        /// <summary>
        /// Gets value indicating the string is empty.
        /// </summary>
        public bool IsEmpty => (_flags & Flags.IsNonEmpty) == 0;

        /// <summary>
        /// The string is represented internally as array of chunks.
        /// </summary>
        private bool IsArrayOfChunks => (_flags & Flags.IsArrayOfChunks) != 0;

        /// <summary>
        /// Gets the count of characters and binary characters.
        /// </summary>
        public int Length
        {
            get
            {
                if (_length < 0)
                    _length = GetLength();

                return _length;
            }
        }

        #region Construction

        /// <summary>
        /// Empty immutable string.
        /// </summary>
        public static readonly PhpString Empty = new PhpString();

        public PhpString()
        {
            _chunks = null;
            _chunksCount = 0;
            _flags = Flags.None;
        }

        public PhpString(string x, string y)
        {
            if (string.IsNullOrEmpty(y))
            {
                Append(x);
            }
            else if (string.IsNullOrEmpty(x))
            {
                Append(y);
            }
            else
            {
                _chunks = new object[2] { x, y };
                _chunksCount = 2;
                _flags = Flags.IsArrayOfChunks | Flags.IsNonEmpty;
            }
        }

        public PhpString(string value)
        {
            Append(value);
        }

        public PhpString(byte[] value)
        {
            Append(value);
        }

        #endregion

        static void AssertChunkObject(object chunk)
        {
            Debug.Assert(chunk is byte[] || chunk is string || chunk is char[] || chunk is PhpString);
        }

        void AddChunk(object newchunk)
        {
            AssertChunkObject(newchunk);

            // TODO: EnsureWritable

            var chunks = _chunks;
            if (chunks != null)
            {
                Debug.Assert(!this.IsEmpty);

                // TODO: Compact byte[] chunks together

                if (IsArrayOfChunks)
                {
                    Debug.Assert(chunks.GetType() == typeof(object[]));
                    AddChunkToArray((object[])chunks, newchunk);
                }
                else
                {
                    AssertChunkObject(chunks);
                    _chunks = new object[2] { chunks, newchunk };
                    _chunksCount = 2;
                    _flags |= Flags.IsArrayOfChunks;
                }
            }
            else
            {
                _chunks = newchunk;
                _flags |= Flags.IsNonEmpty;
            }

            //
            _string = null;
            _length = -1;
        }

        void AddChunkToArray(object[] chunks, object newchunk)
        {
            Debug.Assert(chunks != null);
            Debug.Assert(_chunksCount > 0);

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

        int GetLength()
        {
            var chunks = _chunks;
            if (chunks != null)
            {
                if (chunks.GetType() == typeof(object[]))
                {
                    Debug.Assert(IsArrayOfChunks);

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
            else if (chunk.GetType() == typeof(byte[])) ctx.OutputStream.Write((byte[])chunk);
            else if (chunk.GetType() == typeof(PhpString)) ((PhpString)chunk).Output(ctx);
            else if (chunk.GetType() == typeof(char[])) ctx.Output.Write((char[])chunk);
            else throw new ArgumentException();
        }

        static void OutputChunks(Context ctx, object[] chunks, int count)
        {
            for (int i = 0; i < count; i++)
            {
                OutputChunk(ctx, chunks[i]);
            }
        }

        #region Append

        public void Append(PhpValue value)
        {
            if (value.TypeCode == PhpTypeCode.WritableString)
                Append(value.WritableString);
            else
                Append(value.ToString());
        }

        public void Append(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                AddChunk(value);
            }
        }

        public void Append(PhpString value)
        {
            if (value != null && !value.IsEmpty)
            {
                Debug.Assert(value._chunks != null);

                if (value.IsArrayOfChunks)
                {
                    AddChunk(value);
                }
                else
                {
                    AddChunk(value._chunks);    // if containing only one chunk, add it directly
                }

                _flags |= (value._flags & Flags.ContainsBinary);    // maintain the binary data flag
            }
        }

        public void Append(byte[] value)
        {
            if (value != null && value.Length != 0)
            {
                AddChunk(value);
                _flags |= Flags.ContainsBinary;
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

        #region IPhpArray

        /// <summary>
        /// Gets number of items in the collection.
        /// </summary>
        int IPhpArray.Count => this.Length;

        /// <summary>
        /// Gets value at given index.
        /// Gets <c>void</c> value in case the key is not found.
        /// </summary>
        PhpValue IPhpArray.GetItemValue(IntStringKey key)
        {
            int index = key.IsInteger ? key.Integer : (int)Convert.StringToLongInteger(key.String);

            return (index >= 0 && index < this.Length)
                ? PhpValue.Create(this[index].ToString())
                : PhpValue.Create(string.Empty);
        }

        /// <summary>
        /// Sets value at specific index. Value must not be an alias.
        /// </summary>
        void IPhpArray.SetItemValue(IntStringKey key, PhpValue value)
        {
            int index = key.IsInteger ? key.Integer : (int)Convert.StringToLongInteger(key.String);

            char ch;

            switch (value.TypeCode)
            {
                case PhpTypeCode.Long:
                    ch = (char)value.Long;
                    break;

                case PhpTypeCode.String:
                    ch = (value.String.Length != 0) ? value.String[0] : '\0';
                    break;

                case PhpTypeCode.WritableString:
                    ch = value.WritableString[0];
                    break;

                // TODO: other types

                default:
                    throw new NotSupportedException(value.TypeCode.ToString());
            }

            this[key.Integer] = ch;
        }

        /// <summary>
        /// Writes aliased value at given index.
        /// </summary>
        void IPhpArray.SetItemAlias(IntStringKey key, PhpAlias alias) { throw new NotSupportedException(); }

        /// <summary>
        /// Add a value to the end of array.
        /// Value can be an alias.
        /// </summary>
        void IPhpArray.AddValue(PhpValue value) { throw new NotSupportedException(); }

        /// <summary>
        /// Removes a value matching given key.
        /// In case the value is not found, the method does nothing.
        /// </summary>
        void IPhpArray.RemoveKey(IntStringKey key) { throw new NotSupportedException(); }

        /// <summary>
        /// Ensures the item at given index is alias.
        /// </summary>
        PhpAlias IPhpArray.EnsureItemAlias(IntStringKey key) { throw new NotSupportedException(); }

        /// <summary>
        /// Ensures the item at given index is class object.
        /// </summary>
        object IPhpArray.EnsureItemObject(IntStringKey key) { throw new NotSupportedException(); }

        /// <summary>
        /// Ensures the item at given index is array.
        /// </summary>
        IPhpArray IPhpArray.EnsureItemArray(IntStringKey key) { throw new NotSupportedException(); }

        #endregion

        #region this[int]

        /// <summary>
        /// Gets or sets character at given index according to PHP semantics.
        /// </summary>
        /// <param name="index">Character index.</param>
        /// <returns>Character at given position.</returns>
        public char this[int index]
        {
            get
            {
                var chunks = _chunks;
                if (chunks != null)
                {
                    if (chunks.GetType() == typeof(object[]))
                    {
                        var arr = (object[])chunks;
                        foreach (var ch in arr)
                        {
                            int ch_length = ChunkLength(ch);
                            if (index < ch_length)
                            {
                                return GetCharInChunk(ch, index);
                            }
                            index -= ch_length;
                        }
                    }
                    else
                    {
                        return GetCharInChunk(chunks, index);
                    }
                }

                throw new ArgumentOutOfRangeException();
            }
            set
            {
                if (index >= this.Length)
                {
                    if (index == this.Length)
                    {
                        this.Append(value.ToString());
                    }
                    else
                    {
                        this.Append(new string('\0', index - this.Length) + value.ToString());
                    }
                }
                else if (index >= 0)
                {
                    // TODO: EnsureWritable

                    var chunks = _chunks;
                    if (chunks != null)
                    {
                        if (chunks.GetType() == typeof(object[]))
                        {
                            var arr = (object[])chunks;
                            for (int i = 0; i < arr.Length; i++)
                            {
                                int ch_length = ChunkLength(arr[i]);
                                if (index < ch_length)
                                {
                                    SetCharInChunk(ref arr[i], index, value);
                                }
                                index -= ch_length;
                            }
                        }
                        else
                        {
                            SetCharInChunk(ref _chunks, index, value);
                        }
                    }
                }
                else
                {
                    // index < 0, ignored
                }
            }
        }

        char GetCharInChunk(object chunk, int index)
        {
            AssertChunkObject(chunk);

            if (chunk.GetType() == typeof(string)) return ((string)chunk)[index];
            if (chunk.GetType() == typeof(byte[])) return (char)((byte[])chunk)[index];
            if (chunk.GetType() == typeof(PhpString)) return ((PhpString)chunk)[index];
            if (chunk.GetType() == typeof(char[])) return ((char[])chunk)[index];

            throw new ArgumentException(chunk.GetType().ToString());
        }

        void SetCharInChunk(ref object chunk, int index, char ch)
        {
            AssertChunkObject(chunk);

            if (chunk.GetType() == typeof(string))
            {
                var chars = ((string)chunk).ToCharArray();
                chars[index] = ch;
                chunk = chars;
            }
            else if (chunk.GetType() == typeof(byte[])) ((byte[])chunk)[index] = (byte)ch;
            else if (chunk.GetType() == typeof(PhpString)) ((PhpString)chunk)[index] = ch;
            else if (chunk.GetType() == typeof(char[])) ((char[])chunk)[index] = ch;
            else throw new ArgumentException(chunk.GetType().ToString());
        }

        #endregion

        public override string ToString() => _string ?? (_string = ToString(Encoding.UTF8));

        public string ToString(Encoding encoding)
        {
            // TODO: cache the result for current chunks version

            var chunks = _chunks;
            if (chunks != null)
            {
                return (chunks.GetType() == typeof(object[]))
                    ? ChunkToString(encoding, (object[])chunks, _chunksCount)
                    : ChunkToString(encoding, chunks);
            }
            else
            {
                return string.Empty;
            }
        }

        static string ChunkToString(Encoding encoding, object[] chunks, int count)
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
            if (chunk.GetType() == typeof(byte[])) return encoding.GetString((byte[])chunk);
            if (chunk.GetType() == typeof(PhpString)) return ((PhpString)chunk).ToString();
            if (chunk.GetType() == typeof(char[])) return new string((char[])chunk);
            throw new ArgumentException(chunk.GetType().ToString());
        }

        public byte[] ToBytes(Encoding encoding)
        {
            var chunks = _chunks;
            if (chunks != null)
            {
                return (chunks.GetType() == typeof(object[]))
                    ? ChunkToBytes(encoding, (object[])chunks, _chunksCount)
                    : ChunkToBytes(encoding, chunks);
            }
            else
            {
                return ArrayUtils.EmptyBytes;
            }
        }

        static byte[] ChunkToBytes(Encoding encoding, object[] chunks, int count)
        {
            if (count == 1)
            {
                return ChunkToBytes(encoding, chunks[0]);
            }
            else
            {
                var buffer = new List<byte>();

                for (int i = 0; i < count; i++)
                {
                    buffer.AddRange(ChunkToBytes(encoding, chunks[i]));
                }

                return buffer.ToArray();
            }
        }

        static byte[] ChunkToBytes(Encoding encoding, object chunk)
        {
            AssertChunkObject(chunk);

            if (chunk.GetType() == typeof(byte[])) return (byte[])chunk;
            if (chunk.GetType() == typeof(string)) return encoding.GetBytes((string)chunk);
            if (chunk.GetType() == typeof(PhpString)) return ((PhpString)chunk).ToBytes(encoding);
            if (chunk.GetType() == typeof(char[])) return encoding.GetBytes((char[])chunk);
            throw new ArgumentException(chunk.GetType().ToString());
        }
    }
}
