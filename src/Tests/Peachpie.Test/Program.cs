using System;
using System.IO;
using Pchp.Core;

namespace Peachpie.Test
{
    class Program
    {
        const string ScriptPath = "index.php";

        static void Main(string[] args)
        {
            // bootstrapper that compiles, loads and runs our script file

            var provider = (Context.IScriptingProvider)new Library.Scripting.ScriptingProvider();
            var fullpath = Path.Combine(Directory.GetCurrentDirectory(), ScriptPath);

            using (var ctx = Context.CreateConsole(args))
            {
                //
                var script = provider.CreateScript(new Context.ScriptOptions()
                {
                    Context = ctx,
                    Location = new Location(fullpath, 0, 0),
                    EmitDebugInformation = true,
                    IsSubmission = false,
                }, File.ReadAllText(fullpath));

                //
                script.Evaluate(ctx, ctx.Globals, null);
            }
        }
    }
}