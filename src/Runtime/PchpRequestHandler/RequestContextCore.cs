using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Context for ASP.NET Core.
    /// </summary>
    sealed class RequestContextCore : RequestContextBase
    {
        //static ScriptInfo DefaultDocument
        //{
        //    get
        //    {
        //        if (_lazyDefaultDocument.HasValue)
        //        {
        //            return _lazyDefaultDocument.Value;
        //        }

        //        _lazyDefaultDocument = ScriptsMap.GetDeclaredScript("index.php");
        //        return _lazyDefaultDocument.Value;
        //    }
        //}
        //static ScriptInfo? _lazyDefaultDocument;

        public static ScriptInfo ResolveScript(HttpRequest req)
        {
            var path = req.Path.Value.Trim('\\', '/');    // TODO
            return ScriptsMap.GetDeclaredScript(path);
        }

        ///// <summary>
        ///// Application physical root directory including trailing slash.
        ///// </summary>
        //public override string RootPath => HttpRuntime.AppDomainAppPath;

        public RequestContextCore(HttpContext context)
            : base()
        {
            this.InitOutput(context.Response.Body);
        }

        public override Encoding StringEncoding => Encoding.UTF8;
    }
}
