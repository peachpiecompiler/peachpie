using System;
using System.IO;
using System.Text;
using ComponentAce.Compression.Libs.zlib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Peachpie.Runtime.Tests
{
    [TestClass]
    public class ClrFeaturesTests
    {
        string CompileAndRun(string code)
        {
            using (var ctx = Context.CreateEmpty())
            {
                return CompileAndRun(ctx, code);
            }
        }

        string CompileAndRun(Context ctx, string code)
        {
            var outputStream = new MemoryStream();

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

        [TestMethod]
        public void Property()
        {
            using (var ctx = Context.CreateEmpty())
            {
                CompileAndRun(@"<?php

class C {
    function get_MyProperty() { return 1; }
    function set_MyProperty($value) {}

    function get_ValueOnly(): int { return 2; }

    function get_StringOnly(): \System\String { return 'Hello'; }
}");

                var c = ctx.Create("C");

                var c_phptype = c.GetPhpTypeInfo();
                Assert.IsNull(c_phptype.GetDeclaredProperty("MyProperty")); // not visible as PHP property

                var c_type = c.GetType();
                Assert.IsNotNull(c_type.GetProperty("MyProperty")); // visible as CLR property

                var valueprop = c_type.GetProperty("ValueOnly");
                Assert.IsNotNull(valueprop);
                Assert.AreEqual(2L, valueprop.GetValue(c));

                Assert.AreEqual("Hello", (string)c_type.GetProperty("StringOnly").GetValue(c));
            }
        }
    }
}
