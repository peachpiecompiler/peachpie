using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    [Export(typeof(Context.IScriptingProvider))]
    public class ScriptingProvider : Context.IScriptingProvider
    {
        readonly Dictionary<string, List<Script>> _scripts = new Dictionary<string, List<Script>>();
        readonly PhpCompilationFactory _builder = new PhpCompilationFactory();

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
            var script = CacheLookup(options, code, data);
            if (script == null)
            {
                // TODO: rwlock cache[code]
                script = Script.Create(options, code, _builder, data.Submissions);
                EnsureCache(code).Add(script);
            }

            Debug.Assert(script != null);

            //
            data.Submissions.Add(script);

            //
            return script;
        }
    }
}
