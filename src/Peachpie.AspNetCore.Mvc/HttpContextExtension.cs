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

namespace Peachpie.AspNetCore.Mvc
{
    /// <summary>
    /// Extension methods to <see cref="HttpContext"/>.
    /// </summary>
    public static class HttpContextExtension
    {
        /// <summary>
        /// Renders Razor View or Partial View to the output.
        /// </summary>
        public static Task RenderViewAsync<TModel>(this HttpContext context, TextWriter output, IViewEngine viewEngine, string viewName, TModel model)
        {
            var appctx = new ActionContext(context, new RouteData(), new ActionDescriptor());

            var view = viewEngine.FindView(appctx, viewName, isMainPage: false);

            if (view.Success)
            {
                return RenderViewAsync(context, output, view.View, model);
            }
            else
            {
                throw new ArgumentException(nameof(viewName));
            }
        }

        /// <summary>
        /// Renders Razor View or Partial View to the output.
        /// </summary>
        public static Task RenderViewAsync<TModel>(this HttpContext context, TextWriter output, IView view, TModel model)
        {
            var viewdata = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()); // TODO: as parameter

            return view.RenderAsync(new ViewContext()
            {
                HttpContext = context,
                View = view,
                Writer = output,
                ViewData = new ViewDataDictionary<TModel>(viewdata, model)
            });
        }
    }
}
