using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Pchp.Core;
using Peachpie.Library.Scripting;
using Xunit;
using Xunit.Sdk;

namespace ScriptsTest
{
    public class ScriptsTest
    {
        static readonly Context.IScriptingProvider _provider = new ScriptingProvider();

        [Theory]
        [ScriptsListData]
        public void ScriptRunTest(string dir, string fname)
        {
            var path = Path.Combine(dir, fname);

            using (var ctx = Context.CreateEmpty())
            {
                // Compile and load 
                var script = _provider.CreateScript(new Context.ScriptOptions()
                {
                    Context = ctx,
                    IsSubmission = false,
                    EmitDebugInformation = true,
                    Location = new Location(path, 0, 0),
                }, File.ReadAllText(path));

                // run
                script.Evaluate(ctx, ctx.Globals, null);
            }
        }
    }
}
