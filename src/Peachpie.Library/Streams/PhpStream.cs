using Pchp.Core;
using Pchp.Core.Resources;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library.Streams
{
    #region Enumerations (StreamParameterOptions, CheckAccessMode, CheckAccessOptions)

    /// <summary>
    /// Parameter identifier for <see cref="PhpStream.SetParameter"/>.
    /// </summary>
    public enum StreamParameterOptions
    {
        /// <summary>Set the synchronous/asynchronous operation mode (<c>value</c> is <see cref="bool"/>.</summary>
        BlockingMode = 1,
        /// <summary>Set the read buffer size (<c>value</c> is <see cref="int"/>).</summary>
        ReadBufferSize = 2,
        /// <summary>Set the write buffer size (<c>value</c> is <see cref="int"/>).</summary>
        WriteBufferSize = 3,
        /// <summary>Set the read timeout in seconds (<c>value</c> is <see cref="double"/>).</summary>
        ReadTimeout = 4,
        /// <summary>Set the read chunk size (<c>value</c> is <see cref="int"/>).</summary>
        SetChunkSize = 5,
        /// <summary>Set file locking (<c>value</c> is <see cref="int"/>).</summary>
        Locking = 6,
        /// <summary>Set memory mapping. Unimplemented.</summary>
        MemoryMap = 9,
        /// <summary>Truncate the stream at the current position.</summary>
        Truncate = 10
    }

    /// <summary>
    /// Mode selector of <see cref="PhpStream.CheckAccess"/>.
    /// </summary>
    public enum CheckAccessMode
    {
        /// <summary>Return invalid <c>false</c> if file does not exist (<c>fopen()</c>).</summary>
        FileExists = 0,
        /// <summary>Return valid <c>true</c> if file does not exist (for example <c>rename()</c>.</summary>
        FileNotExists = 1,
        /// <summary>If file does not exist, check directory (for example <c>stat()</c>).</summary>
        FileOrDirectory = 2,
        /// <summary>Only check directory (needed for <c>mkdir</c>, <c>opendir</c>).</summary>
        Directory = 3,
        /// <summary>Only check file.</summary>
        FileMayExist = 5
    }

    /// <summary>
    /// Additional options for <see cref="PhpStream.CheckAccess"/>.
    /// </summary>
    public enum CheckAccessOptions
    {
        /// <summary>Empty option (default).</summary>
        Empty = 0,
        /// <summary>If <c>true</c> then the include paths are searched for the file too (1).</summary>
        UseIncludePath = StreamOptions.UseIncludePath,
        /// <summary>Suppress display of error messages (2).</summary>
        Quiet = StreamStatOptions.Quiet
    }

    #endregion

    /// <summary>
    /// Abstraction of streaming behavior for PHP.
    /// PhpStreams are opened by StreamWrappers on a call to fopen().
    /// </summary>
    /// <remarks>
    /// <para>
    /// PhpStream is a descendant of PhpResource,
    /// it contains a StreamContext (may be empty) and two ordered lists of StreamFilters
    /// (input and output filters).
    /// PhpStream may be cast to a .NET stream (using its RawStream property).
    /// </para>
    /// <para>
    /// Various stream types are defined by overriding the <c>Raw*</c> methods
    /// that provide direct access to the underlying physical stream.
    /// Corresponding public methods encapsulate these accessors with
    /// buffering and filtering. Raw stream access is performed at the <c>byte[]</c> level.
    /// ClassLibrary functions may use either the <c>Read/WriteBytes</c>
    /// or <c>Read/WriteString</c> depending on the nature of the PHP function.
    /// Data are converted using the <see cref="ApplicationConfiguration.GlobalizationSection.PageEncoding"/>
    /// as necessary.
    /// </para>
    /// <para>
    /// When reading from a stream, the stream data is read in binary format
    /// in chunks of predefined size (8kB). Stream filters (if any) are then applied
    /// in a cascade to the whole block. Filtered blocks are stored in a
    /// <see cref="Queue"/> of either strings or PhpBytes depending on the last
    /// filter output (note that after filtering not all blocks have necessarily
    /// the original chunk size; when appending a filter to the filter-chain
    /// all the buffered data is passed through this one too). The input queue is being 
    /// filled until the required data length is available. The <see cref="readPosition"/> 
    /// property holds the index into the first chunk of data. When this chunk is 
    /// entirely consumed it is dequeued.
    /// </para>
    /// <para>
    /// Writing to a stream is buffered too (unless it is disabled using <c>stream_set_write_buffer</c>). 
    /// When the data passes through the filter-chain it is appended to the 
    /// write buffer (using the <see cref="writePosition"/> property). 
    /// When the write buffer is full it is flushed to the underlying stream.
    /// </para>
    /// </remarks>
    public abstract class PhpStream : PhpResource
    {
        #region PhpStream Opening

        /// <summary>
        /// Simple version of the stream opening function
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="path">URI or filename of the resource to be opened</param>
        /// <param name="mode">File access mode</param>
        /// <returns></returns>
        public static PhpStream Open(Context ctx, string path, StreamOpenMode mode)
        {
            string modeStr;
            switch (mode)
            {
                case StreamOpenMode.ReadBinary: modeStr = "rb"; break;
                case StreamOpenMode.WriteBinary: modeStr = "wb"; break;
                case StreamOpenMode.ReadText: modeStr = "rt"; break;
                case StreamOpenMode.WriteText: modeStr = "wt"; break;
                default: throw new ArgumentException();
            }

            return Open(ctx, path, modeStr, StreamOpenOptions.Empty, StreamContext.Default);
        }

        public static PhpStream Open(Context ctx, string path, string mode)
        {
            return Open(ctx, path, mode, StreamOpenOptions.Empty, StreamContext.Default);
        }

        public static PhpStream Open(Context ctx, string path, string mode, StreamOpenOptions options)
        {
            return Open(ctx, path, mode, options, StreamContext.Default);
        }

        /// <summary>
        /// Checks if the given path is a filesystem path or an URL and returns the corresponding scheme.
        /// </summary>
        /// <param name="path">The path to be canonicalized.</param>
        /// <param name="filename">The filesystem path before canonicalization (may be both relative or absolute).</param>
        /// <returns>The protocol portion of the given URL or <c>"file"</c>o for local files.</returns>
        internal static string GetSchemeInternal(string path, out string filename)
        {
            int colon_index = path.IndexOf(':');
            if (colon_index == -1)
            {
                // No scheme, no root directory, it's a relative path.
                filename = path;
                return "file";
            }

            if (Path.IsPathRooted(path))
            {
                // It already is an absolute path.
                filename = path;
                return "file";
            }

            if (path.Length < colon_index + 3 || path[colon_index + 1] != '/' || path[colon_index + 2] != '/')
            {
                // There is no "//" following the colon.
                filename = path;
                return "file";
            }

            // Otherwise it is an URL (including file://), set the filename and return the scheme.
            filename = path.Substring(colon_index + "://".Length);
            return path.Substring(0, colon_index);
        }

        /// <summary>
        /// Openes a PhpStream using the appropriate StreamWrapper.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="path">URI or filename of the resource to be opened.</param>
        /// <param name="mode">A file-access mode as passed to the PHP function.</param>
        /// <param name="options">A combination of <see cref="StreamOpenOptions"/>.</param>
        /// <param name="context">A valid StreamContext. Must not be <c>null</c>.</param>
        /// <returns></returns>
        public static PhpStream Open(Context ctx, string path, string mode, StreamOpenOptions options, StreamContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Debug.Assert(ctx != null);

            if (ResolvePath(ctx, ref path, out var wrapper, CheckAccessMode.FileMayExist, (CheckAccessOptions)options))
            {
                return wrapper.Open(ctx, ref path, mode, options, context);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Opening utilities

        /// <summary>
        /// Merges the path with the current working directory
        /// to get a canonicalized absolute pathname representing the same file.
        /// </summary>
        /// <remarks>
        /// This method is an analogy of <c>main/safe_mode.c: php_checkuid</c>.
        /// Looks for the file in the <c>include_path</c> and checks for <c>open_basedir</c> restrictions.
        /// </remarks>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="path">An absolute or relative path to a file.</param>
        /// <param name="wrapper">The wrapper found for the specified file or <c>null</c> if the path resolution fails.</param>
        /// <param name="mode">The checking mode of the <see cref="CheckAccess"/> method (file, directory etc.).</param>
        /// <param name="options">Additional options for the <see cref="CheckAccess"/> method.</param>
        /// <returns><c>true</c> if all the resolution and checking passed without an error, <b>false</b> otherwise.</returns>
        /// <exception cref="PhpException">Security violation - when the target file 
        /// lays outside the tree defined by <c>open_basedir</c> configuration option.</exception>
        public static bool ResolvePath(Context ctx, ref string path, out StreamWrapper wrapper, CheckAccessMode mode, CheckAccessOptions options)
        {
            // Path will contain the absolute path without file:// or the complete URL; filename is the relative path.
            string filename, scheme = GetSchemeInternal(path, out filename);
            wrapper = StreamWrapper.GetWrapper(ctx, scheme, (StreamOptions)options);
            if (wrapper == null) return false;

            if (wrapper.IsUrl)
            {
                // Note: path contains the whole URL, filename the same without the scheme:// portion.
                // What to check more?
            }
            else if (scheme != "php")
            {
                try
                {
                    // Filename contains the original path without the scheme:// portion, check for include path.
                    bool isInclude = false;
                    if ((options & CheckAccessOptions.UseIncludePath) > 0)
                    {
                        isInclude = CheckIncludePath(ctx, filename, ref path);
                    }

                    // Path will now contain an absolute path (either to an include or actual directory).
                    if (!isInclude)
                    {
                        path = Path.GetFullPath(Path.Combine(ctx.WorkingDirectory, filename));
                    }
                }
                catch (System.Exception)
                {
                    if ((options & CheckAccessOptions.Quiet) == 0)
                        PhpException.Throw(PhpError.Warning, ErrResources.stream_filename_invalid, FileSystemUtils.StripPassword(path));
                    return false;
                }

                // NOTE: we should let OS & Security configuration to decide
                //var global_config = Configuration.Global;

                //// Note: extensions check open_basedir too -> double check..
                //if (!global_config.SafeMode.IsPathAllowed(path))
                //{
                //    if ((options & CheckAccessOptions.Quiet) == 0)
                //        PhpException.Throw(PhpError.Warning, ErrResources.open_basedir_effect, path, global_config.SafeMode.GetAllowedPathPrefixesJoin());
                //    return false;
                //}

                // Replace all '/' with '\'.
                // path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                Debug.Assert(
                    path.IndexOf(Path.AltDirectorySeparatorChar) == -1 ||
                    (Path.AltDirectorySeparatorChar == Path.DirectorySeparatorChar),    // on Mono, so ignore it
                    string.Format("'{0}' should not contain '{1}' char.", path, Path.AltDirectorySeparatorChar));

                // The file wrapper expects an absolute path w/o the scheme, others expect the scheme://url.
                if (scheme != "file")
                {
                    path = string.Format("{0}://{1}", scheme, path);
                }
            }

            return true;
        }

        /// <summary>
        /// Check if the path lays inside of the directory tree specified 
        /// by the <c>open_basedir</c> configuration option and return the resulting <paramref name="absolutePath"/>.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="relativePath">The filename to search for.</param>
        /// <param name="absolutePath">The combined absolute path (either in the working directory 
        /// or in an include path wherever it has been found first).</param>
        /// <returns><c>true</c> if the file was found in an include path.</returns>
        private static bool CheckIncludePath(Context ctx, string relativePath, ref string absolutePath)
        {
            // Note: If the absolutePath exists, it overtakse the include_path search.
            if (Path.IsPathRooted(relativePath)) return false;
            if (File.Exists(absolutePath)) return false;

            var paths = ctx.IncludePaths;
            if (paths == null || paths.Length == 0) return false;

            foreach (string s in paths)
            {
                if (string.IsNullOrEmpty(s)) continue;
                string abs = Path.GetFullPath(Path.Combine(s, relativePath));
                if (File.Exists(abs))
                {
                    absolutePath = abs;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Performs all checks on a path passed to a PHP function.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method performs a check similar to <c>safe_mode.c: php_checkuid_ex()</c>
        /// together with <c>open_basedir</c> check.
        /// </para>
        /// <para>
        /// The <paramref name="filename"/> may be one of the following:
        /// <list type="bullet">
        /// <item>A relative path. The path is resolved regarding the <c>include_path</c> too if required
        /// and checking continues as in the next case.</item>
        /// <item>An absolute path. The file or directory is checked for existence and for access permissions<sup>1</sup>
        /// according to the given <paramref name="mode"/>.</item>
        /// </list>
        /// <sup>1</sup> Regarding the <c>open_basedir</c> configuration option. 
        /// File access permissions are checked at the time of file manipulation
        /// (opening, copying etc.).
        /// </para>
        /// </remarks>
        /// <param name="filename">A resolved path. Must be an absolute path to a local file.</param>
        /// <param name="mode">One of the <see cref="CheckAccessMode"/>.</param>
        /// <param name="options"><c>true</c> to suppress error messages.</param>
        /// <returns><c>true</c> if the function may continue with file access,
        /// <c>false</c>to fail.</returns>
        /// <exception cref="PhpException">If the file can not be accessed
        /// and the <see cref="CheckAccessOptions.Quiet"/> is not set.</exception>
        public static bool CheckAccess(string filename, CheckAccessMode mode, CheckAccessOptions options)
        {
            Debug.Assert(Path.IsPathRooted(filename));
            string url = FileSystemUtils.StripPassword(filename);
            bool quiet = (options & CheckAccessOptions.Quiet) > 0;

            switch (mode)
            {
                case CheckAccessMode.FileMayExist:
                    break;

                case CheckAccessMode.FileExists:
                    if (!File.Exists(filename))
                    {
                        if (!quiet) PhpException.Throw(PhpError.Warning, ErrResources.stream_file_not_exists, url);
                        return false;
                    }
                    break;

                case CheckAccessMode.FileNotExists:
                    if (File.Exists(filename))
                    {
                        if (!quiet) PhpException.Throw(PhpError.Warning, ErrResources.stream_file_exists, url);
                        return false;
                    }
                    break;

                case CheckAccessMode.FileOrDirectory:
                    if ((!System.IO.Directory.Exists(filename)) && (!File.Exists(filename)))
                    {
                        if (!quiet) PhpException.Throw(PhpError.Warning, ErrResources.stream_path_not_exists, url);
                        return false;
                    }
                    break;

                case CheckAccessMode.Directory:
                    if (!System.IO.Directory.Exists(filename))
                    {
                        if (!quiet) PhpException.Throw(PhpError.Warning, ErrResources.stream_directory_not_exists, url);
                        return false;
                    }
                    break;

                default:
                    Debug.Assert(false);
                    return false;
            }

            return true;
        }

        #endregion

        #region PhpResource override methods

        /// <summary>
        /// PhpStream is created by a StreamWrapper together with the
        /// encapsulated RawStream (the actual file opening is handled 
        /// by the wrapper).
        /// </summary>
        /// <remarks>
        /// This class newly implements the auto-remove behavior too
        /// (see <see cref="StreamAccessOptions.Temporary"/>).
        /// </remarks>
        /// <param name="enc_provider">Runtime string encoding provider.</param>
        /// <param name="openingWrapper">The parent instance.</param>
        /// <param name="accessOptions">The additional options parsed from the <c>fopen()</c> mode.</param>
        /// <param name="openedPath">The absolute path to the opened resource.</param>
        /// <param name="context">The stream context passed to fopen().</param>
        public PhpStream(IEncodingProvider enc_provider, StreamWrapper openingWrapper, StreamAccessOptions accessOptions, string openedPath, StreamContext context)
            : base(PhpStreamTypeName)
        {
            Debug.Assert(enc_provider != null);
            Debug.Assert(context != null);

            _encoding = enc_provider;
            _context = context;

            this.Wrapper = openingWrapper;
            this.OpenedPath = openedPath;

            // Stream modifiers (defined in open-time).
            this.Options = accessOptions;

            // Allocate the text conversion filters for this stream.
            if ((accessOptions & StreamAccessOptions.UseText) > 0)
            {
                if ((accessOptions & StreamAccessOptions.Read) > 0)
                {
                    textReadFilter = new TextReadFilter();
                }
                if ((accessOptions & StreamAccessOptions.Write) > 0)
                {
                    textWriteFilter = new TextWriteFilter();
                }
            }

            this.readTimeout = (enc_provider is Context ctx)    // NOTE: provider is Context, this remains here for historical reasons, refactor if you don't like it
                ? ctx.Configuration.Core.DefaultSocketTimeout
                : 60;
        }

        /// <summary>
        /// PhpResource.FreeManaged overridden to get rid of the contained context on Dispose.
        /// </summary>
        protected override void FreeManaged()
        {
            // Flush the underlying stream before closing.
            if ((writeFilters != null) && (writeFilters.Count > 0))
            {
                // Pass an empty data with closing == true through all the filters.
                WriteData(TextElement.Empty, true);
            }

            Flush();

            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }

            //writeBuffer = null;

            base.FreeManaged();
        }

        /// <summary>
        /// PhpResource.FreeUnmanaged overridden to remove a temporary file on Dispose.
        /// </summary>
        protected override void FreeUnmanaged()
        {
            // Note: this method is called after FreeManaged, so the stream is already closed.
            base.FreeUnmanaged();
            if (this.IsTemporary)
            {
                TryUnlink();
            }
        }

        private void TryUnlink()
        {
            try
            {
                this.Wrapper.Unlink(OpenedPath, StreamUnlinkOptions.Empty, StreamContext.Default);  // File.Delete(this.OpenedPath);
            }
            catch (System.Exception)
            {
            }
        }

        #endregion

        #region Raw byte access (mandatory)

        protected abstract int RawRead(byte[] buffer, int offset, int count);

        protected abstract int RawWrite(byte[] buffer, int offset, int count);

        protected abstract bool RawFlush();

        protected abstract bool RawEof { get; }

        #endregion

        #region Seeking (optional)

        public virtual bool CanSeek { get { return false; } }

        protected virtual int RawTell()
        {
            PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Seek");
            return -1;
        }

        protected virtual bool RawSeek(int offset, SeekOrigin whence)
        {
            PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Seek");
            return false;
        }

        /// <summary>
        /// Gets the length of the stream.
        /// </summary>
        /// <returns>Count of bytes in the stream or <c>-1</c> if seek is not supported.</returns>
        protected virtual int RawLength()
        {
            if (!CanSeek) return -1;
            int current = RawTell();
            if ((current < 0) || !RawSeek(0, SeekOrigin.End)) return -1;
            int rv = RawTell();
            if ((rv < 0) || !RawSeek(current, SeekOrigin.Begin)) return -1;
            return rv;
        }

        #endregion

        #region SetParameter (optional)

        public virtual bool SetParameter(StreamParameterOptions option, PhpValue value)
        {
            // Do not display error messages here, the caller will.
            // EX: will have to distinguish between failed and unsupported.
            // (use additional message when fails)

            // Descendants may call this default implementation for unhandled options
            switch (option)
            {
                case StreamParameterOptions.BlockingMode:
                    // Unimplemented in Win32 PHP.
                    return false;

                case StreamParameterOptions.ReadBufferSize:
                    // Unused option (only turns buffering off)
                    return false;

                case StreamParameterOptions.WriteBufferSize:
                    if (value.IsInteger())
                    {
                        // Let the write buffer reset on next write operation.
                        FlushWriteBuffer();
                        writeBuffer = null;
                        // Set the new size (0 to disable write buffering).
                        writeBufferSize = (int)value.ToLong();
                        if (writeBufferSize < 0) writeBufferSize = 0;
                        return true;
                    }
                    return false;

                case StreamParameterOptions.ReadTimeout:
                    // Set the read timeout for network-based streams (overrides DefaultTimeout).
                    this.readTimeout = (double)value;
                    return false;

                case StreamParameterOptions.SetChunkSize:
                    if (value.IsInteger())
                    {
                        // This setting will affect reading after the buffers are emptied.
                        readChunkSize = (int)value.ToLong();
                        if (readChunkSize < 1) readChunkSize = 1;
                        return true;
                    }
                    return false;

                case StreamParameterOptions.Locking:
                    return false;

                case StreamParameterOptions.MemoryMap:
                    return false;

                case StreamParameterOptions.Truncate:
                    // EX: [Truncate] Override SetParameter in NativeStream to truncate a local file.
                    return false;

                default:
                    Debug.Assert(false); // invalid option
                    return false;
            }
        }

        #endregion

        #region High-level Stream Access (Buffering and Filtering)

        #region High-level Reading

        public bool Eof
        {
            get
            {
                // The raw stream reached EOF and all the data is processed.
                if (RawEof)
                {
                    // Check the buffers as quickly as possible.
                    if ((readBuffers == null) || (readBuffers.Count == 0)) return true;

                    // There is at least one buffer, check position.
                    int firstLength = readBuffers.Peek().Length;
                    if (firstLength > readPosition) return false;

                    if (ReadBufferLength == 0) return true;
                }
                return false;
            }
        }

        #region Buffered Reading

        private int ReadBufferScan(out int nlpos)
        {
            int total = 0;
            nlpos = -1;
            if (readBuffers == null) return 0;

            // Yields to 0 for empty readBuffers.
            foreach (var o in readBuffers)
            {
                var read = o.Length;

                if ((nlpos == -1) && (total <= readPosition) && (total + read > readPosition))
                {
                    // Find the first occurence of \n.
                    nlpos = total + FindEoln(o, readPosition - total);
                }

                total += read;
            }

            // Substract the count of data already processed.
            total -= readPosition;
            return total;
        }

        /// <summary>
        /// Gets the number of <c>byte</c>s or <c>char</c>s available
        /// in the <see cref="readBuffers"/>.
        /// </summary>
        protected int ReadBufferLength
        {
            get
            {
                int nlpos;
                return ReadBufferScan(out nlpos);
            }
        }

        /// <summary>
        /// Fills the <see cref="readBuffers"/> with more data from the underlying stream
        /// passed through all the stream filters. 
        /// </summary>
        /// <param name="chunkSize">Maximum number of bytes to be read from the stream.</param>
        /// <returns>A <see cref="string"/> or <see cref="PhpBytes"/> containing the 
        /// data as returned from the last stream filter or <b>null</b> in case of an error or <c>EOF</c>.</returns>
        protected TextElement ReadFiltered(int chunkSize)
        {
            byte[] chunk = new byte[chunkSize];
            var filtered = TextElement.Null;

            while (filtered.IsNull)
            {
                // Read data until there is an output or error or EOF.
                if (RawEof) return TextElement.Null;
                int read = RawRead(chunk, 0, chunkSize);
                if (read <= 0)
                {
                    // Error or EOF.
                    return TextElement.Null;
                }

                if (read < chunkSize)
                {
                    byte[] sub = new byte[read];
                    Array.Copy(chunk, 0, sub, 0, read);
                    chunk = sub;
                }
                filtered = new TextElement(chunk);

                bool closing = RawEof;

                if (textReadFilter != null)
                {
                    // First use the text-input filter if any.
                    filtered = textReadFilter.Filter(_encoding, filtered, closing);
                }

                if (readFilters != null)
                {
                    // After that apply the user-filters.
                    foreach (IFilter f in readFilters)
                    {
                        if (filtered.IsNull)
                        {
                            // This is the last chance to output something. Give chance to all filters.
                            if (closing) filtered = TextElement.Empty;
                            else break; // Continue with next RawRead()
                        }
                        filtered = f.Filter(_encoding, filtered, closing);
                    } // foreach
                } // if
            } // while 

            return filtered;
        }

        /// <summary>
        /// Put a buffer at the end of the <see cref="readBuffers"/>.
        /// </summary>
        /// <param name="data">The buffer to append.</param>
        internal void EnqueueReadBuffer(TextElement data)
        {
            Debug.Assert(!data.IsNull);

            // This may be the first access to the buffers.
            if (readBuffers == null)
                readBuffers = new Queue<TextElement>(2);

            // Append the filtered output to the buffers.
            readBuffers.Enqueue(data);
        }

        /// <summary>
        /// Remove the (entirely consumed) read buffer from the head of the read buffer queue.
        /// </summary>
        /// <returns><c>true</c> if there are more buffers in the queue.</returns>
        protected bool DropReadBuffer()
        {
            Debug.Assert(readBuffers != null);
            Debug.Assert(readBuffers.Count > 0);

            var data = readBuffers.Dequeue();
            int length = data.Length;
            Debug.Assert(length > 0);

            // Add the new offset to the total one.
            readOffset += length;

            readPosition = 0;
            return readBuffers.Count != 0;
        }

        /// <summary>
        /// Joins the read buffers to get at least <paramref name="length"/> characters
        /// in a <see cref="string"/>. 
        /// </summary>
        /// <remarks>
        /// It is assumed that there already is length bytes in the buffers.
        /// Otherwise an InvalidOperationException is raised.
        /// </remarks>
        /// <param name="length">The desired maximum result length.</param>
        /// <returns>A <see cref="string"/> dequeued from the buffer or <c>null</c> if the buffer is empty.</returns>
        /// <exception cref="InvalidOperationException">If the buffers don't contain enough data.</exception>
        protected string ReadTextBuffer(int length)
        {
            if (length == 0) return string.Empty;

            string peek = readBuffers.Peek().AsText(_encoding.StringEncoding);
            if (peek == null) throw new InvalidOperationException(ErrResources.buffers_must_not_be_empty);
            Debug.Assert(peek.Length >= readPosition);

            if (peek.Length - readPosition >= length)
            {
                // Great! We can just take a substring.
                string res = peek.Substring(readPosition, length);
                readPosition += length;

                if (peek.Length == readPosition)
                {
                    // We just consumed the entire string. Dequeue it.
                    DropReadBuffer();
                }
                return res;
            }
            else
            {
                // Start building the string from the remainder in the buffer.
                var sb = new StringBuilder(peek, readPosition, peek.Length - readPosition, length);
                length -= peek.Length - readPosition;

                // We just consumed the entire string. Dequeue it.
                DropReadBuffer();

                while (length > 0)
                {
                    peek = readBuffers.Peek().AsText(_encoding.StringEncoding);
                    if (peek == null) throw new InvalidOperationException(ErrResources.too_little_data_buffered);
                    if (peek.Length > length)
                    {
                        // This string is long enough. It is the last one.
                        sb.Append(peek, 0, length);
                        readPosition = length;
                        length = 0;
                        break;
                    }
                    else
                    {
                        // Append just another whole buffer to the StringBuilder.
                        sb.Append(peek);
                        length -= peek.Length;
                        DropReadBuffer();

                        // When this is the last buffer (it's probably an EOF), return.
                        if (readBuffers.Count == 0)
                            break;
                    }
                } // while

                Debug.Assert(sb.Length > 0);
                return sb.ToString();
            } // else
        }

        /// <summary>
        /// Joins the read buffers to get at least <paramref name="length"/> bytes
        /// in a <see cref="PhpBytes"/>. 
        /// </summary>
        /// <param name="length">The desired maximum result length.</param>
        /// <returns>A <see cref="PhpBytes"/> dequeued from the buffer or <c>null</c> if the buffer is empty.</returns>
        protected byte[] ReadBinaryBuffer(int length)
        {
            if (length == 0) return ArrayUtils.EmptyBytes;

            byte[] peek = readBuffers.Peek().AsBytes(_encoding.StringEncoding);
            Debug.Assert(peek.Length >= readPosition);

            //
            byte[] data = new byte[length];

            if (peek.Length - readPosition >= length)
            {
                // Great! We can just take a sub-data.
                Array.Copy(peek, readPosition, data, 0, length);
                readPosition += length;

                if (peek.Length == readPosition)
                {
                    // We just consumed the entire string. Dequeue it.
                    DropReadBuffer();
                }

                return data;
            }
            else
            {
                // Start building the data from the remainder in the buffer.
                int buffered = this.ReadBufferLength;
                if (buffered < length) length = buffered;

                int copied = peek.Length - readPosition;
                Array.Copy(peek, readPosition, data, 0, copied); readPosition += copied;
                length -= copied;

                // We just consumed the entire data. Dequeue it.
                DropReadBuffer();

                while (length > 0)
                {
                    peek = readBuffers.Peek().AsBytes(_encoding.StringEncoding);
                    if (peek.Length > length)
                    {
                        // This data is long enough. It is the last one.
                        Array.Copy(peek, 0, data, copied, length);
                        readPosition = length;
                        length = 0;
                        break;
                    }
                    else
                    {
                        // Append just another whole buffer to the array.
                        Array.Copy(peek, 0, data, copied, peek.Length);
                        length -= peek.Length;
                        copied += peek.Length;
                        DropReadBuffer();

                        // When this is the last buffer (it's probably an EOF), return.
                        if (readBuffers.Count == 0)
                            break;
                    }
                } // while

                Debug.Assert(copied > 0);
                if (copied < length)
                {
                    Array.Resize(ref data, copied);
                }
                return data;
            } // else
        }

        #endregion

        #region Data Block Conversions

        //       /// <summary>
        //       /// Casts the input parameter as <see cref="PhpBytes"/>, converting it
        //       /// using the page encoding if necessary.
        //       /// </summary>
        //       /// <param name="input">The input passed to the filter. Must not be <c>null</c>.</param>
        //       /// <returns>The input cast to <see cref="PhpBytes"/> or <see cref="PhpBytes.Empty"/> for empty input.</returns>
        //       public static PhpBytes AsBinary(object input)
        //       {
        //           return Core.Convert.ObjectToPhpBytes(input);
        //       }


        //       /// <summary>
        //       /// Casts the input parameter as <see cref="string"/>, converting it
        //       /// using the page encoding if necessary.
        //       /// </summary>
        //       /// <param name="input">The input passed to the filter. Must not be <c>null</c>.</param>
        //       /// <param name="count">The maximum count of input entities to convert.</param>
        //       /// <returns>The input cast to <see cref="PhpBytes"/> or <see cref="PhpBytes.Empty"/> for empty input.</returns>
        //       public static PhpBytes AsBinary(object input, int count)
        //       {
        //           if (input == null) return PhpBytes.Empty;

        //           // Use only the necessary portion of the string
        //           string str = input as string;
        //           if (str != null)
        //           {
        //               if (count > str.Length)
        //                   return new PhpBytes(Configuration.Application.Globalization.PageEncoding.GetBytes(str));

        //               byte[] sub = new byte[count];
        //               Configuration.Application.Globalization.PageEncoding.GetBytes(str, 0, count, sub, 0);
        //               return new PhpBytes(sub);
        //           }

        //           // All other types treat as one case.
        //           PhpBytes bin = Core.Convert.ObjectToPhpBytes(input);
        //           if (count >= bin.Length) return bin;
        //           byte[] sub2 = new byte[count];
        //           Array.Copy(bin.ReadonlyData, 0, sub2, 0, count);
        //           return new PhpBytes(sub2);
        //       }

        //       /// <summary>
        //       /// Casts the input parameter as <see cref="string"/>, converting it
        //       /// using the page encoding if necessary.
        //       /// </summary>
        //       /// <param name="input">The input passed to the filter.</param>
        //       /// <returns>The input cast to <see cref="string"/> or <see cref="string.Empty"/> for empty input.</returns>
        //       public static string AsText(object input)
        //       {
        //           return Core.Convert.ObjectToString(input);
        //       }

        //       /// <summary>
        //       /// Casts the input parameter as <see cref="string"/>, converting it
        //       /// using the page encoding if necessary.
        //       /// </summary>
        //       /// <param name="input">The input passed to the filter.</param>
        //       /// <param name="count">The count of input entities to convert.</param>
        //       /// <returns>The input cast to <see cref="string"/> or <see cref="string.Empty"/> for empty input.</returns>
        //       public static string AsText(object input, int count)
        //       {
        //           if (input == null) return string.Empty;

        //           // Use only the necessary portion of the PhpBytes
        //           PhpBytes bin = input as PhpBytes;
        //           if (bin != null)
        //           {
        //               if (count > bin.Length) count = bin.Length;
        //               return Configuration.Application.Globalization.PageEncoding.GetString(bin.ReadonlyData, 0, count);
        //           }

        //           string str = Core.Convert.ObjectToString(input);
        //           if (count >= str.Length) return str;
        //           return str.Substring(0, count);
        //       }

        #endregion

        #region Block Reading

        /// <summary>
        /// Reads a block of data from the stream up to <paramref name="length"/>
        /// characters or up to EOLN if <paramref name="length"/> is negative.
        /// </summary>
        /// <remarks>
        /// ReadData first looks for data into the <see cref="readBuffers"/>. 
        /// While <paramref name="length"/> is not satisfied, new data from the underlying stream are processed.
        /// The data is buffered as either <see cref="string"/> or <see cref="PhpBytes"/>
        /// but consistently. The type of the first buffer thus specifies the return type.
        /// </remarks>
        /// <param name="length">The number of bytes to return, when set to <c>-1</c>
        /// reading carries on up to EOLN or EOF.</param>
        /// <param name="ending">If <c>true</c>, the buffers are first searched for \n.</param>
        /// <returns>A <see cref="string"/> or <see cref="byte"/>[] containing the 
        /// data as returned from the last stream filter or <b>null</b> in case of an error or <c>EOF</c>.</returns>
        public TextElement ReadData(int length, bool ending)
        {
            if (length == 0) return TextElement.Null;

            // Allow length to be -1 for ReadLine.
            Debug.Assert((length > 0) || ending);
            Debug.Assert(length >= -1);

            // Set file access to reading
            CurrentAccess = FileAccess.Read;
            if (!CanRead) return TextElement.Null;

            // If (length < 0) read up to \n, otherwise up to length bytes      
            // Unbuffered works only for Read not for ReadLine (blocks).
            if (!IsReadBuffered && (readBuffers == null))
            {
                // The stream is a "pure" unbuffered. Read just the first packet.
                var packet = TextElement.Null;
                bool done = false;
                while (!done)
                {
                    int count = (length > 0) ? length : readChunkSize;
                    packet = ReadFiltered(count);
                    if (packet.IsNull) return TextElement.Null;

                    int filteredLength = packet.Length;
                    done = filteredLength > 0;
                    readFilteredCount += filteredLength;

                    if (ending)
                    {
                        // If the data contains the EOLN, store the rest into the buffers, otherwise return the whole packet.
                        int eoln = FindEoln(packet, 0);
                        if (eoln > 0)
                        {
                            TextElement rv, enq;
                            SplitData(packet, eoln, out rv, out enq);
                            if (enq.Length != 0) EnqueueReadBuffer(enq);
                            return rv;
                        }
                    }
                }
                return packet;
            }

            // Try to fill the buffers with enough data (to satisfy length).
            int nlpos, buffered = ReadBufferScan(out nlpos), read = 0, newLength = length;
            TextElement data = TextElement.Null;

            if (ending && (nlpos >= readPosition))
            {
                // Found a \n in the buffered data (return the line inluding the EOLN).
                // Network-based streams may be satisfied too.
                newLength = nlpos - readPosition + 1;
            }
            else if ((length > 0) && (buffered >= length))
            {
                // Great! Just take some of the data in the buffers.
                // NOP
            }
            else if (!IsReadBuffered && (buffered > 0))
            {
                // Use the first available packet for network-based streams.
                newLength = buffered;
            }
            else
            {
                // There is not enough data in the buffers, read more.
                for (; ; )
                {
                    data = ReadFiltered(readChunkSize);
                    if (data.IsNull)
                    {
                        // There is an EOF, return as much data as possible.
                        newLength = buffered;
                        break;
                    }
                    read = data.Length;
                    readFilteredCount += read;
                    if (read > 0) EnqueueReadBuffer(data);
                    buffered += read;

                    // For unbuffered streams accept the first packet and go check for EOLN.
                    if (!IsReadBuffered) newLength = buffered;

                    // First check for satisfaciton of the ending.
                    if (ending && !data.IsNull)
                    {
                        // Find the EOLN in the most recently read buffer.
                        int eoln = FindEoln(data, 0);
                        if (eoln >= 0)
                        {
                            // Read all the data up to (and including) the EOLN.
                            newLength = buffered - read + eoln + 1;
                            break;
                        }
                    }

                    // Check if there is enough data in the buffers (first packet etc).
                    if (length > 0)
                    {
                        if (buffered >= length) break;
                    }
                }
            }

            // Apply the restriction of available data size or newline position
            if ((newLength < length) || (length == -1)) length = newLength;

            // Eof?
            if ((readBuffers == null) || (readBuffers.Count == 0))
                return TextElement.Null;

            // Read the rest of the buffered data if no \n is found and there is an EOF.
            if (length < 0) length = buffered;

            if (this.IsText)
                return new TextElement(ReadTextBuffer(length));
            else
                return new TextElement(ReadBinaryBuffer(length));
            // Data may only be a string or byte[] (and consistently throughout all the buffers).
        }

        /// <summary>
        /// Reads binary data from the stream. First looks for data into the 
        /// <see cref="readBuffers"/>. When <paramref name="length"/> is not
        /// satisfied, new data from the underlying stream are processed.
        /// </summary>
        /// <param name="length">The number of bytes to return.</param>
        /// <returns><see cref="PhpBytes"/> containing the binary data read from the stream.</returns>
        public byte[] ReadBytes(int length)
        {
            Debug.Assert(this.IsBinary);
            // Data may only be a string or PhpBytes.
            return ReadData(length, false).AsBytes(_encoding.StringEncoding);
        }

        /// <summary>
        /// Reads text data from the stream. First looks for data into the 
        /// <see cref="readBuffers"/>. When <paramref name="length"/> is not
        /// satisfied, new data from the underlying stream are processed.
        /// </summary>
        /// <param name="length">The number of characters to return.</param>
        /// <returns><see cref="string"/> containing the text data read from the stream.</returns>
        public string ReadString(int length)
        {
            Debug.Assert(this.IsText);
            // Data may only be a string or PhpBytes.
            return ReadData(length, false).AsText(_encoding.StringEncoding);
        }

        /// <summary>
        /// Finds the '\n' in a string or PhpBytes and returns its offset or <c>-1</c>
        /// if not found.
        /// </summary>
        /// <param name="data">Data to scan.</param>
        /// <param name="from">Index of the first character to scan.</param>
        /// <returns></returns>
        private static int FindEoln(TextElement data, int from)
        {
            Debug.Assert(!data.IsNull);

            if (data.IsText)
            {
                return data.GetText().IndexOf('\n', from);
            }
            else
            {
                Debug.Assert(data.IsBinary);
                return Array.IndexOf(data.GetBytes(), (byte)'\n', from);
            }
        }

        /// <summary>
        /// Split a <see cref="String"/> or <see cref="PhpBytes"/> to "upto" bytes at left and the rest or <c>null</c> at right.
        /// </summary>
        private static void SplitData(TextElement data, int upto, out TextElement left, out TextElement right)
        {
            Debug.Assert(upto >= 0);
            //if (this.IsText)
            if (data.IsText)
            {
                string s = data.GetText();
                if (upto < s.Length - 1)
                {
                    left = new TextElement(s.Substring(0, upto + 1));
                    right = new TextElement(s.Substring(upto + 2));
                }
                else
                {
                    left = data;
                    right = TextElement.Null;
                }
            }
            else
            {
                Debug.Assert(data.IsBinary);
                var bin = data.GetBytes();
                if (upto < bin.Length - 1)
                {
                    byte[] l = new byte[upto + 1], r = new byte[bin.Length - upto - 1];
                    Array.Copy(bin, 0, l, 0, upto + 1);
                    Array.Copy(bin, upto + 1, r, 0, bin.Length - upto - 1);
                    left = new TextElement(l);
                    right = new TextElement(r);
                }
                else
                {
                    left = data;
                    right = TextElement.Null;
                }
            }
        }

        /// <summary>
        /// Split a <see cref="string"/> to "upto" bytes at left and the rest or <c>null</c> at right.
        /// </summary>
        private static void SplitData(string s, int upto, out string left, out string right)
        {
            if (upto < s.Length - 1)
            {
                left = s.Substring(0, upto + 1);
                right = s.Substring(upto + 2);
            }
            else
            {
                left = s;
                right = null;
            }
        }

        /// <summary>
        /// Splits a <see cref="string"/> to "upto" bytes at left, ignores "separator" and the rest or <c>null</c> at right.
        /// </summary>
        static void SplitStringAt(string str, int upto, int separator, out string left, out string right)
        {
            if (upto < str.Length)
            {
                left = str.Remove(upto);

                if (upto + separator < str.Length)
                {
                    right = str.Substring(upto + separator);
                }
                else
                {
                    right = null;
                }
            }
            else
            {
                left = str;
                right = null;
            }
        }

        #endregion

        #region Maximum Block Reading

        /// <summary>
        /// Gets the number of bytes or characters in the first read-buffer or next chunk size.
        /// </summary>
        /// <returns>The number of bytes or characters the next call to ReadMaximumData would return.</returns>
        public int GetNextDataLength()
        {
            return ((readBuffers != null) && (readBuffers.Count != 0))
                ? readBuffers.Peek().Length
                : readChunkSize;
        }

        /// <summary>
        /// Most effecient access to the buffered stream consuming one whole buffer at a time.
        /// Performs no unnecessary conversions (although attached stream filters may do so).
        /// </summary>
        /// <remarks>
        /// Use the <see cref="readChunkSize"/> member to affect the amount of data returned at a time.
        /// </remarks>
        /// <returns>A <see cref="string"/> or <see cref="PhpBytes"/> containing data read from the stream.</returns>
        public TextElement ReadMaximumData()
        {
            // Set file access to reading
            CurrentAccess = FileAccess.Read;
            if (!CanRead) return TextElement.Null;

            TextElement data;

            //
            if ((readBuffers == null) || (readBuffers.Count == 0))
            {
                // Read one block without storing it in the buffers.
                data = ReadFiltered(readChunkSize);
                int filteredLength = data.Length;
                readFilteredCount += filteredLength;
            }
            else
            {
                // Dequeue one whole buffer.
                data = readBuffers.Peek();
                DropReadBuffer();
            }

            //
            return data;
        }

        /// <summary>
        /// Effecient access to the buffered and filtered stream consuming one whole buffer at a time.
        /// </summary>
        /// <returns>A <see cref="byte"/>[] containing data read from the stream.</returns>
        public byte[] ReadMaximumBytes()
        {
            return ReadMaximumData().AsBytes(_encoding.StringEncoding);
        }

        /// <summary>
        /// Effecient access to the buffered and filtered stream consuming one whole buffer at a time.
        /// </summary>
        /// <returns>A <see cref="string"/> containing data read from the stream.</returns>
        public string ReadMaximumString()
        {
            return ReadMaximumData().AsText(_encoding.StringEncoding);
        }

        #endregion

        #region Entire Stream Reading

        public TextElement ReadContents() => ReadContents(-1, -1);

        public TextElement ReadContents(int maxLength) => ReadContents(maxLength, -1);

        public TextElement ReadContents(int maxLength, int offset)
        {
            if (offset > -1 && !Seek(offset, SeekOrigin.Begin))
                return TextElement.Null;

            return (IsText)
                ? new TextElement(ReadStringContents(maxLength))
                : new TextElement(ReadBinaryContents(maxLength));
        }

        public string ReadStringContents(int maxLength)
        {
            if (!CanRead) return null;
            var result = StringBuilderUtilities.Pool.Get();

            if (maxLength >= 0)
            {
                while (maxLength > 0 && !Eof)
                {
                    string data = ReadString(maxLength);
                    if (data == null && data.Length > 0) break; // EOF or error.
                    maxLength -= data.Length;
                    result.Append(data);
                }
            }
            else
            {
                while (!Eof)
                {
                    string data = ReadMaximumString();
                    if (data == null) break; // EOF or error.
                    result.Append(data);
                }
            }

            return StringBuilderUtilities.GetStringAndReturn(result);
        }

        public byte[] ReadBinaryContents(int maxLength)
        {
            if (!CanRead)
            {
                return null;
            }

            var result = new MemoryStream();

            if (maxLength >= 0)
            {
                while (maxLength > 0 && !Eof)
                {
                    var data = ReadBytes(maxLength);
                    if (data.Length != 0) break; // EOF or error.
                    maxLength -= data.Length;
                    result.Write(data, 0, data.Length);
                }
            }
            else
            {
                while (!Eof)
                {
                    var data = ReadMaximumBytes();
                    if (data.Length == 0) break; // EOF or error.
                    result.Write(data, 0, data.Length);
                }
            }

            return result.ToArray();
        }

        #endregion

        #region Parsed Reading (ReadLine)

        /// <summary>
        /// Reads one line (text ending with the <paramref name="ending"/> delimiter)
        /// from the stream up to <paramref name="length"/> characters long.
        /// </summary>
        /// <param name="length">Maximum length of the returned <see cref="string"/> or <c>-1</c> for unlimited result.</param>
        /// <param name="ending">Delimiter of the returned line or <b>null</b> to use the system default.</param>
        /// <returns>A <see cref="string"/> containing one line from the input stream.</returns>
        public string ReadLine(int length, string ending)
        {
            // A length has to be specified if we want to use the delimiter.
            Debug.Assert((length > 0) || (ending == null));

            var str = ReadData(length, ending == null) // null ending => use \n
                .AsText(_encoding.StringEncoding);

            if (ending != null)
            {
                int pos = (ending.Length == 1) ? str.IndexOf(ending[0]) : str.IndexOf(ending);
                if (pos >= 0)
                {
                    SplitStringAt(str, pos, ending.Length, out var left, out var right);
                    if (right != null)
                    {
                        int returnedLength = right.Length;
                        var rightElement = this.IsBinary
                            ? new TextElement(_encoding.StringEncoding.GetBytes(right))
                            : new TextElement(right);

                        if (readBuffers.Count != 0)
                        {
                            // EX: Damn. Have to put the data to the front of the queue :((
                            // Better first look into the buffers for the ending..
                            var newBuffers = new Queue<TextElement>(readBuffers.Count + 2);
                            newBuffers.Enqueue(rightElement);
                            foreach (var o in readBuffers)
                            {
                                var data = o;
                                if (readPosition > 0)
                                {
                                    // the buffered portion of text was read
                                    Debug.Assert(data.Length > readPosition);
                                    data = this.IsBinary
                                        ? new TextElement(data.GetBytes().Slice(readPosition))
                                        : new TextElement(data.GetText().Substring(readPosition));
                                    readPosition = 0;
                                }
                                newBuffers.Enqueue(data);
                            }
                            readBuffers = newBuffers;
                        }
                        else
                        {
                            readBuffers.Enqueue(rightElement);
                        }
                        // Update the offset as the data gets back.
                        readOffset -= returnedLength;
                    }

                    return left;
                }
            }
            // ReadLine now works on binary files too but only for the \n ending.
            return str;
        }

        #endregion

        #region Filter Chains

        /// <summary>
        /// Adds a filter to one of the read or write filter chains.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="where">The position in the chain.</param>
        public void AddFilter(IFilter filter, FilterChainOptions where)
        {
            Debug.Assert((where & FilterChainOptions.ReadWrite) != FilterChainOptions.ReadWrite);
            List<IFilter> list;

            // Which chain.
            if ((where & FilterChainOptions.Read) != 0)
            {
                list = readFilters ??= new List<IFilter>();
            }
            else
            {
                list = writeFilters ??= new List<IFilter>();
            }

            // Position in the chain.
            if ((where & FilterChainOptions.Tail) != 0)
            {
                list.Add(filter);
                if ((list == readFilters) && (ReadBufferLength > 0))
                {
                    // Process all the data in the read buffers.
                    var q = new Queue<TextElement>();
                    foreach (var o in readBuffers)
                    {
                        q.Enqueue(filter.Filter(_encoding, o, false));
                    }

                    readBuffers = q;
                }
            }
            else
            {
                list.Insert(0, filter);
            }
        }

        /// <summary>
        /// Removes a filter from the filter chains.
        /// </summary>
        public bool RemoveFilter(IFilter filter, FilterChainOptions where)
        {
            var list = (where & FilterChainOptions.Read) != 0 ? readFilters : writeFilters;
            return list != null && list.Remove(filter);
        }

        /// <summary>
        /// Get enumerator of chained read/write filters.
        /// </summary>
        public IEnumerable<PhpFilter> StreamFilters
        {
            get
            {
                var result = Enumerable.Empty<PhpFilter>();

                if (readFilters != null)
                {
                    result = result.Concat(readFilters.Cast<PhpFilter>());
                }

                if (writeFilters != null)
                {
                    result = result.Concat(writeFilters.Cast<PhpFilter>());
                }

                return result;
            }
        }

        #endregion

        #endregion

        #region High-level Writing

        #region Buffered Writing

        /// <summary>
        /// Write all the output buffer to the underlying stream and flush it.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> on error.</returns>
        public bool Flush()
        {
            return FlushWriteBuffer() && RawFlush();
        }

        /// <summary>
        /// Writes all the output buffer to the underlying stream.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> on error.</returns>
        protected bool FlushWriteBuffer()
        {
            // Stream may not have been used for output yet.
            if ((writeBufferSize == 0) || (writeBuffer == null)) return true;

            int flushPosition = 0;
            while (flushPosition < writePosition)
            {
                // Send as much data as possible to the underlying stream.
                int written = RawWrite(writeBuffer, flushPosition, writePosition - flushPosition);

                if (written <= 0)
                {
                    // An error occured. Clear flushed data and return.
                    if (flushPosition > 0)
                    {
                        byte[] buf = new byte[writeBufferSize];
                        Array.Copy(writeBuffer, flushPosition, buf, 0, writePosition - flushPosition);
                        writeBuffer = buf;
                    }

                    PhpException.Throw(PhpError.Warning, ErrResources.stream_write_failed, flushPosition.ToString(), writePosition.ToString());

                    return false;
                }
                else
                {
                    // Move for the next chunk.
                    flushPosition += written;
                    writeOffset += written;
                }
            }

            // All the data has been successfully flushed.
            writePosition = 0;
            return true;
        }

        #endregion

        #region Block Writing

        /// <summary>
        /// Apppends the binary data to the output buffer passing through the output filter-chain. 
        /// When the buffer is full or buffering is disabled, pass the data to the low-level stream.
        /// </summary>
        /// <param name="data">The <see cref="PhpBytes"/> to store.</param>
        /// <returns>Number of bytes successfully written or <c>-1</c> on an error.</returns>
        public int WriteBytes(byte[] data)
        {
            Debug.Assert(this.IsBinary);
            return WriteData(new TextElement(data), false);
        }

        /// <summary>
        /// Apppends the text data to the output buffer passing through the output filter-chain. 
        /// When the buffer is full or buffering is disabled, pass the data to the low-level stream.
        /// </summary>
        /// <param name="data">The <see cref="string"/> to store.</param>
        /// <returns>Number of characters successfully written or <c>-1</c> on an error.</returns>
        public int WriteString(string data)
        {
            Debug.Assert(this.IsText);
            return WriteData(new TextElement(data), false);
        }

        /// <summary>
        /// Passes the data through output filter-chain to the output buffer. 
        /// When the buffer is full or buffering is disabled, passes the data to the low-level stream.
        /// </summary>
        /// <param name="data">The data to store (filters will handle the type themselves).</param>
        /// <param name="closing"><c>true</c> when this method is called from <c>close()</c>
        /// to prune all the pending filters with closing set to <c>true</c>.</param>
        /// <returns>Number of character entities successfully written or <c>-1</c> on an error.</returns>
        public int WriteData(TextElement data, bool closing = false)
        {
            // Set file access to writing
            CurrentAccess = FileAccess.Write;
            if (!CanWrite) return -1;

            Debug.Assert(!data.IsNull);

            int consumed = data.Length;
            writeFilteredCount += consumed;
            if (writeFilters != null)
            {
                // Process the data through the custom write filters first.
                foreach (IFilter f in writeFilters)
                {
                    if (data.IsNull)
                    {
                        // When closing, feed all the filters with data.
                        if (closing) data = TextElement.Empty;
                        else return consumed; // Eaten all
                    }
                    data = f.Filter(_encoding, data, closing);
                    if (closing) f.OnClose();
                }
            }

            if (textWriteFilter != null)
            {
                // Then pass it through the text-conversion filter if any.
                data = textWriteFilter.Filter(_encoding, data, closing);
            }

            // From now on, the data is treated just as binary
            byte[] bin = data.AsBytes(_encoding.StringEncoding);
            if (bin.Length == 0)
            {
                return consumed;
            }

            // Append the resulting data to the output buffer if any.
            if (IsWriteBuffered)
            {
                // Is this the first access?
                if (writeBuffer == null)
                {
                    writeBuffer = new byte[writeBufferSize];
                    writePosition = 0;
                }

                // The whole binary data fits in the buffer, great!
                if (writeBufferSize - writePosition > bin.Length)
                {
                    Array.Copy(bin, 0, writeBuffer, writePosition, bin.Length);
                    writePosition += bin.Length;
                    return consumed;
                }

                int copied = 0;

                // Use the buffer for small data only
                if (writeBufferSize > bin.Length)
                {
                    // Otherwise fill the buffer and flush it.
                    copied = writeBufferSize - writePosition;
                    Array.Copy(bin, 0, writeBuffer, writePosition, copied);
                    writePosition += copied;
                }

                // Flush the buffer
                if ((writePosition > 0) && (!FlushWriteBuffer()))
                    return (copied > 0) ? copied : -1; // It is an error but still some output was written.

                if (bin.Length - copied >= writeBufferSize)
                {
                    // If the binary data is really big, write it directly to stream.
                    while (copied < bin.Length)
                    {
                        int written = RawWrite(bin, copied, bin.Length - copied);
                        if (written <= 0)
                        {
                            PhpException.Throw(PhpError.Warning, ErrResources.stream_write_failed, copied.ToString(), bin.Length.ToString());
                            return (copied > 0) ? copied : -1; // It is an error but still some output was written.
                        }
                        copied += written;
                        writeOffset += written;
                    }
                }
                else
                {
                    // Otherwise just start a new buffer with the rest of the data.
                    Array.Copy(bin, copied, writeBuffer, 0, bin.Length - copied);
                    writePosition = bin.Length - copied;
                }

                return consumed;
            }
            else
            {
                // No write buffer. Write the data directly.
                int copied = 0;
                while (copied < bin.Length)
                {
                    int written = RawWrite(bin, copied, bin.Length - copied);
                    if (written <= 0)
                    {
                        PhpException.Throw(PhpError.Warning, ErrResources.stream_write_failed, copied.ToString(), bin.Length.ToString());
                        return (copied > 0) ? copied : -1; // ERROR but maybe some was written.
                    }
                    copied += written;
                    writeOffset += written;
                }

                return consumed;
            }
        }

        #endregion

        #endregion

        public virtual bool CanReadWithoutLock => true;

        public virtual bool CanWriteWithoutLock => true;

        /// <summary>
        /// Sets the read/write pointer in the stream to a new position.
        /// </summary>
        /// <param name="offset">The offset from the position denoted by <paramref name="whence"/>.</param>
        /// <param name="whence">One of the <see cref="SeekOrigin"/> flags.</param>
        /// <returns><c>true</c> if the operation was successful.</returns>
        public bool Seek(int offset, SeekOrigin whence)
        {
            if (!CanSeek)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Seek");
                return false;
            }

            // This is supported by any stream.
            int current = Tell();
            int newpos = -1;
            if (whence == SeekOrigin.Begin) newpos = offset;
            else if (whence == SeekOrigin.Current) newpos = current + offset;
            else if (whence == SeekOrigin.End)
            {
                int len = RawLength();
                if (len >= 0) newpos = len + offset;
            }

            switch (CurrentAccess)
            {
                case FileAccess.ReadWrite:
                    // Stream not R/W accessed yet. Prepare location and offset.
                    return SeekInternal(offset, current, whence);

                case FileAccess.Read:
                    // Maybe we will be able to seek inside the buffers.
                    if ((newpos >= readOffset) && (newpos < readOffset + ReadBufferLength))
                    {
                        int streamPosition = readOffset + ReadPosition;
                        if (newpos > streamPosition)
                        {
                            // Seek forward
                            // This asserts that ReadBufferLength > 0.
                            int len = readBuffers.Peek().Length;
                            while (newpos - readOffset >= len)
                            {
                                DropReadBuffer();
                                len = readBuffers.Peek().Length;
                            }
                            Debug.Assert(readBuffers.Count > 0);

                            // All superfluous buffers are dropped, seek in the head one.
                            readPosition = newpos - readOffset;
                        }
                        else if (newpos < streamPosition)
                        {
                            // The required position is still in the first buffer
                            //. Debug.Assert(streamPosition == readOffset + readPosition);
                            readPosition = newpos - readOffset;
                        }
                    }
                    else
                    {
                        // Drop all the read buffers and proceed to the actual seeking.
                        readBuffers = null;

                        // Notice that for a filtered stream, seeking is not a good idea
                        if (IsReadFiltered)
                        {
                            PhpException.Throw(PhpError.Notice,
                                ErrResources.stream_seek_filtered, (textReadFilter != null) ? "text" : "filtered");
                        }
                        return SeekInternal(offset, current, whence);
                    }
                    break;

                case FileAccess.Write:
                    // The following does not currently work since other methods do not take unempty writebuffer into account

                    //// Maybe we can seek inside of the buffer but we allow only backward skips.
                    //if ((newpos >= writeOffset) && (newpos < writeOffset + writePosition))
                    //{
                    //    // We are inside the current buffer, great.
                    //    writePosition = newpos - writeOffset;
                    //}
                    //else
                    //{

                    // Flush write buffers and proceed to the default handling.
                    FlushWriteBuffer();

                    // Notice that for a filtered stream, seeking is not a good idea
                    if (IsWriteFiltered)
                    {
                        PhpException.Throw(PhpError.Notice,
                            ErrResources.stream_seek_filtered, (textWriteFilter != null) ? "text" : "filtered");
                    }
                    return SeekInternal(offset, current, whence);
            }
            return true;
            // CHECKME: [PhpStream.Seek]
        }

        /// <summary>
        /// Perform the actual seek on the stream. Report errors.
        /// </summary>
        /// <param name="offset">New position in the stream.</param>
        /// <param name="current">Current position in the stream.</param>
        /// <param name="whence">Where to count from.</param>
        /// <returns><c>true</c> if successful</returns>
        /// <exception cref="PhpException">In case that Seek is not supported by this stream type.</exception>
        internal bool SeekInternal(int offset, int current, SeekOrigin whence)
        {
            try
            {
                if (!CanSeek)
                {
                    PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Seek");
                    return false;
                }

                if (!RawSeek(offset, whence)) return false;
                int expectedOffset = 0, absoluteOffset = RawTell();

                switch (whence)
                {
                    case SeekOrigin.Begin:
                        expectedOffset = offset;
                        break;
                    case SeekOrigin.Current:
                        expectedOffset = current + offset;
                        break;
                    case SeekOrigin.End:
                        expectedOffset = RawLength() + offset;
                        break;
                    default:
                        PhpException.Throw(PhpError.Warning, ErrResources.invalid_argument_value, "whence", whence.ToString());
                        return false;
                }

                readOffset = writeOffset = absoluteOffset;

                // No data should be buffered when seeking the underlying stream!
                Debug.Assert(readBuffers == null);
                Debug.Assert(writeBuffer == null || writePosition == 0);
                readPosition = writePosition = 0;

                // EX: This is inaccurate, but there is no better information avalable (w/o processing the whole stream)
                readFilteredCount = readOffset;
                writeFilteredCount = readOffset;

                return absoluteOffset == expectedOffset;
                // Seek is successful if the two values match.
            }
            catch (System.Exception)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Seek");
                return false;
            }
        }

        /// <summary>
        /// Gets the current position in the stream.
        /// </summary>
        /// <remarks>
        /// <newpara>
        /// The problem with tell() in PHP is that although the write offset 
        /// is calculated in the raw byte stream (just before buffering)
        /// the read one is calculated in the filtered string buffers.
        /// </newpara>
        /// <newpara>
        /// In other words the value returned by tell() for output streams
        /// is the real position in the raw stream but may differ from the
        /// number of characters written. On the other hand the value returned for
        /// input streams corresponds with the number of characters retreived 
        /// but not with the position in the raw stream. It is important
        /// to remember that seeking on a filtered stream (such as a file
        /// opened with a "rt" mode) has undefined behavior.
        /// </newpara>
        /// </remarks>
        /// <returns>The position in the filtered or raw stream depending on last 
        /// read or write access type respectively or -1 if the stream does not support seeking.</returns>
        public int Tell()
        {
            if (!CanSeek)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Seek");
                return -1;
            }
            switch (currentAccess)
            {
                default:
                    // Stream not yet R/W accessed (but maybe with Seek).
                    return readOffset;
                case FileAccess.Read:
                    return ReadPosition;
                case FileAccess.Write:
                    return WritePosition;
            }
        }

        #endregion

        #region Conversions

        /// <exception cref="InvalidCastException">When casting is not supported.</exception>
        public virtual Stream RawStream
        {
            get
            {
                throw new InvalidCastException(ErrResources.casting_to_stream_unsupported);
            }
        }

        /// <summary>
        /// Check that the resource handle contains a valid
        /// PhpStream resource and cast the handle to PhpStream.
        /// </summary>
        /// <param name="handle">A PhpResource passed to the PHP function.</param>
        /// <returns>The handle cast to PhpStream.</returns>
        public static PhpStream GetValid(PhpResource handle)
        {
            if (handle is PhpStream result && result.IsValid)
            {
                return result;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, ErrResources.invalid_stream_resource);
                return null;
            }
        }

        public static PhpStream GetValid(PhpResource handle, FileAccess desiredAccess)
        {
            PhpStream result = GetValid(handle);

            if (result != null)
            {
                if ((desiredAccess & FileAccess.Write) != 0 && !result.CanWrite)
                {
                    PhpException.Throw(PhpError.Warning, ErrResources.stream_write_off);
                    return null;
                }

                if ((desiredAccess & FileAccess.Read) != 0 && !result.CanRead)
                {
                    PhpException.Throw(PhpError.Warning, ErrResources.stream_read_off);
                    return null;
                }
            }

            return result;
        }

        #endregion

        #region Stream properties

        /// <summary>
        /// The stream context options resource.
        /// </summary>
        public StreamContext Context
        {
            get { return _context; }
        }

        /// <summary>
        /// The stream context options resource.
        /// </summary>
        protected StreamContext _context;

        /// <summary>
        /// Runtime string encoding.
        /// </summary>
        protected readonly IEncodingProvider _encoding;

        /// <summary>
        /// Gets the Auto-remove option of this stream.
        /// </summary>
        public bool IsTemporary
        {
            get
            {
                return (Options & StreamAccessOptions.Temporary) != 0;
            }
        }

        /// <summary>
        /// Gets or sets the read fragmentation behavior.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Network and console input streams return immediately after a nonempty data is read from the underlying stream.
        /// Buffered streams try to fill the whole given buffer while the underlying stream is providing data
        /// to satisfy the caller-specified length or <see cref="readChunkSize"/>.
        /// </para>
        /// <para>
        /// Still the input buffer may contain valid data even for unbuffered streams.
        /// This may happen for example when a <c>fgets</c> has to return unconsumed data
        /// (following the first <c>EOL</c>) back to the stream.
        /// </para>
        /// </remarks>
        public bool IsReadBuffered
        {
            get
            {
                return isReadBuffered;
            }
            set
            {
                isReadBuffered = value;
            }
        }

        /// <summary>
        /// Gets the write fragmentation behavior.
        /// </summary>
        /// <remarks>
        /// When the write is not buffered then all the fwrite calls
        /// pass the data immediately to the underlying stream.
        /// </remarks>
        public bool IsWriteBuffered
        {
            get
            {
                return writeBufferSize > 0;
            }
            set
            {
                if (value) writeBufferSize = DefaultBufferSize;
                else writeBufferSize = 0;
            }
        }

        /// <summary>
        /// Gets the filtering status of this stream. 
        /// <c>true</c> when there is at least one input filter on the stream.
        /// </summary>
        protected bool IsReadFiltered
        {
            get
            {
                return (((readFilters != null) && (readFilters.Count != 0))
                    || (textReadFilter != null));
            }
        }

        /// <summary>
        /// Gets the filtering status of this stream. 
        /// <c>true</c> when there is at least one output filter on the stream.
        /// </summary>
        protected bool IsWriteFiltered
        {
            get
            {
                return (((writeFilters != null) && (writeFilters.Count != 0))
                    || (textWriteFilter != null));
            }
        }

        /// <summary>Gets or sets the current Read/Write access mode.</summary>
        protected FileAccess CurrentAccess
        {
            get
            {
                return currentAccess;
            }
            set
            {
                switch (value)
                {
                    case FileAccess.Read:
                        if (!CanRead)
                        {
                            PhpException.Throw(PhpError.Warning, ErrResources.stream_read_off);
                            break;
                        }
                        if ((currentAccess == FileAccess.Write) && CanSeek)
                        {
                            // Flush the write buffers, switch to reading at the write position
                            int offset = Tell();
                            FlushWriteBuffer();
                            writeOffset = writePosition = 0;
                            currentAccess = value;
                            Seek(offset, SeekOrigin.Begin);
                        }
                        currentAccess = value;
                        break;

                    case FileAccess.Write:
                        if (!CanWrite)
                        {
                            PhpException.Throw(PhpError.Warning, ErrResources.stream_write_off);
                            break;
                        }
                        if ((currentAccess == FileAccess.Read) && CanSeek)
                        {
                            // Drop the read buffers, switch to writing at the read position
                            int offset = Tell();
                            //DropReadBuffer();
                            readBuffers = null;
                            readOffset = readPosition = 0;
                            currentAccess = value;
                            Seek(offset, SeekOrigin.Begin);
                        }
                        currentAccess = value;
                        break;

                    default:
                        throw new ArgumentException();
                }
            }

            // CHECKME: [CurrentAccess]
        }

        /// <summary>Gets the writing pointer position in the buffered stream.</summary>
        public int WritePosition
        {
            get
            {
                if (CurrentAccess != FileAccess.Write) return -1;

                // Data passed via filters to output buffers (not filtered yet!)
                return writeFilteredCount;
                //try
                //{
                //  return RawTell() + this.writePosition;
                //}
                //catch (Exception)
                //{
                //  return this.writeOffset + this.writePosition;
                //}
            }
        }

        /// <summary>Gets the reading pointer position in the buffered stream.</summary>
        public int ReadPosition
        {
            get
            {
                if (CurrentAccess != FileAccess.Read) return -1;

                // Data physically read - data still in buffers
                return readFilteredCount - ReadBufferLength;
                //try
                //{
                //  return RawTell() - ReadBufferLength;
                //  // The position in the stream minus the data remaining in the buffers
                //}
                //catch (Exception)
                //{
                //  return this.readOffset + this.readPosition;
                //}
            }
        }

        /// <summary>The lists of StreamFilters associated with this stream.</summary>
        protected List<IFilter> readFilters = null, writeFilters = null;

        /// <summary>The text-mode conversion filter of this stream used for reading.</summary>
        protected IFilter textReadFilter = null;

        /// <summary>The text-mode conversion filter of this stream used for writing.</summary>
        protected IFilter textWriteFilter = null;

        /// <summary>
        /// The StreamWrapper responsible for opening this stream.
        /// </summary>
        /// <remarks>
        /// Used for example to access the correct section of context
        /// and for wrapper-notifications too.
        /// </remarks>
        public readonly StreamWrapper Wrapper;

        /// <summary>
        /// PHP wrapper specific data. See wrapper_data array item.
        /// Can be <c>null</c>.
        /// </summary>
        public object WrapperSpecificData
        {
            get;
            internal set;
        }

        /// <summary>
        /// The absolute path to the resource.
        /// </summary>
        public readonly string OpenedPath;

        /// <summary>
        /// <c>true</c> if the stream was opened for writing.
        /// </summary>
        public bool CanWrite
        {
            get { return (Options & StreamAccessOptions.Write) > 0; }
        }

        /// <summary>
        /// <c>true</c> if the stream was opened for reading.
        /// </summary>
        public bool CanRead
        {
            get { return (Options & StreamAccessOptions.Read) > 0; }
        }

        /// <summary>
        /// <c>true</c> if the stream was opened in the text access-mode.
        /// </summary>
        public bool IsText
        {
            get { return (Options & StreamAccessOptions.UseText) > 0; }
        }

        /// <summary>
        /// <c>true</c> if the stream was opened in the binary access-mode.
        /// </summary>
        public bool IsBinary
        {
            get { return (Options & StreamAccessOptions.UseText) == 0; }
        }

        /// <summary>
        /// <c>true</c> if the stream persists accross multiple scripts.
        /// </summary>
        public bool IsPersistent
        {
            get { return (Options & StreamAccessOptions.Persistent) != 0; }
        }

        /// <summary>
        /// Additional stream options defined at open-time.
        /// </summary>
        public readonly StreamAccessOptions Options;

        /// <summary>
        /// Gets the type of last stream access (initialized to FileAccess.ReadWrite if not accessed yet).
        /// </summary>
        protected FileAccess currentAccess = FileAccess.ReadWrite;

        /// <summary>
        /// For <c>fgetss()</c> to handle multiline tags.
        /// </summary>
        public int StripTagsState
        {
            get { return fgetssState; }
            set { fgetssState = value; }
        }

        /// <summary>For <c>fgetss()</c> to handle multiline tags.</summary>
        protected int fgetssState = 0;

        /// <summary>For future use. Persistent streams are not implemented so far.</summary>
        protected bool isPersistent = false;

        /// <summary>The default size of read/write buffers.</summary>
        public const int DefaultBufferSize = 8 * 1024;

        /// <summary>The default size of a single read chunk in the readBuffers.</summary>
        protected int readChunkSize = DefaultBufferSize;

        /// <summary>Whether the read operations are interated for a single <c>fread</c> call.</summary>
        protected bool isReadBuffered = true;

        /// <summary>The maximum count of buffered output bytes. <c>0</c> to disable buffering.</summary>
        protected int writeBufferSize = DefaultBufferSize;

        /// <summary>Store the filtered input data queued as <see cref="TextElement"/>s (either a <see cref="string"/> or <see cref="byte"/>[]).</summary>
        protected Queue<TextElement> readBuffers = null;

        /// <summary>Store the filtered output data in a <c>byte[]</c> up to <see cref="writeBufferSize"/> bytes.</summary>
        protected byte[] writeBuffer = null;

        /// <summary>The offset from the beginning of the raw stream to the
        /// first byte stored in the <see cref="readBuffers"/>.</summary>
        /// <remarks>This offset is incremented when a consumed buffer is dropped.</remarks>
        protected int readOffset = 0;

        /// <summary>
        /// The offset from the beginning of the raw stream to the
        /// first byte of the <see cref="writeBuffer"/>.
        /// </summary>
        /// <remarks>
        /// This offset is incremented when the buffer is being flushed
        /// or the data is written to a non-buffered stream.
        /// </remarks>
        protected int writeOffset = 0;

        /// <summary>The position in the first buffer in the <see cref="readBuffers"/>.</summary>
        protected int readPosition = 0;

        /// <summary>Total bytes passed through the ReadData function (after input filtering)</summary>
        protected int readFilteredCount = 0;

        /// <summary>Total bytes passed through the WriteData function (before output filtering)</summary>
        protected int writeFilteredCount = 0;

        /// <summary>The actual write position in the <see cref="writeBuffer"/>.</summary>
        protected int writePosition = 0;

        /// <summary>Timeout for network-based streams in seconds.</summary>
        protected double readTimeout = 0;

        /// <summary>
        /// The type name displayed when printing a variable of type PhpStream.
        /// </summary>
        public const string PhpStreamTypeName = "stream";

        #endregion

        #region Stat (optional)

        public virtual StatStruct Stat()
        {
            return (this.Wrapper != null)
                ? this.Wrapper.Stat(OpenedPath, StreamStatOptions.Empty, StreamContext.Default, true)
                : StreamWrapper.StatUnsupported();
        }

        #endregion
    }
}
