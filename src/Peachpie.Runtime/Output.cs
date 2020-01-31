using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Pchp.Core.Utilities;

namespace Pchp.Core
{
    /// <summary>
	/// Provides output buffering functionality. 
	/// </summary>
	[DebuggerNonUserCode]
    public class BufferedOutput : TextWriter
    {
        /// <summary>
        /// Position of a chunk of buffered data. 
        /// </summary>
        [Flags]
        public enum ChunkPosition
        {
            First = 1,
            Middle = 2,
            Last = 4
        }

        #region Nested Classes: BufferElement, LevelElement

        // data chunk on one level of buffering:
        private class BufferElement
        {
            /// <summary>
            /// The number of valid bytes/chars of the data array.
            /// </summary>
            public int size;

            /// <summary>
            /// Array containing buffered data.
            /// </summary>
            public Array data;
        }

        /// <summary>
        /// Representation of one level of buffering.
        /// </summary>
        private class LevelElement
        {
            public LevelElement(int index)
            {
                this.Index = index;
                this.buffers = new List<BufferElement>();
            }

            /// <summary>
            /// Copies index, name and filter from the element.
            /// </summary>
            /// <param name="element"></param>
            public LevelElement(LevelElement/*!*/element)
                : this(element.Index)
            {
                filter = element.filter;
                levelName = element.levelName;
                userData = element.userData;
            }

            /// <summary>
            /// The index of the level in levels array list.
            /// </summary>
            public readonly int Index;

            public int size;                   // the size (chars + bytes) of all data stored in the buffers list
            public int[] freeSpace = { 0, 0 };    // the number of free bytes/chars in the last byte/char buffer of buffers
            public List<BufferElement> buffers;          // the list of buffers where data are stored   // TODO: PhpString
            public bool containsByteData;      // whether any buffer in the buffers list is of type byte[]
            public bool containsCharData;      // whether any buffer in the buffers list is of type char[]
            public IPhpCallable filter;        // user supplied filtering callback
            public object userData;            // arbitrary data supplied by the user
            public string levelName;           // the PHP name of the level, can be null
        }

        #endregion

        #region Fields and Properties

        /// <summary>
        /// The list of LevelElements.
        /// </summary>
        readonly List<LevelElement> _levels;

        // the current level of buffering (usually the last one); null iff the buffering is disabled
        private LevelElement _level;

        /// <summary>
        /// Minimal sizes of buffers. 
        /// </summary>
        internal readonly int[] _minBufferSize = { 2 * 1024, 20 * 1024 };

        /// <summary>
        /// The writer through which character data will be written.
        /// </summary>
        public TextWriter CharSink { get { return _charSink; } set { _charSink = value; } }
        private TextWriter _charSink;

        /// <summary>
        /// The stream through which binary data will be written.
        /// </summary>
        public Stream ByteSink { get { return _byteSink; } set { _byteSink = value; } }
        private Stream _byteSink;

        /// <summary>
        /// Encoding used to convert between single-byte string and Unicode string.
        /// </summary>
        public override Encoding Encoding { get { return _ctx.StringEncoding; } }
        
        /// <summary>
        /// The buffered binary stream used as for loading binary data to buffers.
        /// </summary>
        public BufferedOutputStream Stream { get; }

        /// <summary>
        /// Runtime context.
        /// </summary>
        readonly Context _ctx;

        /// <summary>
        /// Current buffer level starting from 1. Zero if buffering is disabled.
        /// </summary>
        public int Level { get { return (_level != null) ? _level.Index + 1 : 0; } }

        /// <summary>
        /// The total length of data written to the current level of buffering.
        /// Returns -1 if buffering is disabled.
        /// </summary>
        public int Length { get { return (_level != null) ? _level.size : -1; } }

        #endregion

        #region Construction

        /// <summary>
        /// Creates buffered output with specified sinks.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="enableBuffering">Whether to immediately enable buffering, i.e. increase the level.</param>
        /// <param name="charSink">A writer through which character data will be written.</param>
        /// <param name="byteSink">A stream through which binary data will be written.</param>
        public BufferedOutput(Context ctx, bool enableBuffering, TextWriter charSink, Stream byteSink)
        {
            Debug.Assert(ctx != null);

            _ctx = ctx;
            _charSink = charSink;
            _byteSink = byteSink;
            Stream = new BufferedOutputStream(this);
            _levels = new List<LevelElement>();

            if (enableBuffering)
            {
                IncreaseLevel();
            }
        }

        ///// <summary>
        ///// Creates an instance of <see cref="BufferedOutput"/> having enabled buffering and with sinks set to null sinks.
        ///// </summary>
        //public BufferedOutput()
        //    : this(true, TextWriter.Null, System.IO.Stream.Null, Configuration.Application.Globalization.PageEncoding)
        //{
        //}

        #endregion

        #region Buffer allocation, level changing

        /// <summary>
        /// Gets a buffer where data of requested size and type can be stored. 
        /// </summary>
        /// <param name="sizeNeeded">The number of characters or bytes to be allocated.</param>
        /// <param name="binary">Whether allocated data are bytes or chars.</param>
        /// <param name="buffer">Returns the buffer where data can be written to.</param>
        /// <param name="position">Returns the position where data can be written on.</param>
        /// <returns>The number of allocated characters or bytes.</returns>
        /// <remarks>
        /// The buffer may already exist or new one may be created.
        /// Works on the current level of buffering.
        /// </remarks>
        private int AllocateBuffer(int sizeNeeded, bool binary, out Array buffer, out int position)
        {
            Debug.Assert(_level != null);

            BufferElement element;
            int chunk;
            int kind = binary ? 1 : 0;

            // close binary buffer:
            _level.freeSpace[1 - kind] = 0;

            if (binary) _level.containsByteData = true; else _level.containsCharData = true;

            // no free space for characters found (no buffer exists, the top buffer isn't a character buffer
            // or the top buffer is full character buffer):
            if (_level.freeSpace[kind] == 0)
            {
                // computes the size of buffer to be allocated as min{sizeNeeded,dafaultBufferSize}:
                int size = sizeNeeded;
                if (size < _minBufferSize[kind])
                {
                    size = _minBufferSize[kind];
                    _level.freeSpace[kind] = size - sizeNeeded;
                }
                else
                    _level.freeSpace[kind] = 0; // all space in allocated buffer will be occupied

                // allocates a new buffer element for data:
                element = new BufferElement();
                if (binary) buffer = new byte[size]; else buffer = new char[size];
                element.data = buffer;
                element.size = sizeNeeded;   //sizeNeeded <= (buffer size)
                _level.buffers.Add(element);

                position = 0;
                chunk = sizeNeeded;

            }
            else
            // some free space found:
            {
                Debug.Assert(_level.buffers.Count > 0);

                // available space:
                chunk = (_level.freeSpace[kind] < sizeNeeded) ? _level.freeSpace[kind] : sizeNeeded;

                element = _level.buffers[_level.buffers.Count - 1];
                buffer = element.data;
                position = element.data.Length - _level.freeSpace[kind];
                element.size += chunk;
                _level.freeSpace[kind] -= chunk;
            }
            _level.size += chunk;
            return chunk;
        }


        /// <summary>
        /// Adds a new level of buffering on the top of the levels stack.
        /// </summary>
        /// <remarks>Returns the new level index.</remarks>
        public int IncreaseLevel()
        {
            _levels.Add(_level = new LevelElement(_levels.Count));
            return _level.Index;
        }

        /// <summary>
        /// Checks output buffer is not disabled.
        /// </summary>
        /// <exception cref="InvalidOperationException">When buffering is disabled (<see cref="_level"/> is <c>null</c>).</exception>
        void ThrowIfDisabled()
        {
            if (_level == null)
                throw new InvalidOperationException(Resources.ErrResources.output_buffering_disabled);
        }

        /// <summary>
        /// Destroys the top level of buffering. 
        /// </summary>
        /// <param name="flush">Whether to flush data on the current level. Data will be discarded if not set.</param>
        /// <remarks>Returns the current level index after decreasing.</remarks>
        public int DecreaseLevel(bool flush)
        {
            ThrowIfDisabled();

            if (flush) InternalFlush();

            int top = _levels.Count - 1;
            _levels.RemoveAt(top);

            if (top != 0)
            {
                _level = _levels[top - 1];
                return top - 1;
            }
            else
            {
                _level = null;
                return -1;
            }
        }

        #endregion

        #region Filtering

        /// <summary>
        /// Assignes an arbitrary data to the specified level of buffering.
        /// </summary>
        /// <param name="data">Null reference clears assigned data.</param>
        /// <param name="levelIndex">The level of buffering which the filter to associate with.</param>
        /// <remarks>Data are filtered when flushed.</remarks>
        public void SetUserData(object data, int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= _levels.Count) throw new ArgumentOutOfRangeException("levelIndex");

            _levels[levelIndex].userData = data;
        }


        /// <summary>
        /// Assignes the arbitrary data to the current level of buffering. 
        /// </summary>
        /// <param name="data">The reference to data.</param>
        /// <remarks>Data are filtered when flushed.</remarks>
        public void SetUserData(object data)
        {
            ThrowIfDisabled();

            _level.userData = data;
        }


        /// <summary>
        /// Assignes the filtering callback to the specified level of buffering.
        /// </summary>
        /// <param name="filter">The filter. Null reference means no filter.</param>
        /// <param name="levelIndex">The level of buffering which the filter to associate with.</param>
        /// <remarks>Data are filtered when flushed.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="levelIndex"/> is out of range.</exception>
        public void SetFilter(IPhpCallable filter, int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= _levels.Count)
                throw new ArgumentOutOfRangeException("levelIndex");

            _levels[levelIndex].filter = filter;
        }


        /// <summary>
        /// Assignes the filtering callback to the current level of buffering. 
        /// </summary>
        /// <param name="filter">The filter. Null reference means no filter.</param>
        /// <remarks>Data are filtered when flushed.</remarks>
        /// <exception cref="InvalidOperationException">Output buffering is disabled.</exception>
        public void SetFilter(IPhpCallable filter)
        {
            ThrowIfDisabled();

            _level.filter = filter;
        }

        /// <summary>
        /// Gets the filtering callback defined on the specified level of buffering.
        /// </summary>
        /// <param name="levelIndex">The level of buffering which the filter to associate with.</param>
        /// <returns>The callback or <B>null</B> if no filter has been defined.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="levelIndex"/> is out of range.</exception>
        public IPhpCallable GetFilter(int levelIndex) => _levels[levelIndex].filter;

        /// <summary>
        /// Gets the filtering callback defined on the current level of buffering.
        /// </summary>
        /// <returns>The callback or <B>null</B> if no filter has been defined.</returns>
        /// <exception cref="InvalidOperationException">Output buffering is disabled.</exception>
        public IPhpCallable GetFilter()
        {
            ThrowIfDisabled();

            return _level.filter;
        }

        /// <summary>
        /// Set the level name.
        /// </summary>
        /// <param name="levelIndex">Index of the level from 1.</param>
        /// <param name="levelName">New name of the level.</param>
        public void SetLevelName(int levelIndex, string levelName)
        {
            if (levelIndex < 0 || levelIndex >= Level)
                throw new ArgumentOutOfRangeException("levelIndex");

            _levels[levelIndex].levelName = levelName;
        }

        /// <summary>
        /// Get the name of the level. If the level name is null, the filter.ToString() is used.
        /// </summary>
        /// <param name="levelIndex">Index of the level from 1.</param>
        /// <returns></returns>
        public string GetLevelName(int levelIndex)
        {
            if (levelIndex >= 0 && levelIndex < Level)
            {
                return GetLevelName(_levels[levelIndex]);
            }
            else
            {
                throw new ArgumentOutOfRangeException("levelIndex");
            }
        }

        private string GetLevelName(LevelElement element)
        {
            if (element.levelName != null)
            {
                return element.levelName;
            }
            else
            {
                return (element.filter != null) ? element.filter.ToPhpValue().ToString(_ctx) : "default output handler";
            }
        }

        #endregion

        #region Clean, Flush, FlushAll

        /// <summary>
        /// Discards data on the current level of buffering.
        /// </summary>
        public void Clean()
        {
            if (_level == null)
                return;

            _levels[_level.Index] = _level = new LevelElement(_level);
        }


        /// <summary>
        /// Flushes all data from all buffers to sinks. Discards all data and all levels of buffering.
        /// Disables output buffering.
        /// </summary>
        public void FlushAll()
        {
            for (int i = _levels.Count - 1; i >= 0; i--)
            {
                _level = _levels[i];
                InternalFlush();
            }

            _levels.Clear();
            _level = null;
        }


        /// <summary>
        /// Flushes data on current level of buffering to sinks or to the previous level and discards them.
        /// </summary>
        public void FlushLevel()
        {
            if (_level != null)
            {
                InternalFlush();
                Clean();
            }
        }

        public override void Flush()
        {
            // nothing to do in .NET's System.IO.Stream.Flush
        }

        /// <summary>
        /// Flushes data on current level of buffering to the sinks or to the previous level.
        /// The current level clean up MUST follow this method's call.
        /// </summary>
        internal void InternalFlush()
        {
            Debug.Assert(_level != null);

            if (_level.filter == null)
            {
                if (_level.Index == 0)
                {
                    // TODO: PhpString buffers
                    // NOTE: avoid calling non-async on ASP.NET Core 3.0; consider changing to async

                    // writes top-level data to sinks:
                    for (int i = 0; i < _level.buffers.Count; i++)
                    {
                        var element = _level.buffers[i];
                        if (element.data is char[] chars)
                        {
                            _charSink.Write(chars, 0, element.size);
                        }
                        else
                        {
                            _byteSink
                                .WriteAsync((byte[])element.data, 0, element.size)
                                .GetAwaiter()
                                .GetResult();
                        }
                    }
                }
                else
                {
                    // joins levels (data are not copied => the current level MUST be cleaned up after the return from this method):
                    if (_level.size > 0)
                    {
                        var lower_level = _levels[_level.Index - 1];

                        lower_level.buffers.AddRange(_level.buffers);
                        lower_level.size += _level.size;
                        lower_level.freeSpace = _level.freeSpace;      // free space in the last buffer of the level
                        lower_level.containsByteData |= _level.containsByteData;
                        lower_level.containsCharData |= _level.containsCharData;
                    }
                }
            }
            else
            {
                // gets data from user's callback:
                var data = _level.filter.Invoke(_ctx, GetContent(), PhpValue.Create((int)(ChunkPosition.First | ChunkPosition.Middle | ChunkPosition.Last)));
                if (!data.IsEmpty)
                {
                    var bytes = data.AsBytesOrNull(_ctx);

                    // writes data to the current level of buffering or to sinks depending on the level count:
                    if (_level.Index == 0)
                    {
                        // checks whether the filtered data are binary at first; if not so, converts them to a string:

                        if (bytes != null)
                        {
                            _byteSink.Write(bytes);
                        }
                        else
                        {
                            // TODO: PhpString containing both string and byte[]
                            _charSink.Write(data.ToString(_ctx));
                        }
                    }
                    else
                    {
                        // temporarily decreases the level of buffering toredirect writes to the lower level:
                        var old_level = _level;
                        _level = _levels[_level.Index - 1];

                        // checks whether the filtered data are binary at first; if not so, converts them to a string:
                        if (bytes != null)
                        {
                            Stream.Write(bytes);
                        }
                        else
                        {
                            // TODO: PhpString containing both string and byte[]
                            this.Write(data.ToString(_ctx));
                        }

                        // restore the level of buffering:
                        _level = old_level;
                    }
                }
            }
        }

        #endregion

        #region GetContent

        /// <summary>
        /// Gets a content of buffers on current buffering level.
        /// </summary>
        /// <returns>The content as <see cref="string"/> or array of <see cref="byte"/>s or a 
        /// <c>null</c> reference if output buffering is disabled.</returns>
        public PhpString GetContent()
        {
            if (_level == null)
                return default; // FALSE

            if (_level.size == 0)
                return string.Empty;

            // TODO: return level.buffers directly once it is implemented as PhpString

            if (!_level.containsByteData)
            {
                // contains characters only:
                var result = new StringBuilder(_level.size, _level.size);

                for (int i = 0; i < _level.buffers.Count; i++)
                {
                    var element = _level.buffers[i];
                    result.Append((char[])element.data, 0, element.size);
                }
                return result.ToString();
            }
            else if (!_level.containsCharData)
            {
                // contains bytes only:
                var result = new byte[_level.size];

                for (int i = 0, k = 0; i < _level.buffers.Count; i++)
                {
                    var element = _level.buffers[i];
                    Array.Copy(element.data, 0, result, k, element.size);
                    k += element.size;
                }
                return new PhpString(result);
            }
            else
            {
                // contains both bytes and characters:
                var result = new PhpString.Blob();

                for (int i = 0; i < _level.buffers.Count; i++)
                {
                    var element = _level.buffers[i];

                    if (element.data is char[] chars)
                    {
                        result.Add(new string(chars, 0, element.size));
                    }
                    else
                    {
                        var data = new byte[element.size];
                        Array.Copy(element.data, 0, data, 0, data.Length);
                        result.Add(data);
                    }
                }
                return new PhpString(result);
            }
        }

        #endregion

        #region Write, WriteBytes

        /// <summary>
        /// Writes an array of bytes to the output buffer.
        /// </summary>
        /// <param name="value">Bytes to be written.</param>
        public void WriteBytes(byte[] value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            ThrowIfDisabled();

            WriteInternal(value, true, 0, value.Length);
        }


        /// <summary>
        /// Writes a subarray of bytes to the output buffer.
        /// </summary>
        /// <param name="value">Bytes to be written.</param>
        /// <param name="index">Starting index in the array.</param>
        /// <param name="count">The number of characters to write.</param>
        public void WriteBytes(byte[] value, int index, int count)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (index < 0 || index + count > value.Length)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            ThrowIfDisabled();

            WriteInternal(value, true, index, count);
        }


        /// <summary>
        /// Writes a subarray of characters to the output buffer.
        /// </summary>
        /// <param name="value">Characters to be written.</param>
        public override void Write(char[] value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            ThrowIfDisabled();

            WriteInternal(value, false, 0, value.Length);
        }


        /// <summary>
        /// Writes a subarray of characters to the output buffer.
        /// </summary>
        /// <param name="value">The value to be written.</param>
        /// <param name="index">Starting index in the array.</param>
        /// <param name="count">The number of characters to write.</param>
        public override void Write(char[] value, int index, int count)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (index < 0 || index + count > value.Length)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            ThrowIfDisabled();

            WriteInternal(value, false, index, count);
        }

        /// <summary>
        /// Writes a subarray to the output buffer.
        /// </summary>
        /// <param name="value">The value to be written.</param>
        /// <param name="binary">The type of items in array (byte/char).</param>
        /// <param name="index">Starting index in the array.</param>
        /// <param name="count">The number of items to write.</param>
        internal void WriteInternal(Array value, bool binary, int index, int count)
        {
            int position;
            Array buffer;
            int length = count;
            int chunk;

            // writes initial sequence of characters to buffer:
            chunk = AllocateBuffer(length, binary, out buffer, out position);
            length -= chunk;
            Array.Copy(value, index, buffer, position, chunk);

            // if not all characters has been written writes the rest to the next buffer:
            if (length > 0)
            {
                AllocateBuffer(length, binary, out buffer, out position);
                Array.Copy(value, index + chunk, buffer, position, length);
            }
        }

        /// <summary>
        /// Writes a single character to the output buffer.
        /// </summary>
        /// <param name="value">The value to be written.</param>
        public override void Write(char value)
        {
            ThrowIfDisabled();

            int position;
            Array buffer;

            AllocateBuffer(1, false, out buffer, out position);
            ((char[])buffer)[position] = value;
        }


        /// <summary>
        /// Writes a string value to the output buffer.
        /// </summary>
        /// <param name="value">The value to be written.</param>
        public override void Write(string value)
        {
            ThrowIfDisabled();

            if (value == null) value = String.Empty;

            int position;
            Array buffer;
            int length = value.Length;
            int chunk;

            // writes initial sequence of characters to buffer:
            chunk = AllocateBuffer(length, false, out buffer, out position);
            length -= chunk;
            value.CopyTo(0, (char[])buffer, position, chunk);

            // if not all characters written then writes the rest to the next buffer:
            if (length > 0)
            {
                AllocateBuffer(length, false, out buffer, out position);
                value.CopyTo(chunk, (char[])buffer, position, length);
            }
        }

        #endregion

        #region GetLevelInfo

        /// <summary>
        /// Gets some information about a specified level.
        /// </summary>
        /// <param name="levelIndex">Level index starting from 1.</param>
        /// <param name="filter">Filtering callback (if any).</param>
        /// <param name="size">Data size.</param>
        /// <param name="name">Optionally the level name.</param>
        public void GetLevelInfo(int levelIndex, out IPhpCallable filter, out int size, out string name)
        {
            if (levelIndex < 1 || levelIndex > Level)
                throw new ArgumentOutOfRangeException("levelIndex");
            
            var element = _levels[levelIndex - 1];
            filter = element.filter;
            size = element.size;
            name = GetLevelName(element);
        }

        /// <summary>
        /// Find level index by the filter callback.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public int FindLevelByFilter(IPhpCallable filter)
        {
            if (_levels != null && filter != null)
                for (int i = 0; i < Level; i++)
                    if (ReferenceEquals(_levels[i].filter, filter))
                        return i;

            return -1;
        }

        #endregion
    }


    /// <summary>
    /// Provides output buffering of streams.
    /// </summary>
    public class BufferedOutputStream : Stream
    {
        private BufferedOutput output;

        public BufferedOutputStream(BufferedOutput output)
        {
            this.output = output;
        }

        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }

        public override void Flush()
        {
            output.Flush();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            output.WriteBytes(buffer, offset, count);
        }


        #region Unsupported functionality

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #endregion

    }
}
