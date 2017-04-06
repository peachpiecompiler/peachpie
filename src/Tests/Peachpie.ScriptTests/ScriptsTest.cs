using System.Diagnostics;
using System.IO;
using System.Text;
using Pchp.Core;
using Peachpie.Library.Scripting;
using Xunit;
using Xunit.Abstractions;

namespace ScriptsTest
{
    public class ScriptsTest
    {
        static readonly Context.IScriptingProvider _provider = new ScriptingProvider();

        private readonly ITestOutputHelper _output;

        public ScriptsTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [ScriptsListData]
        public void ScriptRunTest(string dir, string fname)
        {
            var path = Path.Combine(dir, fname);

            _output.WriteLine("Testing {0} ...", path);

            // test script compilation and run it
            var result = CompileAndRun(path);

            // invoke php.exe if possible and compare results
            var phpresult = result;

            try
            {
                phpresult = Interpret(path);
            }
            catch
            {
                _output.WriteLine("Running PHP failed.");
                return;
            }

            //
            Assert.Equal(phpresult, result);
        }

        string CompileAndRun(string path)
        {
            var outputStream = new MemoryStream();

            using (var ctx = Context.CreateEmpty())
            {
                // redirect text output
                ctx.Output = new StreamWriter(outputStream, Encoding.UTF8) { AutoFlush = true };
                ctx.OutputStream = outputStream;

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

            //
            outputStream.Position = 0;
            return new StreamReader(outputStream, Encoding.UTF8).ReadToEnd();
        }

        static string Interpret(string path)
        {
            return RunProcess("php", path);
        }

        static string RunProcess(string exe, string args)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(exe, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
            };

            //
            process.Start();

            // To avoid deadlocks, always read the output stream first and then wait.
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            //
            if (!string.IsNullOrEmpty(error))
                return error;

            //
            return output;
        }
    }
}
