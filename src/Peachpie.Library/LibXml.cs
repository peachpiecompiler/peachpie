using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library
{
    #region LibXMLError

    /// <summary>
    /// Contains various information about errors thrown by libxml.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class LibXMLError
    {
        public readonly int level, code, line, column;
        public readonly string message, file;

        public string LevelString
        {
            [PhpHidden]
            get
            {
                switch (this.level)
                {
                    case PhpLibXml.LIBXML_ERR_NONE: return "notice";
                    case PhpLibXml.LIBXML_ERR_WARNING: return "warning";
                    case PhpLibXml.LIBXML_ERR_ERROR: return "error";
                    case PhpLibXml.LIBXML_ERR_FATAL: return "fatal error";
                    default:
                        return null;
                }
            }
        }
        
        internal LibXMLError(int level, int code, int line, int column, string message, string file)
        {
            this.level = level;
            this.code = code;
            this.line = line;
            this.column = column;
            this.message = message;
            this.file = file;
        }

        /// <summary>
        /// Returns string representation of the error.
        /// </summary>
        public override string ToString()
        {
            if (this.file != null)
                return string.Format("LibXml {4} ({0}): {1} in {2}, line: {3}", this.code, this.message, this.file, this.line, this.LevelString);
            else
                return string.Format("LibXml {3} ({0}): {1} in Entity, line: {2}", this.code, this.message, this.line, this.LevelString);
        }
    }

    #endregion

    [PhpExtension("libxml")]
    public static class PhpLibXml
    {
        #region libxml constants

        /// <summary>
        /// Activate small nodes allocation optimization. This may speed up your application without needing to change the code.
        /// </summary>
        public const int LIBXML_COMPACT = 65536;

        /// <summary>
        /// Default DTD attributes.
        /// </summary>
        public const int LIBXML_DTDATTR = 8;

        /// <summary>
        /// Load the external subset
        /// </summary>
        public const int LIBXML_DTDLOAD = 4;

        /// <summary>
        /// Validate with the DTD.
        /// </summary>
        public const int LIBXML_DTDVALID = 16;

        /// <summary>
        /// Remove blank nodes.
        /// </summary>
        public const int LIBXML_NOBLANKS = 256;

        /// <summary>
        /// Merge CDATA as text nodes.
        /// </summary>
        public const int LIBXML_NOCDATA = 16384;

        /// <summary>
        /// Expand empty tags (e.g. &lt;br/&gt; to &lt;br&gt;&lt;/br&gt;).
        /// </summary>
        public const int LIBXML_NOEMPTYTAG = 4;

        /// <summary>
        /// Substitute entities.
        /// </summary>
        public const int LIBXML_NOENT = 2;

        /// <summary>
        /// Suppress error reports.
        /// </summary>
        public const int LIBXML_NOERROR = 32;

        /// <summary>
        /// Disable network access when loading documents.
        /// </summary>
        public const int LIBXML_NONET = 2048;

        /// <summary>
        /// Suppress warning reports.
        /// </summary>
        public const int LIBXML_NOWARNING = 64;

        /// <summary>
        /// Drop the XML declaration when saving a document.
        /// </summary>
        public const int LIBXML_NOXMLDECL = 2;

        /// <summary>
        /// Remove redundant namespaces declarations.
        /// </summary>
        public const int LIBXML_NSCLEAN = 8192;

        /// <summary>
        /// Sets XML_PARSE_HUGE flag, which relaxes any hardcoded limit from the parser.
        /// This affects limits like maximum depth of a document or the entity recursion, as well as limits of the size of text nodes.
        /// </summary>
        public const int LIBXML_PARSEHUGE = 524288;

        /// <summary>
        /// Implement XInclude substitution.
        /// </summary>
        public const int LIBXML_XINCLUDE = 1024;

        /// <summary>
        /// A recoverable error.
        /// </summary>
        public const int LIBXML_ERR_ERROR = 2;

        /// <summary>
        /// A fatal error.
        /// </summary>
        public const int LIBXML_ERR_FATAL = 3;

        /// <summary>
        /// No errors.
        /// </summary>
        public const int LIBXML_ERR_NONE = 0;

        /// <summary>
        /// A simple warning.
        /// </summary>
        public const int LIBXML_ERR_WARNING = 1;

        /// <summary>
        /// libxml version.
        /// </summary>
        public const int LIBXML_VERSION = -1;

        /// <summary>
        /// libxml version like 2.6.5 or 2.6.17.
        /// </summary>
        public const string LIBXML_DOTTED_VERSION = "";

        #endregion

        #region Fields

        [ThreadStatic]
        private static List<LibXMLError> error_list;

        [ThreadStatic]
        private static Action<LibXMLError> error_handler;

        #endregion

        #region Initialization

        // TODO: Achieve this in Peachpie
        //static PhpLibXml()
        //{
        //    // restores libxml at the request end,
        //    // clears error list and handlers:
        //    RequestContext.RequestEnd += () =>
        //    {
        //        error_list = null;
        //        error_handler = null;
        //    };
        //}

        #endregion

        #region IssueXmlError

        /// <summary>
        /// Reports a <see cref="LibXMLError"/> containing given information using internal error handler or forwards
        /// the error to common error handler.
        /// </summary>
        [PhpHidden]
        public static void IssueXmlError(int level, int code, int line, int column, string message, string file)
        {
            var err = new LibXMLError(level, code, line, column, message, file);

            if (error_handler != null)
            {
                error_handler(err);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, err.ToString());
            }
        }

        #endregion

        #region libxml

        public static void libxml_clear_errors()
        {
            error_list = null;
        }

        public static bool libxml_disable_entity_loader(bool disable = true)
        {
            return false;
        }

        public static PhpArray/*!*/libxml_get_errors()
        {
            if (error_list == null)
                return new PhpArray();

            return new PhpArray(error_list);
        }

        [return: CastToFalse]
        public static LibXMLError libxml_get_last_error()
        {
            if (error_list == null || error_list.Count == 0)
                return null;

            return error_list[error_list.Count - 1];
        }

        public static void libxml_set_streams_context(PhpResource streams_context)
        {
        }

        /// <summary>
        /// Disable libxml errors and allow user to fetch error information as needed.
        /// </summary>
        /// <param name="use_errors">Enable (TRUE) user error handling or disable (FALSE) user error handling. Disabling will also clear any existing libxml errors.</param>
        /// <returns>This function returns the previous value of <paramref name="use_errors"/>.</returns>
        public static bool libxml_use_internal_errors(bool use_errors = false)
        {
            bool previousvalue = error_handler != null;

            if (use_errors)
            {
                error_handler = (err) =>
                {
                    if (error_list == null)
                        error_list = new List<LibXMLError>();

                    error_list.Add(err);
                };
                //error_list = error_list;// keep error_list as it is
            }
            else
            {
                error_handler = null;   // outputs xml errors
                error_list = null;
            }

            return previousvalue;
        }

        #endregion
    }
}
