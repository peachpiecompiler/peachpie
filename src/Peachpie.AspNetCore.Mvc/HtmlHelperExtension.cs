using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pchp.Core;
using Peachpie.AspNetCore.Web;

namespace Peachpie.AspNetCore.Mvc
{
    /// <summary>
    /// Extension for <see cref="IHtmlHelper"/>.
    /// </summary>
    public static class HtmlHelperExtension
    {
        /// <summary>
        /// Helper class implementing <see cref="IHtmlContent"/>.
        /// </summary>
        sealed class PhpContent : IHtmlContent
        {
            readonly Context _ctx;
            readonly Context.ScriptInfo _script;
            readonly object _this;

            public PhpContent(Context ctx, Context.ScriptInfo script, object @this)
            {
                _ctx = ctx;
                _script = script;
                _this = @this;
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                var buff = _ctx.BufferedOutput;

                // start output buffering
                buff.IncreaseLevel();
                _ctx.IsOutputBuffered = true;

                // execute the script
                _script.Evaluate(_ctx,
                    locals: _ctx.Globals,
                    @this: _this,
                    self: _this != null ? _this.GetType().TypeHandle : default(RuntimeTypeHandle));

                // end output buffering and get the output
                var output = buff.GetContent();
                if (buff.DecreaseLevel(false) < 0)
                {
                    _ctx.IsOutputBuffered = false;
                }

                //
                writer.Write(output.ToString(_ctx));
            }
        }

        /// <summary>
        /// Returns output for the specified PHP script.
        /// </summary>
        /// <exception cref="ArgumentException">Given script path is not declared.</exception>
        public static IHtmlContent Php(this IHtmlHelper htmlHelper, string relativePath, object @this = null)
        {
            var script = Context.TryGetDeclaredScript(relativePath);
            if (script.IsValid)
            {
                return new PhpContent(htmlHelper.ViewContext.HttpContext.GetOrCreateContext(), script, @this);
            }
            else
            {
                throw new ArgumentException(nameof(relativePath));
            }
        }

        /// <summary>
        /// Renders specified PHP script.
        /// </summary>
        /// <exception cref="ArgumentException">Given script path is not declared.</exception>
        public static void RenderPhp(this IHtmlHelper htmlHelper, string relativePath, object @this = null)
        {
            Php(htmlHelper, relativePath, @this).WriteTo(htmlHelper.ViewContext.Writer, null);
        }
    }
}
