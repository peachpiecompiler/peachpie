using System;
using System.Diagnostics;
using System.Web;
using System.Web.SessionState;
using Pchp.Core;
using Pchp.Core.Reflection;
using Pchp.Library;

namespace Peachpie.RequestHandler.Session
{
    sealed class AspNetSessionHandler : PhpSessionHandler
    {
        public static readonly PhpSessionHandler Default = new AspNetSessionHandler();

        /// <summary>
        /// The session item with serialized PHP <c>$_SESSION</c> array.
        /// It should be always there, even it is empty; in this way it keeps the ASP.NET session alive.
        /// </summary>
        const string PhpSessionVars = "Peachpie.SessionVars";

        static PhpSerialization.PhpSerializer Serializer => PhpSerialization.PhpSerializer.Instance;

        private AspNetSessionHandler() { }

        /// <summary>
        /// Gets current <see cref="HttpContext"/>.
        /// Cannot be <c>null</c>.
        /// </summary>
        static HttpContext/*!*/GetHttpContext(IHttpPhpContext webctx)
        {
            Debug.Assert(webctx is RequestContextAspNet);
            return ((RequestContextAspNet)webctx).HttpContext;
        }

        /// <summary>
		/// Ensures that Session ID is set, so calls to Flush() don't cause issues
		/// (if flush() is called, session ID can't be set because cookie can't be created).
		/// </summary>
        string EnsureSessionId(HttpContext httpContext)
        {
            Debug.Assert(httpContext != null);
            if (httpContext.Session != null && httpContext.Session.IsNewSession && httpContext.Session.Count == 0)
            {
                // Ensure the internal method SessionStateModule.DelayedGetSessionId() is called now,
                // not after the request is processed if no one uses SessionId during the request.
                // Otherwise it causes an attempt to save the Session ID when the response stream was already flushed.
                var ensureId = httpContext.Session.SessionID;

                Debug.WriteLine("SessionId: " + ensureId);

                return ensureId;
            }

            return null;
        }

        /// <summary>
        /// Gets the session name.
        /// </summary>
        public override string GetSessionName(IHttpPhpContext webctx) => AspNetSessionHelpers.GetConfigCookieName();

        /// <summary>
        /// Sets the session name.
        /// </summary>
        public override bool SetSessionName(IHttpPhpContext webctx, string name) => false; // throw new NotSupportedException();

        public override string HandlerName => "AspNet";

        public override void Abandon(IHttpPhpContext webctx)
        {
            GetHttpContext(webctx).Session.Abandon();
        }

        public override string GetSessionId(IHttpPhpContext webctx) => GetHttpContext(webctx).Session.SessionID;

        public override PhpArray Load(IHttpPhpContext webctx)
        {
            var ctx = (Context)webctx;
            var httpContext = GetHttpContext(webctx);

            EnsureSessionId(httpContext);

            var session = httpContext.Session;

            // session contains both ASP.NET session and PHP session variables

            PhpArray result = null;

            // serialized $_SESSION array
            if (session[PhpSessionVars] is byte[] data)
            {
                if (Serializer.TryDeserialize(ctx, data, out var value))
                {
                    result = value.ArrayOrNull();
                }
            }

            // .NET session items
            if (session.Mode == SessionStateMode.InProc)
            {
                foreach (string name in session)
                {
                    if (name == PhpSessionVars)
                    {
                        continue;
                    }

                    if (result == null)
                    {
                        result = new PhpArray(session.Count);
                    }

                    var value = PhpValue.FromClr(session[name]);
                    //if (value.IsObject)
                    //{
                    //    // CONSIDER: what if value has reference to a different Context - change it? clone the value? do not store it into the session in the first place?
                    //}
                    result[name] = value;
                }
            }
            else
            {
                // TODO: deserialize .NET session variables
                // CONSIDER: isn't it too much of overhead?
            }

            return result ?? PhpArray.NewEmpty();
        }

        /// <summary>
        /// Initiates the session.
        /// </summary>
        public override bool StartSession(Context ctx, IHttpPhpContext webctx)
        {
            if (base.StartSession(ctx, webctx))
            {
                var httpContext = GetHttpContext(webctx);
                var session = httpContext.Session;

                // override HttpSessionState._container : IHttpSessionState
                var container = session.GetContainer();
                if (container != null)
                {
                    if (container is PhpSessionStateContainer)
                    {
                        // unexpected; session already bound
                        throw new InvalidOperationException("Session was not closed properly.");
                    }

                    // overwrite IHttpSessionState
                    session.SetContainer(new PhpSessionStateContainer(container, ctx.Session));
                }
                else
                {
                    // cannot resolve the IHttpSessionState container
                }

                //
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool Persist(IHttpPhpContext webctx, PhpArray session)
        {
            var ctx = (Context)webctx;
            var httpContext = GetHttpContext(webctx);
            var state = httpContext.Session;

            // at this point, all the session items are stored in $_SESSION already
            // store non-PHP objects into ASP.NET session state, serialize the rest as PHP array
            PhpArray sessionvars = null;

            if (state.Mode == SessionStateMode.InProc)
            {
                // removes all items (some could be changed or removed in PHP):
                state.Clear();

                // populates session collection from variables:
                var enumerator = session.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    var value = enumerator.CurrentValue.GetValue(); // dereferenced value

                    // skips resources
                    if (value.IsResource) continue;

                    if (value.IsObject && value.Object.GetPhpTypeInfo().IsPhpType)
                    {
                        // PHP objects will be serialized using PHP serializer
                        if (sessionvars == null) sessionvars = new PhpArray(session.Count);
                        sessionvars[enumerator.CurrentKey] = value;
                    }
                    else
                    {
                        state[enumerator.CurrentKey.ToString()] = value.ToClr();
                    }
                }
            }
            else
            {
                // If the session is maintained out-of-process,
                // serialize the entire $_SESSION autoglobal as it is.
                sessionvars = session;
            }

            // Add the serialized $_SESSION to ASP.NET session.
            // Even in case this array is empty, this keeps the ASP.NET session state alive.
            state[PhpSessionVars] = sessionvars != null && sessionvars.Count != 0
                ? Serializer.Serialize(ctx, sessionvars, default(RuntimeTypeHandle)).ToBytes(ctx)
                : Array.Empty<byte>();

            //
            return true;
        }

        /// <summary>
        /// Close the session (either abandon or persist).
        /// </summary>
        public override void CloseSession(Context ctx, IHttpPhpContext webctx, bool abandon)
        {
            // set the original IHttpSessionState back
            var session = GetHttpContext(webctx).Session;
            if (session.GetContainer() is PhpSessionStateContainer shared)
            {
                session.SetContainer(shared.UnderlayingContainer);
            }

            // persist $_SESSION
            base.CloseSession(ctx, webctx, abandon);
        }
    }
}
