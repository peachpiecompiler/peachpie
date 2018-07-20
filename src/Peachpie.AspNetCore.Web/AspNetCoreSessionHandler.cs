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

namespace Peachpie.AspNetCore.Web
{
    /// <summary>
    /// Session handler for ASP.NET Core.
    /// </summary>
    sealed class AspNetCoreSessionHandler : PhpSessionHandler
    {
        public static readonly PhpSessionHandler Default = new AspNetCoreSessionHandler();

        private AspNetCoreSessionHandler() { }

        static ISession GetSession(IHttpPhpContext webctx) => ((RequestContextCore)webctx).HttpContext.Session;

        static PhpSerialization.Serializer Serializer => PhpSerialization.PhpSerializer.Instance;

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
            var ctx = (RequestContextCore)webctx;
            try
            {
                var session = ctx.HttpContext.Session; // throws if session is not configured
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
            var isession = GetSession(webctx);
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
            var ctx = (RequestContextCore)webctx;
            var isession = ctx.HttpContext.Session; // throws if session is not configured
            if (isession.IsAvailable)
            {
                var result = new PhpArray();

                foreach (var key in isession.Keys)
                {
                    if (isession.TryGetValue(key, out byte[] bytes))
                    {
                        // try to deserialize bytes using php serializer
                        // gets FALSE if bytes are in incorrect format
                        result[key] = Serializer.Deserialize(ctx, new PhpString(bytes), default(RuntimeTypeHandle));
                    }
                }

                if (result.Count == 0)
                {
                    // store/remove a dummy item to invoke `TryEstablishSession()`
                    // causing session cookies to be posted at the right time
                    isession.Set(DummySessionItem, Array.Empty<byte>());
                    isession.Remove(DummySessionItem);
                }

                return result;
            }

            return PhpArray.NewEmpty();
        }

        public override bool Persist(IHttpPhpContext webctx, PhpArray session)
        {
            var ctx = (RequestContextCore)webctx;
            var isession = ctx.HttpContext.Session; // throws if session is not configured

            //
            isession.Clear();

            //
            if (session != null && session.Count != 0)
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
    }
}
