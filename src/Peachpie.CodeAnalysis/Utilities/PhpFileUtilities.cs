using System;
using System.Linq;
using Roslyn.Utilities;

namespace Pchp.CodeAnalysis.Utilities
{
    internal static class PhpFileUtilities
    {
        /// <summary>
        /// Normalizes backward slashes to forward slashes.
        /// </summary>
        public static string NormalizeSlashes(string path) => path.Replace('\\', '/').Replace("//", "/");

        public static string GetRelativePath(string path, string basedir)
        {
            int levelups = 0;

            while (!path.StartsWith(basedir, StringComparison.CurrentCultureIgnoreCase))
            {
                levelups++;
                basedir = PathUtilities.GetDirectoryName(basedir)
                    .TrimEnd(PathUtilities.AltDirectorySeparatorChar, PathUtilities.DirectorySeparatorChar);

                if (basedir == null)
                {
                    throw new ArgumentException();  // cannot make relative path
                }

                if (levelups > 64)
                {
                    return path;
                }
            }

            return
                string.Join(string.Empty, Enumerable.Repeat(".." + PathUtilities.DirectorySeparatorStr, levelups)) +
                path.Substring(basedir.Length + 1);
        }
    }
}
