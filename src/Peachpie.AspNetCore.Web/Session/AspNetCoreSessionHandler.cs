using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using Pchp.Core;
using Pchp.Library;

namespace Peachpie.AspNetCore.Web.Session
{
    /// <summary>
    /// Session handler for ASP.NET Core.
    /// </summary>
    sealed class AspNetCoreSessionHandler : PhpSessionHandler
    {
        public static readonly PhpSessionHandler Default = new AspNetCoreSessionHandler();

        private AspNetCoreSessionHandler() { }

        static HttpContext GeHttpContext(IHttpPhpContext webctx) => ((RequestContextCore)webctx).HttpContext;

        static PhpSerialization.PhpSerializer Serializer => PhpSerialization.PhpSerializer.Instance;

        /// <summary>
        /// Gets the session name.
        /// </summary>
        public override string GetSessionName(IHttpPhpContext webctx) => SessionDefaults.CookieName;

        /// <summary>
        /// Sets the session name.
        /// </summary>
        public override bool SetSessionName(IHttpPhpContext webctx, string name) => false; // throw new NotSupportedException();

        public override string HandlerName => "AspNetCore";

        /// <summary>
        /// Checks if sessions were configured.
        /// </summary>
        public override bool IsEnabled(IHttpPhpContext webctx)
        {
            var httpctx = GeHttpContext(webctx);
            try
            {
                var session = httpctx.Session; // throws if session is not configured
                return session != null;
            }
            catch
            {
                return false;
            }
        }

        public override void Abandon(IHttpPhpContext webctx)
        {
            // TODO: abandon asp.net core session
            throw new NotImplementedException();
        }

        public override string GetSessionId(IHttpPhpContext webctx)
        {
            var isession = GeHttpContext(webctx).Session;
            if (isession != null)
            {
                return isession.Id;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Called when sessions are started.
        /// </summary>
        public override PhpArray Load(IHttpPhpContext webctx)
        {
            var result = new PhpArray();

            var ctx = (RequestContextCore)webctx;
            var isession = ctx.HttpContext.Session; // throws if session is not configured
            if (isession.IsAvailable)
            {
                foreach (var key in isession.Keys)
                {
                    if (isession.TryGetValue(key, out byte[] bytes))
                    {
                        // try to deserialize bytes using php serializer
                        if (Serializer.TryDeserialize(ctx, bytes, out var value))
                        {
                            result[key] = value;
                        }
                        else
                        {
                            // it was not serialized using PHP serializer or format is invalid
                            // TODO: deserialize .NET session variable
                        }
                    }
                }

                if (result.Count == 0)
                {
                    // store/remove a dummy item to invoke `TryEstablishSession()`
                    // causing session cookies to be posted at the right time
                    isession.Set(DummySessionItem, Array.Empty<byte>());
                    isession.Remove(DummySessionItem);
                }
            }

            return result;
        }

        /// <summary>
        /// Initiates the session.
        /// </summary>
        public override bool StartSession(Context ctx, IHttpPhpContext webctx)
        {
            if (base.StartSession(ctx, webctx))
            {
                //var httpctx = GeHttpContext(webctx);

                //if (httpctx.Session is SharedSession)
                //{
                //    // unexpected; session already bound
                //}
                //else
                //{
                //    // overwrite ISession
                //    httpctx.Session = new SharedSession(httpctx.Session, ctx.Session);
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
            var ctx = (RequestContextCore)webctx;
            var isession = ctx.HttpContext.Session; // throws if session is not configured

            //
            // TODO: do not delete .NET session variables
            isession.Clear();

            //
            if (session != null)
            {
                var enumerator = session.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    // serialize value using php serializer
                    // and save to underlaying ISession
                    var bytes = Serializer.Serialize(ctx, enumerator.CurrentValue.GetValue(), default(RuntimeTypeHandle));
                    isession.Set(enumerator.CurrentKey.ToString(), bytes.ToBytes(ctx));
                }
            }

            //
            isession.CommitAsync();

            //
            return true;
        }

        /// <summary>
        /// Close the session (either abandon or persist).
        /// </summary>
        public override void CloseSession(Context ctx, IHttpPhpContext webctx, bool abandon)
        {
            base.CloseSession(ctx, webctx, abandon);

            // set the original ISession back
            var httpctx = GeHttpContext(webctx);
            if (httpctx.Session is SharedSession shared && ReferenceEquals(shared.PhpSession, ctx.Session))
            {
                httpctx.Session = shared.UnderlayingSession;
            }
        }
    }
}
