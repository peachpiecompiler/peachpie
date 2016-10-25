using Pchp.Core;
using Pchp.Core.Resources;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
    public abstract class StreamWrapper : IDisposable
    {
        #region Mandatory Wrapper Operations

        public abstract PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context);

        public abstract string Label { get; }

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

        public virtual string[] Listing(string path, StreamListingOptions options, StreamContext context)
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

        #endregion

        #region Helper methods (ParseMode, FileSystemUtils.StripPassword)

        /// <summary>
        /// Parse the <paramref name="mode"/> argument passed to <c>fopen()</c>
        /// and make the appropriate <see cref="FileMode"/> and <see cref="FileAccess"/>
        /// combination.
        /// Integrate the relevant options from <see cref="StreamOpenOptions"/> too.
        /// </summary>
        /// <param name="mode">Mode as passed to <c>fopen()</c>.</param>
        /// <param name="options">The <see cref="StreamOpenOptions"/> passed to <c>fopen()</c>.</param>
        /// <param name="fileMode">Resulting <see cref="FileMode"/> specifying opening mode.</param>
        /// <param name="fileAccess">Resulting <see cref="FileAccess"/> specifying read/write access options.</param>
        /// <param name="accessOptions">Resulting <see cref="StreamAccessOptions"/> giving 
        /// additional information to the stream opener.</param>
        /// <returns><c>true</c> if the given mode was a valid file opening mode, otherwise <c>false</c>.</returns>
        public bool ParseMode(string mode, StreamOpenOptions options, out FileMode fileMode, out FileAccess fileAccess, out StreamAccessOptions accessOptions)
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
                    fileAccess = FileAccess.Read;
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
                //LocalConfiguration config = Configuration.Local;

                //// checks whether default mode is applicable:
                //if (config.FileSystem.DefaultFileOpenMode == "b")
                //{
                //    forceBinary = true;
                //}
                //else if (config.FileSystem.DefaultFileOpenMode == "t")
                //{
                //    forceText = true;
                //}
                //else
                //{
                //    PhpException.Throw(PhpError.Warning, ErrResources.ambiguous_file_mode, mode);
                //}
                throw new NotImplementedException("Configuration.FileSystem.DefaultFileOpenMode");

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
        /// <param name="mode">Mode as passed to <c>fopen()</c>.</param>
        /// <param name="options">The <see cref="StreamOpenOptions"/> passed to <c>fopen()</c>.</param>
        /// <param name="accessOptions">Resulting <see cref="StreamAccessOptions"/> giving 
        /// additional information to the stream opener.</param>
        /// <returns><c>true</c> if the given mode was a valid file opening mode, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">If the <paramref name="mode"/> is not valid.</exception>
        internal bool ParseMode(string mode, StreamOpenOptions options, out StreamAccessOptions accessOptions)
        {
            FileMode fileMode;
            FileAccess fileAccess;

            return (ParseMode(mode, options, out fileMode, out fileAccess, out accessOptions));
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

        ///// <summary>
        ///// Insert a new wrapper to the list of user StreamWrappers.
        ///// </summary>
        ///// <remarks>
        ///// Each script has its own set of user StreamWrappers registered
        ///// by stream_wrapper_register() stored in the ScriptContext.
        ///// </remarks>
        ///// <param name="protocol">The scheme portion of URLs this wrapper can handle.</param>
        ///// <param name="wrapper">An instance of the corresponding StreamWrapper descendant.</param>
        ///// <returns>True if succeeds, false if the scheme is already registered.</returns>
        //public static bool RegisterUserWrapper(string protocol, StreamWrapper wrapper)
        //{
        //    // Userwrappers may be initialized to null
        //    if (UserWrappers == null)
        //        CreateUserWrapperTable();

        //    UserWrappers.Add(protocol, wrapper);
        //    return true;
        //}

        /// <summary>
        /// Register a new system wrapper
        /// </summary>
        /// <param name="wrapper">An instance of the corresponding StreamWrapper descendant.</param>
        /// <returns>True if succeeds, false if the scheme is already registered.</returns>
        public static bool RegisterSystemWrapper(StreamWrapper wrapper)
        {
            if (!systemStreamWrappers.ContainsKey(wrapper.Scheme))
            {
                systemStreamWrappers.Add(wrapper.Scheme, wrapper);
                return true;
            }
            return false;
        }

        ///// <summary>
        ///// Checks if a wrapper is already registered for the given scheme.
        ///// </summary>
        ///// <param name="scheme">The scheme.</param>
        ///// <returns><c>true</c> if exists.</returns>
        //public static bool Exists(string scheme)
        //{
        //    return GetWrapperInternal(scheme) != null;
        //}

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

            //if (!ScriptContext.CurrentContext.Config.FileSystem.AllowUrlFopen)
            //{
            //    if (wrapper.IsUrl)
            //    {
            //        PhpException.Throw(PhpError.Warning, ErrResources.url_fopen_disabled);
            //        return null;
            //    }
            //}

            Debug.Assert(wrapper != null);
            return wrapper;
        }

        /// <summary>
        /// Gets the list of built-in stream wrapper schemes.
        /// </summary>
        /// <returns></returns>
        public static ICollection<string> GetSystemWrapperSchemes()
        {
            var keys = new string[systemStreamWrappers.Count];
            systemStreamWrappers.Keys.CopyTo(keys, 0);
            return keys;
        }

        ///// <summary>
        ///// Gets the list of user wrapper schemes.
        ///// </summary>
        ///// <returns></returns>
        //public static ICollection<string> GetUserWrapperSchemes()
        //{
        //    if (UserWrappers == null)
        //        return Core.Utilities.ArrayUtils.EmptyStrings;

        //    return UserWrappers.Keys;
        //}

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
            StreamWrapper result;

            // Note: FileStreamWrapper is returned both for "file" and for "".
            if (string.IsNullOrEmpty(scheme))
            {
                scheme = FileStreamWrapper.scheme;
            }

            // First search the system wrappers (always at least an empty Hashtable)
            if (!SystemStreamWrappers.TryGetValue(scheme, out result))
            {

                // Then look if the wrapper is implemented but not instantiated
                switch (scheme)
                {
                    case FileStreamWrapper.scheme:
                        return (StreamWrapper)(SystemStreamWrappers[scheme] = new FileStreamWrapper());
                    //case HttpStreamWrapper.scheme:
                    //    return (StreamWrapper)(SystemStreamWrappers[scheme] = new HttpStreamWrapper());
                    //case InputOutputStreamWrapper.scheme:
                    //    return (StreamWrapper)(SystemStreamWrappers[scheme] = new InputOutputStreamWrapper());
                }

                //// Next search the user wrappers (if present)
                //if (UserWrappers != null)
                //{
                //    UserWrappers.TryGetValue(scheme, out result);
                //}
            }

            //
            return result;  // can be null
        }

        ///// <summary>
        ///// Make new instance of Hashtable for the userwrappers
        ///// in the ScriptContext.
        ///// </summary>
        //internal static void CreateUserWrapperTable()
        //{
        //    ScriptContext script_context = ScriptContext.CurrentContext;

        //    Debug.Assert(script_context.UserStreamWrappers == null);
        //    script_context.UserStreamWrappers = new Dictionary<string, StreamWrapper>(5);
        //}

        ///// <summary>
        ///// Table of user-registered stream wrappers.
        ///// Stored as an instance variable in ScriptContext
        ///// (for every script there is one, it is initialized
        ///// to null - instance is created on first user-wrapper insertion).
        ///// </summary>
        //internal static Dictionary<string, StreamWrapper> UserWrappers
        //{
        //    get
        //    {
        //        return ScriptContext.CurrentContext.UserStreamWrappers;
        //    }
        //}

        /// <summary>
        /// Registered system stream wrappers for all requests.
        /// </summary>
        public static Dictionary<string, StreamWrapper> SystemStreamWrappers { get { return systemStreamWrappers; } }

        private static readonly Dictionary<string, StreamWrapper> systemStreamWrappers = new Dictionary<string, StreamWrapper>(5);  // TODO: thread safe

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

            if (!ParseMode(mode, options, out fileMode, out fileAccess, out ao)) return null;

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
            StatStruct invalid = new StatStruct();
            invalid.st_size = -1;
            Debug.Assert(path != null);

            // Note: path is already absolute w/o the scheme, the permissions have already been checked.
            return PhpPath.HandleFileSystemInfo(invalid, path, (p) =>
            {
                FileSystemInfo info = null;

                info = new DirectoryInfo(p);
                if (!info.Exists)
                {
                    info = new FileInfo(p);
                    if (!info.Exists)
                    {
                        return invalid;
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

        public override string[] Listing(string path, StreamListingOptions options, StreamContext context)
        {
            Debug.Assert(path != null);
            Debug.Assert(Path.IsPathRooted(path));

            try
            {
                string[] listing = Directory.GetFileSystemEntries(path);
                bool root = Path.GetPathRoot(path) == path;
                int index = root ? 0 : 2;
                string[] rv = new string[listing.Length + index];

                // Remove the absolute path information (PHP returns only filenames)
                int pathLength = path.Length;
                if (path[pathLength - 1] != Path.DirectorySeparatorChar) pathLength++;

                // Check for the '.' and '..'; they should be present
                if (!root)
                {
                    rv[0] = ".";
                    rv[1] = "..";
                }
                for (int i = 0; i < listing.Length; i++)
                {
                    rv[index++] = listing[i].Substring(pathLength);
                }
                return rv;
            }
            catch (DirectoryNotFoundException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_bad_directory, FileSystemUtils.StripPassword(path));
            }
            catch (UnauthorizedAccessException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_file_access_denied, FileSystemUtils.StripPassword(path));
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_error, FileSystemUtils.StripPassword(path), e.Message);
            }
            return null;
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
                    if (!Directory.Exists(parent))
                    {
                        PhpException.Throw(PhpError.Warning, ErrResources.stream_directory_make_parent, FileSystemUtils.StripPassword(path));
                        return false;
                    }
                }

                // Creates the whole path
                Directory.CreateDirectory(path);
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
                Directory.Delete(path, false);
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
}
