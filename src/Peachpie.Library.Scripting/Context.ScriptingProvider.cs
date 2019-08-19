using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    public sealed class ScriptingProvider : Context.IScriptingProvider
    {
        readonly Dictionary<string, List<Script>> _scripts = new Dictionary<string, List<Script>>(StringComparer.Ordinal);
        readonly ReaderWriterLockSlim _scriptsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        readonly PhpCompilationFactory _builder = new PhpCompilationFactory();

        Script/*!*/TryGetOrCreateScript(string code, Context.ScriptOptions options, ScriptingContext context)
        {
            var script = default(Script);

            _scriptsLock.EnterUpgradeableReadLock();
            try
            {
                if (!_scripts.TryGetValue(code, out var subsmissions))
                {
                    _scriptsLock.EnterWriteLock();
                    try
                    {
                        if (!_scripts.TryGetValue(code, out subsmissions))
                        {
                            _scripts[code] = subsmissions = new List<Script>(1);
                        }
                    }
                    finally
                    {
                        _scriptsLock.ExitWriteLock();
                    }
                }

                if ((script = CacheLookupNoLock(subsmissions, options, code, context)) == null)
                {
                    _scriptsLock.EnterWriteLock();
                    try
                    {
                        if ((script = CacheLookupNoLock(subsmissions, options, code, context)) == null)
                        {
                            subsmissions.Add((script = Script.Create(options, code, _builder, context.Submissions)));
                        }
                    }
                    finally
                    {
                        _scriptsLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _scriptsLock.ExitUpgradeableReadLock();
            }

            return script;
        }

        Script CacheLookupNoLock(List<Script> candidates, Context.ScriptOptions options, string code, ScriptingContext context)
        {
            foreach (var c in candidates)
            {
                Debug.Assert(c.DependingSubmissions != null);

                // candidate requires that all its dependencies were loaded into context
                // TODO: resolve the compiled code dependencies - referenced types and declared functions - instead of "DependingSubmissions"
                if (c.DependingSubmissions.All(context.Submissions.Contains))
                {
                    return c;
                }
            }

            return null;
        }

        Context.IScript Context.IScriptingProvider.CreateScript(Context.ScriptOptions options, string code)
        {
            var context = ScriptingContext.EnsureContext(options.Context);
            var script = TryGetOrCreateScript(code, options, context);

            Debug.Assert(script != null);

            //
            context.Submissions.Add(script);

            //
            return script;
        }
    }
}
