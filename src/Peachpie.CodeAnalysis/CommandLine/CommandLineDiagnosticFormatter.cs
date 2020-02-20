using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Pchp.CodeAnalysis.CommandLine
{
    internal sealed class CommandLineDiagnosticFormatter : DiagnosticFormatter
    {
        private readonly string _baseDirectory;
        private readonly Lazy<string> _lazyNormalizedBaseDirectory;
        private readonly bool _displayFullPaths;

        internal CommandLineDiagnosticFormatter(string baseDirectory, bool displayFullPaths)
        {
            _baseDirectory = baseDirectory;
            _displayFullPaths = displayFullPaths;
            _lazyNormalizedBaseDirectory = new Lazy<string>(() => FileUtilities.TryNormalizeAbsolutePath(baseDirectory));
        }

        internal override string FormatSourcePath(string path, string basePath, IFormatProvider formatter)
        {
            var normalizedPath = FileUtilities.NormalizeRelativePath(path, basePath, _baseDirectory);
            if (normalizedPath == null)
            {
                return path;
            }

            // By default, specify the name of the file in which an error was found.
            // When The /fullpaths option is present, specify the full path to the file.
            return _displayFullPaths ? normalizedPath : RelativizeNormalizedPath(normalizedPath);
        }

        /// <summary>
        /// Get the path name starting from the <see cref="_baseDirectory"/>
        /// </summary>
        internal string RelativizeNormalizedPath(string normalizedPath)
        {
            var normalizedBaseDirectory = _lazyNormalizedBaseDirectory.Value;
            if (normalizedBaseDirectory == null)
            {
                return normalizedPath;
            }

            var normalizedDirectory = PathUtilities.GetDirectoryName(normalizedPath);
            if (PathUtilities.IsSameDirectoryOrChildOf(normalizedDirectory, normalizedBaseDirectory))
            {
                return normalizedPath.Substring(
                    PathUtilities.IsDirectorySeparator(normalizedBaseDirectory.Last())
                        ? normalizedBaseDirectory.Length
                        : normalizedBaseDirectory.Length + 1);
            }

            return normalizedPath;
        }
    }
}
