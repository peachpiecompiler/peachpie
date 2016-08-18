using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Pchp.Core
{
    sealed class RequestContextAspNet : RequestContextBase
    {
        /// <summary>
        /// Application physical root directory including trailing slash.
        /// </summary>
        public override string RootPath => HttpRuntime.AppDomainAppPath;

        public RequestContextAspNet(HttpContext context)
            : base(context.Response.OutputStream)
        {
            Debug.Assert(HttpRuntime.UsingIntegratedPipeline);
        }

        /// <summary>
        /// Includes requested script file.
        /// </summary>
        public void Include(HttpRequest req)
        {
            this.Include(string.Empty, req.PhysicalPath.Substring(req.PhysicalApplicationPath.Length), false, true);
        }
    }
}
