using Pchp.Core;
using Pchp.Core.Resources;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Pchp.Library.Streams.PhpStreams;

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
        /// <param name="context">A script context to be provided to the StreamWrapper.</param>
        /// <returns>The file resource or false in case of failure.</returns>
        [return: CastToFalse]
        public static PhpResource fopen(Context ctx, string path, string mode, FileOpenOptions flags = FileOpenOptions.Empty, PhpResource context = null)
        {
            StreamContext sc = StreamContext.GetValid(context, true);
            if (sc == null) return null;

            if (string.IsNullOrEmpty(path))
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg_empty", "path"));
                //return null;
                throw new ArgumentException(nameof(path));
            }

            if (string.IsNullOrEmpty(mode))
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg_empty", "mode"));
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
        [return: CastToFalse]
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

        #region fprintf, fscanf

        /// <summary>
        /// Writes the string formatted using <c>sprintf</c> to the given stream.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="handle">A stream opened for writing.</param>
        /// <param name="format">The format string. For details, see PHP manual.</param>
        /// <param name="arguments">The arguments.
        /// See <A href="http://www.php.net/manual/en/function.sprintf.php">PHP manual</A> for details.
        /// Besides, a type specifier "%C" is applicable. It converts an integer value to Unicode character.</param>
        /// <returns>Number of characters written of <c>false</c> in case of an error.</returns>
        [return: CastToFalse]
        public static int fprintf(Context ctx, PhpResource handle, string format, params PhpValue[] arguments)
        {
            var formatted = Strings.sprintf(ctx, format, arguments);
            return string.IsNullOrEmpty(formatted)
                ? 0
                : WriteInternal(ctx, handle, new PhpString(formatted), -1);
        }

        /// <summary>
        /// Parses input from a file according to a format.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="format"></param>
        /// <returns>A <see cref="PhpArray"/> containing the parsed values.</returns>
        [return: CastToFalse]
        public static PhpArray fscanf(PhpResource handle, string format)
        {
            //PhpStream stream = PhpStream.GetValid(handle);
            //if (stream == null) return null;
            //string line = stream.ReadLine(-1, null);
            //return Strings.sscanf(line, format);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parses input from a file according to a format.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        /// <param name="arguments"></param>
        /// <returns>The number of assigned values.</returns>
        [return: CastToFalse]
        public static int fscanf(PhpResource handle, string format, PhpAlias arg, params PhpAlias[] arguments)
        {
            //PhpStream stream = PhpStream.GetValid(handle);
            //if (stream == null) return -1;
            //string line = stream.ReadLine(-1, null);
            //return Strings.sscanf(line, format, arg, arguments);
            throw new NotImplementedException();
        }

        #endregion

        #region fgetcsv, fputcsv, str_getcsv

        const char DefaultCsvDelimiter = ',';
        const char DefaultCsvEnclosure = '"';
        const char DefaultCsvEscape = '\\';

        public static PhpArray str_getcsv(string input, char delimiter = DefaultCsvDelimiter, char enclosure = DefaultCsvEnclosure, char escape = DefaultCsvEscape)
        {
            bool firstLine = true;
            return ReadLineCsv(delegate ()
            {
                if (!firstLine)
                    return null;

                firstLine = false;
                return input;
            },
            delimiter, enclosure, escape);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="length"></param>
        /// <param name="delimiter"></param>
        /// <param name="enclosure"></param>
        /// <param name="escape_char">The escape character used in the CSV string.</param>
        /// <returns>Returns an indexed array containing the fields read.
        /// fgetcsv() returns NULL if an invalid handle is supplied or FALSE on other errors, including end of file.</returns>
        public static PhpValue fgetcsv(PhpResource handle, int length = 0, char delimiter = DefaultCsvDelimiter, char enclosure = DefaultCsvEnclosure, char escape_char = DefaultCsvEscape)
        {
            // check arguments
            PhpStream stream = PhpStream.GetValid(handle, FileAccess.Read);
            if (stream == null) return PhpValue.Null;
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length)); // TODO: Err //PhpException.InvalidArgument("length", LibResources.GetString("arg_negative"));
            if (length <= 0) length = -1;    // no length limit
            if (stream.Eof) return PhpValue.False;

            return (PhpValue)ReadLineCsv(() => (stream.Eof ? null : stream.ReadLine(length, null)), delimiter, enclosure, escape_char);
        }

        /// <summary>
        /// CSV data line reader.
        /// In case of stream, it returns stream.GetLine() or null in case of EOF.
        /// In case of string input, it returns string for the first time, then null.
        /// ...
        /// </summary>
        /// <returns>Next line of CSV data or NULL in case of EOF.</returns>
        delegate string CsvLineReader();

        static PhpArray ReadLineCsv(CsvLineReader reader, char delimiter/*=','*/, char enclosure/*='"'*/, char escape_char /*= '\\'*/ )
        {
            // collect results
            PhpArray result = new PhpArray();

            int i = 0;  // index of currently scanned char
            string line = reader(); // currently scanned string
            bool eof = false;

            if (line == null)
            {
                result.Add(null);
                return result;
            }

            for (; ; )
            {
                Debug.Assert(i - 1 < line.Length);
                bool previous_field_delimited = (i == 0 || line[i - 1] == delimiter);

                // skip initial whitespace:
                while (i < line.Length && Char.IsWhiteSpace(line[i]) && line[i] != delimiter)
                    ++i;

                if (i >= line.Length)
                {
                    if (result.Count == 0)
                        result.Add(null);
                    else if (previous_field_delimited)
                        result.Add(string.Empty);

                    break;
                }
                else if (line[i] == delimiter)
                {
                    if (previous_field_delimited)
                        result.Add(string.Empty);
                    ++i;
                }
                else if (line[i] == enclosure)
                {
                    // enclosed string follows:
                    int start = ++i;
                    var field_builder = StringBuilderUtilities.Pool.Get();

                    for (; ; )
                    {
                        // read until enclosure character found:
                        while (i < line.Length && line[i] != enclosure)
                        {
                            if (i + 1 < line.Length && line[i] == escape_char)
                                ++i;// skip escape char

                            ++i;    // skip following char
                        }

                        // end of line:
                        if (i == line.Length)
                        {
                            // append including eoln:
                            field_builder.Append(line, start, line.Length - start);

                            // field continues on the next line:
                            string nextLine = reader();
                            if (nextLine == null)
                            {
                                eof = true;
                                break;
                            }

                            line = nextLine;
                            start = i = 0;
                        }
                        else
                        {
                            Debug.Assert(line[i] == enclosure);
                            i++;

                            if (i < line.Length && line[i] == enclosure)
                            {
                                // escaped enclosure; add previous text including enclosure:
                                field_builder.Append(line, start, i - start);
                                start = ++i;
                            }
                            else
                            {
                                // end of enclosure:
                                field_builder.Append(line, start, i - 1 - start);
                                start = i;
                                break;
                            }
                        }
                    }

                    if (!eof)//if (!stream.Eof)
                    {
                        Debug.Assert(start == i && line.Length > 0);

                        int end = GetCsvDisclosedTextEnd(line, delimiter, ref i, escape_char);

                        field_builder.Append(line, start, end - start);
                    }

                    //result.Add(Core.Convert.Quote(field_builder.ToString(), context));
                    //result.Add(StringUtils.EscapeStringCustom(field_builder.ToString(), charsToEscape, escape));
                    result.Add(StringBuilderUtilities.GetStringAndReturn(field_builder));
                }
                else
                {
                    // disclosed text:

                    int start = i;
                    int end = GetCsvDisclosedTextEnd(line, delimiter, ref i, escape_char);

                    //result.Add( Core.Convert.Quote(line.Substring(start, end - start), context));
                    //result.Add(StringUtils.EscapeStringCustom(line.Substring(start, end - start), charsToEscape, escape));
                    result.Add(line.Substring(start, end - start));
                }
            }

            return result;
        }

        static int GetCsvDisclosedTextEnd(string line, char delimiter, ref int i, char escape_char)
        {
            // disclosed text follows enclosed one:
            while (i < line.Length && line[i] != delimiter)
            {
                i++;
            }

            // field ended by eoln or delimiter:
            if (i == line.Length)
            {
                // do not add eoln to the field:
                int dec = 0;
                if (line[i - 1] == '\n')
                {
                    dec++;
                    if (i > 1 && line[i - 2] == '\r')
                        dec++;
                }
                return i - dec;
            }
            else
            {
                Debug.Assert(line[i] == delimiter);

                // skip delimiter:
                return i++;
            }
        }

        /// <remarks>
        /// Affected by run-time quoting (data are unqouted before written)
        /// (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
        /// </remarks>
        public static int fputcsv(Context ctx, PhpResource handle, PhpArray fields, char delimiter = DefaultCsvDelimiter, char enclosure = DefaultCsvEnclosure)
        {
            PhpStream stream = PhpStream.GetValid(handle, FileAccess.Write);
            if (stream == null || !stream.CanWrite) return -1;

            char[] special_chars = { delimiter, ' ', '\\', '\t', '\r', '\n' };
            string str_enclosure = enclosure.ToString();
            string str_delimiter = delimiter.ToString();

            int initial_position = stream.WritePosition;
            var enumerator = fields.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                var str_field = StringUtils.StripCSlashes(enumerator.CurrentValue.ToString(ctx));

                if (stream.WritePosition > initial_position)
                    stream.WriteString(str_delimiter);

                int special_char_index = str_field.IndexOfAny(special_chars);
                int enclosure_index = str_field.IndexOf(enclosure);

                if (special_char_index >= 0 || enclosure_index >= 0)
                {
                    stream.WriteString(str_enclosure);

                    if (enclosure_index >= 0)
                    {
                        // escapes enclosure characters:
                        int start = 0;
                        for (; ; )
                        {
                            // writes string starting after the last enclosure and ending by the next one:
                            stream.WriteString(str_field.Substring(start, enclosure_index - start + 1));
                            stream.WriteString(str_enclosure);

                            start = enclosure_index + 1;
                            if (start >= str_field.Length) break;

                            enclosure_index = str_field.IndexOf(enclosure, start);
                            if (enclosure_index < 0)
                            {
                                // remaining substring: 
                                stream.WriteString(str_field.Substring(start));
                                break;
                            }
                        }
                    }
                    else
                    {
                        stream.WriteString(str_field);
                    }

                    stream.WriteString(str_enclosure);
                }
                else
                {
                    stream.WriteString(str_field);
                }
            }

            stream.WriteString("\n");

            return (initial_position == -1) ? stream.WritePosition : stream.WritePosition - initial_position;
        }

        #endregion

        #region fread, fgetc, fwrite, fputs, fpassthru, readfile

        /// <summary>
        /// Binary-safe file read.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="handle">A file stream opened for reading.</param>
        /// <param name="length">Number of bytes to be read.</param>
        /// <returns>
        /// The <see cref="string"/> or <see cref="PhpBytes"/>
        /// of the specified length depending on file access mode.
        /// </returns>
        [return: CastToFalse]
        public static PhpString fread(Context ctx, PhpResource handle, int length)
        {
            // returns an object (string or PhpBytes depending on fopen mode)
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return default(PhpString);
            }

            return stream.IsText
                ? new PhpString(stream.ReadString(length))
                : new PhpString(stream.ReadBytes(length));
        }

        /// <summary>
        /// Gets character from file pointer.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="handle">A file stream opened for reading.</param>
        /// <returns>A <see cref="string"/> or <see cref="byte"/>[] containing one character from the 
        /// given stream or <c>false</c> on EOF.</returns>
        [return: CastToFalse]
        public static PhpString fgetc(Context ctx, PhpResource handle)
        {
            return feof(handle) ? default(PhpString) : fread(ctx, handle, 1);
        }

        /// <summary>
        /// Binary-safe file write.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="handle">The file stream (opened for writing). </param>
        /// <param name="data">The data to be written.</param>
        /// <param name="length">
        /// If the length argument is given, writing will stop after length bytes 
        /// have been written or the end of string is reached, whichever comes first.
        /// </param>
        /// <returns>Returns the number of bytes written, or FALSE on error. </returns>
        [return: CastToFalse]
        public static int fwrite(Context ctx, PhpResource handle, PhpString data, int length = -1)
        {
            //data = Core.Convert.Unquote(data, ScriptContext.CurrentContext);
            return WriteInternal(ctx, handle, data, length);
        }

        /// <summary>
        /// Binary-safe file write. Alias for <see cref="fwrite(PhpResource, PhpString, int)"/>.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="handle">The file stream (opened for writing). </param>
        /// <param name="data">The data to be written.</param>
        /// <param name="length">If the length argument is given, writing will stop after length bytes 
        /// have been written or the end of string is reached, whichever comes first. </param>
        /// <returns>Returns the number of bytes written, or FALSE on error. </returns>
        [return: CastToFalse]
        public static int fputs(Context ctx, PhpResource handle, PhpString data, int length = -1)
        {
            return fwrite(ctx, handle, data, length);
        }

        /// <summary>
        /// Binary-safe file write implementation.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="handle">The file stream (opened for writing). </param>
        /// <param name="data">The data to be written.</param>
        /// <param name="length">The number of characters to write or <c>-1</c> to use the whole <paramref name="data"/>.</param>
        /// <returns>Returns the number of bytes written, or FALSE on error. </returns>
        static int WriteInternal(Context ctx, PhpResource handle, PhpString data, int length)
        {
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return -1;
            }

            if (data.IsEmpty)
            {
                return 0;
            }

            // Note: Any data type is converted using implicit conversion in AsText/AsBinary.
            if (stream.IsText)
            {
                // If file OpenMode is text then use string access methods.
                var sub = data.ToString(ctx);
                if (length > 0 && length < sub.Length) sub = sub.Remove(length);

                return stream.WriteString(sub);
            }
            else
            {
                // File OpenMode is binary.
                byte[] sub = data.ToBytes(ctx);
                if (length > 0 && length < sub.Length)
                {
                    var bytes = new byte[length];
                    Array.Copy(sub, bytes, length);
                    sub = bytes;
                }

                return stream.WriteBytes(sub);
            }
        }


        /// <summary>
        /// Outputs all remaining data on a file pointer.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="handle">The file stream (opened for reading). </param>
        /// <returns>Number of bytes written.</returns>
        [return: CastToFalse]
        public static int fpassthru(Context ctx, PhpResource handle)
        {
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return -1;
            }

            int rv = 0;

            if (stream.IsText)
            {
                // Use the text output buffers.
                while (!stream.Eof)
                {
                    string str = stream.ReadMaximumString();
                    ctx.Output.Write(str);
                    rv += str.Length;
                }
            }
            else // (stream.IsBinary)
            {
                // Write directly to the binary output buffers.
                //return stream_copy_to_stream(stream, InputOutputStreamWrapper.ScriptOutput(ctx));

                var writing = Task.CompletedTask;

                while (!stream.Eof)
                {
                    var data = stream.ReadMaximumData();
                    if (data.IsNull) break; // EOF or error.

                    var bytes = data.AsBytes(ctx.StringEncoding);
                    rv += bytes.Length;

                    writing = writing.IsCompleted
                        ? ctx.OutputStream.WriteAsync(bytes, 0, bytes.Length)
                        : writing.ContinueWith((_) => ctx.OutputStream.WriteAsync(bytes));
                }

                if (!writing.IsCompleted)
                {
                    writing.GetAwaiter().GetResult();
                }
            }

            //
            return rv;
        }

        /// <summary>
        /// Reads a file and writes it to the output buffer.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">The file to open.</param>
        /// <param name="flags">Searches for the file in the <c>include_path</c> if set to <c>1</c>.</param>
        /// <param name="context">A <see cref="StreamContext"/> resource with additional information for the stream.</param>
        /// <returns>Returns the number of bytes read from the file. If an error occurs, <c>false</c> is returned.</returns>
        [return: CastToFalse]
        public static int readfile(Context ctx, string path, FileOpenOptions flags = FileOpenOptions.Empty, PhpResource context = null)
        {
            StreamContext sc = StreamContext.GetValid(context, true);
            if (sc == null) return -1;

            using (PhpStream res = PhpStream.Open(ctx, path, "rb", ProcessOptions(ctx, flags), sc))
            {
                if (res == null) return -1;

                // Note: binary file access is the most efficient (no superfluous filtering
                // and no conversions - PassThrough will write directly to the OutputStream).
                return fpassthru(ctx, res);
            }
        }

        #endregion

        #region fgets, fgetss

        /// <summary>
        /// Gets one line of text from file pointer including the end-of-line character. 
        /// </summary>
        /// <param name="handle">The file stream opened for reading.</param>
        /// <returns>A <see cref="string"/> or <see cref="PhpBytes"/> containing the line of text or <c>false</c> in case of an error.</returns>
        /// <remarks>
        /// <para>
        ///   Result is affected by run-time quoting 
        ///   (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
        /// </para>
        /// </remarks>
        [return: CastToFalse]
        public static PhpString fgets(PhpResource handle)
        {
            PhpStream stream = PhpStream.GetValid(handle);
            if (stream == null) return default(PhpString);

            // Use the default accessor to the stream breaking at \n, no superfluous conversion.
            //return Core.Convert.Quote(stream.ReadData(-1, true), ScriptContext.CurrentContext);
            return stream.ReadData(-1, true).ToPhpString();
        }

        /// <summary>
        /// Gets one line of text from file pointer including the end-of-line character. 
        /// </summary>
        /// <param name="length">Maximum length of the returned text.</param>
        /// <param name="handle">The file stream opened for reading.</param>
        /// <returns>A <see cref="string"/> or <see cref="PhpBytes"/> containing the line of text or <c>false</c> in case of an error.</returns>
        /// <remarks>
        /// <para>
        ///   Returns a string of up to <paramref name="length"/><c> - 1</c> bytes read from 
        ///   the file pointed to by <paramref name="handle"/>.
        /// </para>
        /// <para>
        ///   The <paramref name="length"/> parameter became optional in PHP 4.2.0, if omitted, it would
        ///   assume 1024 as the line length. As of PHP 4.3, omitting <paramref name="length"/> will keep
        ///   reading from the stream until it reaches the end of the line. 
        ///   If the majority of the lines in the file are all larger than 8KB, 
        ///   it is more resource efficient for your script to specify the maximum line length.
        /// </para>
        /// <para>
        ///   Result is affected by run-time quoting 
        ///   (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
        /// </para>
        /// </remarks>
        [return: CastToFalse]
        public static PhpString fgets(PhpResource handle, int length)
        {
            if (length <= 0)
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg_negative", "Length"));
                //return null;
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            PhpStream stream = PhpStream.GetValid(handle);
            if (stream == null) return default(PhpString);

            // Use the default accessor to the stream breaking at \n, no superfluous conversion.
            //return Core.Convert.Quote(stream.ReadData(length, true), ScriptContext.CurrentContext);
            return stream.ReadData(length, true).ToPhpString();
        }

        /// <summary>
        /// Gets a whole line from file pointer and strips HTML tags.
        /// </summary>
        [return: CastToFalse]
        public static string fgetss(PhpResource handle)
        {
            return ReadLineStripTagsInternal(handle, -1, null);
        }

        /// <summary>
        /// Gets one line from file pointer and strips HTML tags.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="length"></param>
        /// <param name="allowableTags"></param>
        /// <returns></returns>
        [return: CastToFalse]
        public static string fgetss(PhpResource handle, int length, string allowableTags = null)
        {
            if (length <= 0 && length != -1)
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg_negative", "Length"));
                //return null;
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return ReadLineStripTagsInternal(handle, length, allowableTags);
        }

        static string ReadLineStripTagsInternal(PhpResource handle, int length, string allowableTags)
        {
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return null;
            }

            var line = stream.ReadLine(length, null);
            if (line != null)
            {
                int state = stream.StripTagsState;
                line = Strings.StripTags(line, new Strings.TagsHelper(allowableTags), ref state);
                stream.StripTagsState = state;
            }

            return line;
        }

        #endregion

        #region file, file_get_contents, file_put_contents

        /// <summary>
        /// Reads entire file into an array.
        /// </summary>
        /// <remarks>
        /// <para>
        ///   The input file is split at '\n' and the separator is included in every line.
        /// </para>
        /// <para>
        ///   Result is affected by run-time quoting 
        ///   (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
        /// </para>
        /// </remarks>
        [return: CastToFalse]
        public static PhpArray file(Context ctx, string path, FileOptions flags = FileOptions.Empty, PhpResource context = null)
        {
            var sc = StreamContext.GetValid(context, true);
            if (sc == null) return null;

            using (var stream = PhpStream.Open(ctx, path, "rt", ProcessOptions(ctx, (FileOpenOptions)flags), sc))
            {
                if (stream == null) return null;

                PhpArray rv = new PhpArray();

                while (!stream.Eof)
                {
                    // Note: The last line does not contain the \n delimiter, but may be empty
                    var line = stream.ReadData(-1, true).AsText(ctx.StringEncoding);
                    if ((flags & FileOptions.TrimLineEndings) > 0)
                    {
                        int len = line.Length;
                        if ((len > 0) && (line[len - 1] == '\n'))
                            line = line.Substring(0, len - 1);
                    }
                    if ((flags & FileOptions.SkipEmptyLines) > 0)
                    {
                        if (line.Length == 0) continue;
                    }

                    rv.Add(line);
                }

                return rv;
            }
        }

        /// <summary>
        /// Reads entire file into a string.
        /// </summary>
        [return: CastToFalse]
        public static PhpString file_get_contents(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.Locals)] PhpArray locals, string path, FileOpenOptions flags = FileOpenOptions.Empty, PhpResource context = null, int offset = -1, int maxLength = -1)
        {
            var sc = StreamContext.GetValid(context, true);
            if (sc == null)
            {
                return default(PhpString);
            }

            using (PhpStream stream = PhpStream.Open(ctx, path, "rb", ProcessOptions(ctx, flags), sc))
            {
                if (stream == null)
                {
                    return default(PhpString);
                }

                // when HTTP protocol requested, store responded headers into local variable $http_response_header:
                if (string.Compare(stream.Wrapper.Scheme, HttpStreamWrapper.scheme, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var headers = stream.WrapperSpecificData as PhpArray;
                    locals.SetItemValue(new IntStringKey(HttpResponseHeaderName), (PhpValue)headers);
                }

                //
                //return Core.Convert.Quote(stream.ReadContents(maxLength, offset), ScriptContext.CurrentContext);
                return stream.ReadContents(maxLength, offset).ToPhpString();
            }
        }

        [return: CastToFalse]
        public static int file_put_contents(Context ctx, string path, PhpValue data, WriteContentsOptions flags = WriteContentsOptions.Empty, PhpResource context = null)
        {
            StreamContext sc = StreamContext.GetValid(context, true);
            if (sc == null) return -1;

            string mode = (flags & WriteContentsOptions.AppendContents) > 0 ? "ab" : "wb";
            using (PhpStream to = PhpStream.Open(ctx, path, mode, ProcessOptions(ctx, (FileOpenOptions)flags), sc))
            {
                if (to == null) return -1;

                // passing array is equivalent to file_put_contents($filename, join('', $array))
                var array = data.ArrayOrNull();
                if (array != null)
                {
                    int total = 0;

                    var enumerator = array.GetFastEnumerator();
                    while (enumerator.MoveNext())
                    {
                        int written = to.WriteBytes(enumerator.CurrentValue.ToBytes(ctx));
                        if (written == -1) return total;
                        total += written;
                    }

                    return total;
                }

                // as of PHP 5.1.0, you may also pass a stream resource to the data parameter
                var resource = data.AsResource();
                if (resource != null)
                {
                    PhpStream from = PhpStream.GetValid(resource);
                    if (from == null) return -1;

                    return PhpStreams.stream_copy_to_stream(from, to);
                }

                return to.WriteBytes(data.ToBytes(ctx));
            }
        }

        #endregion

        #region Seek (fseek, rewind, ftell, ftruncate)

        /// <summary>
        /// Seeks on a file pointer.
        /// </summary>
        /// <param name="handle">A file stream resource.</param>
        /// <param name="offset">The number of bytes to seek.</param>
        /// <param name="whence">The position in stream to seek from.
        /// May be one of the <see cref="SeekOptions"/> flags.</param>
        /// <returns>Upon success, returns 0; otherwise, returns -1. 
        /// Note that seeking past EOF is not considered an error.</returns>
        public static int fseek(PhpResource handle, int offset, int whence = SEEK_SET)
        {
            PhpStream stream = PhpStream.GetValid(handle);
            if (stream == null) return -1;
            return stream.Seek(offset, (SeekOrigin)whence) ? 0 : -1;
        }

        /// <summary>
        /// Rewind the position of a file pointer.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static bool rewind(PhpResource handle)
        {
            var stream = PhpStream.GetValid(handle);
            return stream != null && stream.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        /// Tells file pointer read/write position.
        /// </summary>
        /// <param name="handle">A file stream resource.</param>
        /// <returns></returns>
        [return: CastToFalse]
        public static int ftell(PhpResource handle)
        {
            var stream = PhpStream.GetValid(handle);
            return (stream == null) ? -1 : stream.Tell();
        }

        /// <summary>
        /// Truncates a file to a given length.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static bool ftruncate(PhpResource handle, int size)
        {
            PhpStream stream = PhpStream.GetValid(handle);
            if (stream == null) return false;

            if (stream.RawStream != null && stream.RawStream.CanWrite && stream.RawStream.CanSeek)
            {
                stream.RawStream.SetLength(size);
                return true;
            }

            return false;
        }

        #endregion

        #region FileSystem Access (copy, rename, unlink, mkdir, rmdir, flock)

        /// <summary>
        /// Copies a file (even accross different stream wrappers).
        /// </summary>
        /// <remarks>
        /// If the destination file already exists, it will be overwritten. 
        /// <para>
        /// Note: As of PHP 4.3.0, both source and dest may be URLs if the 
        /// "fopen wrappers" have been enabled. See <c>fopen()</c> for more details. 
        /// If dest is an URL, the copy operation may fail if the wrapper does 
        /// not support overwriting of existing files. 
        /// </para> 
        /// </remarks>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="source">Source URL.</param>
        /// <param name="dest">Destination URL.</param>
        /// <returns><c>true</c> on success or <c>false</c> on failure.</returns>
        public static bool copy(Context ctx, string source, string dest)
        {
            StreamWrapper reader, writer;
            if ((!PhpStream.ResolvePath(ctx, ref source, out reader, CheckAccessMode.FileExists, CheckAccessOptions.Empty))
                || (!PhpStream.ResolvePath(ctx, ref dest, out writer, CheckAccessMode.FileExists, CheckAccessOptions.Empty)))
                return false;

            if ((reader.Scheme == "file") && (writer.Scheme == "file"))
            {
                // Copy the file.
                try
                {
                    File.Copy(source, dest, true);
                    return true;
                }
                catch (System.Exception)
                {
                    return false;
                }
            }
            else
            {
                // Copy the two files using the appropriate stream wrappers.
                using (PhpResource from = reader.Open(ctx, ref source, "rb", StreamOpenOptions.Empty, StreamContext.Default))
                {
                    if (from == null) return false;
                    using (PhpResource to = writer.Open(ctx, ref dest, "wb", StreamOpenOptions.Empty, StreamContext.Default))
                    {
                        if (to == null) return false;

                        int copied = PhpStreams.stream_copy_to_stream(from, to);
                        return copied >= 0;
                    }
                }
            }
        }

        /// <summary>
        /// Renames a file.
        /// </summary>
        /// <remarks>
        /// Both the <paramref name="oldpath"/> and the <paramref name="newpath"/> must be handled by the same wrapper.
        /// </remarks>
        public static bool rename(Context ctx, string oldpath, string newpath)
        {
            StreamWrapper oldwrapper, newwrapper;
            if ((!PhpStream.ResolvePath(ctx, ref oldpath, out oldwrapper, CheckAccessMode.FileExists, CheckAccessOptions.Empty))
                || (!PhpStream.ResolvePath(ctx, ref newpath, out newwrapper, CheckAccessMode.FileMayExist, CheckAccessOptions.Empty)))
                return false;

            if (oldwrapper != newwrapper)
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("wrappers_must_match"));
                throw new ArgumentException("wrappers_must_match"); // TODO: Err
            }

            return oldwrapper.Rename(oldpath, newpath, StreamRenameOptions.Empty, StreamContext.Default);
        }

        /// <summary>
        /// Deletes a file using a StreamWrapper corresponding to the given URL.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">An URL of a file to be deleted.</param>
        /// <param name="context">StreamContext.</param>
        /// <returns>True in case of success.</returns>
        public static bool unlink(Context ctx, string path, PhpResource context = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg_empty", "path"));
                //return false;
                throw new ArgumentException(nameof(path));
            }

            var sc = StreamContext.GetValid(context, true);
            if (sc == null) // PHP warning is thrown by StreamContext.GetValid
            {
                return false;
            }

            StreamWrapper wrapper;
            if (!PhpStream.ResolvePath(ctx, ref path, out wrapper, CheckAccessMode.FileExists, CheckAccessOptions.Empty))
                return false;

            // Clear the cache (the currently deleted file may have been cached)
            clearstatcache();

            //
            return wrapper.Unlink(path, 0, sc);
        }


        /// <summary>
        /// Portable advisory file locking.
        /// </summary>
        public static bool flock(PhpResource handle, int operation)
        {
            int dummy = 0;
            return flock(handle, operation, ref dummy);
        }

        /// <summary>
        /// Portable advisory file locking.
        /// </summary>
        /// <param name="handle">A file system pointer resource that is typically created using fopen().</param>
        /// <param name="operation">Operation is one of the following:
        /// <c>LOCK_SH</c> to acquire a shared lock (reader).
        /// <c>LOCK_EX</c> to acquire an exclusive lock (writer).
        /// <c>LOCK_UN</c> to release a lock (shared or exclusive).
        /// 
        /// It is also possible to add <c>LOCK_NB</c> as a bitmask to one of the above operations if you don't want flock() to block while locking. (not supported on Windows)
        /// </param>
        /// <param name="wouldblock">The optional third argument is set to TRUE if the lock would block (EWOULDBLOCK errno condition). (not supported on Windows)</param>
        /// <returns>Returns <c>true</c> on success or <c>false</c> on failure.</returns>
        public static bool flock(PhpResource handle, int operation, ref int wouldblock)
        {
            // Get the native file handle for the PHP resource
            var phpStream = PhpStream.GetValid(handle);
            if (phpStream == null) return false;

            var fileStream = phpStream.RawStream as FileStream;
            if (fileStream == null) return false;

            //

            PhpException.FunctionNotSupported("flock");
            return false;
        }

        #endregion
    }
}
