using Pchp.Core;
using Pchp.Core.Resources;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static partial class PhpPath
    {
        /// <summary>
		/// Creates a <see cref="PhpArray"/> from the <see cref="StatStruct"/> 
		/// copying the structure members into the array.
		/// </summary>
		/// <remarks>
		/// The resulting PhpArray has following associative keys in the given order
		/// (each has a corresponding numeric index starting from zero).
		/// As of ordering, first come all the numeric indexes and then come all the associative indexes.
		/// <list type="table">
		/// <item><term>dev</term><term>Drive number of the disk containing the file (same as st_rdev). </term></item>
		/// <item><term>ino</term><term>Number of the information node (the inode) for the file (UNIX-specific). On UNIX file systems, the inode describes the file date and time stamps, permissions, and content. When files are hard-linked to one another, they share the same inode. The inode, and therefore st_ino, has no meaning in the FAT, HPFS, or NTFS file systems. </term></item>
		/// <item><term>mode</term><term>Bit mask for file-mode information. The _S_IFDIR bit is set if path specifies a directory; the _S_IFREG bit is set if path specifies an ordinary file or a device. User read/write bits are set according to the file's permission mode; user execute bits are set according to the path extension. </term></item>
		/// <item><term>nlink</term><term>Always 1 on non-NTFS file systems. </term></item>
		/// <item><term>uid</term><term>Numeric identifier of user who owns file (UNIX-specific). This field will always be zero on Windows NT systems. A redirected file is classified as a Windows NT file. </term></item>
		/// <item><term>gid</term><term>Numeric identifier of group that owns file (UNIX-specific) This field will always be zero on Windows NT systems. A redirected file is classified as a Windows NT file. </term></item>
		/// <item><term>rdev</term><term>Drive number of the disk containing the file (same as st_dev). </term></item>
		/// <item><term>size</term><term>Size of the file in bytes; a 64-bit integer for _stati64 and _wstati64 </term></item>
		/// <item><term>atime</term><term>Time of last access of file. Valid on NTFS but not on FAT formatted disk drives. Gives the same </term></item>
		/// <item><term>mtime</term><term>Time of last modification of file. </term></item>
		/// <item><term>ctime</term><term>Time of creation of file. Valid on NTFS but not on FAT formatted disk drives. </term></item>
		/// <item><term>blksize</term><term>Always -1 on non-NTFS file systems. </term></item>
		/// <item><term>blocks</term><term>Always -1 on non-NTFS file systems. </term></item>
		/// </list>
		/// </remarks>
		/// <param name="stat">A <see cref="StatStruct"/> returned by a stream wrapper.</param>
		/// <returns>A <see cref="PhpArray"/> in the format of the <c>stat()</c> PHP function.</returns>
		static PhpArray BuildStatArray(StatStruct stat)
        {
            // An unitialized StatStruct means an error.
            if (stat.st_ctime == 0) return null;
            PhpArray result = new PhpArray(13, 13);

            result.Add(0, (int)stat.st_dev);         // device number 
            result.Add(1, (int)stat.st_ino);         // inode number 
            result.Add(2, (int)stat.st_mode);        // inode protection mode 
            result.Add(3, (int)stat.st_nlink);       // number of links 
            result.Add(4, (int)stat.st_uid);         // userid of owner 
            result.Add(5, (int)stat.st_gid);         // groupid of owner 
            result.Add(6, (int)stat.st_rdev);        // device type, if inode device -1
            result.Add(7, (int)stat.st_size);        // size in bytes (reset by caller)
            result.Add(8, unchecked((int)stat.st_atime));       // time of last access (unix timestamp) 
            result.Add(9, unchecked((int)stat.st_mtime));       // time of last modification (unix timestamp) 
            result.Add(10, unchecked((int)stat.st_ctime));      // time of last change (unix timestamp) 
            result.Add(11, (int)-1);                 // blocksize of filesystem IO (-1)
            result.Add(12, (int)-1);                 // number of blocks allocated  (-1)

            result.Add("dev", (int)stat.st_dev);     // device number 
            result.Add("ino", (int)stat.st_ino);     // inode number 
            result.Add("mode", (int)stat.st_mode);   // inode protection mode 
            result.Add("nlink", (int)stat.st_nlink); // number of links 
            result.Add("uid", (int)stat.st_uid);     // userid of owner 
            result.Add("gid", (int)stat.st_gid);     // groupid of owner 
            result.Add("rdev", (int)stat.st_rdev);   // device type, if inode device -1
            result.Add("size", (int)stat.st_size);   // size in bytes (reset by caller)
            result.Add("atime", unchecked((int)stat.st_atime)); // time of last access (unix timestamp) 
            result.Add("mtime", unchecked((int)stat.st_mtime)); // time of last modification (unix timestamp) 
            result.Add("ctime", unchecked((int)stat.st_ctime)); // time of last change (unix timestamp) 
            result.Add("blksize", (int)-1);          // blocksize of filesystem IO (-1)
            result.Add("blocks", (int)-1);           // number of blocks allocated  (-1)

            return result;
        }

        /// <summary>
        /// Check input parameters, resolves absolute path and corresponding stream wrapper.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">The path passed to stat().</param>
        /// <param name="quiet">Wheter to suppress warning message if argument is empty.</param>
        /// <param name="wrapper">If passed, it will contain valid StremWrapper to the given <paramref name="path"/>.</param>
        /// <returns>True if check passed.</returns>
        static bool ResolvePath(Context ctx, ref string path, bool quiet, out StreamWrapper wrapper)
        {
            if (string.IsNullOrEmpty(path))
            {
                wrapper = null;
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:empty", "path"));
                //return false;
                throw new ArgumentException(nameof(path));
            }

            return PhpStream.ResolvePath(ctx, ref path, out wrapper, CheckAccessMode.FileOrDirectory, quiet ? CheckAccessOptions.Quiet : CheckAccessOptions.Empty);
        }

        static StatStruct ResolveStat(Context ctx, string path, bool quiet)
        {
            StreamWrapper wrapper;

            return ResolvePath(ctx, ref path, quiet, out wrapper)   // TODO: stat cache
                ? wrapper.Stat(path, quiet ? StreamStatOptions.Quiet : StreamStatOptions.Empty, StreamContext.Default, false)
                : StatStruct.Invalid;
        }

        /// <summary>
        /// Handles file system exceptions and rethrows PHP exceptions.
        /// </summary>
        /// <typeparam name="T">The return value type.</typeparam>
        /// <param name="invalid">Invalid value.</param>
        /// <param name="path">Path to the resource passed to the <paramref name="action"/>. Also used for error control.</param>
        /// <param name="action">Action to try. The first argument is the path.</param>
        /// <returns>The value of <paramref name="action"/>() or <paramref name="invalid"/>.</returns>
        internal static T HandleFileSystemInfo<T>(T invalid, string path, Func<string, T>/*!*/action)
        {
            try
            {
                return action(path);
            }
            catch (ArgumentException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_stat_invalid_path, FileSystemUtils.StripPassword(path));
            }
            catch (PathTooLongException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_stat_invalid_path, FileSystemUtils.StripPassword(path));
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_error, FileSystemUtils.StripPassword(path), e.Message);
            }

            return invalid;
        }

        #region lstat, stat, fstat, clearstatcache

        /// <summary>
		/// Gives information about a file or symbolic link. 
		/// </summary>
		/// <remarks>
		/// Behaves just like a <see cref="Stat"/> since there are no symbolic links on Windows.
		/// </remarks>
		/// <param name="ctx">Runtime context.</param>
        /// <param name="path">Path to a file to <c>stat</c>.</param>
		/// <returns>A <see cref="PhpArray"/> containing the stat information.</returns>
		[return: CastToFalse]
        public static PhpArray lstat(Context ctx, string path) => stat(ctx, path);

        /// <summary>
        /// Gives information about a file.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">Path to a file to <c>stat</c>.</param>
        /// <returns>A <see cref="PhpArray"/> containing the stat information.</returns>
        [return: CastToFalse]
        public static PhpArray stat(Context ctx, string path)
        {
            return BuildStatArray(ResolveStat(ctx, path, false));
        }

        /// <summary>
        /// Gets information about a file using an open file pointer.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static PhpArray fstat(PhpResource handle)
        {
            // Note: no cache here.
            PhpStream stream = PhpStream.GetValid(handle);
            if (stream == null) return null;
            return BuildStatArray(stream.Stat());
        }

        /// <summary>
        /// Remove all the cached <c>stat()</c> entries.
        /// Function has no return value.
        /// </summary>
        /// <remarks>
        /// The intermediary <see cref="StatStruct"/> used in the last stat-related function call
        /// is cached together with the absolute path or URL to the resource.
        /// The next call to one of the following functions will use the cached
        /// structure unless <see cref="clearstatcache"/> is called.
        /// <para>
        /// The affected functions are:
        /// <c>stat()</c>, <c>lstat()</c>, <c>file_exists()</c>, <c>is_writable()</c>, <c>is_readable()</c>, <c>
        /// is_executable()</c>, <c>is_file()</c>, <c>is_dir()</c>, <c>is_link()</c>, <c>filectime()</c>, <c>
        /// fileatime()</c>, <c>filemtime()</c>, <c>fileinode()</c>, <c>filegroup()</c>, <c>fileowner()</c>, <c>
        /// filesize()</c>, <c>filetype()</c> <c>and fileperms()</c>. 
        /// </para>
        /// </remarks>
        public static void clearstatcache(bool clear_realpath_cache = false, string filename = null)
        {
            //if (!string.IsNullOrEmpty(filename) && !clear_realpath_cache)
            //{
            //    // TODO: throw warning
            //}
        }

        #endregion

        #region file_exists, touch

        /// <summary>
		/// Checks whether a file exists
		/// </summary>
        /// <param name="ctx">Runtime context.</param>
		/// <param name="path">The file to be checked.</param>
		/// <returns>True if the file exists.</returns>
		public static bool file_exists(Context ctx, string path)
        {
            StreamWrapper wrapper;
            return
                !string.IsNullOrEmpty(path) &&  // check empty parameter quietly
                ResolvePath(ctx, ref path, true, out wrapper) &&
                HandleFileSystemInfo(false, path, p => File.Exists(p) || Directory.Exists(p));
        }

        /// <summary>
        /// Sets access and modification time of file.
        /// </summary>
        /// <remarks>
        /// Attempts to set the access and modification time of the file named by 
        /// path to the value given by time. If the option time is not given, 
        /// uses the present time. If the third option atime is present, the access 
        /// time of the given path is set to the value of atime. Note that 
        /// the access time is always modified, regardless of the number of parameters. 
        /// If the file does not exist, it is created. 
        /// </remarks>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">The file to touch.</param>
        /// <param name="mtime">The new modification time.</param>
        /// <param name="atime">The desired access time.</param>
        /// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
        public static bool touch(Context ctx, string path, int mtime = 0, int atime = 0)
        {
            // Create the file if it does not already exist (performs all checks).
            //PhpStream file = (PhpStream)Open(path, "ab");
            //if (file == null) return false;
            StreamWrapper wrapper;
            if (!PhpStream.ResolvePath(ctx, ref path, out wrapper, CheckAccessMode.FileMayExist, CheckAccessOptions.Quiet))
                return false;

            if (!file_exists(ctx, path))
            {
                // Open and close => create new.
                wrapper.Open(ctx, ref path, "wb", StreamOpenOptions.Empty, StreamContext.Default)
                    ?.Dispose();
            }

            var access_time = (atime > 0) ? DateTimeUtils.UnixTimeStampToUtc(atime) : System.DateTime.UtcNow;
            var modification_time = (mtime > 0) ? DateTimeUtils.UnixTimeStampToUtc(mtime) : System.DateTime.UtcNow;

            //access_time -= DateTimeUtils.GetDaylightTimeDifference(access_time, System.DateTime.UtcNow);
            //modification_time -= DateTimeUtils.GetDaylightTimeDifference(modification_time, System.DateTime.UtcNow);

            try
            {
                File.SetLastWriteTimeUtc(path, modification_time);
                File.SetLastAccessTimeUtc(path, access_time);

                // Clear the cached stat values
                clearstatcache();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_file_access_denied, FileSystemUtils.StripPassword(path));
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_error, FileSystemUtils.StripPassword(path), e.Message);
            }
            return false;
        }

        #endregion

        #region Disk Stats (disk_free_space, diskfreespace, disk_total_space)

        /// <summary>
        /// Given a string containing a directory, this function will return 
        /// the number of free bytes on the corresponding filesystem or disk partition. 
        /// </summary>
        /// <param name="directory">The directory specifying the filesystem or disk partition to be examined.</param>
        /// <returns>Nuber of free bytes available or <c>FALSE</c> on an error.</returns>
        [return: CastToFalse]
        public static double disk_free_space(string directory) => GetDiskFreeSpaceInternal(directory, false);

        /// <summary>
        /// Given a string containing a directory, this function will return 
        /// the number of free bytes on the corresponding filesystem or disk partition. 
        /// </summary>
        /// <param name="directory">The directory specifying the filesystem or disk partition to be examined.</param>
        /// <returns>Nuber of free bytes available or <c>FALSE</c> on an error.</returns>
        [return: CastToFalse]
        public static double diskfreespace(string directory) => disk_free_space(directory);

        /// <summary>
        /// Given a string containing a directory, this function will return 
        /// the number of total bytes on the corresponding filesystem or disk partition. 
        /// </summary>
        /// <param name="directory">The directory specifying the filesystem or disk partition to be examined.</param>
        /// <returns>Total nuber of bytes on the specified filesystem or disk partition or <c>FALSE</c> on an error.</returns>
        [return: CastToFalse]
        public static double disk_total_space(string directory) => GetDiskFreeSpaceInternal(directory, true);

        /// <summary>
        /// Given a string containing a directory, this function will return 
        /// the number of bytes (total or free depending on <paramref name="total"/> 
        /// on the corresponding filesystem or disk partition. 
        /// </summary>
        /// <param name="directory">The directory specifying the filesystem or disk partition to be examined.</param>
        /// <param name="total"><c>true</c> to return total space available, <c>false</c> to return free space only.</param>
        /// <returns>Nuber of bytes available or <c>FALSE</c> on an error.</returns>
        private static double GetDiskFreeSpaceInternal(string directory, bool total)
        {
            // TODO: System.IO.FileSystem.DriveInfo 4.3.0
            throw new NotImplementedException();
        }

        #endregion
    }
}
