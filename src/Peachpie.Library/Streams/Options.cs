using Pchp.Core;
using System;
using System.IO;

namespace Pchp.Library.Streams
{
    #region Open-Mode decoded options

    /// <summary>
    /// Flags returned by <see cref="StreamWrapper.ParseMode(string, StreamOpenOptions, out FileMode, out FileAccess, out StreamAccessOptions)"/> indicating
    /// additional information to the parsed <see cref="FileMode"/>
    /// and <see cref="FileAccess"/>.
    /// </summary>
    [Flags]
    public enum StreamAccessOptions
    {
        /// <summary>Empty (invalid) value (0).</summary>
        Empty = 0,
        /// <summary>The stream was opened for reading (1).</summary>
        Read = FileAccess.Read,
        /// <summary>The stream was opened for writing (2).</summary>
        Write = FileAccess.Write,
        /// <summary>Use text access to the stream (default is binary) (4).</summary>
        UseText = 0x04,
        /// <summary>Seek to the end of the stream is required (8).</summary>
        /// <remarks>
        /// The given mode requires "a+", which is not supported
        /// by .NET Framework; mode is reset to "r+" and a seek is required.
        /// </remarks>
        SeekEnd = 0x08,
        /// <summary>The mode starts with 'x' which requires 
        /// a Warning if the file already exists. It is not applicable
        /// to remote files (16).</summary>
        Exclusive = 0x10,
        /// <summary>This file may be searched in the include_path
        /// if requested (only the modes opening existing files) (32).</summary>
        FindFile = 0x20,
        /// <summary>When a local file is opened using tmpfile() it should be removed when closed (256).</summary>
        Temporary = 0x100,
        /// <summary>Denotes a persistent version of the stream (2048).</summary>
        Persistent = StreamOptions.Persistent
    }
    #endregion

    #region Stream opening flags

    /// <summary>
    /// Flags passed in the options argument to the <see cref="StreamWrapper.Open"/> method.
    /// </summary>
    [Flags, PhpHidden]
    public enum StreamOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>If path is relative, Wrapper will search for the resource using the include_path (1).</summary>
        UseIncludePath = 1,
        /// <summary>When this flag is set, only the file:// wrapper is considered. (2)</summary>
        IgnoreUrl = 2,
        /// <summary>Apply the <c>safe_mode</c> permissions check when opening a file (4).</summary>
        EnforceSafeMode = 4,
        /// <summary>If this flag is set, the Wrapper is responsible for raising errors using 
        /// trigger_error() during opening of the stream. If this flag is not set, she should not raise any errors (8).</summary>
        ReportErrors = 8,
        /// <summary>If you don't need to write to the stream, but really need to 
        /// be able to seek, use this flag in your options (16).</summary>
        MustSeek = 16,

        /// <summary>
        /// If you are going to end up casting the stream into a FILE* or
        /// a socket, pass this flag and the streams/wrappers will not use
        /// buffering mechanisms while reading the headers, so that HTTP wrapped 
        /// streams will work consistently.  If you omit this flag, streams will 
        /// use buffering and should end up working more optimally (32).
        /// </summary>
        WillCast = 32,
        /// <summary> This flag applies to php_stream_locate_url_wrapper (64). </summary>
        LocateWrappersOnly = 64,
        /// <summary> This flag is only used by include/require functions (128).</summary>
        OpenForInclude = 128,
        /// <summary> This flag tells streams to ONLY open urls (256).</summary>
        UseUrl = 256,
        /// <summary> This flag is used when only the headers from HTTP request are to be fetched (512).</summary>
        OnlyGetHeaders = 512,
        /// <summary>Don't apply open_basedir checks (1024).</summary>
        DisableOpenBasedir = 1024,
        /// <summary>Get (or create) a persistent version of the stream (2048).</summary>
        Persistent = 2048
    }

    /// <summary>
    /// <see cref="StreamOptions"/> relevant to the Open method.
    /// </summary>
    [Flags]
    public enum StreamOpenOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>If path is relative, Wrapper will search for the resource using the include_path (1).</summary>
        UseIncludePath = StreamOptions.UseIncludePath,
        /// <summary>Apply the <c>safe_mode</c> permissions check when opening a file (4).</summary>
        EnforceSafeMode = StreamOptions.EnforceSafeMode,
        /// <summary>If this flag is set, the Wrapper is responsible for raising errors using 
        /// trigger_error() during opening of the stream. If this flag is not set, user should not raise any errors (8).</summary>
        ReportErrors = StreamOptions.ReportErrors,
        /// <summary> This flag is only used by include/require functions (128).</summary>
        OpenForInclude = StreamOptions.OpenForInclude,
        /// <summary>Don't apply open_basedir checks (1024).</summary>
        DisableOpenBasedir = StreamOptions.DisableOpenBasedir,
        /// <summary>Get (or create) a persistent version of the stream (2048).</summary>
        Persistent = StreamOptions.Persistent,
        /// <summary>When a local file is opened using tmpfile() it should be removed when closed (256).</summary>
        Temporary = StreamAccessOptions.Temporary
    }

    /// <summary>
    /// <see cref="StreamOptions"/> relevant to the Listing method.
    /// </summary>
    [Flags]
    public enum StreamListingOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>Don't apply open_basedir checks (1024).</summary>
        DisableOpenBasedir = StreamOptions.DisableOpenBasedir
    }

    /// <summary>
    /// <see cref="StreamOptions"/> relevant to the Unlink method.
    /// </summary>
    [Flags]
    public enum StreamUnlinkOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>Apply the <c>safe_mode</c> permissions check when opening a file (4).</summary>
        EnforceSafeMode = StreamOptions.EnforceSafeMode,
        /// <summary>If this flag is set, the Wrapper is responsible for raising errors using 
        /// trigger_error() during opening of the stream. If this flag is not set, she should not raise any errors (8).</summary>
        ReportErrors = StreamOptions.ReportErrors
    }

    /// <summary>
    /// <see cref="StreamOptions"/> relevant to the Rename method.
    /// </summary>
    public enum StreamRenameOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0
    }

    /// <summary>
    /// Specific options of the Stat method.
    /// </summary>
    [Flags]
    public enum StreamStatOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>Stat the symbolic link itself instead of the linked file (1).</summary>
        Link = 0x1,
        /// <summary>Do not complain if the file does not exist (2).</summary>
        Quiet = 0x2,
    }

    /// <summary>
    /// Specific options of the MakeDirectory method.
    /// </summary>
    public enum StreamMakeDirectoryOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>Create the whole path leading to the specified directory if necessary (1).</summary>
        Recursive = 0x1
    }

    /// <summary>
    /// <see cref="StreamOptions"/> relevant to the RemoveDirectory method.
    /// </summary>
    public enum StreamRemoveDirectoryOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0
    }

    /// <summary>
    /// File attribute flags used in fileperms.
    /// </summary>
    [Flags]
    public enum FileModeFlags : uint
    {
       // #define S_IFMT  00170000
       // #define S_IFSOCK 0140000
       // #define S_IFLNK  0120000
       // #define S_IFREG  0100000
       // #define S_IFBLK  0060000
       // #define S_IFDIR  0040000
       // #define S_IFCHR  0020000
       // #define S_IFIFO  0010000
       // #define S_ISUID  0004000
       // #define S_ISGID  0002000
       // #define S_ISVTX  0001000

        /// <summary>Mask for file type.</summary>
        FileTypeMask = Directory | File | Character | Pipe,
        /// <summary>Regular file.</summary>
        File = 0x8000,
        /// <summary>Directory.</summary>
        Directory = 0x4000,
        /// <summary>Character special.</summary>
        Character = 0x2000,
        /// <summary>FIFO.</summary>
        Pipe = 0x1000,
        /// <summary>Read permissions; owner, group, others.</summary>
        Read = 4 + 4 * 8 + 4 * 8 * 8,
        /// <summary>Write permissions; owner, group, others.</summary>
        Write = 2 + 2 * 8 + 2 * 8 * 8,
        /// <summary>Execute permissions; owner, group, others.</summary>
        Execute = 1 + 8 + 8 * 8,
        /// <summary>All permissions for owner, group and others.</summary>
        ReadWriteExecute = Read | Write | Execute,
        /// <summary>Symbolic link.</summary>
        Link = 0x120000,
    }

    #endregion
}
