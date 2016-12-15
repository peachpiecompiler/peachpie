using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    public static class PathUtils
    {
        public const char DirectorySeparator = '\\';
        public const char AltDirectorySeparator = '/';

        public static bool IsDirectorySeparator(this char ch) => ch == DirectorySeparator || ch == AltDirectorySeparator;
    }

    #region FileSystemUtils

    /// <summary>
    /// File system utilities.
    /// </summary>
    public static partial class FileSystemUtils
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

            int url_start = url.LastIndexOf("://");
            if (url_start > 0)
            {
                url_start += "://".Length;
                int pass_end = url.IndexOf('@', url_start);
                if (pass_end > url_start)
                {
                    StringBuilder sb = new StringBuilder(url.Length);
                    sb.Append(url.Substring(0, url_start));
                    sb.Append("...");
                    sb.Append(url.Substring(pass_end));  // results in: scheme://...@host
                    return sb.ToString();
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
    }

    #endregion
}
