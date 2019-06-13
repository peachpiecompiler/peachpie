using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ComponentAce.Compression.Libs.zlib;
using Pchp.Core;

namespace Pchp.Library.Streams
{
    /// <summary>
    /// Mode of DeflateFilter.
    /// </summary>
    public enum DeflateFilterMode
    {
        /// <summary>
        /// Normal compression.
        /// </summary>
        Normal,

        /// <summary>
        /// Filter-only compression.
        /// </summary>
        Filter,

        /// <summary>
        /// Huffman-only compression.
        /// </summary>
        Huffman
    }

    /// <summary>
    /// Base for zlib filters. Contains generic algorithm for reading and writing into zlib stream.
    /// </summary>
    public abstract class ZlibFilter : PhpFilter
    {
        /// <summary>
        /// State of the filter.
        /// </summary>
        private enum ZlibState
        {
            NotStarted,
            Data,
            Finished,
            Failed
        }

        protected ZStream _stream;
        private ZlibState _state;

        /// <summary>
        /// Performs the generic zlib stream filter operation.
        /// </summary>
        /// <param name="input">Input chunk of bytes.</param>
        /// <param name="inputOffset">Current position within the chunk.</param>
        /// <param name="closing">Value indicating whether the stream will be closed.</param>
        /// <returns>Array of available bytes (even empty one). Null on non-critical error.</returns>
        protected byte[] FilterInner(byte[] input, ref int inputOffset, bool closing)
        {
            if (_state == ZlibState.Finished)
            {
                //if stream already ended, throw an error
                PhpException.Throw(PhpError.Warning, "using zlib stream that is already finished");
                return null;
            }

            if (_state == ZlibState.Failed)
            {
                //if stream already ended, throw an error
                PhpException.Throw(PhpError.Warning, "using zlib stream that failed");
                return null;
            }

            List<(byte[] Data, int Length)> subchunks = null;
            int status = zlibConst.Z_OK;

            // initialize if necessary
            if (_state == ZlibState.NotStarted)
            {
                _stream = new ZStream();

                // init algorithm
                status = InitZlibOperation(_stream);

                // check for error
                if (status != zlibConst.Z_OK)
                {
                    _state = ZlibState.Failed;
                    PhpException.Throw(PhpError.Error, Zlib.zError(status));
                    return null;
                }

                _state = ZlibState.Data;
            }

            if (_state == ZlibState.Data)
            {
                // input chunk
                _stream.next_in = input;
                _stream.next_in_index = inputOffset;
                _stream.avail_in = input.Length - inputOffset;

                long initial_total_out = _stream.total_out;
                long initial_total_in = _stream.total_in;

                int nextBufferSize = 8;
                int bufferSizeMax = 65536;

                // do while operation does some progress
                do
                {
                    _stream.next_out = new byte[nextBufferSize];
                    _stream.next_out_index = 0;
                    _stream.avail_out = _stream.next_out.Length;

                    if (nextBufferSize < bufferSizeMax)
                    {
                        nextBufferSize *= 2;
                    }

                    long previous_total_out = _stream.total_out;

                    status = PerformZlibOperation(_stream, GetFlushFlags(closing));

                    if (_stream.total_out - previous_total_out > 0)
                    {
                        // if the list was not initialize, do so
                        if (subchunks == null)
                            subchunks = new List<(byte[], int)>();

                        // add the subchunk to the list only when it contains some data
                        subchunks.Add((_stream.next_out, (int)(_stream.total_out - previous_total_out)));
                    }
                }
                // we continue only when progress was made and there is input available
                while ((status == zlibConst.Z_OK || status == zlibConst.Z_BUF_ERROR) && (_stream.avail_in > 0 || (_stream.avail_in == 0 && _stream.avail_out == 0)));

                // if the last op wasn't the end of stream (this happens only with Z_FINISH) or general success, return error
                if (status != zlibConst.Z_STREAM_END && status != zlibConst.Z_OK)
                {
                    _state = ZlibState.Failed;
                    PhpException.Throw(PhpError.Warning, Zlib.zError(status));
                    return null;
                }

                // end the algorithm if requested
                if (closing || status == zlibConst.Z_STREAM_END)
                {
                    _state = ZlibState.Finished;

                    status = EndZlibOperation(_stream);

                    if (status != zlibConst.Z_OK)
                    {
                        _state = ZlibState.Failed;
                        PhpException.Throw(PhpError.Warning, Zlib.zError(status));
                        return null;
                    }
                }

                inputOffset = _stream.next_in_index;

                // if the chunk ended or everything is OK, connect the subchunks and return
                if (subchunks != null && subchunks.Count > 0)
                {
                    byte[] result = new byte[_stream.total_out - initial_total_out];
                    long resultPos = 0;

                    for (int i = 0; i < subchunks.Count; i++)
                    {
                        Buffer.BlockCopy(
                            subchunks[i].Data,
                            0,
                            result,
                            (int)resultPos,
                            (int)Math.Min(subchunks[i].Length, _stream.total_out - resultPos));

                        resultPos += subchunks[i].Length;
                    }

                    return result;
                }
                else
                {
                    return new byte[0];
                }
            }

            Debug.Fail(null);
            return null;
        }

        /// <summary>
        /// Gets flush flags to be used with zlib operation.
        /// </summary>
        /// <param name="closing">Value indicating whether the stream will be closed.</param>
        /// <returns>Zlib flags.</returns>
        protected abstract int GetFlushFlags(bool closing);

        /// <summary>
        /// Ends the Zlib operation.
        /// </summary>
        /// <param name="zs">Zlib stream.</param>
        /// <returns>Zlib status.</returns>
        protected abstract int EndZlibOperation(ZStream zs);

        /// <summary>
        /// Performs the Zlib operation.
        /// </summary>
        /// <param name="zs">Zlib stream.</param>
        /// <param name="flush">Flush flags.</param>
        /// <returns>Zlib status.</returns>
        protected abstract int PerformZlibOperation(ZStream zs, int flush);

        /// <summary>
        /// Initializes the Zlib operation.
        /// </summary>
        /// <param name="zs">Zlib stream.</param>
        /// <returns>Zlib status.</returns>
        protected abstract int InitZlibOperation(ZStream zs);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public ZlibFilter()
        {
            _state = ZlibState.NotStarted;
        }
    }

    /// <summary>
    /// Filter for deflate algorithm without any header or trailer. Filter mode is not currently supported.
    /// </summary>
    public class DeflateFilter : ZlibFilter
    {
        int _level;
        DeflateFilterMode _mode;

        public DeflateFilter(int level, DeflateFilterMode mode)
            : base()
        {
            _level = level;
            _mode = mode;
        }

        protected override int GetFlushFlags(bool closing)
        {
            return closing ? zlibConst.Z_FINISH : zlibConst.Z_NO_FLUSH;
        }

        protected override int InitZlibOperation(ZStream zs)
        {
            // -MAX_WBITS stands for absense of Zlib header and trailer (needed for GZIP compression and decompression)
            return zs.deflateInit(_level, -Zlib.MAX_WBITS);
        }

        protected override int PerformZlibOperation(ZStream zs, int flush)
        {
            return zs.deflate(flush);
        }

        protected override int EndZlibOperation(ZStream zs)
        {
            return zs.deflateEnd();
        }

        public override TextElement Filter(IEncodingProvider enc, TextElement input, bool closing)
        {
            var bInput = input.AsBytes(enc.StringEncoding);
            if (bInput != null)
            {
                int offset = 0;
                return new TextElement(FilterInner(bInput, ref offset, closing));
            }
            else
            {
                Debug.Fail("DeflateFilter expects chunks to be convertible to PhpBytes.");
                return TextElement.Null;
            }
        }
    }

    /// <summary>
    /// Filter for inflate algorithm without any header or trailer.
    /// </summary>
    public class InflateFilter : ZlibFilter
    {
        public InflateFilter()
            : base()
        {
        }

        protected override int GetFlushFlags(bool closing)
        {
            return closing ? zlibConst.Z_FINISH : zlibConst.Z_NO_FLUSH;
        }

        protected override int InitZlibOperation(ZStream zs)
        {
            // -MAX_WBITS stands for absense of Zlib header and trailer (needed for GZIP compression and decompression)
            return zs.inflateInit(-Zlib.MAX_WBITS);
        }

        protected override int PerformZlibOperation(ZStream zs, int flush)
        {
            return zs.inflate(flush);
        }

        protected override int EndZlibOperation(ZStream zs)
        {
            return zs.inflateEnd();
        }

        public override TextElement Filter(IEncodingProvider enc, TextElement input, bool closing)
        {
            var bInput = input.AsBytes(enc.StringEncoding);
            if (bInput != null)
            {
                int offset = 0;
                return new TextElement(FilterInner(bInput, ref offset, closing));
            }
            else
            {
                Debug.Fail("InflateFilter expects chunks to be convertible to PhpBytes.");
                return TextElement.Null;
            }
        }
    }

    /// <summary>
    /// Gzip compression filter.
    /// </summary>
    public sealed class GzipCompresionFilter : DeflateFilter
    {
        private enum CompressionState
        {
            Header,
            Data,
            Finished,
            Failed
        }

        PhpHash.HashPhpResource.CRC32B _crc;
        CompressionState _state;

        public GzipCompresionFilter(int level, DeflateFilterMode mode)
            : base(level, mode)
        {
            _crc = new PhpHash.HashPhpResource.CRC32B();
            _state = CompressionState.Header;
        }

        public override TextElement Filter(IEncodingProvider enc, TextElement input, bool closing)
        {
            var bInput = input.AsBytes(enc.StringEncoding);

            if (bInput != null)
            {
                if (_state == CompressionState.Failed)
                {
                    PhpException.Throw(PhpError.Warning, "using filter in failed state");
                    return TextElement.Null;
                }

                if (_state == CompressionState.Finished)
                {
                    PhpException.Throw(PhpError.Warning, "using filter in finished state");
                    return TextElement.Null;
                }

                byte[] header = null;
                byte[] footer = null;

                if (_state == CompressionState.Header)
                {
                    header = new byte[Zlib.GZIP_HEADER_LENGTH];
                    header[0] = Zlib.GZIP_HEADER[0];
                    header[1] = Zlib.GZIP_HEADER[1];
                    header[2] = Zlib.Z_DEFLATED;
                    header[3] = 0;
                    // 3-8 represent time and are set to zero
                    header[9] = Zlib.OS_CODE;

                    _crc.Init();

                    _state = CompressionState.Data;
                }

                int outputOffset = 0;
                byte[] output;

                try
                {
                    output = FilterInner(bInput, ref outputOffset, closing);
                }
                catch
                {
                    _state = CompressionState.Failed;
                    throw;
                }

                if (output == null)
                {
                    _state = CompressionState.Failed;
                    return TextElement.Null;
                }

                // input should be read to the end
                Debug.Assert(outputOffset == bInput.Length);

                _crc.Update(bInput);

                if (closing)
                {
                    byte[] crcBytes = _crc.Final();

                    footer = new byte[Zlib.GZIP_FOOTER_LENGTH];

                    // well this implementation simply has the hash inverted compared to C implementation
                    footer[0] = crcBytes[3];
                    footer[1] = crcBytes[2];
                    footer[2] = crcBytes[1];
                    footer[3] = crcBytes[0];

                    footer[4] = (byte)(_stream.total_in & 0xFF);
                    footer[5] = (byte)((_stream.total_in >> 8) & 0xFF);
                    footer[6] = (byte)((_stream.total_in >> 16) & 0xFF);
                    footer[7] = (byte)((_stream.total_in >> 24) & 0xFF);

                    _state = CompressionState.Finished;
                }

                if (header != null || footer != null)
                {
                    int offset = 0;
                    byte[] appended = new byte[(header != null ? header.Length : 0) + output.Length + (footer != null ? footer.Length : 0)];

                    if (header != null)
                    {
                        Buffer.BlockCopy(header, 0, appended, 0, header.Length);
                        offset += header.Length;
                    }

                    if (output != null && output.Length > 0)
                    {
                        Buffer.BlockCopy(output, 0, appended, offset, output.Length);
                        offset += output.Length;
                    }

                    if (footer != null)
                    {
                        Buffer.BlockCopy(footer, 0, appended, offset, footer.Length);
                    }

                    return new TextElement(appended);
                }
                else
                {
                    return new TextElement(output);
                }
            }
            else
            {
                Debug.Fail("GzipCompresionFilter expects chunks to be of type PhpBytes.");
                return TextElement.Null;
            }
        }
    }

    /// <summary>
    /// Filter for gzip uncompression algorithm.
    /// </summary>
    public sealed class GzipUncompressionFilter : InflateFilter
    {
        private enum UncompressionState
        {
            Header,
            HeaderExtraField,
            HeaderFilename,
            HeaderComment,
            HeaderCRC,
            Data,
            Trailer,
            PostTrailer,
            Passthrough,
            Failed,
            Finished
        }

        PhpHash.HashPhpResource.CRC32B _crc;
        int? _headerFlags;
        UncompressionState _state;
        BinaryChunkQueue _chunkQueue;
        int? _extraHeaderLength;

        public GzipUncompressionFilter()
            : base()
        {
            _crc = new PhpHash.HashPhpResource.CRC32B();
            _state = UncompressionState.Header;
            _chunkQueue = new BinaryChunkQueue();
        }

        public override TextElement Filter(IEncodingProvider enc, TextElement input, bool closing)
        {
            // TODO: not the most efficient method - after the filters are upgraded to bucket lists, update this

            var bInput = input.AsBytes(enc.StringEncoding);
            if (bInput != null)
            {
                if (_state == UncompressionState.Failed)
                {
                    // failed filter should not get any more calls
                    PhpException.Throw(PhpError.Warning, "using filter in failed state");
                    return TextElement.Null;
                }

                if (_state == UncompressionState.PostTrailer)
                {
                    // post trailer - ignore everything
                    if (closing)
                    {
                        _state = UncompressionState.Finished;
                    }

                    return TextElement.Empty;
                }

                if (_state == UncompressionState.Finished)
                {
                    // finished filter should not get any more data
                    PhpException.Throw(PhpError.Warning, "using filter in finished state");
                    return TextElement.Null;
                }

                if (_state == UncompressionState.Passthrough)
                {
                    // this is not gzip data format - pass the data through
                    return new TextElement(bInput);
                }

                // enqueue the block
                _chunkQueue.EnqueueByteBlock(bInput, 0, bInput.Length);

                if (_state == UncompressionState.Header)
                {
                    #region Header handling
                    //beginning of the stream
                    byte[] beginning = _chunkQueue.DequeueByteBlock(Zlib.GZIP_HEADER_LENGTH);

                    if (beginning == null && !closing)
                    {
                        // we do not have enough data, but we know there would be more data ahead
                        return TextElement.Empty;
                    }
                    else
                    {
                        //check the header format
                        if (beginning.Length >= 2 && beginning[0] == Zlib.GZIP_HEADER[0] && beginning[1] == Zlib.GZIP_HEADER[1])
                        {
                            //header magic bytes are OK
                            if (beginning.Length < Zlib.GZIP_HEADER_LENGTH)
                            {
                                // header is too short -> this is an error
                                PhpException.Throw(PhpError.Warning, "unexpected end of file");
                                return TextElement.Null;
                            }
                            else
                            {
                                // check the rest of the header
                                if (beginning[2] != Zlib.Z_DEFLATED)
                                {
                                    PhpException.Throw(PhpError.Warning, "unknown compression method");
                                    return TextElement.Null;
                                }

                                if ((beginning[3] & Zlib.GZIP_HEADER_RESERVED_FLAGS) != 0)
                                {
                                    PhpException.Throw(PhpError.Warning, "unknown header flags set");
                                    return TextElement.Null;
                                }

                                _headerFlags = beginning[3];

                                //change the header state based on the header flags
                                UpdateHeaderState();
                            }
                        }
                        else
                        {
                            // this is not a gzip format -> passthrough the data
                            _state = UncompressionState.Passthrough;
                            return new TextElement(beginning);
                        }
                    }
                    #endregion
                }

                if (_state == UncompressionState.HeaderExtraField)
                {
                    #region Header Extra Field Handling
                    if (_extraHeaderLength == null)
                    {
                        //length was not yet detected
                        if (_chunkQueue.AvailableBytes < 2)
                        {
                            //wait for more input
                            return TextElement.Empty;
                        }
                        else
                        {
                            //assemble length
                            _extraHeaderLength = _chunkQueue.DequeueByte();
                            _extraHeaderLength &= (_chunkQueue.DequeueByte() << 8);
                        }
                    }

                    if (_extraHeaderLength != null)
                    {
                        //length was already read
                        if (_chunkQueue.AvailableBytes < _extraHeaderLength)
                        {
                            //wait for more input
                            return TextElement.Empty;
                        }
                        else
                        {
                            Debug.Assert(_extraHeaderLength.HasValue);

                            //skip the extra header
                            _chunkQueue.SkipByteBlock(_extraHeaderLength.Value);

                            UpdateHeaderState();
                        }
                    }
                    #endregion
                }

                if (_state == UncompressionState.HeaderFilename || _state == UncompressionState.HeaderComment)
                {
                    #region Header Filename and Comment Handling
                    // filename or comment

                    // cycle until input ends or zero character is encountered
                    while (true)
                    {
                        byte? nextByte = _chunkQueue.DequeueByte();

                        if (nextByte == null)
                        {
                            //wait for more input
                            return TextElement.Empty;
                        }

                        if (nextByte == 0)
                        {
                            // end the cycle
                            break;
                        }
                    }

                    // go to the next state
                    UpdateHeaderState();
                    #endregion
                }

                if (_state == UncompressionState.HeaderCRC)
                {
                    #region CRC Handling
                    // header CRC

                    if (_chunkQueue.AvailableBytes < 2)
                    {
                        //wait for more input
                        return TextElement.Empty;
                    }
                    else
                    {
                        //skip the CRC
                        _chunkQueue.DequeueByte();
                        _chunkQueue.DequeueByte();

                        UpdateHeaderState();
                    }
                    #endregion
                }

                //filled by data handling and sometimes returned by trailer handling
                byte[] output = null;

                if (_state == UncompressionState.Data)
                {
                    #region Deflated Data Handling

                    //get all available bytes
                    byte[] inputBytes = _chunkQueue.DequeueByteBlock(_chunkQueue.AvailableBytes);
                    int inputOffset = 0;

                    // perform the inner operation
                    try
                    {
                        output = FilterInner(inputBytes, ref inputOffset, closing);
                    }
                    catch
                    {
                        // exception was thrown
                        _state = UncompressionState.Failed;
                        throw;
                    }

                    if (output == null)
                    {
                        // error happened and exception was not thrown
                        _state = UncompressionState.Failed;
                        return TextElement.Null;
                    }

                    // update the hash algorithm
                    _crc.Update(output);

                    if (inputOffset != inputBytes.Length)
                    {
                        // push the rest of the data into the chunk queue
                        _chunkQueue.PushByteBlock(inputBytes, inputOffset, inputBytes.Length - inputOffset);

                        // end of deflated block reached
                        _state = UncompressionState.Trailer;

                        // pass through to Trailer handling
                    }
                    else
                    {
                        //normal decompressed block - return it
                        return new TextElement(output);
                    }

                    #endregion
                }

                if (_state == UncompressionState.Trailer)
                {
                    #region Trailer Handling
                    // the deflate block has already ended, we are processing trailer
                    if (closing || _chunkQueue.AvailableBytes >= Zlib.GZIP_FOOTER_LENGTH)
                    {
                        byte[] trailer;

                        trailer = _chunkQueue.DequeueByteBlock(_chunkQueue.AvailableBytes);

                        if (trailer.Length >= Zlib.GZIP_FOOTER_LENGTH)
                        {
                            byte[] crc = _crc.Final();

                            if (crc[3] != trailer[0] || crc[2] != trailer[1] || crc[1] != trailer[2] || crc[0] != trailer[3])
                            {
                                _state = UncompressionState.Failed;
                                PhpException.Throw(PhpError.Warning, "incorrect data check");
                                return TextElement.Null;
                            }

                            if (BitConverter.ToInt32(trailer, 4) != _stream.total_out)
                            {
                                _state = UncompressionState.Failed;
                                PhpException.Throw(PhpError.Warning, "incorrect length check");
                                return TextElement.Null;
                            }

                            _state = closing ? UncompressionState.Finished : UncompressionState.PostTrailer;

                            // everything is fine, return the output if available
                            return output != null ? new TextElement(output) : TextElement.Empty;
                        }
                        else
                        {
                            _state = UncompressionState.Failed;
                            PhpException.Throw(PhpError.Warning, "unexpected end of file");
                            return TextElement.Null;
                        }
                    }
                    else
                    {
                        //stream is not closing yet - return the remaining output, otherwise empty
                        return output != null ? new TextElement(output) : TextElement.Empty;
                    }
                    #endregion
                }

                //this should not happen
                Debug.Fail(null);
                return TextElement.Null;
            }
            else
            {
                Debug.Fail("GzipUncompressionFilter expects chunks to be convertible to PhpBytes.");
                return TextElement.Null;
            }
        }

        /// <summary>
        /// Changes state based on header flags. Is called by header-handling states only.
        /// </summary>
        private void UpdateHeaderState()
        {
            switch (_state)
            {
                case UncompressionState.Header:
                    if (HeaderFlag(Zlib.GZIP_HEADER_EXTRAFIELD))
                        _state = UncompressionState.HeaderExtraField;
                    else if (HeaderFlag(Zlib.GZIP_HEADER_FILENAME))
                        _state = UncompressionState.HeaderFilename;
                    else if (HeaderFlag(Zlib.GZIP_HEADER_COMMENT))
                        _state = UncompressionState.HeaderComment;
                    else if (HeaderFlag(Zlib.GZIP_HEADER_CRC))
                        _state = UncompressionState.HeaderCRC;
                    else
                        _state = UncompressionState.Data;
                    break;
                case UncompressionState.HeaderExtraField:
                    if (HeaderFlag(Zlib.GZIP_HEADER_FILENAME))
                        _state = UncompressionState.HeaderFilename;
                    else if (HeaderFlag(Zlib.GZIP_HEADER_COMMENT))
                        _state = UncompressionState.HeaderComment;
                    else if (HeaderFlag(Zlib.GZIP_HEADER_CRC))
                        _state = UncompressionState.HeaderCRC;
                    else
                        _state = UncompressionState.Data;
                    break;
                case UncompressionState.HeaderFilename:
                    if (HeaderFlag(Zlib.GZIP_HEADER_COMMENT))
                        _state = UncompressionState.HeaderComment;
                    else if (HeaderFlag(Zlib.GZIP_HEADER_CRC))
                        _state = UncompressionState.HeaderCRC;
                    else
                        _state = UncompressionState.Data;
                    break;
                case UncompressionState.HeaderComment:
                    if (HeaderFlag(Zlib.GZIP_HEADER_CRC))
                        _state = UncompressionState.HeaderCRC;
                    else
                        _state = UncompressionState.Data;
                    break;
                case UncompressionState.HeaderCRC:
                    _state = UncompressionState.Data;
                    break;
            }
        }

        /// <summary>
        /// Checks if a header flag is present.
        /// </summary>
        /// <param name="flag">Flag.</param>
        /// <returns>True if the header flag is valid, otherwise false.</returns>
        private bool HeaderFlag(byte flag)
        {
            return (_headerFlags & flag) != 0;
        }
    }
}
