using Pchp.Core;
using Pchp.Library.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Streams
{
    public static class PhpStreams
    {
        #region Constants

        /// <summary>
        /// The "whence" options used in PhpStream.Seek().
        /// </summary>
        [PhpHidden]
        public enum SeekOptions
        {
            /// <summary>Seek from the beginning of the file.</summary>
            Set = SeekOrigin.Begin,   // 0 (OK)
                                      /// <summary>Seek from the current position.</summary>
            Current = SeekOrigin.Current, // 1 (OK)
                                          /// <summary>Seek from the end of the file.</summary>
            End = SeekOrigin.End      // 2 (OK)
        }

        public const int SEEK_SET = (int)SeekOptions.Set;
        public const int SEEK_CUR = (int)SeekOptions.Current;
        public const int SEEK_END = (int)SeekOptions.End;

        /// <summary>
        /// Value used as an argument to <c>flock()</c> calls.
        /// Passed to streams using the <see cref="PhpStream.SetParameter"/>
        /// with <c>option</c> set to <see cref="StreamParameterOptions.Locking"/>.
        /// </summary>
        /// <remarks>
        /// Note that not all of these are flags. Only the <see cref="StreamLockOptions.NoBlocking"/> 
        /// may be added to one of the first three values.
        /// </remarks>
        [Flags, PhpHidden]
        public enum StreamLockOptions
        {
            /// <summary>
            /// To acquire a shared lock (reader), set operation to LOCK_SH.
            /// </summary>
            Shared = 1,

            /// <summary>
            /// To acquire an exclusive lock (writer), set operation to LOCK_EX.
            /// </summary>
            Exclusive = 2,

            /// <summary>
            /// To release a lock (shared or exclusive), set operation to LOCK_UN.
            /// </summary>
            Unlock = 3,

            /// <summary>
            /// If you don't want flock() to block while locking, add LOCK_NB to operation.
            /// </summary> 
            NoBlocking = 4
        }

        public const int LOCK_SH = (int)StreamLockOptions.Shared;
        public const int LOCK_EX = (int)StreamLockOptions.Exclusive;
        public const int LOCK_UN = (int)StreamLockOptions.Unlock;
        public const int LOCK_NB = (int)StreamLockOptions.NoBlocking;

        /// <summary>
        /// ImplementsConstant enumeration for various PHP stream-related constants.
        /// </summary>
        [Flags, PhpHidden]
        public enum PhpStreamConstants
        {
            /// <summary>Empty option (default)</summary>
            Empty = 0,
            /// <summary>If path is relative, Wrapper will search for the resource using the include_path (1).</summary>
            UseIncludePath = StreamOptions.UseIncludePath,
            /// <summary>When this flag is set, only the file:// wrapper is considered. (2)</summary>
            IgnoreUrl = StreamOptions.IgnoreUrl,
            /// <summary>Apply the <c>safe_mode</c> permissions check when opening a file (4).</summary>
            EnforceSafeMode = StreamOptions.EnforceSafeMode,
            /// <summary>If this flag is set, the Wrapper is responsible for raising errors using 
            /// trigger_error() during opening of the stream. If this flag is not set, she should not raise any errors (8).</summary>
            ReportErrors = StreamOptions.ReportErrors,
            /// <summary>If you don't need to write to the stream, but really need to 
            /// be able to seek, use this flag in your options (16).</summary>
            MustSeek = StreamOptions.MustSeek,

            /// <summary>Stat the symbolic link itself instead of the linked file (1).</summary>
            StatLink = StreamStatOptions.Link,
            /// <summary>Do not complain if the file does not exist (2).</summary>
            StatQuiet = StreamStatOptions.Quiet,

            /// <summary>Create the whole path leading to the specified directory if necessary (1).</summary>
            MakeDirectoryRecursive = StreamMakeDirectoryOptions.Recursive
        }

        public const int STREAM_USE_PATH = (int)PhpStreamConstants.UseIncludePath;
        public const int STREAM_IGNORE_URL = (int)PhpStreamConstants.IgnoreUrl;
        public const int STREAM_ENFORCE_SAFE_MODE = (int)PhpStreamConstants.EnforceSafeMode;
        public const int STREAM_REPORT_ERRORS = (int)PhpStreamConstants.ReportErrors;
        public const int STREAM_MUST_SEEK = (int)PhpStreamConstants.MustSeek;
        public const int STREAM_URL_STAT_LINK = (int)PhpStreamConstants.StatLink;
        public const int STREAM_URL_STAT_QUIET = (int)PhpStreamConstants.StatQuiet;
        public const int STREAM_MKDIR_RECURSIVE = (int)PhpStreamConstants.MakeDirectoryRecursive;

        [PhpHidden]
        public enum StreamEncryption
        {
            ClientSSL2,
            ClientSSL3,
            ClientSSL23,
            ClientTSL,
            ServerSSL2,
            ServerSSL3,
            ServerSSL23,
            ServerTSL
        }

        public const int STREAM_CRYPTO_METHOD_SSLv2_CLIENT = (int)StreamEncryption.ClientSSL2;
        public const int STREAM_CRYPTO_METHOD_SSLv3_CLIENT = (int)StreamEncryption.ClientSSL3;
        public const int STREAM_CRYPTO_METHOD_SSLv23_CLIENT = (int)StreamEncryption.ClientSSL23;
        public const int STREAM_CRYPTO_METHOD_TLS_CLIENT = (int)StreamEncryption.ClientTSL;
        public const int STREAM_CRYPTO_METHOD_SSLv2_SERVER = (int)StreamEncryption.ServerSSL2;
        public const int STREAM_CRYPTO_METHOD_SSLv3_SERVER = (int)StreamEncryption.ServerSSL3;
        public const int STREAM_CRYPTO_METHOD_SSLv23_SERVER = (int)StreamEncryption.ServerSSL23;
        public const int STREAM_CRYPTO_METHOD_TLS_SERVER = (int)StreamEncryption.ServerTSL;

        #endregion

        #region stream_copy_to_stream

        /// <summary>
        /// Copies data from one stream to another.
        /// </summary>
        /// <param name="source">Stream to copy data from. Opened for reading.</param>
        /// <param name="destination">Stream to copy data to. Opened for writing.</param>
        /// <param name="maxlength">The maximum count of bytes to copy (<c>-1</c> to copy entire <paramref name="source"/> stream.</param>
        /// <param name="offset">The offset where to start to copy data.</param>
        [return: CastToFalse]
        public static int stream_copy_to_stream(PhpResource source, PhpResource destination, int maxlength = -1, int offset = 0)
        {
            var from = PhpStream.GetValid(source);
            var to = PhpStream.GetValid(destination);
            if (from == null || to == null) return -1;
            if (offset < 0) return -1;
            if (maxlength == 0) return 0;

            // Compatibility (PHP streams.c: "in the event that the source file is 0 bytes, 
            // return 1 to indicate success because opening the file to write had already 
            // created a copy"
            if (from.Eof) return 1;

            // If we have positive offset, we will skip the data
            if (offset > 0)
            {
                int haveskipped = 0;

                while (haveskipped != offset)
                {
                    TextElement data;

                    int toskip = offset - haveskipped;
                    if (toskip > from.GetNextDataLength())
                    {
                        data = from.ReadMaximumData();
                        if (data.IsNull) break;
                    }
                    else
                    {
                        data = from.ReadData(toskip, false);
                        if (data.IsNull) break; // EOF or error.
                        Debug.Assert(data.Length <= toskip);
                    }

                    Debug.Assert(haveskipped <= offset);
                }
            }

            // Copy entire stream.
            int haveread = 0, havewritten = 0;
            while (haveread != maxlength)
            {
                TextElement data;

                // Is is safe to read a whole block?
                int toread = maxlength - haveread;
                if ((maxlength == -1) || (toread > from.GetNextDataLength()))
                {
                    data = from.ReadMaximumData();
                    if (data.IsNull) break; // EOF or error.
                }
                else
                {
                    data = from.ReadData(toread, false);
                    if (data.IsNull) break; // EOF or error.
                    Debug.Assert(data.Length <= toread);
                }

                Debug.Assert(!data.IsNull);
                haveread += data.Length;
                Debug.Assert((maxlength == -1) || (haveread <= maxlength));

                int written = to.WriteData(data);
                if (written <= 0)
                {
                    // Warning already thrown at PhpStream.WriteData.
                    return (havewritten > 0) ? haveread : -1;
                }
                havewritten += written;
            }

            return haveread;
        }

        #endregion

        #region stream_get_line, stream_get_meta_data

        /// <summary>Gets line from stream resource up to a given delimiter</summary> 
        /// <param name="handle">A handle to a stream opened for reading.</param>
        /// <param name="ending">A string containing the end-of-line delimiter.</param>
        /// <param name="length">Maximum length of the return value.</param>
        /// <returns>One line from the stream <b>without</b> the <paramref name="ending"/> string at the end.</returns>
        [return: CastToFalse]
        public static string stream_get_line(PhpResource handle, int length, string ending)
        {
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return null;
            }

            if (length <= 0)
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg_negative", "length"));
                //return null;
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (string.IsNullOrEmpty(ending))
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg_empty", "ending"));
                //return null;
                throw new ArgumentException(nameof(ending));
            }

            // The ending is not included in the returned data.
            string rv = stream.ReadLine(length, ending);
            if (rv == null) return null;
            if (rv.Length >= ending.Length)
            {
                rv = rv.Substring(rv.Length - ending.Length);
            }

            return rv;
        }

        /// <summary>
        /// Retrieves header/meta data from streams/file pointers
        /// </summary>
        /// <remarks>
        /// The result array contains the following items:
        /// * timed_out (bool) - TRUE if the stream timed out while waiting for data on the last call to fread() or fgets().
        /// * blocked (bool) - TRUE if the stream is in blocking IO mode. See stream_set_blocking().
        /// * eof (bool) - TRUE if the stream has reached end-of-file. Note that for socket streams this member can be TRUE even when unread_bytes is non-zero. To determine if there is more data to be read, use feof() instead of reading this item.
        /// * unread_bytes (int) - the number of bytes currently contained in the PHP's own internal buffer.
        /// * stream_type (string) - a label describing the underlying implementation of the stream.
        /// * wrapper_type (string) - a label describing the protocol wrapper implementation layered over the stream. See List of Supported Protocols/Wrappers for more information about wrappers.
        /// * wrapper_data (mixed) - wrapper specific data attached to this stream. See List of Supported Protocols/Wrappers for more information about wrappers and their wrapper data.
        /// * filters (array) - and array containing the names of any filters that have been stacked onto this stream. Documentation on filters can be found in the Filters appendix.
        /// * mode (string) - the type of access required for this stream (see Table 1 of the fopen() reference)
        /// * seekable (bool) - whether the current stream can be seeked.
        /// * uri (string) - the URI/filename associated with this stream.
        /// </remarks>
        public static PhpArray stream_get_meta_data(PhpResource resource)
        {
            PhpStream stream = PhpStream.GetValid(resource);
            if (stream == null)
            {
                return null;
            }

            var result = new PhpArray(10);

            // TODO: timed_out (bool) - TRUE if the stream timed out while waiting for data on the last call to fread() or fgets().
            // TODO: blocked (bool) - TRUE if the stream is in blocking IO mode. See stream_set_blocking().
            result["blocked"] = PhpValue.True;
            // eof (bool) - TRUE if the stream has reached end-of-file. Note that for socket streams this member can be TRUE even when unread_bytes is non-zero. To determine if there is more data to be read, use feof() instead of reading this item.
            result["eof"] = (PhpValue)stream.Eof;
            // TODO: unread_bytes (int) - the number of bytes currently contained in the PHP's own internal buffer.
            result["unread_bytes"] = (PhpValue)0;
            // TODO: stream_type (string) - a label describing the underlying implementation of the stream.
            result["stream_type"] = (PhpValue)((stream.Wrapper != null) ? stream.Wrapper.Label : string.Empty);
            // wrapper_type (string) - a label describing the protocol wrapper implementation layered over the stream. See List of Supported Protocols/Wrappers for more information about wrappers.
            result["wrapper_type"] = (PhpValue)((stream.Wrapper != null) ? stream.Wrapper.Scheme : string.Empty);
            // wrapper_data (mixed) - wrapper specific data attached to this stream. See List of Supported Protocols/Wrappers for more information about wrappers and their wrapper data.
            if (stream.WrapperSpecificData != null)
            {
                result["wrapper_data"] = PhpValue.FromClr(stream.WrapperSpecificData);
            }
            // filters (array) - and array containing the names of any filters that have been stacked onto this stream. Documentation on filters can be found in the Filters appendix.
            result["filters"] = (PhpValue)GetFiltersName(stream);
            // mode (string) - the type of access required for this stream (see Table 1 of the fopen() reference)
            result["mode"] = (PhpValue)(stream.CanRead ? (stream.CanWrite ? "r+" : "r") : (stream.CanWrite ? "w" : string.Empty));
            // seekable (bool) - whether the current stream can be seeked.
            result["seekable"] = (PhpValue)stream.CanSeek;
            // uri (string) - the URI/filename associated with this stream.
            result["uri"] = (PhpValue)stream.OpenedPath;

            //
            return result;
        }

        /// <summary>
        /// filters (array)
        /// - array containing the names of any filters that have been stacked onto this stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static PhpArray GetFiltersName(PhpStream/*!*/stream)
        {
            var array = new PhpArray();

            foreach (PhpFilter f in stream.StreamFilters)
            {
                array.Add(f.FilterName);
            }

            return array;
        }

        #endregion

        #region stream_get_contents

        /// <summary>
        /// Reads entire content of the stream.
        /// </summary>
        [return: CastToFalse]
        public static PhpString stream_get_contents(PhpResource handle, int maxLength = -1, int offset = -1)
        {
            var stream = PhpStream.GetValid(handle, FileAccess.Read);
            if (stream == null)
            {
                return null;
            }

            return stream.ReadContents(maxLength, offset).ToPhpString();
        }

        #endregion

        #region stream_set_blocking, stream_set_timeout, set_file_buffer, stream_set_write_buffer

        /// <summary>Set blocking/non-blocking (synchronous/asynchronous I/O operations) mode on a stream.</summary>
        /// <param name="resource">A handle to a stream resource.</param>
        /// <param name="mode"><c>1</c> for blocking, <c>0</c> for non-blocking.</param>
        /// <returns><c>true</c> if the operation is supported and was successful, <c>false</c> otherwise.</returns>
        public static bool stream_set_blocking(PhpResource resource, int mode)
        {
            var stream = PhpStream.GetValid(resource);
            return stream != null && stream.SetParameter(StreamParameterOptions.BlockingMode, (PhpValue)(mode > 0));
        }

        /// <summary>Set timeout period on a stream</summary>
        /// <param name="resource">A handle to a stream opened for reading.</param>
        /// <param name="seconds">The number of seconds.</param>
        /// <param name="microseconds">The number of microseconds.</param>
        /// <returns><c>true</c> if the operation is supported and was successful, <c>false</c> otherwise.</returns>
        public static bool stream_set_timeout(PhpResource resource, int seconds, int microseconds = 0)
        {
            var stream = PhpStream.GetValid(resource);
            if (stream == null) return false;

            double timeout = seconds + (microseconds / 1000000.0);
            if (timeout < 0.0) timeout = 0.0;
            return stream.SetParameter(StreamParameterOptions.ReadTimeout, (PhpValue)timeout);
        }

        /// <summary>Sets file buffering on the given stream.</summary>   
        /// <param name="resource">The stream to set write buffer size to.</param>
        /// <param name="buffer">Number of bytes the output buffer holds before 
        /// passing to the underlying stream.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool set_file_buffer(PhpResource resource, int buffer)
        {
            return stream_set_write_buffer(resource, buffer);
        }

        /// <summary>Sets file buffering on the given stream.</summary>   
        /// <param name="resource">The stream to set write buffer size to.</param>
        /// <param name="buffer">Number of bytes the output buffer holds before 
        /// passing to the underlying stream.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool stream_set_write_buffer(PhpResource resource, int buffer)
        {
            var stream = PhpStream.GetValid(resource);
            if (stream == null) return false;

            if (buffer < 0) buffer = 0;
            return stream.SetParameter(StreamParameterOptions.WriteBufferSize, (PhpValue)buffer);
        }

        #endregion

        #region stream_select

        /// <summary>Runs the equivalent of the select() system call on the given arrays of streams with a timeout specified by tv_sec and tv_usec </summary>   
		public static int stream_select(ref PhpArray read, ref PhpArray write, ref PhpArray except, int tv_sec, int tv_usec = 0)
        {
            //if ((read == null || read.Count == 0) && (write == null || write.Count == 0))
            //    return except == null ? 0 : except.Count;

            //var readResult = new PhpArray();
            //var writeResult = new PhpArray();
            //var i = 0;
            //var timer = Stopwatch.StartNew();
            //var waitTime = tv_sec * 1000 + tv_usec;
            //while (true)
            //{
            //    if (read != null)
            //    {
            //        readResult.Clear();
            //        foreach (var item in read)
            //        {
            //            var stream = item.Value as PhpStream;
            //            if (stream == null)
            //                continue;
            //            if (stream.CanReadWithoutLock())
            //                readResult.Add(item.Key, item.Value);
            //        }
            //    }
            //    if (write != null)
            //    {
            //        writeResult.Clear();
            //        foreach (var item in write)
            //        {
            //            var stream = item.Value as PhpStream;
            //            if (stream == null)
            //                continue;
            //            if (stream.CanWriteWithoutLock())
            //                writeResult.Add(item.Key, item.Value);
            //        }
            //    }
            //    if (readResult.Count > 0 || writeResult.Count > 0 || except.Count > 0)
            //        break;
            //    i++;
            //    if (timer.ElapsedMilliseconds > waitTime)
            //        break;
            //    if (i < 10)
            //        Thread.Yield();
            //    else
            //        Thread.Sleep(Math.Min(i, waitTime));
            //}
            //read = readResult;
            //write = writeResult;
            //return read.Count + write.Count + (except == null ? 0 : except.Count);
            throw new NotImplementedException();
        }

        #endregion
    }
}
