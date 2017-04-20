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

namespace Peachpie.Web
{
    /// <summary>
    /// Session handler for ASP.NET Core.
    /// </summary>
    class AspNetCoreSessionHandler : PhpSessionHandler
    {
        public static readonly PhpSessionHandler Default = new AspNetCoreSessionHandler();

        static ISession GetSession(IHttpPhpContext webctx) => ((RequestContextCore)webctx).HttpContext.Session;

        private AspNetCoreSessionHandler() { }

        public override string SessionName
        {
            get => SessionDefaults.CookieName;
            set => throw new NotSupportedException();
        }
        public override string HandlerName => "AspNetCore";

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
            {
                return string.Empty;
            }
        }

        public override PhpArray Load(IHttpPhpContext webctx)
        {
            var isession = GetSession(webctx);
            if (isession != null && isession.IsAvailable)
            {
                var result = new PhpArray();

                foreach (var key in isession.Keys)
                {
                    if (isession.TryGetValue(key, out byte[] value))
                    {
                        throw new NotImplementedException();
                    }
                }

                return result;
            }

            return PhpArray.NewEmpty();
        }

        public override bool Persist(IHttpPhpContext webctx, PhpArray session)
        {
            var ctx = (RequestContextCore)webctx;
            var isession = ctx.HttpContext.Session;
            if (isession != null)
            {
                isession.Clear();

                //
                if (session != null && session.Count != 0)
                {
                    var enumerator = session.GetFastEnumerator();
                    while (enumerator.MoveNext())
                    {
                        // serialize value using php serializer
                        // and save to underlaying ISession
                        var bytes = PhpSerialization.PhpSerializer.Instance.Serialize(ctx, enumerator.CurrentValue.GetValue(), default(RuntimeTypeHandle));
                        isession.Set(enumerator.CurrentKey.ToString(), bytes.ToBytes(ctx));
                    }
                }

                //
                isession.CommitAsync();

                //
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
