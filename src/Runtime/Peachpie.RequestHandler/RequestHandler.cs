using System;
using System.Diagnostics;
using System.Threading;
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
        /// Factory method to instantiate <see cref="Context"/> for the given <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="context">ASP.NET HTTP context. Must not be <c>null</c>.</param>
        /// <returns>Instance of newly created <see cref="Context"/> representing HTTP request.</returns>
        /// <remarks>
        /// The method always returns a new instance of <see cref="Context"/>.
        /// 
        /// Remember to always dispose the object when the request is finished.
        /// 
        /// The <see cref="Context.RootPath"/> is initialized with given <see cref="HttpContext"/>.<see cref="HttpContext.Request"/>.<see cref="HttpRequest.PhysicalApplicationPath"/> value.
        /// 
        /// The context is only valid when using IIS Integrated Pipeline (<see cref="HttpRuntime.UsingIntegratedPipeline"/> == <c>true</c>).
        /// 
        /// </remarks>
        public static Context CreateRequestContext(HttpContext context)
        {
            return new RequestContextAspNet(context);
        }

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
            var phpctx = (RequestContextAspNet)CreateRequestContext(context);

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
