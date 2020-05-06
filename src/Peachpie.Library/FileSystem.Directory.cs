using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;


namespace Pchp.Library
{
    #region PHP class: Directory

    /// <summary>
    /// User-like class encapsulating enumeration of a Directory. 
    /// Uses the PhpDirectory implementation upon PhpWrapper streams.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("standard")]
    public class Directory
    {
        #region Fields

        /// <summary>
        /// Reference to the directory listing resource.
        /// </summary>
        public PhpValue handle = PhpValue.Null;

        /// <summary>
        /// The opened path (accessible from the PHP script).
        /// </summary>
        public PhpValue path = PhpValue.Null;

        #endregion

        #region Construction

        /// <summary>
        /// Start listing of a directory (intended to be used from C#).
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="directory">The path to the directory.</param>
        public Directory(Context ctx, string directory)
        {
            this.path = (PhpValue)directory;
            this.handle = PhpValue.FromClass(PhpDirectory.opendir(ctx, directory));
        }

        #endregion

        #region read

        /// <summary>
        /// Read next directory entry.
        /// </summary>
        /// <returns>Filename of a contained file (including . and ..).</returns>
        [return: CastToFalse]
        public string read(PhpResource handle = null)
        {
            var res = handle ?? this.handle.AsResource();
            if (res != null)
            {
                return PhpDirectory.readdir(res);
            }
            else
            {
                PhpException.InvalidImplicitCast(nameof(handle), PhpResource.PhpTypeName, "read");
                return null;
            }
        }

        #endregion

        #region rewind

        /// <summary>
        /// Restart the directory listing.
        /// </summary>
        public virtual void rewind(PhpResource handle = null)
        {
            var res = handle ?? this.handle.AsResource();
            if (res != null)
            {
                PhpDirectory.rewinddir(res);
            }
            else
            {
                PhpException.InvalidImplicitCast(nameof(handle), PhpResource.PhpTypeName, "rewind");
            }
        }

        #endregion

        #region close

        /// <summary>
        /// Finish the directory listing.
        /// </summary>
        public virtual void close(PhpResource handle = null)
        {
            var res = handle ?? this.handle.AsResource();
            if (res != null)
            {
                PhpDirectory.closedir(res);
            }
            else
            {
                PhpException.InvalidImplicitCast(nameof(handle), PhpResource.PhpTypeName, "close");
            }
        }

        #endregion
    }

    #endregion

    #region DirectoryListing

    /// <summary>
    /// Enumeration class used for PhpDirectory listings - serves as a PhpResource.
    /// Uses the PhpWrapper stream wrappers only to generate the list of contained files.
    /// No actual resources to be released explicitly.
    /// </summary>
    internal sealed class DirectoryListing : PhpResource
    {
        public readonly IEnumerator<string> Enumerator;

        readonly PhpDirectory.PhpDirectoryContext _dirctx;

        const string DirectoryListingName = "stream";

        //private static int DirectoryListingType = PhpResource.RegisterType(DirectoryListingName);
        // Note: PHP uses the stream mechanism listings (opendir etc.)
        // this is the same but a) faster, b) more memory expensive for large directories
        // (and unfinished listings in script)

        public DirectoryListing(PhpDirectory.PhpDirectoryContext dirctx, IEnumerable<string> listing)
            : base(DirectoryListingName)
        {
            Debug.Assert(listing != null);
            Debug.Assert(dirctx != null);

            _dirctx = dirctx;

            this.Enumerator = listing.GetEnumerator();
        }

        protected override void FreeManaged()
        {
            //
            if (ReferenceEquals(this, _dirctx.LastDirHandle))
            {
                _dirctx.LastDirHandle = null;
            }

            //
            this.Enumerator.Dispose();
        }
    }

    #endregion

    /// <summary>
    /// Gives access to the directory manipulation and itereation.
    /// </summary>
    [PhpExtension("standard")]
    public static partial class PhpDirectory
    {
        #region PhpDirectoryContext

        internal sealed class PhpDirectoryContext
        {
            /// <summary>
            /// Last handle opened by <c>opendir</c>.
            /// </summary>
            public DirectoryListing LastDirHandle { get; set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="ctx"></param>
            /// <returns></returns>
            public static PhpDirectoryContext GetContext(Context ctx) => ctx.GetStatic<PhpDirectoryContext>();
        }

        #endregion

        #region getcwd, chdir, chroot

        /// <summary>Gets the virtual working directory of the current script.</summary>
        /// <remarks></remarks>
        /// <returns>Absolute path to the current directory.</returns>
        public static string getcwd(Context ctx)
        {
            return ctx.WorkingDirectory ?? string.Empty;
        }

        /// <summary>Changes the virtual working directory for the current script.</summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="directory">Absolute or relative path to the new working directory.</param>
        /// <returns>Returns <c>true</c> on success or <c>false</c> on failure.</returns>
        /// <exception cref="PhpException">If the specified directory does not exist.</exception>
        public static bool chdir(Context ctx, string directory)
        {
            if (directory != null)
            {
                string newPath = FileSystemUtils.AbsolutePath(ctx, directory);
                if (System.IO.Directory.Exists(newPath))
                {
                    // Note: open_basedir not applied here, URL will not pass through
                    ctx.WorkingDirectory = newPath;
                    return true;
                }
            }
            PhpException.Throw(PhpError.Warning, string.Format(Resources.LibResources.directory_not_found, directory));
            return false;
        }

        /// <summary>
        /// Changes the root directory of the current process to <paramref name="directory"/>.
        /// Not supported.
        /// </summary>
        /// <remarks>
        /// This function is only available if your system supports it 
        /// and you're using the CLI, CGI or Embed SAPI. 
        /// Note: This function is not implemented on Windows platforms.
        /// </remarks>
        /// <param name="directory">The new value of the root directory.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool chroot(string directory)
        {
            PhpException.FunctionNotSupported("chroot");
            return false;
        }

        #endregion

        #region dir, opendir, readdir, rewinddir, closedir, scandir

        /// <summary>Returns an object encapsulating the directory listing mechanism on a given
        /// <paramref name="directory"/>.</summary>
        /// <remarks>A pseudo-object oriented mechanism for reading a directory. The given directory is opened. 
        /// Two properties are available once the directory has been opened. The handle property 
        /// can be used with other directory functions such as <c>readdir()</c>, <c>rewinddir()</c> and <c>closedir()</c>. 
        /// The path property is set to path the directory that was opened. 
        /// Three methods are available: <see cref="PHP.Library.Directory.read"/>, 
        /// <see cref="PHP.Library.Directory.rewind"/> and <see cref="PHP.Library.Directory.close"/>.</remarks>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="directory">The path to open for listing.</param>
        /// <returns>An instance of <see cref="PHP.Library.Directory"/>.</returns>
        public static Directory dir(Context ctx, string directory) => new Directory(ctx, directory);

        /// <summary>Returns a directory handle to be used in subsequent 
        /// <c>readdir()</c>, <c>rewinddir()</c> and <c>closedir()</c> calls.</summary>
        /// <remarks>
        /// <para>
        /// If path is not a valid directory or the directory can not 
        /// be opened due to permission restrictions or filesystem errors, 
        /// <c>opendir()</c> returns <c>false</c> and generates a PHP error of level <c>E_WARNING</c>. 
        /// </para>
        /// <para>
        /// As of PHP 4.3.0 path can also be any URL which supports directory listing, 
        /// however only the <c>file://</c> url wrapper supports this in PHP 4.3. 
        /// As of PHP 5.0.0, support for the <c>ftp://</c> url wrapper is included as well.
        /// </para>
        /// </remarks>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="directory">The path of the directory to be listed.</param>
        /// <returns>A <see cref="DirectoryListing"/> resource containing the listing.</returns>
        /// <exception cref="PhpException">In case the specified stream wrapper can not be found
        /// or the desired directory can not be opened.</exception>
        [return: CastToFalse]
        public static PhpResource opendir(Context ctx, string directory)
        {
            var dirctx = PhpDirectoryContext.GetContext(ctx);

            if (PhpStream.ResolvePath(ctx, ref directory, out var wrapper, CheckAccessMode.Directory, CheckAccessOptions.Empty))
            {
                var listing = wrapper.Listing(ctx.RootPath, directory, StreamListingOptions.Empty, null);
                if (listing != null)
                {
                    return dirctx.LastDirHandle = new DirectoryListing(dirctx, listing);
                }
            }

            //
            dirctx.LastDirHandle = null;
            return null;
        }

        /// <summary>
        /// Reads an entry from a directory handle. Uses last handle opened by <c>opendir</c>.
        /// </summary>
        [return: CastToFalse]
        public static string readdir(Context ctx)
        {
            return readdir(PhpDirectoryContext.GetContext(ctx).LastDirHandle);
        }

        /// <summary>
        /// Reads an entry from a directory handle.
        /// </summary>
        /// <param name="dirHandle">A <see cref="PhpResource"/> returned by <see cref="Open"/>.</param>
        /// <returns>
        /// Returns the path of the next file from the directory. 
        /// The filenames (including . and ..) are returned in the order 
        /// in which they are stored by the filesystem.
        /// </returns>
        [return: CastToFalse]
        public static string readdir(PhpResource dirHandle)
        {
            var enumerator = ValidListing(dirHandle);
            return (enumerator != null && enumerator.MoveNext())
                ? enumerator.Current
                : null;
        }

        /// <summary>
        /// Rewinds a directory handle. Uses last handle opened by <c>opendir</c>.
        /// </summary>
        public static void rewinddir(Context ctx)
        {
            rewinddir(PhpDirectoryContext.GetContext(ctx).LastDirHandle);
        }

        /// <summary>
        /// Rewinds a directory handle.
        /// Function has no return value.
        /// </summary>
        /// <param name="dirHandle">A <see cref="PhpResource"/> returned by <see cref="Open"/>.</param>
        /// <remarks>
        /// Resets the directory stream indicated by <paramref name="dirHandle"/> to the 
        /// beginning of the directory.
        /// </remarks>
        public static void rewinddir(PhpResource dirHandle)
        {
            ValidListing(dirHandle)?.Reset();
        }

        /// <summary>
        /// Closes a directory handle. Uses last handle opened by <c>opendir</c>.
		/// </summary>
        public static void closedir(Context ctx)
        {
            closedir(PhpDirectoryContext.GetContext(ctx).LastDirHandle);
        }

        /// <summary>
        /// Closes a directory handle.
        /// Function has no return value.
        /// </summary>
        /// <param name="dirHandle">A <see cref="PhpResource"/> returned by <see cref="Open"/>.</param>
        /// <remarks>
        /// Closes the directory stream indicated by <paramref name="dirHandle"/>. 
        /// The stream must have previously been opened by by <see cref="Open"/>.
        /// </remarks>
        public static void closedir(PhpResource dirHandle)
        {
            // Note: PHP allows other all stream resources to be closed with closedir().
            dirHandle?.Dispose();
        }

        public const int SCANDIR_SORT_ASCENDING = 0;
        public const int SCANDIR_SORT_DESCENDING = 1;
        public const int SCANDIR_SORT_NONE = 2;

        /// <summary>
        /// <see cref="scandir(Context, string, ScanDirSortOrder)"/> order options.
        /// </summary>
        public enum ScanDirSortOrder
        {
            Ascending = SCANDIR_SORT_ASCENDING,
            Descending = SCANDIR_SORT_DESCENDING,
            None = SCANDIR_SORT_NONE,
        }

        /// <summary>Lists files and directories inside the specified path.</summary>
        /// <remarks>
        /// Returns an array of files and directories from the <paramref name="directory"/>. 
        /// If <paramref name="directory"/> is not a directory, then boolean <c>false</c> is returned, 
        /// and an error of level <c>E_WARNING</c> is generated. 
        /// </remarks>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="directory">The directory to be listed.</param>
        /// <param name="sorting_order">
        /// By default, the listing is sorted in ascending alphabetical order. 
        /// If the optional sorting_order is used (set to <c>1</c>), 
        /// then sort order is alphabetical in descending order.</param>
        /// <returns>A <see cref="PhpArray"/> of filenames or <c>false</c> in case of failure.</returns>
        /// <exception cref="PhpException">In case the specified stream wrapper can not be found
        /// or the desired directory can not be opened.</exception>
        [return: CastToFalse]
        public static PhpArray scandir(Context ctx, string directory, ScanDirSortOrder sorting_order = ScanDirSortOrder.Ascending)
        {
            if (PhpStream.ResolvePath(ctx, ref directory, out var wrapper, CheckAccessMode.Directory, CheckAccessOptions.Empty))
            {
                var listing = wrapper.Listing(ctx.RootPath, directory, 0, null);
                if (listing != null)
                {
                    var ret = new PhpArray(listing); // create the array from the system one
                    switch (sorting_order)
                    {
                        case ScanDirSortOrder.Ascending:
                            Arrays.sort(ctx, ret, ComparisonMethod.String);
                            break;
                        case ScanDirSortOrder.Descending:
                            Arrays.rsort(ctx, ret, ComparisonMethod.String);
                            break;
                    }
                    return ret;
                }
            }
            return null; // false
        }

        /// <summary>
        /// Casts the given resource handle to the <see cref="DirectoryListing"/> enumerator.
        /// Throw an exception when a wrong argument is supplied.
        /// </summary>
        /// <param name="dir_handle">The handle passed to a PHP function.</param>
        /// <returns>The enumerator over the files in the DirectoryListing.</returns>
        /// <exception cref="PhpException">When the supplied argument is not a valid <see cref="DirectoryListing"/> resource.</exception>
        static IEnumerator<string> ValidListing(PhpResource dir_handle)
        {
            var listing = dir_handle as DirectoryListing;
            if (listing != null)
            {
                return listing.Enumerator;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, Resources.LibResources.invalid_directory_resource);
                return null;
            }
        }

        #endregion

        #region mkdir, rmdir

        /// <summary>
        /// Makes a directory or a branch of directories using the specified wrapper.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="pathname">The path to create.</param>
        /// <param name="mode">A combination of <see cref="StreamMakeDirectoryOptions"/>.</param>
        /// <param name="recursive">Create recursively.</param>
        /// <param name="context">Stream context, can be <c>null</c> to use default context.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool mkdir(Context ctx, string pathname, int mode = (int)FileModeFlags.ReadWriteExecute, bool recursive = false, PhpResource context = null)
        {
            StreamWrapper wrapper;
            return PhpStream.ResolvePath(ctx, ref pathname, out wrapper, CheckAccessMode.Directory, CheckAccessOptions.Empty)
                && wrapper.MakeDirectory(pathname, mode,
                    recursive ? StreamMakeDirectoryOptions.Recursive : StreamMakeDirectoryOptions.Empty,
                    (context as StreamContext) ?? StreamContext.Default);
        }

        /// <summary>
        /// Removes a directory.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="dirname"></param>
        /// <param name="context">Stream context. Can be <c>null</c> to use default context.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool rmdir(Context ctx, string dirname, StreamContext context = null)
        {
            StreamWrapper wrapper;
            return PhpStream.ResolvePath(ctx, ref dirname, out wrapper, CheckAccessMode.Directory, CheckAccessOptions.Empty)
                && wrapper.RemoveDirectory(dirname, StreamRemoveDirectoryOptions.Empty, (context as StreamContext) ?? StreamContext.Default);
        }

        #endregion
    }
}
