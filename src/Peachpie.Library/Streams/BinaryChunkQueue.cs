using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Pchp.Library.Streams
{
    /// <summary>
    /// Queue of binary chunks that can be pushed to and popped from.
    /// </summary>
    internal class BinaryChunkQueue
    {
        /// <summary>
        /// Internal representation of chunk.
        /// </summary>
        private class Chunk
        {
            /// <summary>
            /// Byte array.
            /// </summary>
            public byte[] Bytes { get; private set; }

            /// <summary>
            /// Starting offset.
            /// </summary>
            public int Offset { get; private set; }

            /// <summary>
            /// Length of the chunk.
            /// </summary>
            public int Length { get; private set; }

            /// <summary>
            /// Initializes new binary chunk.
            /// </summary>
            /// <param name="chunk">Non-null reference to byte array.</param>
            /// <param name="offset">Starting offset in the chunk.</param>
            /// <param name="length">Length of the valid area in the chunk.</param>
            public Chunk(byte[] chunk, int offset, int length)
            {
                Bytes = chunk;
                Offset = offset;
                Length = length;
            }
        }

        /// <summary>
        /// Available bytes within the binary chunk queue.
        /// </summary>
        private int _availableBytes;

        /// <summary>
        /// List of chunks.
        /// </summary>
        private LinkedList<Chunk> _chunks;

        /// <summary>
        /// Position within the first chunk, if possible (otherwise -1).
        /// </summary>
        private int _position;

        /// <summary>
        /// Gets total available bytes within the queue.
        /// </summary>
        public int AvailableBytes { get { return _availableBytes; } }

        /// <summary>
        /// Pushes new chunk into the queue.
        /// </summary>
        /// <param name="chunk">Byte array.</param>
        public void Push(byte[] chunk)
        {
            EnqueueByteBlock(chunk, 0, chunk.Length);
        }

        /// <summary>
        /// Pushes new chunk into the queue.
        /// </summary>
        /// <param name="chunk">Byte array.</param>
        /// <param name="offset">Offset of the valid area of the chunk.</param>
        /// <param name="length">Length of valid area of the chunk.</param>
        public void EnqueueByteBlock(byte[] chunk, int offset, int length)
        {
            Debug.Assert(chunk != null);
            Debug.Assert(offset >= 0 && offset < chunk.Length);
            Debug.Assert(length > 0 && offset + length <= chunk.Length);

            Chunk newChunk = new Chunk(chunk, offset, length);

            _chunks.AddLast(newChunk);
            _availableBytes += length;

            if (_position == -1)
            {
                Debug.Assert(_chunks.First.Value == newChunk);
                _position = newChunk.Offset;
            }
        }

        /// <summary>
        /// Pushes new chunk into the beginning of the queue.
        /// </summary>
        /// <param name="chunk">Byte array.</param>
        /// <param name="offset">Offset of the valid area of the chunk.</param>
        /// <param name="length">Length of valid area of the chunk.</param>
        public void PushByteBlock(byte[] chunk, int offset, int length)
        {
            Debug.Assert(chunk != null);
            Debug.Assert(offset >= 0 && offset < chunk.Length);
            Debug.Assert(length > 0 && offset + length <= chunk.Length);

            if (_chunks.First != null && _position != _chunks.First.Value.Offset)
            {
                //change the first chunk if needed
                Chunk old = _chunks.First.Value;
                Chunk replacement = new Chunk(old.Bytes, _position, old.Length - (_position - old.Offset));
                _chunks.RemoveFirst();
                _chunks.AddFirst(replacement);
            }

            Chunk newChunk = new Chunk(chunk, offset, length);

            _chunks.AddFirst(newChunk);
            _availableBytes += length;
            _position = newChunk.Offset;
        }

        /// <summary>
        /// Pops a single byte from the queue, removing it in the process.
        /// </summary>
        /// <returns>Next byte value if there was any present, otherwise null.</returns>
        public byte? DequeueByte()
        {
            if (_chunks.First == null)
            {
                Debug.Assert(_position == -1);
                return null;
            }
            else
            {
                Chunk current = _chunks.First.Value;

                Debug.Assert(_position >= current.Offset);
                Debug.Assert(_position < current.Offset + current.Length);
                Debug.Assert(_availableBytes > 0);

                byte ret = current.Bytes[_position];

                _position++;
                _availableBytes--;

                if (_position >= current.Offset + current.Length)
                {
                    // move to the next chunk and remove the first
                    _chunks.RemoveFirst();

                    if (_chunks.First == null)
                    {
                        _position = -1;
                    }
                    else
                    {
                        _position = _chunks.First.Value.Offset;
                    }
                }

                return ret;
            }
        }

        /// <summary>
        /// Peeks a single byte from the queue.
        /// </summary>
        /// <returns>Next byte value if there was any present, otherwise null.</returns>
        public byte? PeekByte()
        {
            if (_chunks.First == null)
            {
                Debug.Assert(_position == -1);
                return null;
            }
            else
            {
                Chunk current = _chunks.First.Value;

                Debug.Assert(_position >= current.Offset);
                Debug.Assert(_position < current.Offset + current.Length);

                return current.Bytes[_position];
            }
        }

        /// <summary>
        /// Pops a byte block from the queue, removing it in the process.
        /// </summary>
        /// <param name="length">Requested length of the block.</param>
        /// <returns>Block of bytes of requested length if available, otherwise null.</returns>
        public byte[] DequeueByteBlock(int length)
        {
            // non-negative length
            Debug.Assert(length >= 0);

            if (length == 0)
            {
                // fast branch for requested zero length
                return new byte[0];
            }
            else if (_availableBytes < length)
            {
                // block of that length is not available
                return null;
            }
            else if (_position == 0 && _chunks.First != null && _chunks.First.Value.Offset == 0 && _chunks.First.Value.Length == length)
            {
                // fast track for getting the whole first block without copying
                byte[] ret = _chunks.First.Value.Bytes;

                // remove the first block
                _chunks.RemoveFirst();

                //update available bytes
                _availableBytes -= length;

                //update position;
                if (_chunks.First == null)
                {
                    _position = -1;
                }
                else
                {
                    _position = _chunks.First.Value.Offset;
                }

                return ret;
            }
            else
            {
                byte[] block = new byte[length];
                int offset = 0;

                while (offset < length)
                {
                    // there should always be chunk available
                    Debug.Assert(_chunks.First != null);

                    Chunk record = _chunks.First.Value;
                    int remainingLength = record.Offset + record.Length - _position;
                    int copyLength = length - offset < remainingLength ? length - offset : remainingLength;

                    // position should be inside valid area of the chunk and not past the end
                    Debug.Assert(_position >= record.Offset && _position < record.Offset + record.Length);
                    // available bytes should be higher than chunk's remaining bytes
                    Debug.Assert(_availableBytes >= remainingLength);
                    // length to copy should be positive and not higher than block's remaining bytes
                    Debug.Assert(copyLength > 0 && copyLength <= remainingLength);

                    // perform the operation
                    Buffer.BlockCopy(
                        record.Bytes,
                        _position,
                        block,
                        offset,
                        copyLength);

                    // update offset and available bytes
                    offset += copyLength;
                    _availableBytes -= copyLength;

                    Debug.Assert(offset <= length);

                    if (copyLength < remainingLength)
                    {
                        // update the position within the current block
                        _position += copyLength;

                        // this should be always the last iteration
                        Debug.Assert(offset == length);
                    }
                    else
                    {
                        // the chunk is finished - remove it
                        _chunks.RemoveFirst();

                        if (_chunks.First != null)
                        {
                            // there is successor to this chunk
                            _position = _chunks.First.Value.Offset;
                        }
                        else
                        {
                            // there is no successor to this chunk
                            _position = -1;

                            // this should be always the last iteration
                            Debug.Assert(offset == length);
                        }
                    }
                }

                return block;
            }
        }

        /// <summary>
        /// Skips the given count of bytes in the queue.
        /// </summary>
        /// <param name="length">Number of bytes to skip.</param>
        /// <returns>True if the given count of bytes was available, otherwise false.</returns>
        public bool SkipByteBlock(int length)
        {
            Debug.Assert(length >= 0);

            if (length == 0)
            {
                return true;
            }
            else if (_availableBytes < length)
            {
                // cannot advance that much
                return false;
            }
            else
            {
                int alreadySkipped = 0;

                while (alreadySkipped < length)
                {
                    // there should always be chunk available
                    Debug.Assert(_chunks.First != null);

                    Chunk chunk = _chunks.First.Value;
                    int remainingLength = chunk.Offset + chunk.Length - _position;
                    int advanceLength = length - alreadySkipped < remainingLength ? length - alreadySkipped : remainingLength;

                    // position should be inside valid area of the chunk and not past the end
                    Debug.Assert(_position >= chunk.Offset && _position < chunk.Offset + chunk.Length);
                    // available bytes should be higher than chunk's remaining bytes
                    Debug.Assert(_availableBytes >= remainingLength);
                    // count to skip should be positive and not higher than chunk's remaining bytes
                    Debug.Assert(advanceLength > 0 && advanceLength <= remainingLength);


                    // update skipped count and available bytes
                    alreadySkipped += advanceLength;
                    _availableBytes -= advanceLength;

                    Debug.Assert(alreadySkipped <= length);

                    if (advanceLength < remainingLength)
                    {
                        // update the position within the current block
                        _position += advanceLength;

                        // this should be always the last iteration
                        Debug.Assert(alreadySkipped == length);
                    }
                    else
                    {
                        // the chunk is finished - remove it
                        _chunks.RemoveFirst();

                        if (_chunks.First != null)
                        {
                            // there is successor to this chunk
                            _position = _chunks.First.Value.Offset;
                        }
                        else
                        {
                            // there is no successor to this chunk
                            _position = -1;

                            // this should be always the last iteration
                            Debug.Assert(alreadySkipped == length);
                        }
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Initializes new instance of the class.
        /// </summary>
        public BinaryChunkQueue()
        {
            _chunks = new LinkedList<Chunk>();
            _availableBytes = 0;
            _position = -1;
        }
    }

}
