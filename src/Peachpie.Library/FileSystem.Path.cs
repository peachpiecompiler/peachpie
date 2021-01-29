using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pchp.Core.Utilities;

namespace Pchp.Library
{
    public static partial class PhpPath
    {
        #region Constants

        /// <summary>
        /// Fill the "dirname" field in results.
        /// </summary>
        public const int PATHINFO_DIRNAME = (int)PathInfoOptions.DirName;

        /// <summary>
        /// Fill the "basename" field in results.
        /// </summary>
        public const int PATHINFO_BASENAME = (int)PathInfoOptions.BaseName;

        /// <summary>
        /// Fill the "extension" field in results.
        /// </summary>
        public const int PATHINFO_EXTENSION = (int)PathInfoOptions.Extension;

        /// <summary>
        /// Fill the "filename" field in results. Since PHP 5.2.0.
        /// </summary>
        public const int PATHINFO_FILENAME = (int)PathInfoOptions.FileName;

        #endregion

        #region basename, dirname, pathinfo

        /// <summary>
        /// The flags indicating which fields the <see cref="pathinfo(string, PathInfoOptions)"/>
        /// method should fill in the result array.
        /// </summary>
        [Flags]
        public enum PathInfoOptions
        {
            /// <summary>
            /// Fill the "dirname" field in results.
            /// </summary>
            DirName = 1,

            /// <summary>
            /// Fill the "basename" field in results.
            /// </summary>
            BaseName = 2,

            /// <summary>
            /// Fill the "extension" field in results.
            /// </summary>
            Extension = 4,

            /// <summary>
            /// Fill the "filename" field in results. Since PHP 5.2.0.
            /// </summary>
            FileName = 8,

            /// <summary>
            /// All the four options result in an array returned by <see cref="PhpPath.GetInfo"/>.
            /// </summary>
            All = DirName | BaseName | Extension | FileName
        }

        /// <summary>
        /// Returns path component of path.
        /// </summary>
        /// <remarks>
        /// Given a <see cref="string"/> containing a path to a file, this function will return the base name of the file. 
        /// If the path ends in this will also be cut off. 
        /// On Windows, both slash (/) and backslash (\) are used as path separator character. 
        /// In other environments, it is the forward slash (/). 
        /// </remarks>
        /// <param name="path">A <see cref="string"/> containing a path to a file.</param>
        /// <param name="suffix">A <see cref="string"/> containing suffix to be cut off the path if present.</param>
        /// <returns>The path conponent of the given <paramref name="path"/>.</returns>
        public static string basename(string path, string suffix = null)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            int end = path.Length - 1;
            while (end >= 0 && path[end].IsDirectorySeparator()) end--;

            int start = end;
            while (start >= 0 && !path[start].IsDirectorySeparator()) start--;
            start++;

            int name_length = end - start + 1;
            if (!string.IsNullOrEmpty(suffix) &&
                suffix.Length < name_length &&
                String.Compare(path, end - suffix.Length + 1, suffix, 0, suffix.Length, StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                name_length -= suffix.Length;
            }

            return path.Substring(start, name_length);
        }

        /// <summary>
        /// Returns directory name component of path.
        /// </summary>
        /// <param name="path">The full path.</param>
        /// <param name="levels">The number of parent directories to go up. Must be greater than zero.</param>
        /// <returns>The directory portion of the given path.</returns>
        public static string dirname(string path, int levels = 1)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            if (levels < 1) throw new ArgumentOutOfRangeException(nameof(levels));

            var pathspan = path.AsSpan();

            while (levels-- > 0)
            {
                pathspan = dirname(pathspan);
            }

            //
            return pathspan.ToString();
        }

        static ReadOnlySpan<char> dirname(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
            {
                return ReadOnlySpan<char>.Empty;
            }

            if (path.IndexOfAny(CurrentPlatform.DirectorySeparator, CurrentPlatform.AltDirectorySeparator) < 0)
            {
                // If there are no slashes in path, a dot ('.') is returned, indicating the current directory
                return ".".AsSpan();
            }

            int start = 0;
            int end = path.Length - 1;

            // advance start position beyond drive specifier:
            if (path.Length >= 2 && path[1] == ':' && (path[0] >= 'a' && path[0] <= 'z' || path[0] >= 'A' && path[0] <= 'Z'))
            {
                start = 2;
                if (path.Length == 2)
                {
                    return path;
                }
            }

            // strip slashes from the end:
            while (end >= start && path[end].IsDirectorySeparator()) end--;
            if (end < start)
                return (path.Slice(0, end + 1).ToString() + CurrentPlatform.DirectorySeparator).AsSpan();

            // strip file name:
            while (end >= start && !path[end].IsDirectorySeparator()) end--;
            if (end < start)
                return (path.Slice(0, end + 1).ToString() + ".").AsSpan();

            // strip slashes from the end:
            while (end >= start && path[end].IsDirectorySeparator()) end--;
            if (end < start)
                return (path.Slice(0, end + 1).ToString() + CurrentPlatform.DirectorySeparator).AsSpan();

            // result:
            return path.Slice(0, end + 1);
        }

        /// <summary>
        /// Extracts part(s) from a specified path.
        /// </summary>
        /// <param name="path">The path to be parsed.</param>
        /// <param name="options">Flags determining the result.</param>
        /// <returns>
        /// If <paramref name="options"/> is <see cref="PathInfoOptions.All"/> then returns array
        /// keyed by <c>"dirname"</c>, <c>"basename"</c>, and <c>"extension"</c>. Otherwise,
        /// it returns string value containing a single part of the path.
        /// </returns>
        public static PhpValue pathinfo(string path, PathInfoOptions options = PathInfoOptions.All)
        {
            // collect strings
            string dirname = null, basename = null, extension = null, filename = null;

            if ((options & PathInfoOptions.BaseName) != 0 ||
                (options & PathInfoOptions.Extension) != 0 ||
                (options & PathInfoOptions.FileName) != 0)
                basename = PhpPath.basename(path);

            if ((options & PathInfoOptions.DirName) != 0)
                dirname = PhpPath.dirname(path);

            if ((options & PathInfoOptions.Extension) != 0)
            {
                int last_dot = basename.LastIndexOf('.');
                if (last_dot >= 0)
                    extension = basename.Substring(last_dot + 1);
            }

            if ((options & PathInfoOptions.FileName) != 0)
            {
                int last_dot = basename.LastIndexOf('.');
                if (last_dot >= 0)
                    filename = basename.Substring(0, last_dot);
                else
                    filename = basename;
            }

            // return requested value or all of them in an associative array
            if (options == PathInfoOptions.All)
            {
                var result = new PhpArray(4);
                result.Add("dirname", dirname);
                result.Add("basename", basename);
                result.Add("extension", extension);
                result.Add("filename", filename);
                return PhpValue.Create(result);
            }

            if ((options & PathInfoOptions.DirName) != 0)
                return PhpValue.Create(dirname);

            if ((options & PathInfoOptions.BaseName) != 0)
                return PhpValue.Create(basename);

            if ((options & PathInfoOptions.Extension) != 0)
                return PhpValue.Create(extension);

            if ((options & PathInfoOptions.FileName) != 0)
                return PhpValue.Create(filename);

            return PhpValue.Null;
        }

        #endregion

        #region tempnam, realpath, sys_get_temp_dir

        /// <summary>
        /// Creates a file with a unique path in the specified directory. 
        /// If the directory does not exist, <c>tempnam()</c> may generate 
        /// a file in the system's temporary directory, and return the name of that.
        /// </summary>
        /// <param name="ctx">The current runtime context.</param>
        /// <param name="dir">The directory where the temporary file should be created.</param>
        /// <param name="prefix">The prefix of the unique path.</param>
        /// <returns>A unique path for a temporary file 
        /// in the given <paramref name="dir"/>.</returns>
        [return: CastToFalse]
        public static string tempnam(Context ctx, string dir, string prefix)
        {
            // makes "dir" a valid directory:
            dir = FileSystemUtils.AbsolutePath(ctx, dir);                       // Resolve to current working directory (Context.WorkingDirectory)
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir))
            {
                dir = Path.GetTempPath();
            }
            else
            {
                dir += Path.DirectorySeparatorChar;
            }

            // makes "prefix" a valid file prefix:
            if (string.IsNullOrEmpty(prefix) || prefix.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                prefix = "tmp_";
            }

            var suffix = unchecked((ulong)System.DateTime.UtcNow.Ticks / 5) & 0xffff;
            string result;

            try
            {
                for (; ; suffix++)
                {
                    result = string.Concat(dir, prefix, suffix.ToString("x4"), ".tmp");
                    if (!File.Exists(result))
                    {
                        try
                        {
                            File.Open(result, FileMode.CreateNew).Close();
                            break;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // try system temp directory:
                            dir = Path.GetTempPath();
                        }
                        catch (PathTooLongException e)
                        {
                            PhpException.Throw(PhpError.Notice, PhpException.ToErrorMessage(e.Message));
                            return Path.GetTempFileName();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Notice, PhpException.ToErrorMessage(e.Message));
                return null;
            }

            return result;
        }

        /// <summary>
        /// Returns the path of the directory PHP stores temporary files in by default.
        /// </summary>
        /// <returns>Returns the path of the temporary directory.</returns>
        /// <remarks>Path ends with "\"</remarks>
        public static string sys_get_temp_dir() => Path.GetTempPath();

        ///// <summary>
        ///// A counter used to generate unique filenames for <see cref="tempnam(string, string)"/>.
        ///// </summary>
        //static int _tempCounter = 0;

        /// <summary>
        /// Returns canonicalized absolute path name.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">Arbitrary path.</param>
        /// <returns>
        /// The given <paramref name="path"/> combined with the current working directory or
        /// <B>false</B> if the path is invalid or doesn't exists.
        /// </returns>
        [return: CastToFalse]
        public static string realpath(Context ctx, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return ctx.WorkingDirectory;
            }

            // string ending slash
            path = path.TrimEndSeparator();

            //
            var realpath = FileSystemUtils.AbsolutePath(ctx, path);

            if (File.Exists(realpath) ||
                System.IO.Directory.Exists(realpath) ||
                Context.TryResolveScript(ctx.RootPath, realpath).IsValid)   // check a compiled script
            {
                return realpath;
            }

            return null;
        }

        #endregion
    }
}
