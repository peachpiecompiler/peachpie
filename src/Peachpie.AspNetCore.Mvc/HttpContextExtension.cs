using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Pchp.Core;
using Peachpie.AspNetCore.Web;

namespace Peachpie.AspNetCore.Mvc
{
    /// <summary>
    /// Extension methods to <see cref="HttpContext"/>.
    /// </summary>
    public static class HttpContextExtension
    {
        #region nested class: PhpScriptRouter

        sealed class PhpScriptRouter : IRouter
        {
            public VirtualPathData GetVirtualPath(VirtualPathContext context) => null;

            public Task RouteAsync(RouteContext context) => throw new NotSupportedException();
        }

        #endregion

        #region nested class: CustomViewDataDictionary

        sealed class CustomViewDataDictionary : ViewDataDictionary
        {
            public CustomViewDataDictionary(ViewDataDictionary source, object model, Type modelType)
                : base(source, model, modelType)
            {
            }
        }

        #endregion

        /// <summary>
        /// Renders Razor Partial View to the output.
        /// </summary>
        /// <param name="phpContext">Current context. If used within PHP code, this is passed implicitly by compiler.</param>
        /// <param name="viewName">Name of the razor view.</param>
        /// <param name="model">Model object instance.</param>
        /// <remarks>
        /// Can be used within a PHP code.
        /// Note current <see cref="HttpContext.RequestServices"/> must provide <see cref="ICompositeViewEngine"/>
        /// and <see cref="ITempDataDictionaryFactory"/>. These services are susually added by <c>Mvc</c> services.
        /// </remarks>
        public static void RenderPartial(this Context phpContext, string viewName, object model = null)
        {
            var httpcontext = phpContext.GetHttpContext() ?? throw new ArgumentException(nameof(phpContext));

            RenderPartialAsync(httpcontext, phpContext.Output, viewName, model).Wait();
        }

        /// <summary>
        /// Gets Razor Partial View output.
        /// </summary>
        /// <param name="phpContext">Current context. If used within PHP code, this is passed implicitly by compiler.</param>
        /// <param name="viewName">Name of the razor view.</param>
        /// <param name="model">Model object instance.</param>
        /// <remarks>
        /// Can be used within a PHP code.
        /// Note current <see cref="HttpContext.RequestServices"/> must provide <see cref="ICompositeViewEngine"/>
        /// and <see cref="ITempDataDictionaryFactory"/>. These services are susually added by <c>Mvc</c> services.
        /// </remarks>
        public static string Partial(this Context phpContext, string viewName, object model = null)
        {
            var httpcontext = phpContext.GetHttpContext() ?? throw new ArgumentException(nameof(phpContext));

            var output = new StringWriter();
            RenderPartialAsync(httpcontext, output, viewName, model).Wait();
            return output.GetStringBuilder().ToString();
        }

        /// <summary>
        /// Renders Razor View or Partial View to the output.
        /// </summary>
        public static Task RenderPartialAsync(this HttpContext context, TextWriter output,
            string viewName, object model = null,
            ViewDataDictionary viewdata = null,
            IViewEngine viewEngine = null,
            ITempDataDictionaryFactory tmpdataFactory = null,
            HtmlHelperOptions htmlOptions = null)
        {
            // dummy router:
            var routedata = new RouteData();
            routedata.Routers.Add(new PhpScriptRouter());

            // find the view:
            var action = new ActionContext(context, routedata, new ActionDescriptor());
            viewEngine ??= context.RequestServices.GetService<ICompositeViewEngine>() ?? context.RequestServices.GetService<IViewEngine>();
            var view = viewEngine.FindView(action, viewName, isMainPage: false);

            if (view.Success)
            {
                // render the view:
                tmpdataFactory ??= context.RequestServices.GetService<ITempDataDictionaryFactory>();
                viewdata ??= new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());

                var viewctx = new ViewContext(action, view.View,
                    new CustomViewDataDictionary(viewdata, model, model != null ? model.GetType() : typeof(object)),
                    tmpdataFactory.GetTempData(context),
                    output,
                    htmlOptions ?? new HtmlHelperOptions { });

                return view.View.RenderAsync(viewctx);
            }
            else
            {
                throw new ArgumentException(nameof(viewName));
            }

        }
    }
}
