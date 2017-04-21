using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library
{
    [PhpExtension("session")]
    public static class Session
    {
        #region Constants

        /// <summary>
        /// if sessions are disabled.
        /// </summary>
        public const int PHP_SESSION_DISABLED = 0;

        /// <summary>
        /// if sessions are enabled, but none exists.
        /// </summary>
        public const int PHP_SESSION_NONE = 1;

        /// <summary>
        /// if sessions are enabled, and one exists.
        /// </summary>
        public const int PHP_SESSION_ACTIVE = 2;

        #endregion

        #region Helpers

        static IHttpPhpContext EnsureHttpContext(Context ctx)
        {
            var webctx = ctx.HttpPhpContext;
            if (webctx == null)
            {
                PhpException.Throw(PhpError.Warning, Resources.LibResources.web_server_not_available);
            }

            return webctx;
        }

        #endregion

        /// <summary>
        /// Discard session array changes and finish session
        /// </summary>
        public static void session_abort(Context ctx)
        {
            var webctx = EnsureHttpContext(ctx);
            if (webctx != null)
            {
                webctx.SessionHandler.AbortSession(ctx, webctx);
            }
        }

        /// <summary>
        /// Return current cache expire
        /// </summary>
        public static void session_cache_expire() { throw new NotImplementedException(); }

        /// <summary>
        /// Get and/or set the current cache limiter
        /// </summary>
        public static void session_cache_limiter() { throw new NotImplementedException(); }

        /// <summary>
        /// Alias of session_write_close
        /// </summary>
        public static void session_commit(Context ctx)
        {
            var webctx = EnsureHttpContext(ctx);
            if (webctx != null)
            {
                webctx.SessionHandler.CloseSession(ctx, webctx, abandon: false);
            }
        }

        /// <summary>
        /// Create new session id
        /// </summary>
        public static void session_create_id() { throw new NotImplementedException(); }

        /// <summary>
        /// Decodes session data from a session encoded string
        /// </summary>
        public static void session_decode() { throw new NotImplementedException(); }

        /// <summary>
        /// Destroys all data registered to a session
        /// </summary>
        public static void session_destroy(Context ctx)
        {
            var webctx = EnsureHttpContext(ctx);
            if (webctx != null)
            {
                webctx.SessionHandler.CloseSession(ctx, webctx, abandon: true);
            }
        }

        /// <summary>
        /// Encodes the current session data as a session encoded string
        /// </summary>
        public static void session_encode() { throw new NotImplementedException(); }

        /// <summary>
        /// Perform session data garbage collection
        /// </summary>
        public static void session_gc() { throw new NotImplementedException(); }

        /// <summary>
        /// Get the session cookie parameters
        /// </summary>
        public static void session_get_cookie_params() { throw new NotImplementedException(); }

        /// <summary>
        /// Get and/or set the current session id
        /// </summary>
        public static string session_id(Context ctx, string newid = null)
        {
            string id = string.Empty;

            var webctx = EnsureHttpContext(ctx);
            if (webctx != null && webctx.SessionHandler != null)
            {
                id = webctx.SessionHandler.GetSessionId(webctx);

                if (newid != null)
                {
                    if (webctx.SessionState != PhpSessionState.Closed)
                    {
                        // err
                    }

                    // change session id
                    throw new NotSupportedException(nameof(newid));
                }
            }

            //
            return id;
        }

        /// <summary>
        /// Find out whether a global variable is registered in a session
        /// </summary>
        public static void session_is_registered() { throw new NotImplementedException(); }

        /// <summary>
        /// Get and/or set the current session module
        /// </summary>
        public static void session_module_name() { throw new NotImplementedException(); }

        /// <summary>
        /// Get and/or set the current session name
        /// </summary>
        public static string session_name(Context ctx, string newName = null)
        {
            string name = null;

            var webctx = EnsureHttpContext(ctx);
            if (webctx != null)
            {
                name = webctx.SessionHandler.SessionName;

                if (newName != null)
                {
                    webctx.SessionHandler.SessionName = newName;
                }
            }

            //
            return name;
        }

        /// <summary>
        /// Update the current session id with a newly generated one
        /// </summary>
        public static void session_regenerate_id() { throw new NotImplementedException(); }

        /// <summary>
        /// Session shutdown function
        /// </summary>
        public static void session_register_shutdown() { throw new NotImplementedException(); }

        /// <summary>
        /// Register one or more global variables with the current session
        /// </summary>
        public static void session_register() { throw new NotImplementedException(); }

        /// <summary>
        /// Re-initialize session array with original values
        /// </summary>
        public static void session_reset(Context ctx)
        {
            var webctx = EnsureHttpContext(ctx);
            if (webctx != null)
            {
                webctx.SessionHandler.AbortSession(ctx, webctx);
                webctx.SessionHandler.StartSession(ctx, webctx);
            }
        }

        /// <summary>
        /// Get and/or set the current session save path
        /// </summary>
        public static void session_save_path() { throw new NotImplementedException(); }

        /// <summary>
        /// Set the session cookie parameters
        /// </summary>
        public static void session_set_cookie_params() { throw new NotImplementedException(); }

        /// <summary>
        /// Sets user-level session storage functions
        /// </summary>
        public static void session_set_save_handler() { throw new NotImplementedException(); }

        /// <summary>
        /// Start new or resume existing session
        /// </summary>
        /// <param name="options">
        /// If provided, this is an associative array of options that will override the currently set session configuration directives.
        /// The keys should not include the "session" prefix.
        /// 
        /// In addition to the normal set of configuration directives, a <c>read_and_close</c> option may also be provided.
        /// If set to TRUE, this will result in the session being closed immediately after being read,
        /// thereby avoiding unnecessary locking if the session data won't be changed.
        /// </param>
        /// <param name="ctx">Runtime context.</param>
        /// <returns>Whether succeeded.</returns>
        public static bool session_start(Context ctx, PhpArray options = null)
        {
            var webctx = EnsureHttpContext(ctx);
            var handler = webctx?.SessionHandler;
            return handler != null && handler.StartSession(ctx, webctx);
        }

        /// <summary>
        /// Returns the current session status
        /// </summary>
        public static int session_status(Context ctx)
        {
            var webctx = ctx.HttpPhpContext;
            if (webctx == null || webctx.SessionHandler == null || !webctx.SessionHandler.IsEnabled(webctx))
            {
                return PHP_SESSION_DISABLED;
            }
            else if (webctx.SessionState != PhpSessionState.Started)
            {
                return PHP_SESSION_NONE;
            }
            else
            {
                return PHP_SESSION_ACTIVE;
            }
        }

        /// <summary>
        /// Unregister a global variable from the current session
        /// </summary>
        public static void session_unregister() { throw new NotImplementedException(); }

        /// <summary>
        /// Free all session variables
        /// </summary>
        public static void session_unset() { throw new NotImplementedException(); }

        /// <summary>
        /// Write session data and end session
        /// </summary>
        public static void session_write_close(Context ctx) => session_commit(ctx);

    }
}
