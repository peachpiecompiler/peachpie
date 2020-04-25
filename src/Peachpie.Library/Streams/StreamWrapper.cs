using Pchp.Core;
using Pchp.Core.Resources;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core.Reflection;
using System.Text;
using System.Net;

namespace Pchp.Library.Streams
{
    /// <summary>
    /// Abstract base class for PHP stream wrappers. Descendants define 
    /// methods implementing fopen, stat, unlink, rename, opendir, mkdir and rmdir 
    /// for different stream types.
    /// </summary>
    /// <remarks>
    /// Each script has its own copy of registeredWrappers stored in the context.
    /// <para>
    /// PhpStream is created by a StreamWrapper on a call to fopen().
    /// Wrappers are stateless: they provide an instance of PhpStream
    /// on fopen() and an instance of DirectoryListing on opendir().
    /// </para>
    /// </remarks>
    public abstract class StreamWrapper : Context.IStreamWrapper, IDisposable
    {
        #region ContextData

        /// <summary>
        /// State stored within a runtime <see cref="Context"/>.
        /// </summary>
        sealed class ContextData
        {
            public static ContextData GetData(Context ctx) => ctx.GetStatic<ContextData>();

            public Dictionary<string, StreamWrapper> EnsureUserWrappers()
            {
                return UserWrappers ?? (UserWrappers = new Dictionary<string, StreamWrapper>());
            }

            public Dictionary<string, StreamWrapper> UserWrappers { get; private set; }
        }

        #endregion

        #region Mandatory Wrapper Operations

        public abstract PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context);

        public abstract string Label { get; }

        /// <summary>
        /// Stream scheme/protocol.
        /// This value must not be empty. Casing is ignored. Common values are <c>file</c>, <c>http</c>, <c>php</c>, <c>phar</c>.
        /// </summary>
        public abstract string Scheme { get; }

        public abstract bool IsUrl { get; }

        #endregion

        #region Optional Wrapper Operations (Warning)

        /// <remarks>
        /// <seealso cref="StreamUnlinkOptions"/> for the list of additional options.
        /// </remarks>
        public virtual bool Unlink(string path, StreamUnlinkOptions options, StreamContext context)
        {
            // int (*unlink)(php_stream_wrapper *wrapper, char *url, int options, php_stream_context *context TSRMLS_DC); 
            PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Unlink");
            return false;
        }

        /// <summary>
        /// Gets enumeration of entries within given path (directory).
        /// The method can return a <c>null</c> reference.
        /// The method throws PHP warning in case of not supported operation or insufficient permissions.
        /// </summary>
        public virtual IEnumerable<string> Listing(string root, string path, StreamListingOptions options, StreamContext context)
        {
            // php_stream *(*dir_opener)(php_stream_wrapper *wrapper, char *filename, char *mode, int options, char **opened_path, php_stream_context *context STREAMS_DC TSRMLS_DC);
            PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Opendir");
            return null;
        }

        public virtual bool Rename(string fromPath, string toPath, StreamRenameOptions options, StreamContext context)
        {
            // int (*rename)(php_stream_wrapper *wrapper, char *url_from, char *url_to, int options, php_stream_context *context TSRMLS_DC);
            PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Rename");
            return false;
        }

        /// <remarks><seealso cref="StreamMakeDirectoryOptions"/> for the list of additional options.</remarks>
        public virtual bool MakeDirectory(string path, int accessMode, StreamMakeDirectoryOptions options, StreamContext context)
        {
            // int (*stream_mkdir)(php_stream_wrapper *wrapper, char *url, int mode, int options, php_stream_context *context TSRMLS_DC);
            PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Mkdir");
            return false;
        }

        public virtual bool RemoveDirectory(string path, StreamRemoveDirectoryOptions options, StreamContext context)
        {
            // int (*stream_rmdir)(php_stream_wrapper *wrapper, char *url, int options, php_stream_context *context TSRMLS_DC);    
            PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Rmdir");
            return false;
        }

        public virtual StatStruct Stat(string path, StreamStatOptions options, StreamContext context, bool streamStat)
        {
            // int (*url_stat)(php_stream_wrapper *wrapper, char *url, int flags, php_stream_statbuf *ssb, php_stream_context *context TSRMLS_DC);
            return StatUnsupported();
        }

        /// <summary>
        /// Reports warning and creates invalid stat.
        /// </summary>
        internal static StatStruct StatUnsupported()
        {
            // int (*url_stat)(php_stream_wrapper *wrapper, char *url, int flags, php_stream_statbuf *ssb, php_stream_context *context TSRMLS_DC);
            PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Stat");
            return StatStruct.Invalid;
        }

        #endregion

        #region Optional Wrapper Methods (Empty)

        /// <summary>
        /// Wrapper may be notified of closing a stream using this method.
        /// </summary>
        public virtual void OnClose(PhpStream stream) { }

        // int (*stream_closer)(php_stream_wrapper *wrapper, php_stream *stream TSRMLS_DC);

        /// <summary>
        /// Wrapper may override the <c>stat()</c>ing of a stream using this method.
        /// </summary>
        /// <param name="stream">The Wrapper-opened stream to be <c>stat()</c>ed.</param>
        /// <returns></returns>
        public virtual PhpArray OnStat(PhpStream stream) { return null; }

        /// <summary>
        /// Tries to resolve compiled script at given path.
        /// </summary>
        public virtual bool ResolveInclude(Context ctx, string cd, string path, out Context.ScriptInfo script)
        {
            script = default;
            return false;
        }

        #endregion

        #region Helper methods (ParseMode, FileSystemUtils.StripPassword)

        /// <summary>
        /// Parse the <paramref name="mode"/> argument passed to <c>fopen()</c>
        /// and make the appropriate <see cref="FileMode"/> and <see cref="FileAccess"/>
        /// combination.
        /// Integrate the relevant options from <see cref="StreamOpenOptions"/> too.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="mode">Mode as passed to <c>fopen()</c>.</param>
        /// <param name="options">The <see cref="StreamOpenOptions"/> passed to <c>fopen()</c>.</param>
        /// <param name="fileMode">Resulting <see cref="FileMode"/> specifying opening mode.</param>
        /// <param name="fileAccess">Resulting <see cref="FileAccess"/> specifying read/write access options.</param>
        /// <param name="accessOptions">Resulting <see cref="StreamAccessOptions"/> giving 
        /// additional information to the stream opener.</param>
        /// <returns><c>true</c> if the given mode was a valid file opening mode, otherwise <c>false</c>.</returns>
        public bool ParseMode(Context ctx, string mode, StreamOpenOptions options, out FileMode fileMode, out FileAccess fileAccess, out StreamAccessOptions accessOptions)
        {
            accessOptions = StreamAccessOptions.Empty;
            bool forceBinary = false; // The user requested a text stream
            bool forceText = false; // Use text access to the stream (default is binary)

            // First check for relevant options in StreamOpenOptions:

            // Search for the file only if mode=='[ra]*' and use_include_path==true.
            // StreamAccessOptions findFile = 0;
            if ((options & StreamOpenOptions.UseIncludePath) > 0)
            {
                // findFile = StreamAccessOptions.FindFile;
                accessOptions |= StreamAccessOptions.FindFile;
            }

            // Copy the AutoRemove option.
            if ((options & StreamOpenOptions.Temporary) > 0)
            {
                accessOptions |= StreamAccessOptions.Temporary;
            }

            // Now do the actual mode parsing:
            fileMode = FileMode.Open;
            fileAccess = FileAccess.Write;
            if (String.IsNullOrEmpty(mode))
            {
                PhpException.Throw(PhpError.Warning, ErrResources.empty_file_mode);
                return false;
            }

            switch (mode[0])
            {
                case 'r':
                    // flags = 0;
                    // fileMode is already set to Open
                    fileAccess = (mode.Length > 1 && mode[1] == 'w')
                        ? FileAccess.ReadWrite  // rw
                        : FileAccess.Read;      // r
                    //accessOptions |= findFile;

                    break;

                case 'w':
                    // flags = O_TRUNC|O_CREAT;
                    // fileAccess is set to Write
                    fileMode = FileMode.Create;
                    //accessOptions |= findFile;
                    // EX: Note that use_include_path is applicable to all access methods.
                    // Create truncates the existing file to zero length
                    break;

                case 'a':
                    // flags = O_CREAT|O_APPEND;
                    // fileAccess is set to Write
                    fileMode = FileMode.Append;
                    //accessOptions |= findFile;
                    // Note: .NET does not support the "a+" mode, use "r+" and Seek()
                    break;

                case 'x':
                    // flags = O_CREAT|O_EXCL;
                    // fileAccess is set to Write
                    fileMode = FileMode.CreateNew;
                    accessOptions |= StreamAccessOptions.Exclusive;
                    break;

                case 'c':
                    // flags = O_CREAT;
                    fileMode = FileMode.OpenOrCreate;
                    fileAccess = FileAccess.Write;
                    break;

                default:
                    PhpException.Throw(PhpError.Warning, ErrResources.invalid_file_mode, mode);
                    return false;
            }

            if (mode.IndexOf('+') > -1)
            {
                // flags |= O_RDWR;
                fileAccess = FileAccess.ReadWrite;
            }

            if ((fileMode == FileMode.Append) && (fileAccess == FileAccess.ReadWrite))
            {
                // Note: .NET does not support the "a+" mode, use "r+" and Seek()
                fileMode = FileMode.OpenOrCreate;
                fileAccess = FileAccess.ReadWrite;
                accessOptions |= StreamAccessOptions.SeekEnd;
            }

            if (mode.IndexOf('b') > -1)
            {
                // flags |= O_BINARY;
                forceBinary = true;
            }
            if (mode.IndexOf('t') > -1)
            {
                // flags |= _O_TEXT;
                forceText = true;
            }

            // Exactly one of these options is required.
            if ((forceBinary && forceText) || (!forceBinary && !forceText))
            {
                //var cconfig = ctx.Configuration.Core;

                //// checks whether default mode is applicable:
                //if (cconfig.DefaultFileOpenMode == "b")
                //{
                //    forceBinary = true;
                //}
                //else if (cconfig.DefaultFileOpenMode == "t")
                //{
                //    forceText = true;
                //}
                //else
                //{
                //    PhpException.Throw(PhpError.Warning, ErrResources.ambiguous_file_mode, mode);
                //}

                // Binary mode is assumed
            }
            else if (forceText)
            {
                // Default mode is binary (unless the text mode is specified).
                accessOptions |= StreamAccessOptions.UseText;
            }

            // Store the two file-access flags into the access options too.
            accessOptions |= (StreamAccessOptions)fileAccess;

            return true;
        }

        /// <summary>
        /// Overload of <see cref="ParseMode(string, StreamOpenOptions, out FileMode, out FileAccess, out StreamAccessOptions)"/> without the <c>out</c> arguments.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="mode">Mode as passed to <c>fopen()</c>.</param>
        /// <param name="options">The <see cref="StreamOpenOptions"/> passed to <c>fopen()</c>.</param>
        /// <param name="accessOptions">Resulting <see cref="StreamAccessOptions"/> giving 
        /// additional information to the stream opener.</param>
        /// <returns><c>true</c> if the given mode was a valid file opening mode, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">If the <paramref name="mode"/> is not valid.</exception>
        internal bool ParseMode(Context ctx, string mode, StreamOpenOptions options, out StreamAccessOptions accessOptions)
        {
            FileMode fileMode;
            FileAccess fileAccess;

            return ParseMode(ctx, mode, options, out fileMode, out fileAccess, out accessOptions);
        }

        /// <summary>
        /// Checks whether the supported read/write access matches the reqiured one.
        /// </summary>
        /// <param name="accessOptions">The access options specified by the user.</param>
        /// <param name="supportedAccess">The read/write access options supported by the stream.</param>
        /// <param name="path">The path given by user to report errors.</param>
        /// <returns><c>false</c> if the stream does not support any of the required modes, <c>true</c> otherwise.</returns>
        internal bool CheckOptions(StreamAccessOptions accessOptions, FileAccess supportedAccess, string path)
        {
            FileAccess requiredAccess = (FileAccess)accessOptions & FileAccess.ReadWrite;
            FileAccess faultyAccess = requiredAccess & ~supportedAccess;
            if ((faultyAccess & FileAccess.Read) > 0)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_open_read_unsupported, FileSystemUtils.StripPassword(path));
                return false;
            }
            else if ((faultyAccess & FileAccess.Write) > 0)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_open_write_unsupported, FileSystemUtils.StripPassword(path));
                return false;
            }
            return true;
        }

        #endregion

        #region Static wrapper-list handling methods

        /// <summary>
        /// Insert a new wrapper to the list of user StreamWrappers.
        /// </summary>
        /// <remarks>
        /// Each script has its own set of user StreamWrappers registered
        /// by stream_wrapper_register() stored in the ScriptContext.
        /// </remarks>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="protocol">The scheme portion of URLs this wrapper can handle.</param>
        /// <param name="wrapper">An instance of the corresponding StreamWrapper descendant.</param>
        /// <returns>True if succeeds, false if the scheme is already registered.</returns>
        public static bool RegisterUserWrapper(Context ctx, string protocol, StreamWrapper wrapper)
        {
            try
            {
                EnsureUserStreamWrappers(ctx).Add(protocol, wrapper);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Register a new system wrapper
        /// </summary>
        /// <param name="wrapper">An instance of the corresponding StreamWrapper descendant.</param>
        /// <returns>True if succeeds, false if the scheme is already registered.</returns>
        public static bool RegisterSystemWrapper(StreamWrapper wrapper)
        {
            if (Context.GetGlobalStreamWrapper(wrapper.Scheme) == null)
            {
                Context.RegisterGlobalStreamWrapper(new Lazy<Context.IStreamWrapper, string>(() => wrapper, wrapper.Scheme));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a wrapper is already registered for the given scheme.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="scheme">The scheme.</param>
        /// <returns><c>true</c> if exists.</returns>
        public static bool Exists(Context ctx, string scheme) => GetWrapperInternal(ctx, scheme) != null;

        /// <summary>
        /// Retreive the corresponding StreamWrapper respectind the scheme portion 
        /// of the given path. If no scheme is specified, an instance of 
        /// FileStreamWrapper is returned.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="scheme">The scheme portion of an URL.</param>
        /// <param name="options">Additional <see cref="StreamOptions"/> having effect on the wrapper retreival.</param>
        /// <returns>An instance of StreamWrapper to be used to open the specified file.</returns>
        /// <exception cref="PhpException">In case when the required wrapper can not be found.</exception>
        public static StreamWrapper GetWrapper(Context ctx, string scheme, StreamOptions options)
        {
            StreamWrapper wrapper = GetWrapperInternal(ctx, scheme);

            if (wrapper == null)
            {
                PhpException.Throw(PhpError.Notice, ErrResources.stream_bad_wrapper, scheme);
                // Notice:  fopen(): Unable to find the wrapper "*" - did you forget to enable it when you configured PHP? in C:\Inetpub\wwwroot\php\index.php on line 23

                wrapper = GetWrapperInternal(ctx, "file");
                // There should always be the FileStreamWrapper present.
            }

            // EX [GetWrapper]: check for the other StreamOptions here: for example UseUrl, IgnoreUrl

            if (!ctx.Configuration.Core.AllowUrlFopen)
            {
                if (wrapper.IsUrl)
                {
                    PhpException.Throw(PhpError.Warning, ErrResources.url_fopen_disabled);
                    return null;
                }
            }

            Debug.Assert(wrapper != null);
            return wrapper;
        }

        /// <summary>
        /// Gets the list of built-in stream wrapper schemes.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetSystemWrapperSchemes() => Context.GetGlobalStreamWrappers();

        /// <summary>
        /// Gets the list of user wrapper schemes.
        /// </summary>
        /// <returns></returns>
        public static ICollection<string> GetUserWrapperSchemes(Context ctx)
        {
            var data = ContextData.GetData(ctx);
            if (data.UserWrappers == null)
            {
                return Array.Empty<string>();
            }
            else
            {
                return data.UserWrappers.Keys;
            }
        }

        /// <summary>
        /// Search the lists of registered StreamWrappers to find the 
        /// appropriate wrapper for a given scheme. When the scheme
        /// is empty, the FileStreamWrapper is returned.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="scheme">The scheme portion of an URL.</param>
        /// <returns>A StreamWrapper associated with the given scheme.</returns>
        internal static StreamWrapper GetWrapperInternal(Context ctx, string scheme)
        {
            // Note: FileStreamWrapper is returned both for "file" and for "".
            if (string.IsNullOrEmpty(scheme))
            {
                scheme = FileStreamWrapper.scheme;
            }

            // First search the system wrappers (always at least an empty Hashtable)
            var wrapper = (StreamWrapper)Context.GetGlobalStreamWrapper(scheme);
            if (wrapper == null)
            {

                // Then look if the wrapper is implemented but not instantiated
                switch (scheme)
                {
                    case FileStreamWrapper.scheme:
                        RegisterSystemWrapper(wrapper = new FileStreamWrapper());
                        break;
                    case HttpStreamWrapper.scheme:
                    case HttpStreamWrapper.schemes:
                        RegisterSystemWrapper(wrapper = new HttpStreamWrapper(scheme));
                        break;
                    case InputOutputStreamWrapper.scheme:
                        RegisterSystemWrapper(wrapper = new InputOutputStreamWrapper());
                        break;
                }

                if (wrapper == null)
                {
                    // Next search the user wrappers (if present)
                    var data = ContextData.GetData(ctx);
                    if (data.UserWrappers != null)
                    {
                        data.UserWrappers.TryGetValue(scheme, out wrapper);
                    }
                }
            }

            //
            return wrapper;  // can be null
        }

        /// <summary>
        /// Make new instance of Hashtable for the userwrappers
        /// in the ScriptContext.
        /// </summary>
        private static Dictionary<string, StreamWrapper> EnsureUserStreamWrappers(Context ctx)
        {
            return ContextData.GetData(ctx).EnsureUserWrappers();
        }

        #endregion

        #region Optional Dispose

        /// <summary>
        /// Release wrapper resources
        /// </summary>
        public virtual void Dispose() { }

        #endregion
    }

    #region Local Filesystem Wrapper

    /// <summary>
    /// Derived from <see cref="StreamWrapper"/>, this class provides access to 
    /// the local filesystem files.
    /// </summary>
    /// <remarks>
    /// The virtual working directory is handled by the PhpPath class in 
    /// the Class Library. The absolute path resolution (using the working diretory and the <c>include_path</c>
    /// if necessary) and open-basedir check is performed by the <see cref="PhpStream.ResolvePath"/> method.
    /// <newpara>
    /// This wrapper expects the path to be an absolute local filesystem path
    /// without the file:// scheme specifier.
    /// </newpara>
    /// </remarks>
    public class FileStreamWrapper : StreamWrapper
    {
        /// <summary>
        /// The protocol portion of URL handled by this wrapper.
        /// </summary>
        public const string scheme = "file";

        #region Mandatory members

        public override string Label { get { return "plainfile"; } }

        public override string Scheme { get { return scheme; } }

        public override bool IsUrl { get { return false; } }

        #endregion

        #region Opening a file

        public override PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context)
        {
            Debug.Assert(path != null);
            //Debug.Assert(PhpPath.IsLocalFile(path));

            // Get the File.Open modes from the mode string
            FileMode fileMode;
            FileAccess fileAccess;
            StreamAccessOptions ao;

            if (!ParseMode(ctx, mode, options, out fileMode, out fileAccess, out ao)) return null;

            // Open the native stream
            FileStream stream = null;
            try
            {
                // stream = File.Open(path, fileMode, fileAccess, FileShare.ReadWrite);
                stream = new FileStream(path, fileMode, fileAccess, FileShare.ReadWrite | FileShare.Delete);
            }
            catch (FileNotFoundException)
            {
                // Note: There may still be an URL in the path here.
                PhpException.Throw(PhpError.Warning, ErrResources.stream_file_not_exists, FileSystemUtils.StripPassword(path));

                return null;
            }
            catch (IOException e)
            {
                if ((ao & StreamAccessOptions.Exclusive) > 0)
                {
                    PhpException.Throw(PhpError.Warning, ErrResources.stream_file_exists, FileSystemUtils.StripPassword(path));
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, ErrResources.stream_file_io_error, FileSystemUtils.StripPassword(path), PhpException.ToErrorMessage(e.Message));
                }
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_file_access_denied, FileSystemUtils.StripPassword(path));
                return null;
            }
            catch (System.Exception)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_file_invalid, FileSystemUtils.StripPassword(path));
                return null;
            }

            if ((ao & StreamAccessOptions.SeekEnd) > 0)
            {
                // Read/Write Append is not supported. Seek to the end of file manually.
                stream.Seek(0, SeekOrigin.End);
            }

            if ((ao & StreamAccessOptions.Temporary) > 0)
            {
                // Set the file attributes to Temporary too.
                File.SetAttributes(path, FileAttributes.Temporary);
            }

            return new NativeStream(ctx, stream, this, ao, path, context);
        }

        #endregion

        #region Optional Wrapper Operations Implementations

        #region Stat related methods and Stat caching

        /// <summary>
        /// Creates a <see cref="StatStruct"/> from the <see cref="StatStruct"/> filling the common
        /// members (for files and directories) from the given <see cref="FileSystemInfo"/> class.
        /// The <c>size</c> member (numeric index <c>7</c>) may be filled by the caller
        /// for when <paramref name="info"/> is a <see cref="FileInfo"/>.
        /// </summary>
        /// <remarks>
        /// According to these outputs (PHP Win32):
        /// <code>
        /// fstat(somefile.txt):
        ///    [dev] => 0
        ///    [ino] => 0
        ///    [mode] => 33206
        ///    [nlink] => 1
        ///    [uid] => 0
        ///    [gid] => 0
        ///    [rdev] => 0
        ///    [size] => 24
        ///    [atime] => 1091131360
        ///    [mtime] => 1091051699
        ///    [ctime] => 1091051677
        ///    [blksize] => -1
        ///    [blocks] => -1
        /// 
        /// stat(somefile.txt):
        ///    [dev] => 2
        ///    [ino] => 0
        ///    [mode] => 33206 // 0100666
        ///    [nlink] => 1
        ///    [uid] => 0
        ///    [gid] => 0
        ///    [rdev] => 2
        ///    [size] => 24
        ///    [atime] => 1091129621
        ///    [mtime] => 1091051699
        ///    [ctime] => 1091051677
        ///    [blksize] => -1
        ///    [blocks] => -1
        ///    
        /// stat(somedir):
        ///    [st_dev] => 2
        ///    [st_ino] => 0
        ///    [st_mode] => 16895 // 040777
        ///    [st_nlink] => 1
        ///    [st_uid] => 0
        ///    [st_gid] => 0
        ///    [st_rdev] => 2
        ///    [st_size] => 0
        ///    [st_atime] => 1091109319
        ///    [st_mtime] => 1091044521
        ///    [st_ctime] => 1091044521
        ///    [st_blksize] => -1
        ///    [st_blocks] => -1
        /// </code>
        /// </remarks>
        /// <param name="info">A <see cref="FileInfo"/> or <see cref="DirectoryInfo"/>
        /// of the <c>stat()</c>ed filesystem entry.</param>
        /// <param name="attributes">The file or directory attributes.</param>
        /// <param name="path">The path to the file / directory.</param>
        /// <returns>A <see cref="StatStruct"/> for use in the <c>stat()</c> related functions.</returns>    
        internal static StatStruct BuildStatStruct(FileSystemInfo info, FileAttributes attributes, string path)
        {
            StatStruct result;//  = new StatStruct();
            uint device = unchecked((uint)(char.ToLower(info.FullName[0]) - 'a')); // index of the disk

            ushort mode = (ushort)BuildMode(info, attributes, path);

            long atime, mtime, ctime;
            atime = ToStatUnixTimeStamp(info, (_info) => _info.LastAccessTimeUtc);
            mtime = ToStatUnixTimeStamp(info, (_info) => _info.LastWriteTimeUtc);
            ctime = ToStatUnixTimeStamp(info, (_info) => _info.CreationTimeUtc);

            result.st_dev = device;         // device number 
            result.st_ino = 0;              // inode number 
            result.st_mode = mode;          // inode protection mode 
            result.st_nlink = 1;            // number of links 
            result.st_uid = 0;              // userid of owner 
            result.st_gid = 0;              // groupid of owner 
            result.st_rdev = device;        // device type, if inode device -1
            result.st_size = 0;             // size in bytes

            FileInfo file_info = info as FileInfo;
            if (file_info != null)
            {
                result.st_size = file_info.Length;
            }

            result.st_atime = atime;        // time of last access (unix timestamp) 
            result.st_mtime = mtime;        // time of last modification (unix timestamp) 
            result.st_ctime = ctime;        // time of last change (unix timestamp) 
                                            //result.st_blksize = -1;   // blocksize of filesystem IO (-1)
                                            //result.st_blocks = -1;    // number of blocks allocated  (-1)

            return result;
        }

        /// <summary>
        /// Adjusts UTC time of a file by adding Daylight Saving Time difference.
        /// Makes file times working in the same way as in PHP and Windows Explorer.
        /// </summary>
        /// <param name="info"><see cref="FileSystemInfo"/> object reference. Used to avoid creating of closure when passing <paramref name="utcTimeFunc"/>.</param>
        /// <param name="utcTimeFunc">Function obtaining specific <see cref="DateTime"/> from given <paramref name="info"/>.</param>
        private static long ToStatUnixTimeStamp(FileSystemInfo info, Func<FileSystemInfo, System.DateTime> utcTimeFunc)
        {
            System.DateTime utcTime;

            try
            {
                utcTime = utcTimeFunc(info);
            }
            catch (ArgumentOutOfRangeException)
            {
                //On Linux this exception might be thrown if a file metadata are corrupted
                //just catch it and return 0;
                return 0;
            }

            return DateTimeUtils.UtcToUnixTimeStamp(utcTime /*+ DateTimeUtils.GetDaylightTimeDifference(utcTime, System.DateTime.UtcNow)*/);
        }

        ///// <summary>
        ///// Gets the ACL of a file and converts it into UNIX-like file mode
        ///// </summary>
        //public static FileModeFlags GetFileMode(FileInfo info)
        //{
        //    System.Security.AccessControl.AuthorizationRuleCollection acl;

        //    try
        //    {
        //        // Get the collection of authorization rules that apply to the given directory
        //        acl = info.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        //we don't want to throw this exception from getting access list
        //        return 0;
        //    }

        //    return GetFileMode(acl);
        //}

        ///// <summary>
        /////  Gets the ACL of a directory and converts it ACL into UNIX-like file mode
        ///// </summary>
        //public static FileModeFlags GetFileMode(DirectoryInfo info)
        //{
        //    System.Security.AccessControl.AuthorizationRuleCollection acl;

        //    try
        //    {
        //        // Get the collection of authorization rules that apply to the given directory
        //        acl = info.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        //we don't want to throw this exception from getting access list
        //        return 0;
        //    }

        //    return GetFileMode(acl);

        //}

        ///// <summary>
        ///// Converts ACL into UNIX-like file mode
        ///// </summary>
        //private static FileModeFlags GetFileMode(System.Security.AccessControl.AuthorizationRuleCollection rules)
        //{
        //    WindowsIdentity user = System.Security.Principal.WindowsIdentity.GetCurrent();
        //    WindowsPrincipal principal = new WindowsPrincipal(user);
        //    FileModeFlags result;

        //    // These are set to true if either the allow read or deny read access rights are set
        //    bool allowRead = false;
        //    bool denyRead = false;
        //    bool allowWrite = false;
        //    bool denyWrite = false;
        //    bool allowExecute = false;
        //    bool denyExecute = false;


        //    foreach (FileSystemAccessRule currentRule in rules)
        //    {
        //        // If the current rule applies to the current user
        //        if (user.User.Equals(currentRule.IdentityReference) || principal.IsInRole((SecurityIdentifier)currentRule.IdentityReference))
        //        {
        //            switch (currentRule.AccessControlType)
        //            {
        //                case AccessControlType.Deny:

        //                    denyRead |= (currentRule.FileSystemRights & FileSystemRights.ListDirectory | FileSystemRights.Read) != 0;
        //                    denyWrite |= (currentRule.FileSystemRights & FileSystemRights.Write) != 0;
        //                    denyExecute |= (currentRule.FileSystemRights & FileSystemRights.ExecuteFile) != 0;

        //                    break;

        //                case AccessControlType.Allow:

        //                    allowRead |= (currentRule.FileSystemRights & FileSystemRights.ListDirectory | FileSystemRights.Read) != 0;
        //                    allowWrite |= (currentRule.FileSystemRights & FileSystemRights.Write) != 0;
        //                    allowExecute |= (currentRule.FileSystemRights & FileSystemRights.ExecuteFile) != 0;

        //                    break;
        //            }
        //        }
        //    }

        //    result = (allowRead & !denyRead) ? FileModeFlags.Read : 0;
        //    result |= (allowWrite & !denyWrite) ? FileModeFlags.Write : 0;
        //    result |= (allowExecute & !denyExecute) ? FileModeFlags.Execute : 0;

        //    return result;
        //}

        /// <summary>
        /// Creates the UNIX-like file mode depending on the file or directory attributes.
        /// </summary>
        /// <param name="info">Information about file system object.</param>
        /// <param name="attributes">Attributes of the file.</param>
        /// <param name="path">Paths to the file.</param>
        /// <returns>UNIX-like file mode.</returns>
        private static FileModeFlags BuildMode(FileSystemInfo/*!*/info, FileAttributes attributes, string path)
        {
            // Simulates the UNIX file mode.
            FileModeFlags rv;

            if ((attributes & FileAttributes.Directory) != 0)
            {
                // a directory:
                rv = FileModeFlags.Directory;

                //if (EnvironmentUtils.IsDotNetFramework)
                //{
                //    rv |= GetFileMode((DirectoryInfo)info);

                //    // PHP on Windows always shows that directory isn't executable
                //    rv &= ~FileModeFlags.Execute;
                //}
                //else
                {
                    rv |= FileModeFlags.Read | FileModeFlags.Execute | FileModeFlags.Write;
                }
            }
            else
            {
                // a file:
                rv = FileModeFlags.File;

                //if (EnvironmentUtils.IsDotNetFramework)
                //{
                //    rv |= GetFileMode((FileInfo)info);

                //    if ((attributes & FileAttributes.ReadOnly) != 0 && (rv & FileModeFlags.Write) != 0)
                //        rv &= ~FileModeFlags.Write;

                //    if ((rv & FileModeFlags.Execute) == 0)
                //    {
                //        // PHP on Windows checks the file internaly wheather it is executable
                //        // we just look on the extension

                //        string ext = Path.GetExtension(path);
                //        if ((ext.EqualsOrdinalIgnoreCase(".exe")) || (ext.EqualsOrdinalIgnoreCase(".com")) || (ext.EqualsOrdinalIgnoreCase(".bat")))
                //            rv |= FileModeFlags.Execute;
                //    }
                //}
                //else
                {
                    rv |= FileModeFlags.Read; // | FileModeFlags.Execute;

                    if ((attributes & FileAttributes.ReadOnly) == 0)
                        rv |= FileModeFlags.Write;
                }
            }

            //
            return rv;
        }

        public override StatStruct Stat(string path, StreamStatOptions options, StreamContext context, bool streamStat)
        {
            Debug.Assert(path != null);

            // TODO: no cache here

            // Note: path is already absolute w/o the scheme, the permissions have already been checked.
            return PhpPath.HandleFileSystemInfo(StatStruct.Invalid, path, (p) =>
            {
                FileSystemInfo info = null;

                info = new DirectoryInfo(p);
                if (!info.Exists)
                {
                    info = new FileInfo(p);
                    if (!info.Exists)
                    {
                        return StatStruct.Invalid;
                    }
                }

                return BuildStatStruct(info, info.Attributes, p);
            });
        }

        #endregion

        public override bool Unlink(string path, StreamUnlinkOptions options, StreamContext context)
        {
            Debug.Assert(path != null);
            Debug.Assert(Path.IsPathRooted(path));

            try
            {
                File.Delete(path);
                return true;
            }
            catch (DirectoryNotFoundException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_unlink_file_not_found, FileSystemUtils.StripPassword(path));
            }
            catch (UnauthorizedAccessException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_file_access_denied, FileSystemUtils.StripPassword(path));
            }
            catch (IOException e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_unlink_io_error, FileSystemUtils.StripPassword(path), PhpException.ToErrorMessage(e.Message));
            }
            catch (System.Exception)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_unlink_error, FileSystemUtils.StripPassword(path));
            }

            return false;
        }

        static readonly string[] s_default_files = new[] { ".", ".." };
        static readonly Func<Context.ScriptInfo, string> s_script_fname_func = new Func<Context.ScriptInfo, string>(s => Path.GetFileName(s.Path));

        public override IEnumerable<string> Listing(string root, string path, StreamListingOptions options, StreamContext context)
        {
            Debug.Assert(path != null);
            Debug.Assert(Path.IsPathRooted(path));

            var isCompiledDir = Context.TryGetScriptsInDirectory(root, path, out var scripts);
            string[] listing = Array.Empty<string>();

            try
            {
                listing = System.IO.Directory.GetFileSystemEntries(path);
            }
            catch (DirectoryNotFoundException)
            {
                if (!isCompiledDir)
                    PhpException.Throw(PhpError.Warning, ErrResources.stream_bad_directory, FileSystemUtils.StripPassword(path));
            }
            catch (UnauthorizedAccessException)
            {
                if (!isCompiledDir)
                    PhpException.Throw(PhpError.Warning, ErrResources.stream_file_access_denied, FileSystemUtils.StripPassword(path));
            }
            catch (System.Exception e)
            {
                if (!isCompiledDir)
                    PhpException.Throw(PhpError.Warning, ErrResources.stream_error, FileSystemUtils.StripPassword(path), e.Message);
            }

            // entries (files and directories) in the file system directory:
            IEnumerable<string> result = listing.Select(Path.GetFileName);

            if (isCompiledDir)
            {
                // merge with compiled scripts:
                result = result
                    .Concat(scripts.Select(s_script_fname_func))
                    .Distinct(CurrentPlatform.PathComparer);
            }

            if (Path.GetPathRoot(path) != path) // => is not root path
            {
                // .
                // ..
                result = s_default_files.Concat(result);
            }

            return result;
        }

        public override bool Rename(string fromPath, string toPath, StreamRenameOptions options, StreamContext context)
        {
            try
            {
                File.Move(fromPath, toPath);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_file_access_denied, FileSystemUtils.StripPassword(fromPath));
            }
            catch (IOException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_rename_file_exists, FileSystemUtils.StripPassword(fromPath), FileSystemUtils.StripPassword(toPath));
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_error, FileSystemUtils.StripPassword(fromPath), e.Message);
            }
            return false;
        }

        public override bool MakeDirectory(string path, int accessMode, StreamMakeDirectoryOptions options, StreamContext context)
        {
            if ((path == null) || (path == string.Empty))
            {
                PhpException.Throw(PhpError.Warning, ErrResources.path_argument_empty);
                return false;
            }

            try
            {
                // Default Framework MakeDirectory is RECURSIVE, check for other intention. 
                if ((options & StreamMakeDirectoryOptions.Recursive) == 0)
                {
                    int pos = path.Length - 1;
                    if (path[pos] == Path.DirectorySeparatorChar) pos--;
                    pos = path.LastIndexOf(Path.DirectorySeparatorChar, pos);
                    if (pos <= 0)
                    {
                        PhpException.Throw(PhpError.Warning, ErrResources.stream_directory_make_root, FileSystemUtils.StripPassword(path));
                        return false;
                    }

                    // Parent must exist if not recursive.
                    string parent = path.Substring(0, pos);
                    if (!System.IO.Directory.Exists(parent))
                    {
                        PhpException.Throw(PhpError.Warning, ErrResources.stream_directory_make_parent, FileSystemUtils.StripPassword(path));
                        return false;
                    }
                }

                // Creates the whole path
                System.IO.Directory.CreateDirectory(path);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // The caller does not have the required permission.
                PhpException.Throw(PhpError.Warning, ErrResources.stream_directory_access_denied, FileSystemUtils.StripPassword(path));
            }
            catch (IOException)
            {
                // The directory specified by path is read-only or is not empty.
                PhpException.Throw(PhpError.Warning, ErrResources.stream_directory_error, FileSystemUtils.StripPassword(path));
            }
            catch (System.Exception e)
            {
                // The specified path is invalid, such as being on an unmapped drive ...
                PhpException.Throw(PhpError.Warning, ErrResources.stream_error, FileSystemUtils.StripPassword(path), e.Message);
            }
            return false;
        }

        public override bool RemoveDirectory(string path, StreamRemoveDirectoryOptions options, StreamContext context)
        {
            try
            {
                // Deletes the directory (but not the contents - must be empty)
                System.IO.Directory.Delete(path, false);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_file_access_denied, FileSystemUtils.StripPassword(path));
            }
            catch (IOException)
            {
                // Directory not empty.
                PhpException.Throw(PhpError.Warning, ErrResources.stream_rmdir_io_error, FileSystemUtils.StripPassword(path));
            }
            return false;
        }

        #endregion
    }

    #endregion

    #region HTTP Stream Wrapper

    /// <summary>
    /// Derived from <see cref="StreamWrapper"/>, this class provides access to 
    /// remote files using the http protocol.
    /// </summary>
    public class HttpStreamWrapper : StreamWrapper
    {
        public HttpStreamWrapper(string scheme)
        {
            this.Scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
        }

        #region StreamWrapper overrides

        public override PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context)
        {
            //
            // verify parameters
            //
            Debug.Assert(path != null);

            if (mode[0] != 'r')
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_open_write_unsupported, FileSystemUtils.StripPassword(path));
                return null;
            }

            StreamAccessOptions ao;
            if (!ParseMode(ctx, mode, options, out ao) || !CheckOptions(ao, FileAccess.Read, path))
            {
                return null;
            }

            try
            {
                //
                // create HTTP request
                //
                var request = WebRequest.Create(path) as HttpWebRequest;
                if (request == null)
                {
                    // Not a HTTP URL.
                    PhpException.Throw(PhpError.Warning, ErrResources.stream_url_invalid, FileSystemUtils.StripPassword(path));
                    return null;
                }

                //
                // apply stream context parameters
                //
                ApplyContext(ctx, request, context, out double dtimeout);

                //
                // get response synchronously
                //
                var response_async = request.GetResponseAsync();
                if (response_async.Wait((int)(dtimeout * 1000)))
                {
                    var httpResponse = response_async.Result;
                    var httpStream = httpResponse.GetResponseStream();

                    //
                    // create the PhpStream
                    //
                    return new NativeStream(ctx, httpStream, this, ao, path, context)
                    {
                        WrapperSpecificData = CreateWrapperData((HttpWebResponse)httpResponse)
                    };
                }
                else
                {
                    // timeout:
                    PhpException.Throw(PhpError.Warning, ErrResources.stream_error, "timeout"); // TODO: correct error message
                    return null;

                }

                // EX: check for StreamAccessOptions.Exclusive (N/A)
            }
            catch (UriFormatException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_url_invalid, FileSystemUtils.StripPassword(path));
            }
            catch (NotSupportedException)
            {
                // "Any attempt is made to access the method, when the method is not overridden in a descendant class."
                PhpException.Throw(PhpError.Warning, ErrResources.stream_url_method_invalid, FileSystemUtils.StripPassword(path));
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_error, FileSystemUtils.StripPassword(path), e.Message);
            }

            return null;
        }

        /// <summary>
        /// Init the parameters of the HttpWebRequest, use the StreamCOntext and/or default values.
        /// </summary>
        static void ApplyContext(Context ctx, HttpWebRequest request, StreamContext context, out double dtimeout)
        {
            var config = ctx.Configuration.Core;
            var options = context.GetOptions(scheme) ?? PhpArray.Empty;

            //
            // timeout.
            //
            var timeout = options["timeout"];
            dtimeout = (!timeout.IsEmpty) ? timeout.ToDouble() : config.DefaultSocketTimeout;
            // request.ReadWriteTimeout = (int)(dtimeout * 1000); // to be used by Task.Wait

            ////
            //// TODO: max_redirects
            ////
            //var max_redirects = context.GetOption(scheme, "max_redirects");
            //var imax_redirects = (!max_redirects.IsEmpty) ? max_redirects.ToLong() : 20;// default: 20
            //if (imax_redirects > 1)
            //    request.MaximumAutomaticRedirections = imax_redirects;
            //else
            //    request.AllowAutoRedirect = false;

            ////
            //// TODO: protocol_version
            ////
            //var protocol_version = context.GetOption(scheme, "protocol_version");
            //double dprotocol_version = (!protocol_version.IsEmpty) ? protocol_version.ToDouble() : 1.0;// default: 1.0
            //request.ProtocolVersion = new Version(dprotocol_version.ToString("F01", System.Globalization.CultureInfo.InvariantCulture));

            //
            // method - GET, POST, or any other HTTP method supported by the remote server.
            //
            var method = options["method"].AsString();
            if (method != null)
            {
                request.Method = method;
            }

            //
            // user_agent - Value to send with User-Agent: header. This value will only be used if user-agent is not specified in the header context option above.  php.ini setting: user_agent  
            //
            request.Headers["User-Agent"] = options["user_agent"].AsString() ?? config.UserAgent;

            // TODO: proxy - URI specifying address of proxy server. (e.g. tcp://proxy.example.com:5100 ).    
            // TODO: request_fulluri - When set to TRUE, the entire URI will be used when constructing the request. (i.e. GET http://www.example.com/path/to/file.html HTTP/1.0). While this is a non-standard request format, some proxy servers require it.  FALSE 
            // TODO: ssl -> array(verify_peer,verify_host)
            //
            // header - Additional headers to be sent during request. Values in this option will override other values (such as User-agent:, Host:, and Authentication:).    
            //
            var header = options["header"].AsString();
            if (header != null)
            {
                // EX: Use the individual headers, respect the system restricted-header list?
                var lines = new StringReader(header);
                string line;
                while ((line = lines.ReadLine()) != null)
                {
                    int separator = line.IndexOf(':');
                    if (separator <= 0) continue;

                    // TODO: Span<char> for name, value, line

                    string name = line.Substring(0, separator).Trim().ToLowerInvariant();
                    string value = line.Substring(separator + 1, line.Length - separator - 1).Trim();

                    switch (name)
                    {
                        case "content-type":
                            request.ContentType = value;
                            break;
                        //case "content-length":
                        //    request.ContentLength = long.Parse(value);
                        //    break;
                        //case "user-agent":
                        //    request.UserAgent = value;
                        //    break;
                        case "accept":
                            request.Accept = value;
                            break;
                        //case "connection":
                        //    request.Connection = value;
                        //    break;
                        //case "expect":
                        //    request.Expect = value;
                        //    break;
                        case "date":
                            request.Headers["Date"] =
                                System.DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture)
                                .ToUniversalTime()
                                .ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                            break;
                        //case "host":
                        //    request.Host = value;
                        //    break;
                        //case "if-modified-since":
                        //    request.IfModifiedSince = System.Convert.ToDateTime(value);
                        //    break;
                        //case "range":
                        //    request.AddRange(System.Convert.ToInt32(value));
                        //    break;
                        //case "referer":
                        //    request.Referer = value;
                        //    break;
                        //case "transfer-encoding":
                        //    request.TransferEncoding = value;
                        //    break;

                        default:
                            request.Headers[name] = value;
                            break;
                    }
                }
            }

            //
            // content - Additional data to be sent after the headers. Typically used with POST or PUT requests.    
            //
            var content = options["content"].ToBytes(ctx);
            if (content != null && content.Length != 0)
            {
                using (var body = request.GetRequestStreamAsync().Result)
                {
                    body.Write(content, 0, content.Length);
                }
            }
        }

        /// <summary>
        /// Gets (actually constructs) the HTTP response header.
        /// </summary>
        /// <remarks>see Peachpie.Library.Network</remarks>
        static string StatusHeader(HttpWebResponse response) => $"HTTP/{response.ProtocolVersion.ToString(2)} {(int)response.StatusCode} {response.StatusDescription}";

        /// <summary>
        /// see stream_get_meta_data()["wrapper_data"]
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        static object CreateWrapperData(HttpWebResponse response)
        {
            if (response == null)
                return null;

            var array = new PhpArray(1 + response.Headers.Count);

            // HTTP/x.x
            array.Add(StatusHeader(response));

            // other headers
            for (int i = 0; i < response.Headers.Count; i++)
            {
                array.Add(response.Headers[i]);
            }

            //
            return array;
        }

        public override string Label { get { return "HTTP"; } }

        public override string Scheme { get; }

        public override bool IsUrl { get { return true; } }

        /// <summary>
        /// The protocol portion of URL handled by this wrapper.
        /// </summary>
        public const string scheme = "http";

        /// <summary>
        /// The protocol portion of URL handled by this wrapper.
        /// </summary>
        public const string schemes = "https";

        #endregion
    }

    #endregion

    #region Input/Output Stream Wrapper

    /// <summary>
    /// Derived from <see cref="StreamWrapper"/>, this class provides access to the PHP input/output streams.
    /// </summary>
    public partial class InputOutputStreamWrapper : StreamWrapper
    {
        #region StreamWrapper overrides

        public override PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context)
        {
            Stream native = null;

            StreamAccessOptions accessOptions;
            if (!ParseMode(ctx, mode, options, out accessOptions))
                return null;

            // Do not close the system I/O streams.
            accessOptions |= StreamAccessOptions.Persistent;

            // EX: Use a cache of persistent streams (?) instead of static properties.

            // TODO: path may be case insensitive

            FileAccess supportedAccess;
            switch (path)
            {
                // Standard IO streams are not available on Silverlight
                // stdin/stdout/input/stderr, the only supported is 'output'
                case "php://stdin":
                    //rv = InputOutputStreamWrapper.In;
                    native = Console.OpenStandardInput();
                    supportedAccess = FileAccess.Read;
                    break;

                case "php://stdout":
                    // rv = InputOutputStreamWrapper.Out;
                    native = Console.OpenStandardOutput();
                    supportedAccess = FileAccess.Write;
                    break;

                case "php://stderr":
                    // rv = InputOutputStreamWrapper.Error;
                    native = Console.OpenStandardError();
                    supportedAccess = FileAccess.Write;
                    break;

                case "php://input":
                    // rv = InputOutputStreamWrapper.ScriptInput;
                    native = OpenScriptInput(ctx);
                    supportedAccess = FileAccess.Read;
                    break;

                case "php://output":
                    // rv = InputOutputStreamWrapper.ScriptOutput;
                    native = OpenScriptOutput(ctx);
                    supportedAccess = FileAccess.Write;
                    break;

                case "php://memory":
                    native = new MemoryStream();
                    supportedAccess = FileAccess.ReadWrite;
                    break;

                default:
                    const string filter_uri = "php://filter/";
                    const string temp_uri = "php://temp";
                    const string resource_param = "/resource=";

                    // The only remaining option is the "php://filter"
                    if (path.StartsWith(filter_uri, StringComparison.OrdinalIgnoreCase))
                    {
                        int pos = path.IndexOf(resource_param, filter_uri.Length - 1);
                        if (pos > 0)
                        {
                            string arguments = path.Substring(filter_uri.Length, pos - filter_uri.Length);
                            path = path.Substring(pos + resource_param.Length);
                            return OpenFiltered(ctx, path, arguments, mode, options, context);
                        }

                        // No URL resource specified.
                        //PhpException.Throw(PhpError.Warning, CoreResources.GetString("url_resource_missing"));
                        //return null;
                        throw new ArgumentException("No URL resource specified.");  // TODO: Err
                    }
                    else if (path.StartsWith(temp_uri, StringComparison.OrdinalIgnoreCase))
                    {
                        // TODO: /maxmemory:NN option
                        // TODO: use temp file if size in memory exceeds NN (2MB by default)
                        native = new MemoryStream();
                        supportedAccess = FileAccess.ReadWrite;
                        break;
                    }
                    else
                    {
                        // Unrecognized php:// stream name
                        //PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_invalid",
                        //  FileSystemUtils.StripPassword(path)));
                        //return null;
                        throw new ArgumentException("Unrecognized php:// stream name.");  // TODO: Err
                    }
            }

            if (!CheckOptions(accessOptions, supportedAccess, path))
                return null;

            if (native == null)
            {
                //PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_invalid",
                //  FileSystemUtils.StripPassword(path)));
                //return null;
                throw new ArgumentException("stream_file_invalid");  // TODO: Err
            }

            var rv = new NativeStream(ctx, native, this, accessOptions, path, context);
            rv.IsReadBuffered = rv.IsWriteBuffered = false;
            return rv;
        }

        /// <summary>
        /// Opens a PhpStream and appends the stream filters.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">The URL resource.</param>
        /// <param name="arguments">String containig '/'-separated options.</param>
        /// <param name="mode">Original mode.</param>
        /// <param name="options">Original options.</param>
        /// <param name="context">Original context.</param>
        /// <returns></returns>
        private PhpStream OpenFiltered(Context ctx, string path, string arguments, string mode, StreamOpenOptions options, StreamContext context)
        {
            var rv = PhpStream.Open(ctx, path, mode, options, context);
            if (rv == null)
            {
                return null;
            }

            // Note that only the necessary read/write chain is updated (depending on the StreamAccessOptions)
            foreach (string arg in arguments.Split('/'))
            {
                if (string.Compare(arg, 0, "read=", 0, "read=".Length) == 0)
                {
                    foreach (string filter in arg.Substring("read=".Length).Split('|'))
                        PhpFilter.AddToStream(ctx, rv, filter, FilterChainOptions.Tail | FilterChainOptions.Read, PhpValue.Null);
                }
                else if (string.Compare(arg, 0, "write=", 0, "write=".Length) == 0)
                {
                    foreach (string filter in arg.Substring("read=".Length).Split('|'))
                        PhpFilter.AddToStream(ctx, rv, filter, FilterChainOptions.Tail | FilterChainOptions.Write, PhpValue.Null);
                }
                else
                {
                    foreach (string filter in arg.Split('|'))
                        PhpFilter.AddToStream(ctx, rv, filter, FilterChainOptions.Tail | FilterChainOptions.ReadWrite, PhpValue.Null);
                }
            }

            return rv;
        }

        public override string Label => "InputOutput";

        public override string Scheme => scheme;

        public override bool IsUrl => false;

        /// <summary>
        /// Represents the script input stream (containing the raw POST data).
        /// </summary>
        /// <remarks>
        /// It is a persistent binary stream. This means that it is never closed
        /// by <c>fclose()</c> and no EOLN mapping is performed.
        /// </remarks>
        public static PhpStream ScriptInput(Context ctx)
        {
            PhpStream input = null;
            if (input == null)
            {
                input = new NativeStream(ctx, OpenScriptInput(ctx), null, StreamAccessOptions.Read | StreamAccessOptions.Persistent, "php://input", StreamContext.Default)
                {
                    IsReadBuffered = false
                };
                // EX: cache this as a persistent stream
            }
            return input;
        }

        /// <summary>
        /// Represents the script output stream (alias php://output).
        /// </summary>
        /// <remarks>
        /// It is a persistent binary stream. This means that it is never closed
        /// by <c>fclose()</c> and no EOLN mapping is performed.
        /// </remarks>
        public static PhpStream ScriptOutput(Context ctx)
        {
            PhpStream output = null;
            output = new NativeStream(ctx, OpenScriptOutput(ctx), null, StreamAccessOptions.Write | StreamAccessOptions.Persistent, "php://output", StreamContext.Default)
            {
                IsWriteBuffered = false
            };
            // EX: cache this as a persistent stream

            return output;
        }

        /// <summary>
        /// Opens the script input (containing raw POST data).
        /// </summary>
        /// <returns>The corresponding native stream opened for reading.</returns>
        private static Stream OpenScriptInput(Context ctx)
        {
            var httpctx = ctx.HttpPhpContext;
            return (httpctx != null)
                ? httpctx.InputStream   // HttpContext.Request.InputStream
                : Console.OpenStandardInput();
        }

        /// <summary>
        /// Opens the script output (binary output sink of the script).
        /// </summary>
        /// <returns>The corresponding native stream opened for writing.</returns>
        private static Stream OpenScriptOutput(Context ctx) => ctx.OutputStream;

        /// <summary>
        /// The protocol portion of URL handled by this wrapper.
        /// </summary>
        public const string scheme = "php";

        #endregion

        public static bool IsStdIn(PhpStream stream) => stream is NativeStream && stream.OpenedPath == "php://stdin";
        public static bool IsStdOut(PhpStream stream) => stream is NativeStream && stream.OpenedPath == "php://stdout";
        public static bool IsStdErr(PhpStream stream) => stream is NativeStream && stream.OpenedPath == "php://stderr";

        /// <summary>
        /// Represents the console input stream (alias php://stdin).
        /// </summary>
        /// <remarks>
        /// It is a persistent text stream. This means that it is never closed
        /// by <c>fclose()</c> and <c>\r\n</c> is converted to <c>\n</c>.
        /// </remarks>
        public static PhpStream In => s_stdin.Value;

        // EX: cache this as a persistent stream
        static Lazy<PhpStream> s_stdin = new Lazy<PhpStream>(() => new NativeStream(
            Utf8EncodingProvider.Instance, Console.OpenStandardInput(), null,
            StreamAccessOptions.Read | StreamAccessOptions.UseText | StreamAccessOptions.Persistent, 
            "php://stdin", StreamContext.Default)
        {
            IsReadBuffered = false,
        });

        /// <summary>
        /// Represents the console output stream (alias php://stdout).
        /// </summary>
        /// <remarks>
        /// It is a persistent text stream. This means that it is never closed
        /// by <c>fclose()</c> and <c>\n</c> is converted to <c>\r\n</c>.
        /// </remarks>
        public static PhpStream Out => s_stdout.Value;

        // EX: cache this as a persistent stream
        static Lazy<PhpStream> s_stdout = new Lazy<PhpStream>(() => new NativeStream(
            Utf8EncodingProvider.Instance, Console.OpenStandardOutput(), null,
            StreamAccessOptions.Write | StreamAccessOptions.UseText | StreamAccessOptions.Persistent,
            "php://stdout", StreamContext.Default)
        {
            IsWriteBuffered = false,
        });

        /// <summary>
        /// Represents the console error stream (alias php://error).
        /// </summary>
        /// <remarks>
        /// It is a persistent text stream. This means that it is never closed
        /// by <c>fclose()</c> and <c>\n</c> is converted to <c>\r\n</c>.
        /// </remarks>
        public static PhpStream Error => s_stderr.Value;

        // EX: cache this as a persistent stream
        static Lazy<PhpStream> s_stderr = new Lazy<PhpStream>(() => new NativeStream(
            Utf8EncodingProvider.Instance, Console.OpenStandardError(), null,
            StreamAccessOptions.Write | StreamAccessOptions.UseText | StreamAccessOptions.Persistent,
            "php://stderr", StreamContext.Default)
        {
            IsWriteBuffered = false,
        });
    }

    #endregion

    #region FTP Stream Wrapper (N/A)

    ///// <summary>
    ///// Derived from <see cref="StreamWrapper"/>, this class provides access to 
    ///// remote files using the ftp protocol.
    ///// </summary>
    //public class FtpStreamWrapper : StreamWrapper
    //{
    //    /// <summary>
    //    /// The protocol portion of URL handled by this wrapper.
    //    /// </summary>
    //    public const string scheme = "ftp";

    //    #region StreamWrapper overrides

    //    public override PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context)
    //    {
    //        return null;
    //    }

    //    public override string Scheme => scheme;

    //    public override string Label => "FTP";

    //    public override StatStruct Stat(string path, StreamStatOptions options, StreamContext context, bool streamStat)
    //    {
    //        return null;
    //    }

    //    public override bool Unlink(string path, StreamUnlinkOptions options, StreamContext context)
    //    {
    //        return false;
    //    }

    //    public override string[] Listing(string path, StreamListingOptions options, StreamContext context)
    //    {
    //        return null;
    //    }

    //    #endregion
    //}

    #endregion

    #region User-space Stream Wrapper

    /// <summary>
    /// Derived from <see cref="StreamWrapper"/>, this class is built
    /// using reflection upon a user-defined stream wrapper.
    /// A PhpStream descendant is defined upon the instance methods of 
    /// the given PHP class.
    /// </summary>
    public class UserStreamWrapper : StreamWrapper
    {
        private readonly Context/*!*/_ctx;
        private readonly string _scheme;
        private readonly PhpTypeInfo/*!*/_wrapperType;
        private readonly bool _isUrl;

        #region Wrapper methods invocation

        /// <summary>
        /// Lazily instantiated <see cref="_wrapperType"/>. PHP instantiates the wrapper class when used for the first time.
        /// </summary>
        protected object/*!*/WrapperInstance
        {
            get
            {
                if (_wrapperInstance == null)
                {
                    _wrapperInstance = _wrapperType.Creator(_ctx, Array.Empty<PhpValue>());
                }

                return _wrapperInstance;
            }
        }
        private object _wrapperInstance; // lazily instantiated wrapper

        /// <summary>
        /// Invoke wrapper method on wrapper instance.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public PhpValue InvokeWrapperMethod(string method, params PhpValue[] args)
        {
            var routine = _wrapperType.RuntimeMethods[method];
            return (routine != null)
                ? routine.Invoke(_ctx, WrapperInstance, args)
                : PhpValue.Null;
        }

        #endregion

        public UserStreamWrapper(Context/*!*/context, string protocol, PhpTypeInfo/*!*/wrapperType, bool isUrl)
        {
            Debug.Assert(wrapperType != null);
            Debug.Assert(!string.IsNullOrEmpty(protocol));

            // Create a new PhpWrapper instance above the given class (reflection)
            // Note: when a member is not defined (Error): "Call to unimplemented method:
            // variablestream::stream_write is not implemented!"

            _ctx = context;
            _scheme = protocol;
            _wrapperType = wrapperType;
            _isUrl = isUrl;
        }

        #region StreamWrapper overrides

        public override string Label => "user-space";

        public override string Scheme => _scheme;

        public override bool IsUrl => _isUrl;

        public override PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context)
        {
            var opened_path_ref = new PhpAlias((PhpValue)path);
            var result = InvokeWrapperMethod(PhpUserStream.USERSTREAM_OPEN, (PhpValue)path, (PhpValue)mode, (PhpValue)(int)options, PhpValue.Create(opened_path_ref));

            if (result.ToBoolean() == true)
            {
                string opened_path_str = PhpVariable.AsString(opened_path_ref.Value);
                if (opened_path_str != null) path = opened_path_str;

                FileMode fileMode;
                FileAccess fileAccess;
                StreamAccessOptions ao;

                if (ParseMode(ctx, mode, options, out fileMode, out fileAccess, out ao))
                {
                    return new PhpUserStream(ctx, this, ao, path, context);
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        public override void OnClose(PhpStream stream)
        {
            // stream_close:
            InvokeWrapperMethod(PhpUserStream.USERSTREAM_CLOSE);

            (_wrapperInstance as IDisposable)?.Dispose();
            _wrapperInstance = null;

            //
            base.OnClose(stream);
        }

        public override PhpArray OnStat(PhpStream stream)
        {
            return base.OnStat(stream);
        }

        public override bool RemoveDirectory(string path, StreamRemoveDirectoryOptions options, StreamContext context)
        {
            return base.RemoveDirectory(path, options, context);
        }

        public override bool Rename(string fromPath, string toPath, StreamRenameOptions options, StreamContext context)
        {
            return base.Rename(fromPath, toPath, options, context);
        }

        public override StatStruct Stat(string path, StreamStatOptions options, StreamContext context, bool streamStat)
        {
            PhpArray arr = (streamStat ?
                InvokeWrapperMethod(PhpUserStream.USERSTREAM_STAT) :
                InvokeWrapperMethod(PhpUserStream.USERSTREAM_STATURL, (PhpValue)path, (PhpValue)(int)options)).ArrayOrNull();

            if (arr != null)
            {
                return new StatStruct()
                {
                    st_dev = (uint)(arr["dev"]),
                    st_ino = (ushort)(arr["ino"]),
                    st_mode = (ushort)(arr["mode"]),
                    st_nlink = (short)(arr["nlink"]),
                    st_uid = (short)(arr["uid"]),
                    st_gid = (short)(arr["gid"]),
                    st_rdev = (uint)(arr["rdev"]),
                    st_size = (long)(arr["size"]),

                    st_atime = (long)(arr["atime"]),
                    st_mtime = (long)(arr["mtime"]),
                    st_ctime = (long)(arr["ctime"]),

                    //st_blksize = (long)Convert.ObjectToLongInteger(arr["blksize"]),
                    //st_blocks = (long)Convert.ObjectToLongInteger(arr["blocks"]),
                };
            }

            return new StatStruct();
        }

        public override bool Unlink(string path, StreamUnlinkOptions options, StreamContext context)
        {
            return InvokeWrapperMethod(PhpUserStream.USERSTREAM_UNLINK, (PhpValue)path).ToBoolean();
        }

        public override IEnumerable<string> Listing(string root, string path, StreamListingOptions options, StreamContext context)
        {
            return base.Listing(root, path, options, context);
        }

        public override bool MakeDirectory(string path, int accessMode, StreamMakeDirectoryOptions options, StreamContext context)
        {
            return base.MakeDirectory(path, accessMode, options, context);
        }

        #endregion
    }

    #endregion
}
