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
    [DebuggerNonUserCode]
    public partial class PhpString : IPhpConvertible, IPhpArray
    {
        //[StructLayout(LayoutKind.Explicit)]
        //struct Chunk
        //{
        //    [FieldOffset(0)]
        //    object _obj;

        //    [FieldOffset(0)]
        //    public string _string;

        //    [FieldOffset(0)]
        //    public byte[] _bytes;

        //    [FieldOffset(0)]
        //    public char[] _chars;

        //    [FieldOffset(0)]
        //    public PhpString _phpstring;

        //    public bool IsString => _obj != null && _obj.GetType() == typeof(string);

        //    public bool IsByteArray => _obj != null && _obj.GetType() == typeof(byte[]);

        //    public bool IsCharArray => _obj != null && _obj.GetType() == typeof(char[]);

        //    public bool IsPhpString => _obj != null && _obj.GetType() == typeof(PhpString);
        //}

        [Flags]
        enum Flags : byte
        {
            None = 0,

            ContainsBinary = 1,

            IsNonEmpty = 2,

            IsArrayOfChunks = 4,

            /// <summary>
            /// Whether the blob contains mutable instances that have to be cloned when copying.
            /// </summary>
            ContainsMutables = 8,
        }

        sealed class Blob
        {
            #region Fields

            /// <summary>
            /// One string or concatenated string chunks of either <see cref="string"/>, <see cref="byte"/>[], <see cref="char"/>[] or <see cref="PhpString"/>.
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
            /// Gets value indicating that this instance of data is shared accross more <see cref="PhpString"/> instances and cannot be written to.
            /// </summary>
            public bool IsShared => _refs > 1;

            #region Construction

            public static readonly Blob Empty = new Blob() { _refs = 999 };

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

            public Blob(string value)
            {
                Append(value);
            }

            public Blob(byte[] value)
            {
                Append(value);
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
                        var newarr = new object[arr.Length];
                        Array.Copy(arr, newarr, _chunksCount);

                        if ((_flags & Flags.ContainsMutables) != 0)
                        {
                            for (int i = 0; i < _chunksCount; i++)
                            {
                                InplaceDeepCopy(ref newarr[i]);
                            }
                        }
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
                else if (chunk.GetType() == typeof(PhpString)) chunk = ((PhpString)chunk).Clone();
                else chunk = ((Array)chunk).Clone();   // byte[], char[]
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
                        _length = GetLength();

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

            #endregion

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
                if (value != null && !value.IsEmpty)
                {
                    Debug.Assert(value._blob._chunks != null);

                    if (value._blob.IsArrayOfChunks)
                    {
                        AddChunk(value.DeepCopy());
                    }
                    else
                    {
                        // if containing only one chunk, add it directly
                        var chunk = value._blob._chunks;
                        InplaceDeepCopy(ref chunk);
                        AddChunk(chunk);
                    }

                    _flags |= (value._blob._flags & (Flags.ContainsBinary | Flags.ContainsMutables));    // maintain the binary data flag
                }
            }

            public void Append(byte[] value)
            {
                if (value != null && value.Length != 0)
                {
                    AddChunk(value);
                    _flags |= Flags.ContainsBinary | Flags.ContainsMutables;
                }
            }

            #endregion

            #region AddChunk

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

            #endregion

            #region Output

            public void Output(Context ctx)
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

            #endregion

            #region ToString

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
                if (chunk.GetType() == typeof(PhpString)) return ((PhpString)chunk).ToString(encoding);
                if (chunk.GetType() == typeof(char[])) return new string((char[])chunk);
                throw new ArgumentException(chunk.GetType().ToString());
            }

            #endregion

            #region ToBytes

            /// <summary>
            /// Gets string encoded into array of bytes according to corrent string encoding.
            /// </summary>
            public byte[] ToBytes(Context ctx) => ToBytes(ctx.StringEncoding);

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

                        _flags |= Flags.ContainsMutables;   // any immutable value will be converted to a mutable

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

            static char GetCharInChunk(object chunk, int index)
            {
                AssertChunkObject(chunk);

                if (chunk.GetType() == typeof(string)) return ((string)chunk)[index];
                if (chunk.GetType() == typeof(byte[])) return (char)((byte[])chunk)[index];
                if (chunk.GetType() == typeof(PhpString)) return ((PhpString)chunk)[index];
                if (chunk.GetType() == typeof(char[])) return ((char[])chunk)[index];

                throw new ArgumentException(chunk.GetType().ToString());
            }

            static void SetCharInChunk(ref object chunk, int index, char ch)
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
                if (chunk.GetType() == typeof(PhpString)) return ((PhpString)chunk).ToBoolean();
                if (chunk.GetType() == typeof(char[])) return Convert.ToBoolean((char[])chunk);
                throw new ArgumentException();
            }

            #endregion
        }

        /// <summary>
        /// Content of the string, may be shared.
        /// Cannot be <c>null</c>.
        /// </summary>
        Blob _blob;

        /// <summary>
        /// Gets the count of characters and binary characters.
        /// </summary>
        public int Length => _blob.Length;

        /// <summary>
        /// Gets value indicating the string contains <see cref="byte"/> instead of unicode <see cref="string"/>.
        /// </summary>
        public bool ContainsBinaryData => _blob.ContainsBinaryData;

        /// <summary>
        /// Gets value indicating the string is empty.
        /// </summary>
        public bool IsEmpty => _blob.IsEmpty;

        /// <summary>
        /// Empty immutable string.
        /// </summary>
        public static readonly PhpString Empty = new PhpString();

        #region Construction, Append, DeepCopy

        public PhpString()
        {
            _blob = Blob.Empty.AddRef();
        }

        public PhpString(PhpString from)
        {
            _blob = from._blob.AddRef();
        }

        public PhpString(string x, string y)
        {
            _blob = new Blob(x, y);
        }

        public PhpString(string value)
        {
            _blob = new Blob(value);
        }

        public PhpString(byte[] value)
        {
            _blob = new Blob(value);
        }

        public void Append(string value) => EnsureWritable().Append(value);

        public void Append(PhpString value) => EnsureWritable().Append(value);

        public void Append(byte[] value) => EnsureWritable().Append(value);

        public void Append(PhpValue value, Context ctx)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.String:
                    Append(value.String);
                    break;

                case PhpTypeCode.WritableString:
                    Append(value.WritableString.DeepCopy());
                    break;

                case PhpTypeCode.Alias:
                    Append(value.Alias.Value, ctx);
                    break;

                default:
                    Append(value.ToStringOrThrow(ctx));
                    break;
            }
        }

        public PhpString DeepCopy() => new PhpString(this);

        #endregion

        Blob EnsureWritable() => _blob.IsShared ? (_blob = _blob.ReleaseOne()) : _blob;

        /// <summary>
        /// Outputs the string content to the context output streams.
        /// </summary>
        internal void Output(Context ctx) => _blob.Output(ctx);

        // Prepend

        #region IPhpConvertible

        public PhpTypeCode TypeCode => PhpTypeCode.WritableString;

        public bool ToBoolean() => _blob.ToBoolean();

        public double ToDouble() => Convert.StringToDouble(ToString());

        public long ToLong() => Convert.StringToLongInteger(ToString());

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
                return _blob[index];
            }
            set
            {
                EnsureWritable()[index] = value;
            }
        }

        #endregion

        public object Clone() => DeepCopy();

        public override string ToString() => _blob.ToString(Encoding.UTF8);

        public string ToString(Encoding encoding) => _blob.ToString(encoding);

        public byte[] ToBytes(Context ctx) => ToBytes(ctx.StringEncoding);

        public byte[] ToBytes(Encoding encoding) => _blob.ToBytes(encoding);

        public PhpNumber ToNumber()
        {
            double d;
            long l;
            var info = Convert.StringToNumber(ToString(), out l, out d);
            return (info & Convert.NumberInfo.Double) != 0 ? PhpNumber.Create(d) : PhpNumber.Create(l);
        }
    }
}
