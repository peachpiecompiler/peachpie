using Pchp.Core.Text;
using Pchp.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Text
{
    #region BlobChar

    /// <summary>
    /// A character that can be either a single <see cref="byte"/> or Unicode <see cref="char"/>.
    /// Used internally.
    /// </summary>
    [DebuggerNonUserCode]
    internal readonly struct BlobChar
    {
        private readonly short _b;
        private readonly char _ch;

        public bool IsByte => _b >= 0;

        /// <summary>
        /// Character stored as UTF16.
        /// </summary>
        public bool IsUnicode => _ch != 0;

        /// <summary>
        /// Character needs to be stored as a single byte,
        /// conversion to char would change its semantic.
        /// </summary>
        public bool IsBinary => _b > 0x7f;

        public BlobChar(byte b) : this() { _b = b; }
        public BlobChar(char c) : this() { _ch = c; _b = -1; }
        public BlobChar(long l)
        {
            if (l <= 0xff)
            {
                _b = (short)l;
                _ch = (char)0;
            }
            else
            {
                _ch = (char)l;
                _b = -1;
            }
        }

        /// <summary>
        /// Converts the character to a value.
        /// </summary>
        public PhpValue AsValue() => IsBinary
            ? PhpValue.Create(new PhpString.Blob(new[] { (byte)_b }))   // [0x10, 0xff]
            : PhpValue.Create(AsChar().ToString()); // Char is preferred if can be used

        public char AsChar() => IsByte ? (char)_b : _ch;

        public byte AsByte() => IsByte ? (byte)_b : (byte)_ch;

        public int Ord() => IsByte ? _b : (int)_ch;

        //public void Output(Context ctx)
        //{
        //    if (IsByte)
        //        ctx.OutputStream.WriteByte((byte)_b);  // TODO: WriteAsync, do not allocate byte[1]
        //    else
        //        ctx.Output.Write(_ch);
        //}

        public override string ToString() => AsChar().ToString();

        /// <summary>
        /// Converts the value to a character.
        /// </summary>
        public static BlobChar FromValue(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Long:
                    return new BlobChar(value.Long);

                case PhpTypeCode.String:
                    return new BlobChar(Core.Convert.ToChar(value.String));

                case PhpTypeCode.MutableString:
                    return value.MutableStringBlob[0];

                case PhpTypeCode.Alias:
                    return FromValue(value.Alias.Value);

                // TODO: other types

                default:
                    throw new NotSupportedException(value.TypeCode.ToString());
            }
        }

        /// <summary>
        /// Copies characters to a new array of <see cref="char"/>s.
        /// Single-byte chars are encoded to Unicode chars.
        /// </summary>
        public static char[] ToCharArray(BlobChar[] chars, Encoding enc)
        {
            // TODO: more decent code

            var result = new List<char>(chars.Length);

            Debug.Assert(chars != null);

            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i].IsByte)
                {
                    int j = i;
                    while (j < chars.Length && chars[j].IsByte)
                    {
                        j++;
                    }

                    // encode bytes (i..j] to char array
                    var maxchars = enc.GetMaxCharCount(j - i);
                    var tmp = new char[maxchars];
                    var src = new byte[j - i];

                    for (int b = 0; b < src.Length; b++, i++)
                    {
                        src[b] = (byte)chars[i]._b;
                    }

                    var charscount = enc.GetChars(src, 0, src.Length, tmp, 0);
                    result.AddRange(new ArraySegment<char>(tmp, 0, charscount));
                }
                else
                {
                    result.Add(chars[i]._ch);
                }
            }

            return result.ToArray(); // TODO: span of underlaying items
        }

        /// <summary>
        /// Decode characters into a new array of <see cref="byte"/>s.
        /// Unicode chars are decoded to single-byte chars.
        /// </summary>
        public static byte[] ToByteArray(BlobChar[] chars, Encoding enc)
        {
            // TODO: more decent code

            var result = new List<byte>(chars.Length);

            Debug.Assert(chars != null);

            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i].IsByte)
                {
                    result.Add((byte)chars[i]._b);
                }
                else
                {
                    int j = i;
                    while (j < chars.Length && !chars[j].IsByte)
                    {
                        j++;
                    }

                    // encode bytes (i..j] to char array
                    var maxbytes = enc.GetMaxByteCount(j - i);
                    var tmp = new byte[maxbytes];
                    var src = new char[j - i];

                    for (int b = 0; b < src.Length; b++, i++)
                    {
                        src[b] = chars[i]._ch;
                    }

                    var bytescount = enc.GetBytes(src, 0, src.Length, tmp, 0);
                    result.AddRange(new ArraySegment<byte>(tmp, 0, bytescount));
                }
            }

            return result.ToArray(); // TODO: span of underlaying items
        }

        internal static BlobChar[] ToBlobCharArray(string str)
        {
            var arr = new BlobChar[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                arr[i] = str[i];
            }
            return arr;
        }

        internal static BlobChar[] ToBlobCharArray(char[] chars)
        {
            var arr = new BlobChar[chars.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                arr[i] = chars[i];
            }
            return arr;
        }

        internal static BlobChar[] ToBlobCharArray(byte[] bytes)
        {
            var arr = new BlobChar[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                arr[i] = bytes[i];
            }
            return arr;
        }

        public static implicit operator PhpValue(BlobChar b) => b.AsValue();
        public static implicit operator BlobChar(PhpValue value) => FromValue(value);
        public static implicit operator BlobChar(char c) => new BlobChar(c);
        public static implicit operator BlobChar(byte b) => new BlobChar(b);
    }

    #endregion

    #region IMutableString

    /// <summary>
    /// Provides access to <see cref="PhpString"/> with write access.
    /// </summary>
    internal interface IMutableString : IPhpArray
    {
        void Add(string value);
        void Add(byte[] value);
        void Add(PhpString value);
        void Add(PhpValue value, Context ctx);

        BlobChar this[int index] { get; set; }

        int Length { get; }
    }

    #endregion

    #region PhpStringExtension

    /// <summary>
    /// <see cref="PhpString"/> operations.
    /// </summary>
    public static class PhpStringExtension
    {
        /// <summary>
        /// Gets first characters <c>ord</c>.
        /// </summary>
        public static int Ord(this PhpString str) => str.IsEmpty ? 0 : (int)str[0];

        /// <summary>
        /// Gets substring of this instance.
        /// The operation safely maintains single byte and unicode characters, and reuses existing underlaying chunks of text.
        /// </summary>
        public static PhpString Substring(this PhpString str, int startIndex) => Substring(str, startIndex, int.MaxValue);

        /// <summary>
        /// Gets substring of this instance.
        /// The operation safely maintains single byte and unicode characters, and reuses existing underlaying chunks of text.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The start index is less than zero.</exception>
        public static PhpString Substring(this PhpString str, int startIndex, int length) => str.SubstringInternal(startIndex, length);

        /// <summary>
        /// Creates a new string with reversed order of characters.
        /// </summary>
        public static PhpString Reverse(this PhpString str)
        {
            if (str.IsEmpty)
            {
                return PhpString.Empty;
            }

            return str.ReverseInternal();
        }
    }

    #endregion
}

namespace Pchp.Core
{
    /// <summary>
    /// String builder providing fast concatenation and character replacements for hybrid strings (both unicode and binary).
    /// </summary>
    /// <remarks>Optimized for concatenation and output.</remarks>
    [DebuggerDisplay("{ToString()}", Type = PhpVariable.TypeNameString)]
    [DebuggerNonUserCode, DebuggerStepThrough]
    public struct PhpString : IPhpConvertible
    {
        /// <summary>
        /// Writeable string representation consisting of both Unicode (<see cref="string"/> or <see cref="char"/>[]) and Single-byte characters (<see cref="byte"/>[]).
        /// </summary>
        [DebuggerDisplay("{DebugString}")]
        [DebuggerNonUserCode, DebuggerStepThrough]
        public sealed class Blob : IMutableString
        {
            #region enum Flags

            [Flags]
            enum Flags : byte
            {
                None = 0,

                /// <summary>
                /// The blob contains <see cref="byte"/>[].
                /// </summary>
                ContainsBinary = 1,

                IsNonEmpty = 2,

                IsArrayOfChunks = 4,

                /// <summary>
                /// Whether the blob contains mutable instances that have to be cloned when copying.
                /// </summary>
                ContainsMutables = 8,
            }

            #endregion

            #region Fields

            /// <summary>
            /// One string or concatenated string chunks of either <see cref="string"/>, <see cref="byte"/>[], <see cref="char"/>[] or <see cref="Blob"/>.
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

            /// <summary>
            /// References count. More than one reference means the data are shared accross more instances (read-only).
            /// </summary>
            int _refs = 1;

            #endregion

            #region DebugString

            string DebugString
            {
                get
                {
                    if (ContainsBinaryData)
                    {
                        var bytes = ToBytes(Encoding.UTF8);
                        var str = new StringBuilder(bytes.Length);

                        foreach (var b in bytes)
                        {
                            if (b < 0x7f && b >= 0x20)
                            {
                                str.Append((char)b);
                            }
                            else
                            {
                                str.Append("\\x");
                                str.Append(b.ToString("x2"));
                            }
                        }

                        return str.ToString();
                    }
                    else
                    {
                        return ToString(Encoding.UTF8);
                    }
                }
            }

            #endregion

            /// <summary>
            /// Gets value indicating the string contains <see cref="byte"/> in addition to unicode <see cref="char"/>.
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
            /// Gets value indicating that this instance of data is shared and cannot be written to.
            /// </summary>
            public bool IsShared => _refs > 1;

            #region Construction

            public Blob()
            {
                // pre-cache
                _length = 0;
                _string = string.Empty;
            }

            public Blob(string x, string y)
            {
                if (string.IsNullOrEmpty(y))
                {
                    Add(x);
                }
                else if (string.IsNullOrEmpty(x))
                {
                    Add(y);
                }
                else
                {
                    _chunks = new object[] { x, y };
                    _chunksCount = 2;
                    _flags = Flags.IsArrayOfChunks | Flags.IsNonEmpty;
                }
            }

            public Blob(string value)
            {
                Add(value);
            }

            public Blob(byte[] value)
            {
                Add(value);
            }

            private Blob(Blob blob)
            {
                _chunks = blob._chunks;
                _chunksCount = blob._chunksCount;
                _flags = blob._flags;

                // pre-cache
                _length = blob._length;
                _string = blob._string;
            }

            /// <summary>
            /// Makes this instance of string data shared and returns self.
            /// </summary>
            /// <returns>This instance.</returns>
            public Blob AddRef()
            {
                _refs++;
                return this;
            }

            public Blob ReleaseOne()
            {
                _refs--;

                var clone = new Blob(this);
                clone.InplaceDeepCopy();
                return clone;
            }

            void InplaceDeepCopy()
            {
                var chunks = _chunks;
                if (chunks != null)
                {
                    if (chunks.GetType() == typeof(object[]))
                    {
                        Debug.Assert(IsArrayOfChunks);
                        var arr = (object[])chunks;
                        var newarr = new object[_chunksCount];
                        Array.Copy(arr, newarr, _chunksCount);

                        if ((_flags & Flags.ContainsMutables) != 0)
                        {
                            for (int i = 0; i < _chunksCount; i++)
                            {
                                InplaceDeepCopy(ref newarr[i]);
                            }
                        }

                        _chunks = newarr;
                    }
                    else
                    {
                        InplaceDeepCopy(ref _chunks);
                    }
                }
            }

            static void InplaceDeepCopy(ref object chunk)
            {
                AssertChunkObject(chunk);
                if (chunk.GetType() == typeof(string)) { }  // immutable
                else if (chunk.GetType() == typeof(Blob)) chunk = ((Blob)chunk).AddRef();
                else chunk = ((Array)chunk).Clone();   // byte[], char[], BlobChar[]
            }

            #endregion

            #region Length

            /// <summary>
            /// Gets the count of characters and binary characters.
            /// </summary>
            public int Length
            {
                get
                {
                    if (_length < 0)
                    {
                        _length = GetLength();
                    }

                    return _length;
                }
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

                switch (chunk)
                {
                    case string str: return str.Length;
                    case Blob b: return b.Length;
                    default: return ((Array)chunk).Length;
                }
            }

            static int ChunkLength(object[] chunks, int chunksCount)
            {
                int length = 0;

                for (int i = 0; i < chunksCount; i++)
                {
                    length += ChunkLength(chunks[i]);
                }

                return length;
            }

            #endregion

            #region Add, Append

            public void Append(string value) => Add(value);
            public void Append(PhpString value) => Add(value);

            public void Add(string value)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    AddChunk(value);
                }
            }

            public void Add(Blob value)
            {
                Debug.Assert(value != null);

                if (value.IsEmpty)
                {
                    // should not happen:
                    return;
                }

                Debug.Assert(value._chunks != null);

                if (value.IsArrayOfChunks)
                {
                    AddChunk(value.AddRef());
                }
                else
                {
                    // if containing only one chunk, add it directly
                    var chunk = value._chunks;
                    InplaceDeepCopy(ref chunk);
                    AddChunk(chunk);
                }

                _flags |= (value._flags & (Flags.ContainsBinary | Flags.ContainsMutables));    // maintain the binary data flag
            }

            public void Add(byte[] value)
            {
                if (value != null && value.Length != 0)
                {
                    AddChunk(value);
                    _flags |= Flags.ContainsBinary | Flags.ContainsMutables;
                }
            }

            public void Add(char[] value)
            {
                if (value != null && value.Length != 0)
                {
                    AddChunk(value);
                    _flags |= Flags.ContainsMutables;
                }
            }

            internal void Add(BlobChar[] value)
            {
                if (value != null && value.Length != 0)
                {
                    AddChunk(value);
                    _flags |= Flags.ContainsMutables;
                }
            }

            public void Add(PhpString value)
            {
                var data = value._data;
                if (data is Blob b)
                {
                    Add(b);
                }
                else if (data is string s)
                {
                    Add(s);
                }
                else
                {
                    Debug.Assert(value.IsDefault);
                }
            }

            public void Add(PhpValue value, Context ctx)
            {
                switch (value.TypeCode)
                {
                    case PhpTypeCode.Null:
                        break;

                    case PhpTypeCode.String:
                        Add(value.String);
                        break;

                    case PhpTypeCode.MutableString:
                        Add(value.MutableStringBlob);
                        break;

                    case PhpTypeCode.Alias:
                        Add(value.Alias.Value, ctx);
                        break;

                    default:
                        Add(StrictConvert.ToString(value, ctx));
                        break;
                }
            }

            #endregion

            #region AddChunk

            [Conditional("DEBUG")]
            static void AssertChunkObject(object chunk)
            {
                Debug.Assert(chunk is byte[] || chunk is string || chunk is char[] || chunk is Blob || chunk is BlobChar[]);
            }

            void AddChunk(object newchunk)
            {
                AssertChunkObject(newchunk);

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
                        _chunks = new[] { chunks, newchunk, null, null }; // [4]
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

                if (_chunksCount >= chunks.Length)
                {
                    var newarr = new object[(chunks.Length + 1) * 2];
                    Array.Copy(chunks, newarr, chunks.Length);
                    _chunks = chunks = newarr;

                    // TODO: when chunks.Length ~ N => compact
                }

                //
                chunks[_chunksCount++] = newchunk;
            }

            #endregion

            #region Output

            public void Output(Context ctx)
            {
                var chunks = _chunks;
                if (chunks is object[] objs)
                {
                    OutputChunks(ctx, objs, _chunksCount);
                }
                else if (chunks != null)
                {
                    OutputChunk(ctx, chunks);
                }
            }

            static void OutputChunk(Context ctx, object chunk)
            {
                AssertChunkObject(chunk);

                // NOTE: avoid calling non-async IO operation on ASP.NET Core 3.0;
                // CONSIDER changing to async

                switch (chunk)
                {
                    case string str: ctx.Output.Write(str); break;
                    case byte[] barr: ctx.OutputStream.WriteAsync(barr).GetAwaiter().GetResult(); break;
                    case Blob b: b.Output(ctx); break;
                    case char[] carr: ctx.Output.Write(carr); break;
                    case BlobChar[] barr: WriteChunkAsync(ctx, barr).GetAwaiter().GetResult(); break;
                    default: throw new ArgumentException(chunk.GetType().ToString());
                }
            }

            static async Task WriteChunkAsync(Context ctx, BlobChar[] chars) // TODO: ValueTask
            {
                Debug.Assert(chars != null);

                var enc = ctx.StringEncoding;

                //Span<char> ch = stackalloc char[1];
                //Span<byte> bytes = stackalloc byte[enc.GetMaxByteCount(1)];

                var ch = new char[1];
                var bytes = new byte[ReferenceEquals(enc, Encoding.UTF8) ? 8 : enc.GetMaxByteCount(1)];

                //int size = 0;

                //for (int i = 0; i < chars.Length; i++)
                //{
                //    if (chars[i].IsByte)
                //    {
                //        size++;
                //    }
                //    else
                //    {
                //        ch[0] = chars[i].AsChar();
                //        size += enc.GetByteCount(ch);
                //    }
                //}

                for (int i = 0; i < chars.Length; i++)
                {
                    if (chars[i].IsByte)
                    {
                        bytes[0] = chars[i].AsByte();
                        await ctx.OutputStream.WriteAsync(bytes, 0, 1);
                    }
                    else
                    {
                        ch[0] = chars[i].AsChar();
                        var bytescount = enc.GetBytes(ch, 0, 1, bytes, 0);
                        await ctx.OutputStream.WriteAsync(bytes, 0, bytescount);
                    }
                }
            }

            static void OutputChunks(Context ctx, object[] chunks, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    OutputChunk(ctx, chunks[i]);
                }
            }

            #endregion

            #region Substring

            /// <summary>
            /// Adds portion of this instance into <paramref name="target"/>
            /// </summary>
            /// <param name="target">Target blog to copy substring to.</param>
            /// <param name="start">Index of first character to be copied.</param>
            /// <param name="count">Count of charactyers to be copied.</param>
            internal void Substring(Blob target, int start, ref int count)
            {
                Debug.Assert(target != null);

                var chunks = _chunks;
                if (chunks.GetType() == typeof(object[]))
                {
                    Debug.Assert(IsArrayOfChunks);

                    // TODO: cache length for current chunks version
                    ChunkSubstring(target, start, ref count, (object[])chunks, _chunksCount);
                }
                else
                {
                    ChunkSubstring(target, start, ref count, chunks, out int _);
                }
            }

            static void ChunkSubstring(Blob target, int start, ref int count, object chunk, out int chunkLength)
            {
                Debug.Assert(start >= 0);
                Debug.Assert(count > 0);
                AssertChunkObject(chunk);

                switch (chunk)
                {
                    case string str:
                        chunkLength = str.Length;

                        if (start < str.Length)
                        {
                            int append = Math.Min(str.Length - start, count);
                            target.AddChunk(str.Substring(start, append));
                            count -= append;
                        }

                        break;

                    case byte[] barr:
                        chunkLength = barr.Length;

                        if (start < barr.Length)
                        {
                            int append = Math.Min(barr.Length - start, count);
                            var newarr = new byte[append];
                            Array.Copy(barr, start, newarr, 0, append);
                            target.Add(newarr); // flags must be set
                            count -= append;
                        }

                        break;

                    case Blob b:
                        chunkLength = b.Length;
                        b.Substring(target, start, ref count);
                        break;

                    case char[] carr:
                        chunkLength = carr.Length;

                        if (start < carr.Length)
                        {
                            int append = Math.Min(carr.Length - start, count);
                            var newarr = new char[append];
                            Array.Copy(carr, start, newarr, 0, append);
                            target.Add(newarr); // flags must be set
                            count -= append;
                        }
                        break;

                    case BlobChar[] barr:
                        chunkLength = barr.Length;

                        if (start < barr.Length)
                        {
                            int append = Math.Min(barr.Length - start, count);
                            var newarr = new BlobChar[append];
                            Array.Copy(barr, start, newarr, 0, append);
                            target.Add(newarr); // flags must be set
                            count -= append;
                        }
                        break;

                    default:
                        throw new ArgumentException(chunk.GetType().ToString());
                }
            }

            static void ChunkSubstring(Blob target, int start, ref int count, object[] chunks, int chunksCount)
            {
                Debug.Assert(start >= 0);
                Debug.Assert(chunksCount <= chunks.Length);

                for (int i = 0; i < chunksCount && count > 0; i++)
                {
                    ChunkSubstring(target, start, ref count, chunks[i], out int chunkLength);
                    start = Math.Max(start - chunkLength, 0);
                }
            }

            #endregion

            #region Reverse

            internal Blob Reverse()
            {
                var result = new Blob();

                var chunks = _chunks;
                if (chunks is object[] originalchunks)
                {
                    var count = _chunksCount;
                    var reversedchunks = new object[count];
                    for (int i = 0; i < count; i++)
                    {
                        // reverse chunks in reverse order:
                        reversedchunks[count - i - 1] = ReverseInternal(originalchunks[i]);
                    }

                    result._chunks = reversedchunks;
                    result._chunksCount = count;
                }
                else
                {
                    result._chunks = ReverseInternal(chunks);
                }

                //
                result._string = null; // no cached reversed string
                result._length = _length; // same length as before
                result._flags = _flags; // same flags as before

                return result;
            }

            static object ReverseInternal(object chunk)
            {
                switch (chunk)
                {
                    case string str:
                        return StringUtils.Reverse(str);

                    case byte[] barr:
                        return ArrayUtils.Reverse(barr);

                    case Blob b:
                        return b.Reverse();

                    case char[] carr:
                        return ArrayUtils.Reverse(carr);

                    case BlobChar[] barr:
                        return ArrayUtils.Reverse(barr);

                    default:
                        throw new ArgumentException(chunk.GetType().ToString());
                }
            }

            #endregion

            #region GetByteCount

            public int GetByteCount(Encoding encoding)
            {
                var chunks = _chunks;
                if (chunks != null)
                {
                    return (chunks is object[] objs)
                        ? GetByteCount(encoding, objs, _chunksCount)
                        : GetByteCount(encoding, chunks);
                }
                else
                {
                    return 0;
                }
            }

            static int GetByteCount(Encoding encoding, object[] chunks, int count)
            {
                var length = 0;

                for (int i = 0; i < count; i++)
                {
                    length += GetByteCount(encoding, chunks[i]);
                }

                return length;
            }

            static int GetByteCount(Encoding encoding, object chunk)
            {
                AssertChunkObject(chunk);

                switch (chunk)
                {
                    case string str: return encoding.GetByteCount(str);
                    case byte[] barr: return barr.Length;
                    case Blob b: return b.GetByteCount(encoding);
                    case char[] carr: return carr.Length;
                    case BlobChar[] barr: return barr.Length;
                    default: throw new ArgumentException(chunk.GetType().ToString());
                }
            }

            #endregion

            #region ToString

            public override string ToString() => ToString(Encoding.UTF8);

            public string ToString(Encoding encoding)
            {
                if (_string != null)
                {
                    return _string;
                }

                // TODO: cache the result for current chunks version

                var chunks = _chunks;
                if (chunks != null)
                {
                    return (chunks is object[] objs)
                        ? ChunkToString(encoding, objs, _chunksCount)
                        : ChunkToString(encoding, chunks);
                }
                else
                {
                    return (_string = string.Empty);
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
                    var builder = new StringBuilder(32);    // TODO: pooled instance

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

                switch (chunk)
                {
                    case string str: return str;
                    case byte[] barr: return encoding.GetString(barr);
                    case Blob b: return b.ToString(encoding);
                    case char[] carr: return new string(carr);
                    case BlobChar[] barr: return new string(BlobChar.ToCharArray(barr, encoding));
                    default: throw new ArgumentException(chunk.GetType().ToString());
                }
            }

            #endregion

            #region ToBytes

            /// <summary>
            /// Gets string encoded into array of bytes according to corrent string encoding.
            /// </summary>
            public byte[] ToBytes(Context ctx) => ToBytes(ctx.StringEncoding);

            public byte[] ToBytes(Encoding encoding)
            {
                if (IsEmpty)
                {
                    return ArrayUtils.EmptyBytes;
                }
                else
                {
                    var chunks = _chunks;
                    return (chunks.GetType() == typeof(object[]))
                        ? ChunkToBytes(encoding, (object[])chunks, _chunksCount)
                        : ChunkToBytes(encoding, chunks);
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

                switch (chunk)
                {
                    case string str: return encoding.GetBytes(str);
                    case byte[] barr: return barr;
                    case Blob b: return b.ToBytes(encoding);
                    case char[] carr: return encoding.GetBytes(carr);
                    case BlobChar[] barr: return BlobChar.ToByteArray(barr, encoding);
                    default: throw new ArgumentException(chunk.GetType().ToString());
                }
            }

            #endregion

            #region this[int]

            BlobChar IMutableString.this[int index]
            {
                get => this[index];
                set => this[index] = value;
            }

            /// <summary>
            /// Gets or sets character at given index according to PHP semantics.
            /// </summary>
            /// <param name="index">Character index.</param>
            /// <returns>Character at given position.</returns>
            internal BlobChar this[int index]
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
                        if (index > this.Length)
                        {
                            this.AddChunk(new string('\0', index - this.Length));
                        }

                        object chunk;   // byte[] | string
                        if (value.IsBinary)
                        {
                            _flags |= Flags.ContainsBinary;
                            chunk = new[] { value.AsByte() };
                        }
                        else
                        {
                            chunk = value.ToString();
                        }

                        this.AddChunk(chunk);
                    }
                    else if (index >= 0)
                    {
                        // TODO: EnsureWritable

                        _flags |= Flags.ContainsMutables;   // any immutable value will be converted to a mutable

                        if (value.IsByte)
                        {
                            _flags |= Flags.ContainsBinary;
                        }

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
                                        return;
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

            static BlobChar GetCharInChunk(object chunk, int index)
            {
                AssertChunkObject(chunk);

                if (chunk.GetType() == typeof(string)) return ((string)chunk)[index];
                if (chunk.GetType() == typeof(byte[])) return ((byte[])chunk)[index];
                if (chunk.GetType() == typeof(char[])) return ((char[])chunk)[index];
                if (chunk.GetType() == typeof(Blob)) return ((Blob)chunk)[index];
                if (chunk.GetType() == typeof(BlobChar[])) return ((BlobChar[])chunk)[index];

                throw new ArgumentException(chunk.GetType().ToString());
            }

            static void SetCharInChunk(ref object chunk, int index, BlobChar ch)
            {
                AssertChunkObject(chunk);

                if (chunk is string str)
                {
                    if (ch.IsByte)
                    {
                        var chars = BlobChar.ToBlobCharArray(str);
                        chars[index] = ch;
                        chunk = chars;
                    }
                    else
                    {
                        var chars = str.ToCharArray();
                        chars[index] = ch.AsChar();
                        chunk = chars;
                    }
                }
                else if (chunk.GetType() == typeof(byte[]))
                {
                    if (ch.IsByte)
                    {
                        ((byte[])chunk)[index] = ch.AsByte();
                    }
                    else
                    {
                        var chars = BlobChar.ToBlobCharArray((byte[])chunk);
                        chars[index] = ch;
                        chunk = chars;
                    }
                }
                else if (chunk.GetType() == typeof(char[]))
                {
                    if (ch.IsByte)
                    {
                        var chars = BlobChar.ToBlobCharArray((char[])chunk);
                        chars[index] = ch;
                        chunk = chars;
                    }
                    else
                    {
                        ((char[])chunk)[index] = ch.AsChar();
                    }
                }
                else if (chunk.GetType() == typeof(Blob))
                {
                    ((Blob)chunk)[index] = ch;
                }
                else if (chunk.GetType() == typeof(BlobChar[]))
                {
                    ((BlobChar[])chunk)[index] = ch;
                }
                else
                {
                    throw new ArgumentException(chunk.GetType().ToString());
                }
            }

            #endregion

            #region IPhpConvertible

            public bool ToBoolean()
            {
                var chunks = _chunks;
                if (chunks != null)
                {
                    if (chunks.GetType() == typeof(object[]))
                    {
                        return _chunksCount >= 1 && ChunkToBoolean(((object[])chunks)[0]);
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
                if (chunk.GetType() == typeof(byte[])) return Convert.ToBoolean((byte[])chunk);
                if (chunk.GetType() == typeof(Blob)) return ((Blob)chunk).ToBoolean();
                if (chunk.GetType() == typeof(char[])) return Convert.ToBoolean((char[])chunk);
                if (chunk.GetType() == typeof(BlobChar[])) return Convert.ToBoolean((BlobChar[])chunk);
                throw new ArgumentException();
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
                var index = key.IsInteger ? key.Integer : Convert.StringToLongInteger(key.String);
                return (index >= 0 && index < this.Length) ? this[(int)index].AsValue() : PhpValue.Create(string.Empty);
            }

            PhpValue IPhpArray.GetItemValue(PhpValue index)
            {
                if (index.TryToIntStringKey(out IntStringKey key))
                {
                    return ((IPhpArray)this).GetItemValue(key);
                }

                return new PhpValue(string.Empty);
            }

            void IPhpArray.SetItemValue(PhpValue index, PhpValue value)
            {
                if (index.TryToIntStringKey(out var key))
                {
                    ((IPhpArray)this).SetItemValue(key, value);
                }
                else
                {
                    throw new ArgumentException();
                }
            }

            /// <summary>
            /// Sets value at specific index. Value must not be an alias.
            /// </summary>
            void IPhpArray.SetItemValue(IntStringKey key, PhpValue value)
            {
                var index = key.IsInteger ? key.Integer : Convert.StringToLongInteger(key.String);
                if (NumberUtils.IsInt32(index))
                {
                    this[(int)index] = value;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            /// <summary>
            /// Writes aliased value at given index.
            /// </summary>
            void IPhpArray.SetItemAlias(IntStringKey key, PhpAlias alias) { throw new NotSupportedException(); }

            void IPhpArray.SetItemAlias(PhpValue index, PhpAlias alias) { throw new NotSupportedException(); }

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

            void IPhpArray.RemoveKey(PhpValue index)
            {
                throw new NotImplementedException();
            }

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
        }

        /// <summary>
        /// Content of the string.
        /// Can be either <see cref="string"/> or <see cref="Blob"/> or a <c>null</c> reference (because of <c>default</c>).
        /// </summary>
        object _data;

        /// <summary>
        /// Gets the count of characters and binary characters.
        /// </summary>
        public int Length => _data is string str ? str.Length : _data is Blob b ? b.Length : 0;

        /// <summary>
        /// Gets value indicating the string contains <see cref="byte"/> instead of unicode <see cref="string"/>.
        /// </summary>
        public bool ContainsBinaryData => _data is Blob b && b.ContainsBinaryData;

        /// <summary>
        /// Gets value indicating the string is empty.
        /// </summary>
        public bool IsEmpty => IsDefault || (_data is string s && s.Length == 0) || (_data is Blob b && b.IsEmpty);

        /// <summary>
        /// The value is not initialized.
        /// </summary>
        public bool IsDefault => ReferenceEquals(_data, null);

        /// <summary>
        /// Empty immutable string.
        /// </summary>
        public static PhpString Empty => new PhpString(string.Empty);

        #region Construction, DeepCopy

        public PhpString(Blob blob)
        {
            _data = blob;
        }

        public PhpString(PhpString from)
        {
            var data = from._data;

            if (data is Blob b)
            {
                data = b.AddRef();
            }

            _data = data;
        }

        public PhpString(PhpValue x, Context ctx)
        {
            var b = new Blob();
            b.Add(x, ctx);

            _data = b;
        }

        public PhpString(string value)
        {
            _data = value; // can be null or string
        }

        public PhpString(byte[] value)
        {
            _data = new Blob(value);
        }

        public PhpString(string x, string y)
        {
            _data = new Blob(x, y);
        }

        /// <summary>
        /// Converts <see cref="string"/> to <see cref="PhpString"/>.
        /// </summary>
        /// <param name="value">String value, can be <c>null</c>.</param>
        public static implicit operator PhpString(string value) => new PhpString(value);

        /// <summary>
        /// Converts <see cref="byte"/> array to <see cref="PhpString"/>.
        /// </summary>
        /// <param name="value">String value, can be <c>null</c>.</param>
        public static explicit operator PhpString(byte[] value) => new PhpString(value);

        /// <summary>
        /// Converts <see cref="char"/> array to <see cref="PhpString"/>.
        /// </summary>
        /// <param name="value">String value, can be <c>null</c>.</param>
        public static explicit operator PhpString(char[] value)
        {
            var b = new Blob();
            b.Add(value);

            //
            return new PhpString(b);
        }

        public PhpString DeepCopy() => new PhpString(this);

        #endregion

        /// <summary>
        /// Gets mutable access to the string value.
        /// </summary>
        public Blob/*!*/EnsureWritable()
        {
            if (_data is Blob blob)
            {
                if (blob.IsShared)
                {
                    _data = blob = blob.ReleaseOne();
                }
            }
            else if (ReferenceEquals(_data, null))
            {
                _data = blob = new Blob();
            }
            else if (_data is string str)
            {
                _data = blob = new Blob(str);
            }
            else
            {
                throw new InvalidOperationException();
            }

            //
            return blob;
        }

        /// <summary>
        /// Outputs the string content to the context output streams.
        /// </summary>
        internal void Output(Context ctx)
        {
            if (_data is Blob b)
            {
                b.Output(ctx);
            }
            else if (_data is string str)
            {
                ctx.Output.Write(str);
            }
            else
            {
                Debug.Assert(_data == null);
            }
        }

        // Prepend

        #region IPhpConvertible

        public bool ToBoolean() => _data != null && (_data is Blob b ? b.ToBoolean() : Convert.ToBoolean((string)_data));

        public double ToDouble() => Convert.StringToDouble(ToString());

        public long ToLong() => Convert.ToLong(ToString());

        public Convert.NumberInfo ToNumber(out PhpNumber number) => Convert.ToNumber(ToString(), out number);

        public string ToString(Context ctx) => ToString(ctx.StringEncoding);

        public object ToClass() => new stdClass(AsPhpValue(this));

        public PhpArray ToArray() => PhpArray.New(PhpValue.Create(this.DeepCopy()));

        #endregion

        #region this[int]

        /// <summary>
        /// Gets or sets character at given index according to PHP semantics.
        /// </summary>
        /// <param name="index">Character index.</param>
        /// <returns>Character at given position.</returns>
        public char this[int index]
        {
            get => _data is Blob b ? b[index].AsChar() : _data is string str ? str[index] : '\0';
            set => EnsureWritable()[index] = value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates copy of this string.
        /// Internally only increases <c>refcount</c>.
        /// </summary>
        public object Clone() => DeepCopy();

        /// <summary>
        /// Operator that checks the string is default/uninitialized not containing any value.
        /// </summary>
        public static bool IsNull(PhpString value) => value.IsDefault;

        /// <summary>
        /// Gets substring of this instance.
        /// The operation safely maintains single byte and unicode characters, and reuses existing underlaying chunks of text.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The start index is less than zero.</exception>
        internal PhpString SubstringInternal(int startIndex, int length)
        {
            if (IsDefault || length < 0)
            {
                return default(PhpString); // FALSE
            }

            if (length == 0)
            {
                return new PhpString(string.Empty);
            }

            if (_data is string str)
            {
                var end = Math.Min(str.Length, startIndex + length);
                return end > startIndex ? str.Substring(startIndex, end - startIndex) : string.Empty;
            }

            //

            var blob = new PhpString.Blob();

            CopyTo(blob, startIndex, length);

            return new PhpString(blob);
        }

        /// <summary>
        /// Returns reversed string safe to binary data and unicode characters.
        /// </summary>
        internal PhpString ReverseInternal()
        {
            if (_data is string str)
            {
                return str.Reverse();
            }
            else if (_data is Blob b)
            {
                if (b.ContainsBinaryData)
                {
                    return new PhpString(b.Reverse());
                }
                else if (!b.IsEmpty)
                {
                    return new PhpString(b.ToString().Reverse());
                }
            }

            // 
            return string.Empty;
        }

        /// <summary>
        /// Copies portion of this instance to the target string.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The start index is less than zero.</exception>
        public void CopyTo(Blob target, int startIndex, int length)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (!IsDefault && length > 0)
            {
                if (_data is Blob b)
                {
                    if (startIndex < b.Length)
                    {
                        b.Substring(target, startIndex, ref length);
                    }
                }
                else if (_data is string str)
                {
                    if (startIndex < str.Length)
                    {
                        if (startIndex + length > str.Length)
                        {
                            str = str.Substring(startIndex);
                        }
                        else
                        {
                            str = str.Substring(startIndex, length);
                        }

                        target.Add(str);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the value as <see cref="PhpValue"/>.
        /// </summary>
        public static implicit operator PhpValue(PhpString value) => AsPhpValue(value);

        /// <summary>
        /// Gets bytes count when converted to bytes using provided <paramref name="encoding"/>.
        /// </summary>
        /// <param name="encoding">Encoding to be used to decode bytes from unicode string segments.</param>
        /// <returns>Resulting bytes count.</returns>
        public int GetByteCount(Encoding encoding)
        {
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));

            if (_data is string str)
            {
                return encoding.GetByteCount(str);
            }
            else if (_data is Blob b)
            {
                return b.GetByteCount(encoding);
            }
            else
            {
                return 0;
            }
        }

        #endregion

        /// <summary>
        /// Gets instance of blob that is not shared.
        /// </summary>
        public static Blob/*!*/AsWritable(PhpString str) => str.EnsureWritable();

        /// <summary>
        /// Gets read-only array access to the string.
        /// For write access, use <see cref="EnsureWritable()"/>.
        /// </summary>
        public static Blob/*!*/AsArray(PhpString str) => str._data is Blob b ? b : str._data is string s ? new Blob(s) : new Blob(); // TODO: can be removed (?)

        /// <summary>
        /// Wraps the string into <see cref="PhpValue"/>.
        /// </summary>
        internal static PhpValue AsPhpValue(PhpString str)
        {
            return ReferenceEquals(str._data, null) // default
                ? (PhpValue)string.Empty
                : str._data is Blob b
                    ? PhpValue.Create(b.AddRef())
                    : PhpValue.Create((string)str._data);
        }

        /// <summary>
        /// Gets the first character.
        /// </summary>
        internal BlobChar AsCharacter()
        {
            if (_data is Blob b)
            {
                return b.IsEmpty ? default : b[0];
            }
            else if (_data is string str)
            {
                return str.Length != 0 ? str[0] : default;
            }
            else
            {
                Debug.Assert(_data == null);
                return 0;
            }
        }

        public override string ToString() => ToString(Encoding.UTF8);

        public string ToString(Encoding encoding) => _data is Blob b ? b.ToString(encoding) : _data is string str ? str : string.Empty;

        public byte[] ToBytes(Context ctx) => ToBytes(ctx.StringEncoding);

        public byte[] ToBytes(Encoding encoding) =>
            ReferenceEquals(_data, null)
                ? ArrayUtils.EmptyBytes
                : _data is Blob b
                    ? b.ToBytes(encoding)
                    : _data is string str && str.Length != 0
                        ? encoding.GetBytes(str)
                        : ArrayUtils.EmptyBytes;

        public PhpNumber ToNumber()
        {
            double d;
            long l;
            var info = Convert.StringToNumber(ToString(), out l, out d);
            return (info & Convert.NumberInfo.Double) != 0 ? PhpNumber.Create(d) : PhpNumber.Create(l);
        }
    }
}
