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
        #region Constants

        /// <summary>
        /// Options used in the <c>flags</c> argument of the 'fopen' function.
        /// </summary>
        [Flags, PhpHidden]
        public enum FileOpenOptions
        {
            /// <summary>Default option for the <c>flags</c> argument.</summary>
            Empty = 0,
            /// <summary>Search for the file in the <c>include_path</c> too (1).</summary>
            UseIncludePath = 0x1

            // UNUSED    /// <summary>Do not create a default context if none is provided (16).</summary>
            // UNUSED    [ImplementsConstant("FILE_NO_DEFAULT_CONTEXT")] NoDefaultContext = 0x10
        }

        /// <summary>
        /// Search for the file in the <c>include_path</c> too (1).
        /// </summary>
        public const int FILE_USE_INCLUDE_PATH = (int)FileOpenOptions.UseIncludePath;

        /// <summary>
        /// Options used in the <c>flags</c> argument of PHP Filesystem functions.
        /// </summary>
        [Flags, PhpHidden]
        public enum FileOptions
        {
            /// <summary>
            /// Default.
            /// </summary>
            Empty = 0,

            /// <summary>
            /// Search for the file in the <c>include_path</c> too (1).
            /// </summary>
            UseIncludePath = FileOpenOptions.UseIncludePath,

            /// <summary>
            /// Do not include the line break characters to the result in <c>file()</c> (2).
            /// </summary>
            TrimLineEndings = 2,

            /// <summary>
            /// Do not include empty lines to the resulting <see cref="PhpArray"/> in <c>file()</c> (4).
            /// </summary>
            SkipEmptyLines = 4
        }

        /// <summary>
        /// Do not include the line break characters to the result in <c>file()</c> (2).
        /// </summary>
        public const int FILE_IGNORE_NEW_LINES = (int)FileOptions.TrimLineEndings;

        /// <summary>
        /// Do not include empty lines to the resulting <see cref="PhpArray"/> in <c>file()</c> (4).
        /// </summary>
        public const int FILE_SKIP_EMPTY_LINES = (int)FileOptions.SkipEmptyLines;

        /// <summary>
        /// The options used as the <c>flag</c> argument of <see cref="PhpPath.file_put_contents"/>.
        /// </summary>
        [Flags, PhpHidden]
        public enum WriteContentsOptions
        {
            /// <summary>
            /// Empty option (default).
            /// </summary>
            Empty = 0,

            /// <summary>
            /// Search for the file in the <c>include_path</c> too (1).
            /// </summary>
            UseIncludePath = FileOptions.UseIncludePath,

            /// <summary>
            /// Append the given data at the end of the file in <c>file_put_contents</c> (8).
            /// </summary>
            AppendContents = 8,

            /// <summary>
            /// Acquire an exclusive lock on the file.
            /// </summary>
            LockExclusive = 2, // StreamLockOptions.Exclusive
        }

        /// <summary>
        /// Append the given data at the end of the file in <c>file_put_contents</c> (8).
        /// </summary>
        public const int FILE_APPEND = (int)WriteContentsOptions.AppendContents;

        /// <summary>
        /// Name of variable that is filled with response headers in case of file_get_contents and http protocol.
        /// </summary>
        private const string HttpResponseHeaderName = "http_response_header";

        #endregion

        #region fopen, tmpfile, fclose, feof, fflush

        /// <summary>
        /// Opens filename or URL using a registered StreamWrapper.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">The file to be opened. The schema part of the URL specifies the wrapper to be used.</param>
        /// <param name="mode">The read/write and text/binary file open mode.</param>
        /// <param name="flags">If set to true, then the include path is searched for relative filenames too.</param>
        /// <returns>The file resource or false in case of failure.</returns>
        [return: CastToFalse]
        public static PhpResource fopen(Context ctx, string path, string mode, FileOpenOptions flags = FileOpenOptions.Empty)
        {
            return fopen(ctx, path, mode, flags, StreamContext.Default);
        }

        /// <summary>
        /// Opens filename or URL using a registered StreamWrapper.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">The file to be opened. The schema part of the URL specifies the wrapper to be used.</param>
        /// <param name="mode">The read/write and text/binary file open mode.</param>
        /// <param name="flags">If set to true, then the include path is searched for relative filenames too.</param>
        /// <param name="context">A script context to be provided to the StreamWrapper.</param>
        /// <returns>The file resource or false in case of failure.</returns>
        [return: CastToFalse]
        public static PhpResource fopen(Context ctx, string path, string mode, FileOpenOptions flags, PhpResource context)
        {
            StreamContext sc = StreamContext.GetValid(context);
            if (sc == null) return null;

            if (string.IsNullOrEmpty(path))
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:empty", "path"));
                //return null;
                throw new ArgumentException(nameof(path));
            }

            if (string.IsNullOrEmpty(mode))
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:empty", "mode"));
                //return null;
                throw new ArgumentException(nameof(mode));
            }

            return PhpStream.Open(ctx, path, mode, ProcessOptions(ctx, flags), sc);
        }

        /// <summary>
        /// Prevents invalid options from the the options argument for StreamWrapper.Open().
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="flags">Flags passed to stream opening functions.</param>
        /// <returns>The StreamOpenFlags combination for the given arguments.</returns>
        static StreamOpenOptions ProcessOptions(Context ctx, FileOpenOptions flags)
        {
            StreamOpenOptions options = 0;

            if ((flags & FileOpenOptions.UseIncludePath) > 0)
                options |= StreamOpenOptions.UseIncludePath;

            if (!ctx.ErrorReportingDisabled)
                options |= StreamOpenOptions.ReportErrors;

            return options;
        }

        /// <summary>
        /// Creates a temporary file.
        /// </summary>
        /// <remarks>
        /// Creates a temporary file with an unique name in write mode, 
        /// returning a file handle similar to the one returned by fopen(). 
        /// The file is automatically removed when closed (using fclose()), 
        /// or when the script ends.
        /// </remarks>
        /// <returns></returns>
        public static PhpResource tmpfile(Context ctx)
        {
            string path = tempnam(string.Empty, "php");

            StreamWrapper wrapper;
            if (!PhpStream.ResolvePath(ctx, ref path, out wrapper, CheckAccessMode.FileMayExist, CheckAccessOptions.Empty))
                return null;

            return wrapper.Open(ctx, ref path, "w+b", StreamOpenOptions.Temporary, StreamContext.Default);
        }

        /// <summary>
		/// Close an open file pointer.
		/// </summary>
		/// <param name="handle">A PhpResource passed to the PHP function.</param>
		/// <returns>True if successful.</returns>
		public static bool fclose(PhpResource handle)
        {
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return false;
            }

            if (stream.IsPersistent)
            {
                // Do not close persisten streams (incl. for example STDOUT).
                stream.Flush();
            }
            else
            {
                stream.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Tests for end-of-file on a file pointer.
        /// </summary>
        /// <param name="handle">A PhpResource passed to the PHP function.</param>
        /// <returns>True if successful.</returns>
        public static bool feof(PhpResource handle)
        {
            PhpStream stream = PhpStream.GetValid(handle);
            return stream != null && stream.Eof;
        }

        /// <summary>
        /// Flushes the output to a file.
        /// </summary>
        /// <param name="handle">A PhpResource passed to the PHP function.</param>
        /// <returns>True if successful.</returns>
        public static bool fflush(PhpResource handle)
        {
            PhpStream stream = PhpStream.GetValid(handle);
            return stream != null && stream.Flush();
        }

        #endregion
    }
}
