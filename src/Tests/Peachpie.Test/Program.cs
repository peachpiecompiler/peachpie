using System;
using System.IO;
using Pchp.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Peachpie.Test
{
    class Program
    {
        const string ScriptPath = "index.php";

        static void Main(string[] args)
        {
            // bootstrapper that compiles, loads and runs our script file

            var provider = Context.GlobalServices.GetService<Context.IScriptingProvider>(); // use IScriptingProvider singleton 
            var fullpath = Path.Combine(Directory.GetCurrentDirectory(), ScriptPath);

            using (var ctx = Context.CreateConsole(string.Empty, args))
            {
                //
                var script = provider.CreateScript(new Context.ScriptOptions()
                {
                    Context = ctx,
                    Location = new Location(fullpath, 0, 0),
                    EmitDebugInformation = true,
                    IsSubmission = false,
                    AdditionalReferences = new string[] {
                        typeof(Library.Graphics.PhpImage).Assembly.Location,
                        typeof(Library.Network.CURLFunctions).Assembly.Location
                    },
                }, File.ReadAllText(fullpath));

                //
                script.Evaluate(ctx, ctx.Globals, null);
            }
        }
    }
}