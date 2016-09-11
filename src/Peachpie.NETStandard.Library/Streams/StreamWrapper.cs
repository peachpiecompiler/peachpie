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

        public abstract PhpStream Open(ref string path, string mode, StreamOpenOptions options, StreamContext context);

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
            //StreamWrapper result;

            //// Note: FileStreamWrapper is returned both for "file" and for "".
            //if (string.IsNullOrEmpty(scheme))
            //{
            //    scheme = FileStreamWrapper.scheme;
            //}

            //// First search the system wrappers (always at least an empty Hashtable)
            //if (!SystemStreamWrappers.TryGetValue(scheme, out result))
            //{

            //    // Then look if the wrapper is implemented but not instantiated
            //    switch (scheme)
            //    {
            //        case FileStreamWrapper.scheme:
            //            return (StreamWrapper)(SystemStreamWrappers[scheme] = new FileStreamWrapper());
            //        case HttpStreamWrapper.scheme:
            //            return (StreamWrapper)(SystemStreamWrappers[scheme] = new HttpStreamWrapper());
            //        case InputOutputStreamWrapper.scheme:
            //            return (StreamWrapper)(SystemStreamWrappers[scheme] = new InputOutputStreamWrapper());
            //    }

            //    // Next search the user wrappers (if present)
            //    if (UserWrappers != null)
            //    {
            //        UserWrappers.TryGetValue(scheme, out result);
            //    }
            //}

            ////
            //return result;  // can be null
            throw new NotImplementedException();
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
}
