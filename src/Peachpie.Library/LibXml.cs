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
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("libxml")]
    public sealed class LibXMLError
    {
        public readonly int level, code, line, column;
        public readonly string message, file;

        internal string LevelString
        {
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

        /// <summary>
        /// Create default/fixed value nodes during XSD schema validation.
        /// </summary>
        public const int LIBXML_SCHEMA_CREATE = 1;

        #endregion

        sealed class State
        {
            public List<LibXMLError> error_list;

            public Action<LibXMLError> error_handler;

            /// <summary>
            /// Disable the ability to load external entities.
            /// </summary>
            public bool DisableEntityLoader { get; set; }
        }

        /// <summary>
        /// Gets <see cref="State"/> containing lib XML errors information for given context.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        static State GetState(Context ctx) => ctx.GetStatic<State>();

        #region IssueXmlError

        /// <summary>
        /// Reports a <see cref="LibXMLError"/> containing given information using internal error handler or forwards
        /// the error to common error handler.
        /// </summary>
        [PhpHidden]
        public static void IssueXmlError(Context ctx, int level, int code, int line, int column, string message, string file)
        {
            var err = new LibXMLError(level, code, line, column, message, file);

            var state = GetState(ctx);
            if (state.error_handler != null)
            {
                state.error_handler(err);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, err.ToString());
            }
        }

        #endregion

        #region libxml

        public static void libxml_clear_errors(Context ctx)
        {
            GetState(ctx).error_list = null;
        }

        public static bool libxml_disable_entity_loader(bool disable = true)
        {
            PhpException.FunctionNotSupported("libxml_disable_entity_loader");
            return false;
        }

        public static PhpArray/*!*/libxml_get_errors(Context ctx)
        {
            var error_list = GetState(ctx).error_list;
            return error_list == null
                ? PhpArray.NewEmpty()
                : new PhpArray(error_list);
        }

        [return: CastToFalse]
        public static LibXMLError libxml_get_last_error(Context ctx)
        {
            var error_list = GetState(ctx).error_list;
            if (error_list == null || error_list.Count == 0)
                return null;

            return error_list[error_list.Count - 1];
        }

        public static void libxml_set_streams_context(PhpResource streams_context)
        {
            PhpException.FunctionNotSupported("libxml_set_streams_context");
        }

        /// <summary>
        /// Disable libxml errors and allow user to fetch error information as needed.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="use_errors">Enable (TRUE) user error handling or disable (FALSE) user error handling. Disabling will also clear any existing libxml errors.</param>
        /// <returns>This function returns the previous value of <paramref name="use_errors"/>.</returns>
        public static bool libxml_use_internal_errors(Context ctx, bool use_errors = false)
        {
            var state = GetState(ctx);
            bool previousvalue = state.error_handler != null;

            if (use_errors)
            {
                state.error_handler = (err) =>
                {
                    if (state.error_list == null)
                        state.error_list = new List<LibXMLError>();

                    state.error_list.Add(err);
                };
                //error_list = error_list;// keep error_list as it is
            }
            else
            {
                state.error_handler = null;   // outputs xml errors
                state.error_list = null;
            }

            return previousvalue;
        }

        /// <summary>
        /// Disable/enable the ability to load external entities.
        /// </summary>
        /// <returns>Returns the previous value.</returns>
        public static bool libxml_disable_entity_loader(Context ctx, bool disable = true)
        {
            var state = GetState(ctx);
            var previousvalue = state.DisableEntityLoader;

            state.DisableEntityLoader = disable;

            return previousvalue;
        }

        #endregion
    }
}
