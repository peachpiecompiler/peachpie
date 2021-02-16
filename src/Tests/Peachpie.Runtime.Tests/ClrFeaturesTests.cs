using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;

namespace Peachpie.Runtime.Tests
{
    [TestClass]
    public class ClrFeaturesTests
    {
        string CompileAndRun(string code)
        {
            var outputStream = new MemoryStream();

            using (var ctx = Context.CreateEmpty())
            {
                // mimic the execution in the given folder
                ctx.RootPath = ctx.WorkingDirectory = Directory.GetCurrentDirectory();

                // redirect text output
                ctx.Output = new StreamWriter(outputStream, Encoding.UTF8) { AutoFlush = true };
                ctx.OutputStream = outputStream;

                // Compile and load 
                var script = Context.DefaultScriptingProvider.CreateScript(new Context.ScriptOptions()
                {
                    Context = ctx,
                    IsSubmission = false,
                    //EmitDebugInformation = true,
                    Location = new Location(Path.GetFullPath("dummy.php"), 0, 0),
                    //AdditionalReferences = AdditionalReferences,
                }, code);

                // run
                script.Evaluate(ctx, ctx.Globals, null);
            }

            //
            outputStream.Position = 0;
            return new StreamReader(outputStream, Encoding.UTF8).ReadToEnd();
        }

        [TestMethod]
        public void ScriptingProvider()
        {
            Assert.AreEqual("ok", CompileAndRun("<?php echo 'ok';"));
        }
        
        [TestMethod]
        public void Dictionary()
        {
            Assert.AreEqual(
                "ok,key=>ok",
                CompileAndRun(@"<?php
$d = new \System\Collections\Generic\Dictionary<string,string>;

$d['key'] = 'ok';
echo $d['key']; // ok

foreach ($d as $k => $v) {
  echo ',', $k, '=>', $v; // ,key=>ok
}
"));

            Assert.AreEqual(
                "ok",
                CompileAndRun(@"<?php
$d = new \System\Collections\Generic\Dictionary<string,string>;

$d['key'] = 'ok';
if (isset($d['key'])) echo 'ok';
"));
        }
    }
}
