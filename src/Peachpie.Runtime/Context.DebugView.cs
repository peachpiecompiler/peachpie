using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Pchp.Core
{
    [DebuggerDisplay("{DebugDisplay,nq}")]
    [DebuggerTypeProxy(typeof(ContextDebugView))]
    [DebuggerNonUserCode]
    partial class Context
    {
        /// <summary>The debug view text.</summary>
        protected virtual string DebugDisplay => "Context";

        /// <summary>Content in debug view.</summary>
        sealed class ContextDebugView
        {
            readonly Context _ctx;

            public ContextDebugView(Context ctx)
            {
                _ctx = ctx;
            }

            public PhpArray Globals => _ctx.Globals;

            public PhpArray Superglobals => new PhpArray()
            {
                { "$GLOBALS", _ctx.Globals},
                { "$_SERVER", _ctx.Server },
                { "$_GET", _ctx.Get },
                { "$_POST", _ctx.Post },
                { "$_FILES", _ctx.Files },
                { "$_COOKIE", _ctx.Cookie },
                { "$_SESSION", _ctx.Session },
                { "$_REQUEST", _ctx.Request},
                { "$_ENV", _ctx.Env },
            };
        }
    }
}
