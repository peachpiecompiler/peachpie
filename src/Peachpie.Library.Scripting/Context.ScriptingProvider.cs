using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    [Export(typeof(Context.IScriptingProvider))]
    public sealed class ScriptingProvider : Context.IScriptingProvider
    {
        readonly Dictionary<string, List<Script>> _scripts = new Dictionary<string, List<Script>>();
        readonly PhpCompilationFactory _builder = new PhpCompilationFactory();
        readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        List<Script> EnsureCache(string code)
        {
            if (!_scripts.TryGetValue(code, out List<Script> candidates))
            {
                _scripts[code] = candidates = new List<Script>();
            }
            return candidates;
        }

        Script CacheLookup(Context.ScriptOptions options, string code, ScriptingContext data)
        {
            if (_scripts.TryGetValue(code, out List<Script> candidates))
            {
                foreach (var c in candidates)
                {
                    // candidate requires that all its dependencies were loaded into context
                    if (c.DependingSubmissions.All(data.Submissions.Contains))
                    {
                        return c;
                    }
                }
            }

            return null;
        }

        Context.IScript Context.IScriptingProvider.CreateScript(Context.ScriptOptions options, string code)
        {
            var data = ScriptingContext.EnsureContext(options.Context);

            _rwLock.EnterReadLock();
            Script script;
            try
            {
                script = CacheLookup(options, code, data);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            if (script == null)
            {
                script = Script.Create(options, code, _builder, data.Submissions);

                _rwLock.EnterWriteLock();
                try
                {
                    EnsureCache(code).Add(script);
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }

            Debug.Assert(script != null);

            //
            data.Submissions.Add(script);

            //
            return script;
        }
    }
}
