using System;
using System.Diagnostics;
using System.Web;
using System.Web.SessionState;
using Pchp.Core;

namespace Peachpie.RequestHandler
{
    /// <summary>
	/// Process a request and stores references to objects associated with it.
	/// </summary>
	[Serializable]
    public sealed class RequestHandler : IHttpHandler, IRequiresSessionState
    {
        /// <summary>
        /// Invoked by ASP.NET when a request comes from a client.
        /// Single threaded.
        /// </summary>
        /// <param name="context">The reference to web server objects. Cannot be a <c>null</c> reference.</param>
        [DebuggerNonUserCode]
        public void ProcessRequest(HttpContext context)
        {
            Debug.Assert(context != null);
#if DEBUG
            // disables ASP.NET timeout if possible:
            try { context.Server.ScriptTimeout = int.MaxValue; } catch (HttpException) { }
#endif
            var phpctx = new RequestContextAspNet(context);

            try
            {
                // find and process requested file
                phpctx.Include(context.Request);
            }
            catch (ScriptDiedException died)
            {
                // echo eventual status message
                died.ProcessStatus(phpctx);
            }
            catch (Exception exception)
            {
                if (!phpctx.OnUnhandledException(exception))
                {
                    throw;
                }
            }
            finally
            {
                phpctx.Dispose();
                phpctx = null;
            }
        }

        /// <summary>
        /// Whether another request can reuse this instance.
        /// All fields are reinitialized at the beginning of the request thus the instance is reusable.
        /// </summary>
        public bool IsReusable => true;
    }
}
