using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library
{
    #region SessionHandlerInterface

    /// <summary>
    /// Prototype for creating a custom session handler.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public interface SessionHandlerInterface
    {
        /// <summary>
        /// Closes the current session. This function is automatically executed when closing the session, or explicitly via session_write_close().
        /// </summary>
        /// <returns>The return value (usually TRUE on success, FALSE on failure). Note this value is returned internally to PHP for processing.</returns>
        bool close();

        /// <summary>
        /// Destroys a session. Called by session_regenerate_id() (with $destroy = TRUE), session_destroy() and when session_decode() fails.
        /// </summary>
        /// <param name="session_id">The session ID being destroyed.</param>
        /// <returns>The return value (usually TRUE on success, FALSE on failure). Note this value is returned internally to PHP for processing.</returns>
        bool destroy(string session_id);

        /// <summary>
        /// Cleans up expired sessions. Called by session_start(), based on session.gc_divisor, session.gc_probability and session.gc_maxlifetime settings.
        /// </summary>
        /// <param name="maxlifetime">Sessions that have not updated for the last maxlifetime seconds will be removed.</param>
        /// <returns>The return value (usually TRUE on success, FALSE on failure). Note this value is returned internally to PHP for processing.</returns>
        bool gc(long maxlifetime);

        /// <summary>
        /// Re-initialize existing session, or creates a new one. Called when a session starts or when session_start() is invoked.
        /// </summary>
        /// <param name="save_path">The path where to store/retrieve the session.</param>
        /// <param name="session_name">The session name.</param>
        /// <returns>The return value (usually TRUE on success, FALSE on failure). Note this value is returned internally to PHP for processing.</returns>
        bool open(string save_path, string session_name);

        /// <summary>
        /// Reads the session data from the session storage, and returns the results. Called right after the session starts
        /// or when session_start() is called. Please note that before this method is called SessionHandlerInterface::open() is invoked.
        /// 
        /// This method is called by PHP itself when the session is started.This method should retrieve the session data
        /// from storage by the session ID provided.The string returned by this method must be in the same serialized format
        /// as when originally passed to the SessionHandlerInterface::write() If the record was not found, return an empty string.
        /// 
        /// The data returned by this method will be decoded internally by PHP using the unserialization method specified
        /// in session.serialize_handler.The resulting data will be used to populate the $_SESSION superglobal.
        /// 
        /// Note that the serialization scheme is not the same as unserialize() and can be accessed by session_decode().
        /// </summary>
        /// <param name="session_id">The session id.</param>
        /// <returns>Returns an encoded string of the read data. If nothing was read, it must return an empty string. Note this value is returned internally to PHP for processing.</returns>
        PhpString read(string session_id);

        /// <summary>
        /// Writes the session data to the session storage. Called by session_write_close(), when session_register_shutdown() fails,
        /// or during a normal shutdown. Note: SessionHandlerInterface::close() is called immediately after this function.
        /// 
        /// PHP will call this method when the session is ready to be saved and closed. It encodes the session data from
        /// the $_SESSION superglobal to a serialized string and passes this along with the session ID to this method for storage.
        /// The serialization method used is specified in the session.serialize_handler setting.
        /// 
        /// Note this method is normally called by PHP after the output buffers have been closed unless explicitly called by session_write_close()
        /// </summary>
        /// <param name="session_id">The session id.</param>
        /// <param name="session_data">The encoded session data. This data is the result of the PHP internally encoding
        /// the $_SESSION superglobal to a serialized string and passing it as this parameter. Please note sessions use an alternative serialization method.</param>
        /// <returns>The return value (usually TRUE on success, FALSE on failure). Note this value is returned internally to PHP for processing.</returns>
        bool write(string session_id, PhpString session_data);
    }

    #endregion

    #region SessionHandler // file-system based SessionHandlerInterface implementation

    /// <summary>
    /// Default file-system based session handler implementation.
    /// Implements <see cref="SessionHandlerInterface"/> to be used in combination with <see cref="Session.session_set_save_handler(SessionHandlerInterface, bool)"/> function.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class SessionHandler : SessionHandlerInterface
    {
        protected readonly Context _ctx;

        public SessionHandler(Context ctx)
        {
            _ctx = ctx;
        }

        public virtual bool close()
        {
            throw new NotImplementedException();
        }

        public virtual bool destroy(string session_id)
        {
            throw new NotImplementedException();
        }

        public virtual bool gc(long maxlifetime)
        {
            throw new NotImplementedException();
        }

        public virtual bool open(string save_path, string session_name)
        {
            throw new NotImplementedException();
        }

        public virtual PhpString read(string session_id)
        {
            throw new NotImplementedException();
        }

        public virtual bool write(string session_id, PhpString session_data)
        {
            throw new NotImplementedException();
        }

        public virtual string create_sid()
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    [PhpExtension("session")]
    public static class Session
    {
        #region Nested class: UserHandlerInternal

        /// <summary>
        /// Implementation of our <see cref="PhpSessionHandler"/> that uses provided <see cref="SessionHandlerInterface"/>.
        /// </summary>
        sealed class UserHandlerInternal : PhpSessionHandler
        {
            // TODO: static: call _handler.gc() once in a time

            readonly SessionHandlerInterface/*!*/_handler;

            public UserHandlerInternal(SessionHandlerInterface handler)
            {
                _handler = handler;
            }

            public override string HandlerName => "user";

            public override PhpArray Load(IHttpPhpContext webctx)
            {
                // 1. open
                // 2. read

                //var str = _handler.read(GetSessionId(webctx));
                //return unserialize str;
                throw new NotImplementedException();
            }

            public override bool Persist(IHttpPhpContext webctx, PhpArray session)
            {
                // 1. write
                // 2. close

                //_handler.write(GetSessionId(webctx), serialize session)
                //_handler.close();
                throw new NotImplementedException();
            }

            public override void Abandon(IHttpPhpContext webctx)
            {
                // destroy
                _handler.destroy(GetSessionId(webctx));
            }

            public override string GetSessionId(IHttpPhpContext webctx)
            {
                throw new NotImplementedException();
            }

            public override string GetSessionName(IHttpPhpContext webctx)
            {
                throw new NotImplementedException();
            }

            public override bool SetSessionName(IHttpPhpContext webctx, string name)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Nested class: CustomSessionHandler

        /// <summary>
        /// Implementats <see cref="SessionHandlerInterface"/> with callback functions.
        /// </summary>
        sealed class CustomSessionHandler : SessionHandlerInterface
        {
            readonly Context _ctx;
            readonly IPhpCallable
                _open, _close, _read, _write,
                _destroy, _gc,
                _create_sid, _validate_sid, _update_timestamp;

            public CustomSessionHandler(
                Context ctx,
                IPhpCallable open, IPhpCallable close,
                IPhpCallable read, IPhpCallable write,
                IPhpCallable destroy, IPhpCallable gc,
                IPhpCallable create_sid = null,
                IPhpCallable validate_sid = null,
                IPhpCallable update_timestamp = null)
            {
                _ctx = ctx;

                _open = open;
                _close = close;
                _read = read;
                _write = write;
                _destroy = destroy;
                _gc = gc;

                _create_sid = create_sid;
                _validate_sid = validate_sid;
                _update_timestamp = update_timestamp;
            }

            public bool open(string save_path, string session_name) => (bool)_open.Invoke(_ctx, (PhpValue)save_path, (PhpValue)session_name);

            public bool close() => (bool)_close.Invoke(_ctx, Array.Empty<PhpValue>());

            public PhpString read(string session_id) => _read.Invoke(_ctx, (PhpValue)session_id).ToPhpString(_ctx);

            public bool write(string session_id, PhpString session_data) => (bool)_write.Invoke(_ctx, (PhpValue)session_id, PhpValue.Create(session_data));

            public bool destroy(string session_id) => (bool)_destroy.Invoke(_ctx, (PhpValue)session_id);

            public bool gc(long maxlifetime) => (bool)_gc.Invoke(_ctx, (PhpValue)maxlifetime);
        }

        #endregion

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

        /// <summary>
        /// Gets <see cref="IHttpPhpContext"/> if available. Otherwise <c>null</c> is returned and warning throwed.
        /// </summary>
        static IHttpPhpContext GetHttpPhpContext(Context ctx)
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
            var webctx = GetHttpPhpContext(ctx);
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
            var webctx = GetHttpPhpContext(ctx);
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
            var webctx = GetHttpPhpContext(ctx);
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
        public static int session_gc() => 0;

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

            var webctx = GetHttpPhpContext(ctx);
            if (webctx != null && webctx.SessionHandler != null)
            {
                if (webctx.SessionState != PhpSessionState.Closed || newid != null)
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
            }

            //
            return id;
        }

        ///// <summary>
        ///// Find out whether a global variable is registered in a session
        ///// </summary>
        //public static bool session_is_registered(Context ctx, string name) { throw new NotImplementedException(); }   // deprecated and removed

        /// <summary>
        /// Get and/or set the current session module
        /// </summary>
        public static string session_module_name(Context ctx, string newmodule = null)
        {
            var module = string.Empty;
            var webctx = GetHttpPhpContext(ctx);
            if (webctx != null)
            {
                module = webctx.SessionHandler.HandlerName;

                if (newmodule != null)
                {
                    throw new NotImplementedException();
                }
            }

            return module;
        }

        /// <summary>
        /// Get and/or set the current session name
        /// </summary>
        public static string session_name(Context ctx, string newName = null)
        {
            string name = null;

            var webctx = GetHttpPhpContext(ctx);
            if (webctx != null)
            {
                name = webctx.SessionHandler.GetSessionName(webctx);

                if (newName != null && newName != name)
                {
                    webctx.SessionHandler.SetSessionName(webctx, newName);
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
        /// Session shutdown function,
        /// registers <see cref="session_write_close"/> as a shutdown function.
        /// </summary>
        public static void session_register_shutdown(Context ctx)
        {
            ctx.RegisterShutdownCallback(session_write_close);
        }

        ///// <summary>
        ///// Register one or more global variables with the current session
        ///// </summary>
        //public static void session_register() { throw new NotImplementedException(); }    // deprecated and removed

        /// <summary>
        /// Re-initialize session array with original values
        /// </summary>
        public static void session_reset(Context ctx)
        {
            var webctx = GetHttpPhpContext(ctx);
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
        public static bool session_set_save_handler(
            Context ctx,
            IPhpCallable open, IPhpCallable close,
            IPhpCallable read, IPhpCallable write,
            IPhpCallable destroy, IPhpCallable gc,
            IPhpCallable create_sid = null,
            IPhpCallable validate_sid = null,
            IPhpCallable update_timestamp = null)
        {
            if (!ctx.IsWebApplication ||
                !PhpVariable.IsValidBoundCallback(ctx, open) ||
                !PhpVariable.IsValidBoundCallback(ctx, close) ||
                !PhpVariable.IsValidBoundCallback(ctx, read) ||
                !PhpVariable.IsValidBoundCallback(ctx, write) ||
                !PhpVariable.IsValidBoundCallback(ctx, destroy) ||
                !PhpVariable.IsValidBoundCallback(ctx, gc))
            {
                return false;
            }

            session_set_save_handler(
                ctx,
                sessionhandler: new CustomSessionHandler(ctx,
                    open, close, read, write, destroy, gc,
                    create_sid: create_sid, validate_sid: validate_sid, update_timestamp: update_timestamp),
                register_shutdown: false);
            return true;
        }

        /// <summary>
        /// Sets user-level session storage functions
        /// </summary>
        public static void session_set_save_handler(Context ctx, SessionHandlerInterface sessionhandler, bool register_shutdown = true)
        {
            if (sessionhandler == null)
            {
                throw new ArgumentNullException(nameof(sessionhandler));
            }

            var webctx = ctx.HttpPhpContext;
            if (webctx != null)
            {
                webctx.SessionHandler = new UserHandlerInternal(sessionhandler);

                if (register_shutdown)
                {
                    session_register_shutdown(ctx);
                }
            }
        }

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
            var webctx = GetHttpPhpContext(ctx);
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

        ///// <summary>
        ///// Unregister a global variable from the current session
        ///// </summary>
        //public static bool session_unregister(Context ctx, string name) { throw new NotImplementedException(); } // deprecated and removed

        /// <summary>
        /// Free all session variables
        /// </summary>
        public static bool session_unset(Context ctx)
        {
            var webctx = GetHttpPhpContext(ctx);
            if (webctx == null || webctx.SessionState != PhpSessionState.Started)
            {
                return false;
            }

            //
            ctx.Session.Clear();

            //
            return true;
        }

        /// <summary>
        /// Write session data and end session
        /// </summary>
        public static void session_write_close(Context ctx) => session_commit(ctx);

    }
}
