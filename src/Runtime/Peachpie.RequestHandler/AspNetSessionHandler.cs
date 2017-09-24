using System;
using System.Diagnostics;
using System.Web;
using System.Web.SessionState;
using Pchp.Core;
using Pchp.Library;

namespace Peachpie.RequestHandler
{
    sealed class AspNetSessionHandler : PhpSessionHandler
    {
        public static readonly PhpSessionHandler Default = new AspNetSessionHandler();

        const string PhpNetSessionVars = "Peachpie.SessionVars";

        public const string AspNetSessionName = "ASP.NET_SessionId";

        static PhpSerialization.Serializer Serializer => PhpSerialization.PhpSerializer.Instance;

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
                var data = state[PhpNetSessionVars] as byte[];
                if (data != null)
                {
                    result = Serializer.Deserialize((Context)webctx, new PhpString(data), default(RuntimeTypeHandle)).ArrayOrNull();
                }
            }

            return result ?? PhpArray.NewEmpty();
        }

        public override bool Persist(IHttpPhpContext webctx, PhpArray session)
        {
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
                state[PhpNetSessionVars] = Serializer.Serialize((Context)webctx, (PhpValue)session, default(RuntimeTypeHandle)).ToBytes((Context)webctx);
            }

            //
            return true;
        }
    }
}
