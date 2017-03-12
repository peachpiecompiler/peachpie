using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Library
{
    partial class PhpInfo
    {
        private sealed class HtmlTagWriter : IDisposable
        {
            private readonly Context m_ctx;
            private readonly string m_tag;

            public Context Context { get { return this.m_ctx; } }

            public HtmlTagWriter(Context ctx, string tag, object attributes)
            {
                this.m_ctx = ctx;
                this.m_tag = tag;
                WriteBeginTag(ctx, tag, attributes);
                ctx.Echo(">");
            }

            private static void WriteBeginTag(Context ctx, string tag, object attributes)
            {
                ctx.Echo($"<{tag}");
                if (attributes != null)
                {
                    foreach (var prop in attributes.GetType().GetTypeInfo().GetProperties())
                    {
                        ctx.Echo($" {prop.Name}");
                        string value = prop.GetValue(attributes)?.ToString();
                        if (value != null)
                        {
                            ctx.Echo($@"=""{Strings.htmlspecialchars(value, Strings.QuoteStyle.BothQuotes)}""");
                        }
                    }
                }
            }

            internal void EchoTagSelf(string tag, object attributes = null)
            {
                WriteBeginTag(this.m_ctx, tag, attributes);
                this.m_ctx.Echo(" />");
            }

            public void Dispose()
            {
                this.m_ctx.Echo($"</{this.m_tag}>");
            }

            public void EchoRaw(string text)
            {
                this.m_ctx.Echo(text);
            }

            public void EchoTag(string tag, string text, object attributes = null)
            {
                using (HtmlTagWriter w = new HtmlTagWriter(this.m_ctx, tag, attributes))
                {
                    w.EchoEscaped(text);
                }
            }

            public void EchoEscaped(string text)
            {
                if (text != null)
                {
                    this.m_ctx.Echo(Strings.htmlentities(new PhpString(text)));
                }
            }
        }
    }
}
