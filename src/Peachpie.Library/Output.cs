using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    /// <summary>
	/// PHP output control functions implementation. 
	/// </summary>
	/// <threadsafety static="true"/>
    [PhpExtension("Core")]
    public static class Output
    {
        public const int PHP_OUTPUT_HANDLER_START = (int)BufferedOutput.ChunkPosition.First;
        public const int PHP_OUTPUT_HANDLER_CONT = (int)BufferedOutput.ChunkPosition.Middle;
        public const int PHP_OUTPUT_HANDLER_END = (int)BufferedOutput.ChunkPosition.Last;

        #region printf, vprintf

        /// <summary>
        /// Generates output according to the specified formatting string.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="format">The formatting string. See also the <b>sprintf</b> function (<see cref="PhpStrings.Format"/>).</param>
        /// <param name="args">Variables to format.</param>
        /// <returns>Returns the length of the outputted string. </returns>
        public static int printf(Context ctx, string format, params PhpValue[] args)
        {
            var formattedString = Strings.FormatInternal(ctx, format, args);
            ctx.Output.Write(formattedString);
            return formattedString.Length;
        }

        /// <summary>
        /// Generates output according to the specified formatting string.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="format">The formatting string.</param>
        /// <param name="args">Array of variables to format.</param>
        /// <returns>Returns the length of the outputted string. </returns>
        public static int vprintf(Context ctx, string format, PhpArray args)
        {
            var formattedString = Strings.vsprintf(ctx, format, args);
            ctx.Output.Write(formattedString);
            return formattedString.Length;
        }

        #endregion

        #region ob_start

        /// <summary>
        /// Increases the level of buffering, enables output buffering if disabled and assignes the filtering callback
        /// to the new level of buffering.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="filter">The filtering callback. Ignores invalid callbacks.</param>
        /// <param name="chunkSize">Not supported.</param>
        /// <param name="erase">Not supported.</param>
        /// <returns>Whether the filter is valid callback.</returns>
        public static bool ob_start(Context ctx, IPhpCallable filter = null, int chunkSize = 0, bool erase = true)
        {
            if (chunkSize != 0)
                //PhpException.ArgumentValueNotSupported("chunkSize", "!= 0");
                throw new NotSupportedException("chunkSize != 0");
            if (!erase)
                //PhpException.ArgumentValueNotSupported("erase", erase);
                throw new NotSupportedException("erase == false");

            ctx.BufferedOutput.IncreaseLevel();

            bool result = true;

            // skips filter setting if filter is not specified or valid:
            if (filter != null) //  && (result = filter.Bind())) // TODO: PhpCallback.Bind -> Delegate, done by caller
                ctx.BufferedOutput.SetFilter(filter);

            ctx.IsOutputBuffered = true;

            return result;
        }

        #endregion

        #region ob_clean, ob_end_clean, ob_end_flush

        /// <summary>
        /// Discards the contents of the current level of buffering.
        /// No value is returned.
        /// </summary>
        public static void ob_clean(Context ctx)
        {
            ctx.BufferedOutput.Clean();
        }

        /// <summary>
        /// Discards the contents of the current level of buffering and decreases the level.
        /// </summary>
        /// <returns>Whether the content was discarded and the level was decreased.</returns>
        public static bool ob_end_clean(Context ctx) => EndInternal(ctx, false);

        /// <summary>
        /// Flushes the contents of the current level of buffering and decreases the level.
        /// </summary>
        /// <returns>Whether the content was discarded and the level was decreased.</returns>
        public static bool ob_end_flush(Context ctx) => EndInternal(ctx, true);

        /// <summary>
        /// Decreases the level of buffering and discards or flushes data on the current level of buffering.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="flush">Whether to flush data.</param>
        /// <returns>Whether the content was discarded and the level was decreased.</returns>
        private static bool EndInternal(Context/*!*/ ctx, bool flush)
        {
            BufferedOutput buf = ctx.BufferedOutput;

            if (buf.Level == 0)
            {
                //PhpException.Throw(PhpError.Notice, CoreResources.GetString("output_buffering_disabled"));
                //return false;
                throw new NotImplementedException();
            }

            if (buf.DecreaseLevel(flush) < 0)
                ctx.IsOutputBuffered = false;

            return true;
        }

        #endregion

        #region ob_get_clean, ob_get_contents, ob_get_flush, ob_get_level, ob_get_length, ob_get_status

        /// <summary>
        /// Gets the contents of the current buffer and cleans it.
        /// </summary>
        /// <returns>The content of type <see cref="string"/> or <see cref="byte"/> or <c>false</c>.</returns>
        [return: CastToFalse]
        public static PhpString ob_get_clean(Context ctx)
        {
            var bo = ctx.BufferedOutput;

            var result = bo.GetContent();
            bo.Clean();
            EndInternal(ctx, true);

            return result;  // string or FALSE
        }

        /// <summary>
        /// Gets the content of the current buffer.
        /// </summary>
        /// <returns>The content of type <see cref="string"/> or <see cref="byte"/> or <c>false</c>.</returns>
        [return: CastToFalse]
        public static PhpString ob_get_contents(Context ctx)
        {
            return ctx.BufferedOutput.GetContent(); // string or FALSE
        }

        /// <summary>
        /// Gets the content of the current buffer and decreases the level of buffering.
        /// </summary>
        /// <returns>The content of the buffer.</returns>
        [return: CastToFalse]
        public static PhpString ob_get_flush(Context ctx)
        {
            var bo = ctx.BufferedOutput;

            var result = bo.GetContent();
            EndInternal(ctx, true);
            return result;
        }

        /// <summary>
        /// Retrieves the level of buffering.
        /// </summary>
        /// <returns>The level of buffering.</returns>
        public static int ob_get_level(Context ctx)
        {
            return ctx.BufferedOutput.Level;
        }

        /// <summary>
        /// Retrieves the length of the output buffer.
        /// </summary>
        /// <returns>The length of the contents in the output buffer or <B>false</B>, if output buffering isn't active.</returns>
        public static PhpValue ob_get_length(Context ctx)
        {
            var length = ctx.BufferedOutput.Length;
            return (length >= 0) ? PhpValue.Create(length) : PhpValue.False;
        }

        /// <summary>
        /// Get the status of the current or all output buffers.
        /// </summary>
        /// <returns>The array of name => value pairs containing information.</returns>
        public static PhpArray ob_get_status(Context ctx) => ob_get_status(ctx, false);

        /// <summary>
        /// Get the status of the current or all output buffers.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="full">Whether to retrieve extended information about all levels of buffering or about the current one.</param>
        /// <returns>The array of name => value pairs containing information.</returns>
        public static PhpArray ob_get_status(Context ctx, bool full)
        {
            BufferedOutput bo = ctx.BufferedOutput;
            PhpArray result;

            if (full)
            {
                result = new PhpArray(bo.Level);
                for (int i = 1; i <= bo.Level; i++)
                {
                    result.Add(i, GetLevelStatus(bo, i));
                }
            }
            else if (bo.Level > 0)
            {
                result = GetLevelStatus(bo, bo.Level);
                result.Add("level", bo.Level);
            }
            else
            {
                result = PhpArray.NewEmpty();
            }

            return result;
        }

        private static PhpArray/*!*/ GetLevelStatus(BufferedOutput/*!*/ bo, int index)
        {
            var result = new PhpArray(3);

            IPhpCallable filter;
            int size;
            string name;
            bo.GetLevelInfo(index, out filter, out size, out name);

            if (filter != null)
            {
                result.Add("type", 1);
                result.Add("name", name);
            }
            else
            {
                result.Add("type", 0);
            }
            result.Add("buffer_size", size);

            return result;
        }

        #endregion

        #region flush, ob_flush

        /// <summary>
        /// Flush the output buffer of the HTTP server. Has no effect on data buffered in output buffers.
        /// No value is returned.
        /// </summary>
        public static void flush(Context ctx)
        {
            ctx.HttpPhpContext?.Flush();
        }

        /// <summary>
        /// Flushes data from the current level of buffering to the previous one or to the client 
        /// if the current one is the first one. Applies the filter assigned to the current level (if any).
        /// No value is returned.
        /// </summary>
        public static void ob_flush(Context ctx)
        {
            ctx.BufferedOutput.FlushLevel();
        }

        #endregion

        #region ob_implicit_flush

        /// <summary>
        /// Switches implicit flushing on. 
        /// No value is returned.
        /// </summary>
        /// <remarks>Affects the current script context.</remarks>
        public static void ob_implicit_flush(Context ctx)
        {
            //var http_context = ctx.TryGetProperty<HttpContext>();
            //if (http_context != null) http_context.Response.BufferOutput = true;
            throw new NotImplementedException(); // move to pchplib.web.dll
        }

        /// <summary>
        /// Switches implicit flushing on or off.
        /// No value is returned.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="doFlush">Do flush implicitly?</param>
        /// <remarks>
        /// Affects the current script context.
        ///
        /// There is a bug in the PHP implementation of this function: 
        /// "Turning implicit flushing on will disable output buffering, the output buffers current output 
        /// will be sent as if ob_end_flush() had been called."
        /// Actually, this is not true (PHP doesn't do that) and in fact it is nonsense because 
        /// ob_end_flush only flushes and destroys one level of buffering. 
        /// It would be more meaningful if ob_implicit_flush function had flushed and destroyed all existing buffers
        /// and so disabled output buffering. 
        /// </remarks>  
        public static void ob_implicit_flush(Context ctx, bool doFlush)
        {
            //var http_context = ctx.TryGetProperty<HttpContext>();
            //if (http_context != null) http_context.Response.BufferOutput = doFlush;
            throw new NotImplementedException(); // move to pchplib.web.dll
        }

        #endregion

        #region ob_list_handlers

        public static PhpArray ob_list_handlers(Context ctx)
        {
            BufferedOutput bo = ctx.BufferedOutput;
            var result = new PhpArray(bo.Level);

            for (int i = 0; i < bo.Level; i++)
            {
                result.Add(bo.GetLevelName(i));
            }

            return result;
        }

        #endregion

        #region ob_gzhandler

        ///// <summary>
        ///// Compresses data by gzip compression. Not supported.
        ///// </summary>
        ///// <param name="data">Data to compress.</param>
        ///// <returns>Compressed data.</returns>
        //[ImplementsFunction("ob_gzhandler")]
        //public static PhpBytes GzipHandler(string data)
        //{
        //    return GzipHandler(data, 4);
        //}

        /// <summary>
        /// Available content encodings.
        /// </summary>
        /// <remarks>Values correspond to "content-encoding" response header.</remarks>
        private enum ContentEncoding
        {
            gzip, deflate
        }

        /// <summary>
        /// Compresses data by gzip compression.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="data">Data to be compressed.</param>
        /// <param name="mode">Compression mode.</param>
        /// <returns>Compressed data or <c>FALSE</c>.</returns>
        /// <remarks>The function does not support subsequent calls to compress more chunks of data subsequentally.</remarks>
        public static PhpValue ob_gzhandler(Context ctx, PhpValue data, int mode)
        {
            //// TODO: mode is not passed by Core properly. Therefore it is not possible to make subsequent calls to this handler.
            //// Otherwise headers of ZIP stream will be mishmashed.

            //// check input data
            //if (data == null) return false;

            //// check if we are running web application
            //var httpcontext = ctx.TryGetProperty<HttpContext>();
            //System.Collections.Specialized.NameValueCollection headers;
            //if (httpcontext == null ||
            //    httpcontext.Request == null ||
            //    (headers = httpcontext.Request.Headers) == null)
            //    return data ?? false;

            //// check if compression is supported by browser
            //string acceptEncoding = headers["Accept-Encoding"];

            //if (acceptEncoding != null)
            //{
            //    acceptEncoding = acceptEncoding.ToLowerInvariant();

            //    if (acceptEncoding.Contains("gzip"))
            //        return DoGzipHandler(ctx, data, httpcontext, ContentEncoding.gzip);

            //    if (acceptEncoding.Contains("*") || acceptEncoding.Contains("deflate"))
            //        return DoGzipHandler(ctx, data, httpcontext, ContentEncoding.deflate);
            //}

            //return data ?? false;
            throw new NotImplementedException(); // move to pchplib.web.dll
        }

        ///// <summary>
        ///// Compress given data using compressor named in contentEncoding. Set the response header accordingly.
        ///// </summary>
        ///// <param name="data">PhpBytes or string to be compressed.</param>
        ///// <param name="httpcontext">Current HttpContext.</param>
        ///// <param name="contentEncoding">gzip or deflate</param>
        ///// <returns>Byte stream of compressed data.</returns>
        //private static byte[] DoGzipHandler(Context ctx, PhpValue data, HttpContext/*!*/httpcontext, ContentEncoding contentEncoding)
        //{
        //    PhpBytes phpbytes = data as PhpBytes;

        //    var inputbytes = (phpbytes != null) ?
        //        phpbytes.ReadonlyData :
        //        Configuration.Application.Globalization.PageEncoding.GetBytes(PHP.Core.Convert.ObjectToString(data));

        //    using (var outputStream = new System.IO.MemoryStream())
        //    {
        //        System.IO.Stream compressionStream;
        //        switch (contentEncoding)
        //        {
        //            case ContentEncoding.gzip:
        //                compressionStream = new System.IO.Compression.GZipStream(outputStream, System.IO.Compression.CompressionMode.Compress);
        //                break;
        //            case ContentEncoding.deflate:
        //                compressionStream = new System.IO.Compression.DeflateStream(outputStream, System.IO.Compression.CompressionMode.Compress);
        //                break;
        //            default:
        //                throw new ArgumentException("Not recognized content encoding to be compressed to.", "contentEncoding");
        //        }

        //        using (compressionStream)
        //        {
        //            compressionStream.Write(inputbytes, 0, inputbytes.Length);
        //        }

        //        //Debug.Assert(
        //        //    ScriptContext.CurrentContext.Headers["content-encoding"] != contentEncoding,
        //        //    "The content encoding was already set to '" + contentEncoding + "'. The ob_gzhandler() was called subsequently probably.");

        //        ctx.Headers["content-encoding"] = contentEncoding.ToString();

        //        return outputStream.ToArray();
        //    }
        //}

        #endregion
    }
}
