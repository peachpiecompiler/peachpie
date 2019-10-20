using System;
using System.Diagnostics;
using System.Web;
using System.Web.SessionState;
using Pchp.Core;
using Pchp.Library;

namespace Peachpie.RequestHandler.Session
{
    sealed class AspNetSessionHandler : PhpSessionHandler
    {
        public static readonly PhpSessionHandler Default = new AspNetSessionHandler();

        const string PhpNetSessionVars = "Peachpie.SessionVars";

        public const string AspNetSessionName = "ASP.NET_SessionId";

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
        void EnsureSessionId(HttpContext httpContext)
        {
            Debug.Assert(httpContext != null);
            if (httpContext.Session != null && httpContext.Session.IsNewSession && httpContext.Session.Count == 0)
            {
                // Ensure the internal method SessionStateModule.DelayedGetSessionId() is called now,
                // not after the request is processed if no one uses SessionId during the request.
                // Otherwise it causes an attempt to save the Session ID when the response stream was already flushed.
                var ensureId = httpContext.Session.SessionID;

                Debug.WriteLine("SessionId: " + ensureId);
            }
        }

        /// <summary>
        /// Gets the session name.
        /// </summary>
        public override string GetSessionName(IHttpPhpContext webctx) => AspNetSessionName;

        /// <summary>
        /// Sets the session name.
        /// </summary>
        public override bool SetSessionName(IHttpPhpContext webctx, string name) => false; // throw new NotSupportedException();

        public override string HandlerName => "AspNet";

        public override void Abandon(IHttpPhpContext webctx)
        {
            GetHttpContext(webctx).Session.Abandon();
        }

        public override string GetSessionId(IHttpPhpContext webctx)
        {
            return GetHttpContext(webctx).Session.SessionID;
        }

        public override PhpArray Load(IHttpPhpContext webctx)
        {
            var ctx = (Context)webctx;
            var httpContext = GetHttpContext(webctx);

            EnsureSessionId(httpContext);

            //// removes dummy item keeping the session alive:
            //if (httpContext.Session[AspNetSessionHandler.PhpNetSessionVars] as string == AspNetSessionHandler.DummySessionItem)
            //{
            //    httpContext.Session.Remove(AspNetSessionHandler.PhpNetSessionVars);
            //}

            //
            var state = httpContext.Session;

            PhpArray result = null;

            //if (state.Mode == SessionStateMode.InProc)
            //{
            //    result = new PhpArray();

            //    foreach (string name in state)
            //    {
            //        var value = PhpValue.FromClr(state[name]);
            //        // TODO: rebind value.Context
            //        result[name] = value;
            //    }
            //}
            //else
            {
                if (state[PhpNetSessionVars] is byte[] data)
                {
                    if (Serializer.TryDeserialize(ctx, data, out var value))
                    {
                        result = value.ArrayOrNull();
                    }
                }

                // TODO: deserialize .NET session variables
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
                //var httpContext = GetHttpContext(webctx);
                //var session = httpContext.Session;

                //// override HttpSessionState._container : IHttpSessionState
                //var oldcontainer = session.GetContainer();
                //if (oldcontainer is SharedSession)
                //{
                //    // unexpected; session already bound
                //}
                //else if (oldcontainer != null)
                //{
                //    // overwrite IHttpSessionState
                //    session.SetContainer(new SharedSession(oldcontainer, ctx.Session));
                //}

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

            //if (state.Mode == SessionStateMode.InProc)
            //{
            //    // removes all items (some could be changed or removed in PHP):
            //    // TODO: some session variables could be added in ASP.NET application
            //    state.Clear();

            //    // populates session collection from variables:
            //    var enumerator = session.GetFastEnumerator();
            //    while (enumerator.MoveNext())
            //    {
            //        // skips resources:
            //        if (!(enumerator.CurrentValue.Object is PhpResource))
            //        {
            //            state.Add(enumerator.CurrentKey.ToString(), enumerator.CurrentValue.ToClr());
            //        }
            //    }
            //}
            //else
            {
                // if the session is maintained out-of-process, serialize the entire $_SESSION autoglobal
                // add the serialized $_SESSION to ASP.NET session:
                state[PhpNetSessionVars] = Serializer.Serialize(ctx, session, default(RuntimeTypeHandle)).ToBytes(ctx);
            }

            //
            return true;
        }

        /// <summary>
        /// Close the session (either abandon or persist).
        /// </summary>
        public override void CloseSession(Context ctx, IHttpPhpContext webctx, bool abandon)
        {
            base.CloseSession(ctx, webctx, abandon);

            // set the original IHttpSessionState back
            var session = GetHttpContext(webctx).Session;
            var container = session.GetContainer();
            if (container is SharedSession shared && ReferenceEquals(shared.PhpSession, ctx.Session))
            {
                session.SetContainer(shared.UnderlayingContainer);
            }
        }
    }
}
