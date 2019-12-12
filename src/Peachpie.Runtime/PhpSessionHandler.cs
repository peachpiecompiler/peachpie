using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// A session state.
    /// </summary>
    public enum PhpSessionState
    {
        /// <summary>
        /// The default, closed, state.
        /// </summary>
        Default = Closed,

        /// <summary>
        /// Session was not started yet or was closed already.
        /// </summary>
        Closed = 0,

        /// <summary>
        /// Session has started.
        /// </summary>
        Started = 1,

        /// <summary>
        /// Session is in progress of loading or closing.
        /// </summary>
        InProgress = 2,
    }

    /// <summary>
    /// A PHP session handler providing basic session operations.
    /// </summary>
    public abstract class PhpSessionHandler
    {
        /// <summary>
        /// Dummy session item keeping .NET session object alive.
        /// Used by derived class.
        /// </summary>
        protected const string DummySessionItem = "Peachpie_DummySessionKeepAliveItem(\uffff)";

        /// <summary>
        /// Name of PHP <c>SID</c> constant to be set when starting session.
        /// </summary>
        public static string SID_Constant => "SID";

        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        public abstract string GetSessionName(IHttpPhpContext webctx);

        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        public abstract bool SetSessionName(IHttpPhpContext webctx, string name);

        /// <summary>
        /// Gets this handler name.
        /// </summary>
        public abstract string HandlerName { get; }

        /// <summary>
        /// Called when starting a session,
        /// loads session data into an array.
        /// </summary>
        /// <param name="webctx">Current web context.</param>
        public abstract PhpArray Load(IHttpPhpContext webctx);

        /// <summary>
        /// Stores the session array into the server session.
        /// </summary>
        /// <param name="webctx">Current web context.</param>
        /// <param name="session">Session array.</param>
        /// <returns></returns>
        public abstract bool Persist(IHttpPhpContext webctx, PhpArray session);

        /// <summary>
        /// Frees the session.
        /// Next time a new (empty) sesison should be created.
        /// </summary>
        /// <param name="webctx">Current web context.</param>
        public abstract void Abandon(IHttpPhpContext webctx);

        /// <summary>
        /// Gets the session ID (SID constant).
        /// </summary>
        public abstract string GetSessionId(IHttpPhpContext webctx);

        /// <summary>
        /// Sets the session ID (SID constant).
        /// </summary>
        public virtual bool SetSessionId(IHttpPhpContext webctx, string newid) => false;

        /// <summary>
        /// Gets value indicating the sessions are configured and available to use.
        /// </summary>
        public virtual bool IsEnabled(IHttpPhpContext webctx) => true;

        /// <summary>
        /// Starts the session if it is not started yet.
        /// </summary>
        public virtual bool StartSession(Context ctx, IHttpPhpContext webctx)
        {
            // checks and changes session state:
            if (webctx.SessionState != PhpSessionState.Closed) return false;
            webctx.SessionState = PhpSessionState.InProgress;

            try
            {
                // ensures session and reads session data
                var session_array = this.Load(webctx) ?? PhpArray.NewEmpty();

                // sets the auto-global variable (the previous content of $_SESSION array is discarded):
                ctx.Session = session_array;
                
                //if (ctx.Configuration.Core.RegisterGlobals)
                //{
                //    // ctx.RegisterSessionGlobals();
                //}

                // adds/updates a SID constant:
                if (!ctx.DefineConstant(SID_Constant, GetSessionId(webctx), true))
                {
                    throw new InvalidOperationException("SID already set.");    // TODO: allow overwriting
                }
            }
            catch
            {
                webctx.SessionState = PhpSessionState.Closed;
                return false;
            }
            finally
            {
                //
                webctx.SessionState = PhpSessionState.Started;
            }

            //
            return true;
        }

        /// <summary>
        /// Discard session array changes and finish session.
        /// </summary>
        public virtual void AbortSession(Context ctx, IHttpPhpContext webctx)
        {
            if (webctx.SessionState != PhpSessionState.Started) return;
            webctx.SessionState = PhpSessionState.InProgress;

            try
            {
                // TODO: clear $_SESSION ? 
            }
            finally
            {
                webctx.SessionState = PhpSessionState.Closed;
            }
        }

        /// <summary>
        /// Closes the session and either persists the session data or abandons the session.
        /// </summary>
        public virtual void CloseSession(Context ctx, IHttpPhpContext webctx, bool abandon)
        {
            if (webctx.SessionState != PhpSessionState.Started) return;
            webctx.SessionState = PhpSessionState.InProgress;

            //
            try
            {
                if (!abandon)
                {
                    Persist(webctx, ctx.Session ?? PhpArray.Empty);
                }
                else
                {
                    Abandon(webctx);
                }
            }
            finally
            {
                webctx.SessionState = PhpSessionState.Closed;
            }
        }
    }
}
