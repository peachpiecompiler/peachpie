using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Pchp.Core.Resources;

namespace Pchp.Core.Utilities
{
    #region PathUtils

    public static class PathUtils
    {
        /// <summary>
        /// Windows-style path separator (back slash).
        /// </summary>
        public const char DirectorySeparator = '\\';
        
        /// <summary>
        /// Linux-style path separator (forward slash).
        /// </summary>
        public const char AltDirectorySeparator = '/';
        
        static readonly char[] s_DirectorySeparators = new[] { DirectorySeparator, AltDirectorySeparator };

        public static bool IsDirectorySeparator(this char ch) => ch == DirectorySeparator || ch == AltDirectorySeparator;
        
        public static string TrimEndSeparator(this string path)
        {
            return IsDirectorySeparator(path.LastChar())
                ? path.Remove(path.Length - 1)
                : path;
        }

        public static ReadOnlySpan<char> TrimFileName(string path)
        {
            var index = path.LastIndexOfAny(s_DirectorySeparators);
            return (index <= 0)
                ? ReadOnlySpan<char>.Empty
                : path.AsSpan(0, index);
        }
    }

    #endregion

    #region FileSystemUtils

    /// <summary>
    /// File system utilities.
    /// </summary>
    public static class FileSystemUtils
    {
        /// <summary>
        /// Returns the given URL without the username/password information.
        /// </summary>
        /// <remarks>
        /// Removes the text between the last <c>"://"</c> and the following <c>'@'</c>.
        /// Does not check the URL for validity. Works for php://filter paths too.
        /// </remarks>
        /// <param name="url">The URL to modify.</param>
        /// <returns>The given URL with the username:password section replaced by <c>"..."</c>.</returns>
        public static string StripPassword(string url)
        {
            if (url == null) return null;

            int url_start = url.LastIndexOf("://", StringComparison.Ordinal);
            if (url_start > 0)
            {
                url_start += "://".Length;
                int pass_end = url.IndexOf('@', url_start);
                if (pass_end > url_start)
                {
                    var sb = new StringBuilder(url.Length);
                    sb.Append(url, 0, url_start);
                    sb.Append("...");
                    sb.Append(url, pass_end, url.Length - pass_end);  // results in: scheme://...@host
                    url = sb.ToString();
                }
            }

            return url;
        }

        //public static int FileSize(FileInfo fi)//TODO: Move this to PlatformAdaptationLayer
        //{
        //    if (EnvironmentUtils.IsDotNetFramework)
        //    {
        //        // we are not calling full stat(), it is slow
        //        return (int)fi.Length;
        //    }
        //    else
        //    {
        //        //bypass Mono bug in FileInfo.Length
        //        using (FileStream stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        //        {
        //            return unchecked((int)stream.Length);
        //        }
        //    }
        //}

        ///// <summary>
        ///// Gets the time given <paramref name="fsi"/> was modified. Mostly it is the <see cref="FileSystemInfo.LastWriteTimeUtc"/>
        ///// however if the file was modified elsewhere and copied, <see cref="FileSystemInfo.CreationTimeUtc"/> may be greater.
        ///// </summary>
        ///// <param name="fsi">File or a directory.</param>
        ///// <returns>Max of <see cref="FileSystemInfo.LastWriteTimeUtc"/> and <see cref="FileSystemInfo.CreationTimeUtc"/>.</returns>
        //public static DateTime GetLastModifiedTimeUtc(this FileSystemInfo fsi)
        //{
        //    Debug.Assert(fsi != null);
        //    return DateTimeUtils.Max(fsi.LastWriteTimeUtc, fsi.CreationTimeUtc);
        //}

        ///// <summary>
        ///// Gets the time given file at <paramref name="path"/> was modified. Mostly it is the <see cref="FileSystemInfo.LastWriteTimeUtc"/>
        ///// however if the file was modified elsewhere and copied, <see cref="FileSystemInfo.CreationTimeUtc"/> may be greater.
        ///// </summary>
        ///// <param name="path">Path to the file.</param>
        ///// <returns>Max of <see cref="FileSystemInfo.LastWriteTimeUtc"/> and <see cref="FileSystemInfo.CreationTimeUtc"/>.</returns>
        //public static DateTime GetLastModifiedTimeUtc(string path)
        //{
        //    return GetLastModifiedTimeUtc(new FileInfo(path));
        //}

        #region Scheme, Url, Absolute Path

        /// <summary>
        /// Wrapper-safe method of getting the schema portion from an URL.
        /// </summary>
        /// <param name="path">A <see cref="string"/> containing an URL or a local filesystem path.</param>
        /// <returns>
        /// The schema portion of the given <paramref name="path"/> or <c>"file"</c>
        /// for a local filesystem path.
        /// </returns>
        /// <exception cref="ArgumentException">Invalid path.</exception>
        public static string GetScheme(string/*!*/ path)
        {
            if (TryGetScheme(path, out var schemespan) && !Path.IsPathRooted(path))
            {
                return schemespan.ToString();
            }

            // When there is not scheme present (or it's a local path) return "file".
            return "file";
        }

        /// <summary>
        /// Wrapper-safe method of getting the schema portion from an URL.
        /// </summary>
        /// <param name="value">A <see cref="string"/> containing an URL or a local filesystem path.</param>
        /// <param name="scheme">Resulting scheme if any.</param>
        /// <returns>Whether given value contains the scheme.</returns>
        public static bool TryGetScheme(string value, out ReadOnlySpan<char> scheme)
        {
            Debug.Assert(value != null);

            if (value.Length > 3)
            {
                var colon_index = value.IndexOf(':', 1, Math.Min(value.Length - 1, 6)); // examine no more than 6 characters
                if (colon_index > 0 && colon_index < value.Length - 3 && value[colon_index + 1] == '/' && value[colon_index + 2] == '/') // "://"
                {
                    scheme = value.AsSpan(0, colon_index);
                    return true;
                }
            }

            //

            scheme = default;
            return false;
        }

        /// <summary>
        /// Concatenates a scheme with the given absolute path if necessary.
        /// </summary>
        /// <param name="absolutePath">Absolute path.</param>
        /// <returns>The given url or absolute path preceded by a <c>file://</c>.</returns>
        /// <exception cref="ArgumentException">Invalid path.</exception>
        public static string GetUrl(string/*!*/ absolutePath)
        {
            // Assert that the path is absolute
            //Debug.Assert(
            //    !string.IsNullOrEmpty(absolutePath) &&
            //    (absolutePath.IndexOf(':') > 0 ||   // there is a protocol (http://) or path is rooted (c:\)
            //        (Path.VolumeSeparatorChar != ':' && // or on linux, if there is no protocol, file path is rooted
            //            (absolutePath[0] == Path.DirectorySeparatorChar || absolutePath[0] == Path.AltDirectorySeparatorChar)))
            //    );

            if (Path.IsPathRooted(absolutePath))
                return String.Concat("file://", absolutePath);

            // Otherwise assume that it's the string before first ':'.
            return absolutePath;
        }

        /// <summary>
        /// Returns the given filesystem url without the scheme.
        /// </summary>
        /// <param name="path">A path or url of a local filesystem file.</param>
        /// <returns>The filesystem path or <b>null</b> if the <paramref name="path"/> is not a local file.</returns>
        /// <exception cref="ArgumentException">Invalid path.</exception>
        public static string GetFilename(string/*!*/ path)
        {
            if (path.IndexOf(':') == -1 || Path.IsPathRooted(path)) return path;
            if (path.IndexOf("file://", StringComparison.Ordinal) == 0) return path.Substring("file://".Length);
            return null;
        }

        /// <summary>
        /// Check if the given path is a remote url.
        /// </summary>
        /// <param name="url">The path to test.</param>
        /// <returns><c>true</c> if it's a fully qualified name of a remote resource.</returns>
        /// <exception cref="ArgumentException">Invalid path.</exception>
        public static bool IsRemoteFile(string/*!*/ url)
        {
            return GetScheme(url) != "file";
        }

        /// <summary>
        /// Check if the given path is a path to a local file.
        /// </summary>
        /// <param name="url">The path to test.</param>
        /// <returns><c>true</c> if it's not a fully qualified name of a remote resource.</returns>
        /// <exception cref="ArgumentException">Invalid path.</exception>
        public static bool IsLocalFile(string/*!*/ url)
        {
            return GetScheme(url) == "file";
        }

        /// <summary>
        /// Merges the path with the current working directory
        /// to get a canonicalized absolute pathname representing the same path
        /// (local files only). If the provided <paramref name="path"/>
        /// is absolute (rooted local path or an URL) it is returned unchanged.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">An absolute or relative path to a directory or an URL.</param>
        /// <returns>Canonicalized absolute path in case of a local directory or the original 
        /// <paramref name="path"/> in case of an URL.</returns>
        public static string AbsolutePath(Context ctx, string path)
        {
            // Don't combine remote file paths with CWD.
            try
            {
                if (IsRemoteFile(path))
                    return path;

                // Remove the file:// schema if any.
                path = GetFilename(path);

                // Combine the path and simplify it.
                string combinedPath = Path.Combine(ctx.WorkingDirectory ?? string.Empty, path);

                // Note: GetFullPath handles "C:" incorrectly
                if (combinedPath[combinedPath.Length - 1] == ':')
                {
                    combinedPath += PathUtils.DirectorySeparator;
                }

                return Path.GetFullPath(combinedPath);
            }
            catch (System.Exception)
            {
                PhpException.Throw(PhpError.Notice, string.Format(ErrResources.invalid_path, StripPassword(path)));
                return null;
            }
        }

        #endregion
    }

    #endregion
}
