using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/* TODO:
  - Added offset parameter to the stream_copy_to_stream() function. (PHP 5.1.0)
  - Changed stream_filter_(ap|pre)pend() to return resource. (Sara) 
  - Fixed a bug where stream_get_meta_data() did not return the "uri" element for files opened with tmpname(). (Derick) 
  - Fixed crash inside stream_get_line() when length parameter equals 0. (Ilia) 
  - Added (PHP 5.1.0):
      stream_context_get_default() (Wez) 
      stream_wrapper_unregister() (Sara) 
      stream_wrapper_restore() (Sara) 
      stream_filter_remove() (Sara) 
  - Added proxy support to ftp wrapper via http. (Sara) 
  - Added MDTM support to ftp_url_stat. (Sara) 
  - Added zlib stream filter support. (Sara) 
  - Added bz2 stream filter support. (Sara) 
  - Added bindto socket context option. (PHP 5.1.0)
  - Added HTTP/1.1 and chunked encoding support to http:// wrapper. (PHP 5.1.0)
*/

namespace Pchp.Library.Streams
{
    #region Stream Context functions

    /// <summary>
    /// Class containing implementations of PHP functions accessing the <see cref="StreamContext"/>s.
    /// </summary>
    [PhpExtension("standard")]
    public static class PhpContexts
    {
        #region stream_context_create

        /// <summary>Create a new stream context.</summary>
        /// <param name="data">The 2-dimensional array in format "options[wrapper][option]".</param>
        public static PhpResource stream_context_create(PhpArray data = null)
        {
            if (data == null)
            {
                return StreamContext.Default;
            }

            // OK, data lead to a valid stream-context.
            if (CheckContextData(data))
            {
                return new StreamContext(data);
            }

            // Otherwise..
            PhpException.Throw(PhpError.Warning, Resources.LibResources.invalid_context_resource);
            return null;
        }

        /// <summary>
        /// Check whether the provided argument is a valid stream-context data array.
        /// </summary>
        /// <param name="data">The data to be stored into context.</param>
        /// <returns></returns>
        static bool CheckContextData(PhpArray data)
        {
            // Check if the supplied data are correctly formed.
            var enumerator = data.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.CurrentValue.AsArray() == null)
                {
                    return false;
                }

                // Invalid resource - not an array of arrays
            }
            return true;
        }

        /// <summary>
        /// Get the StreamContext from a handle representing either an isolated context or a PhpStream.
        /// </summary>
        /// <param name="stream_or_context">The PhpResource of either PhpStream or StreamContext type.</param>
        /// <param name="createContext">If true then a new context will be created at the place of <see cref="StreamContext.Default"/>.</param>
        /// <returns>The respective StreamContext.</returns>
        /// <exception cref="PhpException">If the first argument is neither a stream nor a context.</exception>
        private static StreamContext FromResource(PhpResource stream_or_context, bool createContext)
        {
            if ((stream_or_context != null) && (stream_or_context.IsValid))
            {
                // Get the context out of the stream
                PhpStream stream = stream_or_context as PhpStream;
                if (stream != null)
                {
                    Debug.Assert(stream.Context != null);
                    stream_or_context = stream.Context;
                }

                StreamContext context = stream_or_context as StreamContext;
                if (context == StreamContext.Default)
                {
                    if (!createContext) return null;
                    context = new StreamContext();
                }
                return context;
            }
            PhpException.Throw(PhpError.Warning, Core.Resources.ErrResources.context_expected);
            return null;
        }

        private static PhpArray GetContextData(PhpResource stream_or_context)
        {
            // Always create a new context if there is the Default one.
            StreamContext context = FromResource(stream_or_context, true);

            // Now create the data if this is a "lazy context".
            if (context != null)
            {
                if (context.Data == null)
                {
                    context.Data = new PhpArray(4);
                }

                return context.Data;
                // Now it is OK.
            }
            return null;
        }

        #endregion

        #region stream_context_get_options, stream_context_set_option, stream_context_set_params

        /// <summary>
        /// Retrieve options for a stream-wrapper or a context itself.
        /// </summary>  
        /// <param name="stream_or_context">The PhpResource of either PhpStream or StreamContext type.</param>
        /// <returns>The contained PhpArray of options.</returns>
        public static PhpArray stream_context_get_options(PhpResource stream_or_context)
        {
            // Do not create a new context if there is the Default one.
            var context = FromResource(stream_or_context, false);
            return context != null ? context.Data : null;
        }

        /// <summary>
        /// Sets an option for a stream/wrapper/context.
        /// </summary> 
        /// <param name="stream_or_context">The PhpResource of either PhpStream or StreamContext type.</param>
        /// <param name="wrapper">The first-level index to the options array.</param>
        /// <param name="option">The second-level index to the options array.</param>
        /// <param name="data">The data to be stored to the options array.</param>
        /// <returns>True on success.</returns>
        public static bool stream_context_set_option(PhpResource stream_or_context, string wrapper, string option, PhpValue data)
        {
            // OK, creates the context if Default, so that Data is always a PhpArray.
            // Fails only if the first argument is not a stream nor context.
            var context_data = GetContextData(stream_or_context);
            if (context_data != null)
            {
                GetWrapperData(context_data, wrapper)[option] = data;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Sets an option for a stream/wrapper/context.
        /// </summary> 
        /// <param name="stream_or_context">The PhpResource of either PhpStream or StreamContext type.</param>
        /// <param name="options">The options to set for <paramref name="stream_or_context"/>.</param>
        /// <returns>True on success.</returns>
        public static bool stream_context_set_option(PhpResource stream_or_context, PhpArray options)
        {
            // OK, creates the context if Default, so that Data is always a PhpArray.
            // Fails only if the first argument is not a stream nor context.
            var context_data = GetContextData(stream_or_context);
            if (context_data != null)
            {
                var e1 = options.GetFastEnumerator();
                while (e1.MoveNext())
                {
                    var wrapper_options = e1.CurrentValue.AsArray();
                    if (wrapper_options != null)
                    {
                        var wrapper_data = GetWrapperData(context_data, e1.CurrentKey.ToString());
                        var e2 = wrapper_options.GetFastEnumerator();
                        while (e2.MoveNext())
                        {
                            wrapper_data[e2.CurrentKey.ToString()] = e2.CurrentValue;
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        static PhpArray GetWrapperData(PhpArray context_data, string wrapper)
        {
            Debug.Assert(context_data != null);

            PhpValue value;
            if (!context_data.TryGetValue(wrapper, out value))
            {
                context_data[wrapper] = value = (PhpValue)new PhpArray();
            }

            return value.ArrayOrNull();
        }

        /// <summary>
        /// Set parameters for a stream/wrapper/context.
        /// </summary>
        public static bool stream_context_set_params(PhpResource stream_or_context, PhpArray parameters)
        {
            // Create the context if the stream does not have one.
            var context = FromResource(stream_or_context, true);
            if (context != null && context.IsValid)
            {
                context.Parameters = parameters;
                return true;
            }

            return false;
        }

        #endregion
    }

    #endregion

    /// <summary>
	/// Gives access to the stream filter chains.
	/// </summary>
    [PhpExtension("standard")]
    public static class PhpFilters
    {
        #region Enums & Constants

        /// <summary>
        /// The status indicators returned by filter's main method.
        /// </summary>
        public enum FilterStatus
        {
            /// <summary>
            /// Error in data stream (1).
            /// </summary>
            FatalError = 0,

            /// <summary>
            /// Filter needs more data; stop processing chain until more is available (2).
            /// </summary>
            MoreData = 1,

            /// <summary>
            /// Filter generated output buckets; pass them on to next in chain (3).
            /// </summary>
            OK = 2,
        }

        public const int PSFS_ERR_FATAL = (int)FilterStatus.FatalError;
        public const int PSFS_FEED_ME = (int)FilterStatus.MoreData;
        public const int PSFS_PASS_ON = (int)FilterStatus.OK;

        /// <summary>
        /// Regular read/write.
        /// </summary>
        public const int PSFS_FLAG_NORMAL = 0;

        /// <summary>
        /// An incremental flush.
        /// </summary>
        public const int PSFS_FLAG_FLUSH_INC = 1;

        /// <summary>
        /// Final flush prior to closing.
        /// </summary>
        public const int PSFS_FLAG_FLUSH_CLOSE = 2;

        /// <summary>
        /// Indicates whether the filter is to be attached to the
        /// input/ouput filter-chain or both.
        /// </summary>
        [Flags]
        public enum FilterChains
        {
            /// <summary>
            /// Insert the filter to the read filter chain of the stream (1).
            /// </summary>
            Read = FilterChainOptions.Read,

            /// <summary>
            /// Insert the filter to the write filter chain of the stream (2).
            /// </summary>
            Write = FilterChainOptions.Write,

            /// <summary>
            /// Insert the filter to both the filter chains of the stream (3).
            /// </summary>
            ReadWrite = Read | Write
        }

        public const int STREAM_FILTER_READ = (int)FilterChains.Read;
        public const int STREAM_FILTER_WRITE = (int)FilterChains.Write;
        public const int STREAM_FILTER_ALL = (int)FilterChains.ReadWrite;

        #endregion

        #region stream_filter_append, stream_filter_prepend, stream_filter_remove

        sealed class StreamFilterResource : PhpResource
        {
            internal PhpStream Stream { get; private set; }
            internal PhpFilter WriteFilter { get; private set; }
            internal PhpFilter ReadFilter { get; private set; }

            public StreamFilterResource(PhpStream stream, PhpFilter writeFilter, PhpFilter readFilter) : base("stream filter")
            {
                this.Stream = stream;
                this.WriteFilter = writeFilter;
                this.ReadFilter = readFilter;
            }

            protected override void FreeManaged()
            {
                Stream = null;
                WriteFilter = null;
                ReadFilter = null;

                base.FreeManaged();
            }
        }

        /// <summary>Adds filtername to the list of filters attached to stream.</summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="stream">The target stream.</param>
        /// <param name="filter">The filter name.</param>
        /// <param name="read_write">Combination of the <see cref="FilterChainOptions"/> flags.</param>
        /// <param name="parameters">Additional parameters for a user filter.</param>
        [return: CastToFalse]
        public static PhpResource stream_filter_append(Context ctx, PhpResource stream, string filter, FilterChainOptions read_write = FilterChainOptions.ReadWrite, PhpValue parameters = default)
        {
            PhpStream s = PhpStream.GetValid(stream);
            if (s == null) return null; // false;

            var where = read_write & FilterChainOptions.ReadWrite;
            var added = PhpFilter.AddToStream(ctx, s, filter, where | FilterChainOptions.Tail, parameters);

            //
            if (added.readFilter != null || added.writeFilter != null)
            {
                return new StreamFilterResource(s, added.writeFilter, added.readFilter);
            }
            else
            {
                return null; // false
            }
        }

        /// <summary>Adds <paramref name="filter"/> to the list of filters attached to <paramref name="stream"/>.</summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="stream">The target stream.</param>
        /// <param name="filter">The filter name.</param>
        /// <param name="read_write">Combination of the <see cref="FilterChainOptions"/> flags.</param>
        /// <param name="parameters">Additional parameters for a user filter.</param>
        [return: CastToFalse]
        public static PhpResource stream_filter_prepend(Context ctx, PhpResource stream, string filter, FilterChainOptions read_write = FilterChainOptions.ReadWrite, PhpValue parameters = default)
        {
            var s = PhpStream.GetValid(stream);
            if (s == null) return null; // false;

            var where = read_write & FilterChainOptions.ReadWrite;
            var added = PhpFilter.AddToStream(ctx, s, filter, where | FilterChainOptions.Head, parameters);

            //
            if (added.readFilter != null || added.writeFilter != null)
            {
                return new StreamFilterResource(s, added.writeFilter, added.readFilter);
            }
            else
            {
                return null; // false
            }
        }

        /// <summary>
        /// Removes a stream filter previously added to a stream with stream_filter_prepend() or stream_filter_append().
        /// Any data remaining in the filter's internal buffer will be flushed through to the next filter before removing it.
        /// </summary>
        public static bool stream_filter_remove(PhpResource stream_filter)
        {
            bool removed = false;

            if (stream_filter is StreamFilterResource s && s.IsValid && s.Stream != null && s.Stream.IsValid)
            {
                s.Stream.Flush();

                if (s.WriteFilter != null)
                {
                    removed |= s.Stream.RemoveFilter(s.WriteFilter, FilterChainOptions.Write);
                }

                if (s.ReadFilter != null)
                {
                    removed |= s.Stream.RemoveFilter(s.ReadFilter, FilterChainOptions.Read);
                }

                s.Dispose();
            }
            else
            {
                // Invalid resource given, not a stream filter
                PhpException.Throw(PhpError.Warning, Core.Resources.ErrResources.invalid_stream_resource);
            }

            //
            return removed;
        }

        #endregion

        #region stream_filter_register, stream_get_filters

        /// <summary>
        /// Registers a user stream filter.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="filter">The name of the filter (may contain wildcards).</param>
        /// <param name="classname">The PHP user class (derived from <c>php_user_filter</c>) implementing the filter.</param>
        /// <returns><c>true</c> if the filter was succesfully added, <c>false</c> if the filter of such name already exists.</returns>
        public static bool stream_filter_register(Context ctx, string filter, string classname)
        {
            return UserFilterFactory.TryRegisterFilter(ctx, filter, classname);
        }

        /// <summary>
        /// Retrieves the list of registered filters.
        /// </summary>
        /// <returns>A <see cref="PhpArray"/> containing the names of available filters. Cannot be <c>null</c>.</returns>
        public static PhpArray stream_get_filters(Context ctx)
        {
            return new PhpArray(PhpFilter.GetFilterNames(ctx));
        }

        #endregion

        #region stream_bucket_***

        /// <summary>
        /// Append bucket to brigade.
        /// </summary>
        public static void stream_bucket_append(UserFilterBucketBrigade brigade, UserFilterBucket bucket)
        {
            if (!bucket.data.IsEmpty)
            {
                brigade.bucket.EnsureWritable().Append(bucket.data);
            }
        }

        /// <summary>
        /// Return a bucket object from the brigade for operating on.
        /// </summary>
        /// <returns>Object or <c>NULL</c>.</returns>
        public static UserFilterBucket stream_bucket_make_writeable(UserFilterBucketBrigade brigade)
        {
            if (brigade == null || brigade.consumed >= brigade.bucket.Length)
            {
                return null;
            }

            brigade.consumed = brigade.bucket.Length;

            return new UserFilterBucket
            {
                data = brigade.bucket,
                datalen = brigade.bucket.Length,
            };
        }

        /// <summary>
        /// Create a new bucket for use on the current stream.
        /// </summary>
        public static UserFilterBucket stream_bucket_new(PhpResource stream, PhpString buffer)
        {
            return new UserFilterBucket
            {
                data = buffer,
                datalen = buffer.Length,
            };
        }

        /// <summary>
        /// Prepend bucket to brigade.
        /// </summary>
        public static void stream_bucket_prepend(UserFilterBucketBrigade brigade, UserFilterBucket bucket)
        {
            if (!bucket.data.IsEmpty)
            {
                var blob = bucket.data.DeepCopy().EnsureWritable();
                blob.Append(brigade.bucket);

                brigade.bucket = new PhpString(blob);
            }
        }

        #endregion
    }

    /// <summary>
	/// Class containing implementations of PHP functions accessing the <see cref="StreamWrapper"/>s.
	/// </summary>
	/// <threadsafety static="true"/>
    [PhpExtension("Core")]
    public static class PhpWrappers
    {
        #region stream_wrapper_register, stream_register_wrapper, stream_get_wrappers

        /// <summary>
        /// Optional flag for <c>stream_wrapper_register</c> function.
        /// </summary>
        public enum StreamWrapperRegisterFlags : int
        {
            Default = 0,
            IsUrl = 1
        }

        public const int STREAM_IS_URL = (int)StreamWrapperRegisterFlags.IsUrl;

        [PhpConditional("CLI")]
        public static PhpStream STDIN => InputOutputStreamWrapper.In;

        [PhpConditional("CLI")]
        public static PhpStream STDOUT => InputOutputStreamWrapper.Out;

        [PhpConditional("CLI")]
        public static PhpStream STDERR => InputOutputStreamWrapper.Error;

        /// <summary>
        /// Registers a user-wrapper specified by the name of a defining user-class.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="protocol">The schema to be associated with the given wrapper.</param>
        /// <param name="classname">Name of the user class implementing the wrapper functions.</param>
        /// <param name="flags">Should be set to STREAM_IS_URL if protocol is a URL protocol. Default is 0, local stream.</param>
        /// <returns>False in case of failure (ex. schema already occupied).</returns>
        public static bool stream_wrapper_register(Context ctx, string protocol, string classname, StreamWrapperRegisterFlags flags = StreamWrapperRegisterFlags.Default)
        {
            // check if the scheme is already registered:
            if (string.IsNullOrEmpty(protocol) || StreamWrapper.GetWrapperInternal(ctx, protocol) == null)
            {
                // TODO: Warning?
                return false;
            }

            var wrapperClass = ctx.GetDeclaredTypeOrThrow(classname, true);
            if (wrapperClass == null)
            {
                return false;
            }

            // EX: [stream_wrapper_register]: create the user wrapper
            var wrapper = new UserStreamWrapper(ctx, protocol, wrapperClass, flags == StreamWrapperRegisterFlags.IsUrl);
            return StreamWrapper.RegisterUserWrapper(ctx, protocol, wrapper);
        }

        /// <summary>
        /// Registers a user-wrapper specified by the name of a defining user-class.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="protocol">The schema to be associated with the given wrapper.</param>
        /// <param name="userWrapperName">Name of the user class implementing the wrapper functions.</param>
        /// <param name="flags">Should be set to STREAM_IS_URL if protocol is a URL protocol. Default is 0, local stream.</param>
        /// <returns>False in case of failure (ex. schema already occupied).</returns>
        public static bool stream_register_wrapper(Context ctx, string protocol, string userWrapperName, StreamWrapperRegisterFlags flags = StreamWrapperRegisterFlags.Default)
        {
            return stream_wrapper_register(ctx, protocol, userWrapperName, flags);
        }

        ///<summary>Retrieve list of registered streams (only the names)</summary>  
        public static PhpArray stream_get_wrappers(Context ctx)
        {
            var ret = new PhpArray(8);

            // First add the internal built-in wrappers.
            var internals = StreamWrapper.GetSystemWrapperSchemes();
            foreach (var scheme in internals)
            {
                ret.Add(scheme);
            }

            // Now add the indexes (schemes) of User wrappers.
            foreach (var scheme in StreamWrapper.GetUserWrapperSchemes(ctx))
            {
                ret.Add(scheme);
            }

            //
            return ret;
        }

        #endregion
    }

    [PhpExtension("standard")]
    public static class PhpStreams
    {
        #region Enums & Constants

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
                rv = rv.Remove(rv.Length - ending.Length);
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

            foreach (var f in stream.StreamFilters)
            {
                array.Add(f.filtername);
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
                return default(PhpString);
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

        /// <summary>Runs the equivalent of the select() system call on the given arrays of streams with a timeout specified by <paramref name="tv_sec"/> and <paramref name="tv_usec"/>.
        /// </summary>   
		public static int stream_select(ref PhpArray read, ref PhpArray write, ref PhpArray except, int tv_sec, int tv_usec = 0)
        {
            if ((read == null || read.Count == 0) &&
                (write == null || write.Count == 0))
            {
                // nothing to select:
                return except == null ? 0 : except.Count;
            }

            //
            var startTime = System.DateTime.UtcNow;
            int wait_ms = (tv_sec >= 0 || tv_usec > 0) ? (tv_sec * 1_000 + tv_usec / 1_000) : -1;

            //
            var readcheck = read != null ? new PhpArray() : null;
            var writecheck = write != null ? new PhpArray() : null;
            var errcheck = except != null ? new PhpArray() : null;

            for (int i = 0; ; i++)
            {
                int count = 0;

                //
                if (read != null && read.Count != 0)
                {
                    foreach (var item in read.Values)
                    {
                        if (item.AsObject() is PhpStream stream && stream.CanReadWithoutLock)
                        {
                            readcheck.Add(item);
                        }
                    }

                    count += readcheck.Count;
                }

                if (write != null && write.Count != 0)
                {
                    foreach (var item in write.Values)
                    {
                        if (item.AsObject() is PhpStream stream && stream.CanWriteWithoutLock)
                        {
                            writecheck.Add(item);
                        }
                    }

                    count += writecheck.Count;
                }

                if (except != null && except.Count != 0)
                {
                    // watch for high priority exceptional ("out-of-band") data arriving
                    // NOTICE: only for SocketStream

                    var list = new List<Socket>(except.Count);

                    foreach (var item in except.Values)
                    {
                        if (item.AsObject() is SocketStream ss && ss.IsValid)
                        {
                            list.Add(ss.Socket);
                        }
                    }

                    try
                    {
                        Socket.Select(null, null, list, wait_ms * 1_000);
                    }
                    catch (SocketException ex)
                    {
                        PhpException.Throw(PhpError.Warning, ex.Message);
                    }

                    if (list.Count != 0)
                    {
                        foreach (var item in except.Values)
                        {
                            if (item.AsObject() is SocketStream ss && list.Contains(ss.Socket))
                            {
                                errcheck.Add(PhpValue.FromClass(ss));
                            }
                        }

                        count += errcheck.Count;
                    }
                }

                // check stream available or timeout
                if (count != 0 || (wait_ms > 0 && (System.DateTime.UtcNow - startTime).TotalMilliseconds >= wait_ms))
                {
                    // update ref parameters and return:
                    read = readcheck;
                    write = writecheck;
                    except = errcheck;

                    //
                    return count;
                }

                // avoids polling CPU without a break:
                if (i < 8)
                    // just spin
                    Thread.Yield();
                else
                    // sleep the thread for [2..100] ms
                    Thread.Sleep(Math.Min(Math.Min((i + 1) * 2, 100), wait_ms));
            }
        }

        #endregion

        #region stream_resolve_include_path

        /// <summary>Resolve filename against the include path</summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="filename">The filename to resolve.</param>
        /// <returns>Returns a string containing the resolved absolute filename, or FALSE on failure.</returns>
        [return: CastToFalse]
        public static string stream_resolve_include_path(Context ctx, string filename)
        {
            if (PhpStream.ResolvePath(ctx, ref filename, out var wrapper, CheckAccessMode.FileExists, CheckAccessOptions.Quiet | CheckAccessOptions.UseIncludePath))
            {
                return filename;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region stream_is_local

        /// <summary>
        /// Checks if a stream, or a URL, is a local one or not.
        /// </summary>
        /// <param name="stream">The stream resource to check.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool stream_is_local(PhpStream stream) => !stream.Wrapper?.IsUrl ?? false;

        /// <summary>
        /// Checks if a stream, or a URL, is a local one or not.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="url">The URL to check.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool stream_is_local(Context ctx, string url)
        {
            string scheme = PhpStream.GetSchemeInternal(url, out var _);
            var wrapper = StreamWrapper.GetWrapperInternal(ctx, scheme);
            return !wrapper?.IsUrl ?? false;
        }

        #endregion

        #region stream_isatty

        /// <summary>
        /// Determines if <paramref name="stream"/> refers to a valid terminal type device.
        /// </summary>
        /// <param name="stream">Stream resource. If <c>null</c> or not a stream resource, warning is thrown and function returns <c>false</c>.</param>
        public static bool stream_isatty(PhpResource stream)
        {
            var s = PhpStream.GetValid(stream);
            if (s != null)
            {
                if (InputOutputStreamWrapper.IsStdIn(s)) return !Console.IsInputRedirected; // -10
                if (InputOutputStreamWrapper.IsStdOut(s)) return !Console.IsOutputRedirected;// -11
                if (InputOutputStreamWrapper.IsStdErr(s)) return !Console.IsErrorRedirected; // -12
            }

            return false;
        }

        #endregion

        #region sapi_windows_vt100_support

        /// <summary>
        /// Resolves one of if possible
        /// <see cref="WindowsPlatform.STD_ERROR_HANDLE"/>,
        /// <see cref="WindowsPlatform.STD_OUTPUT_HANDLE"/>,
        /// <see cref="WindowsPlatform.STD_INPUT_HANDLE"/>.
        /// </summary>
        static bool TryResolveWindowsIoStdHandle(PhpResource stream, out int handle)
        {
            if (CurrentPlatform.IsWindows)
            {
                var s = PhpStream.GetValid(stream);
                if (s != null)
                {
                    if (InputOutputStreamWrapper.IsStdIn(s))
                    {
                        handle = WindowsPlatform.STD_INPUT_HANDLE;
                        return true;
                    }

                    if (InputOutputStreamWrapper.IsStdOut(s))
                    {
                        handle = WindowsPlatform.STD_OUTPUT_HANDLE;
                        return true;
                    }

                    if (InputOutputStreamWrapper.IsStdErr(s))
                    {
                        handle = WindowsPlatform.STD_ERROR_HANDLE;
                        return true;
                    }
                }
            }

            handle = 0;
            return false;
        }

        public static bool sapi_windows_vt100_support(PhpResource stream)
        {
            return TryResolveWindowsIoStdHandle(stream, out var handle) && WindowsPlatform.Has_VT100(handle);
        }

        public static bool sapi_windows_vt100_support(PhpResource stream, bool enable)
        {
            if (TryResolveWindowsIoStdHandle(stream, out var handle))
            {
                return WindowsPlatform.Enable_VT100(handle, enable);
            }

            return false;
        }

        #endregion
    }
}
