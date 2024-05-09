using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
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

        string CompileAndRun(Context ctx, string code, string[] additionalReferences = null)
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
                AdditionalReferences = additionalReferences,
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

        [TestMethod]
        public void ClrEvent()
        {
            using (var ctx = Context.CreateEmpty())
            {
                // $x = new ClrEventTestClass;
                ctx.Globals["x"] = PhpValue.FromClass(
                    new ClrEventTestClass()
                );

                var output = CompileAndRun(ctx, @"<?php

$hook = $x->e->add(
    function ($sender, $args) {
        echo '1';
    }
);
$x->fire(); // invoke event
$hook->close(); // unsubscribe callable
$x->fire(); // invoke empty event

");
                Assert.AreEqual("1", output);
            }
        }

        [TestMethod]
        public void ExplicitOverridesWithGenerics()
        {
            using (var ctx = Context.CreateEmpty())
            {
                // compiler must bind all the generic explicitly defined properties and methods correctly
                CompileAndRun(ctx, $@"<?php
class MyCollection extends {typeof(ObservableCollection).GetPhpTypeInfo().Name} //
{{
}}
",
                    additionalReferences: new[] { typeof(ObservableCollection).Assembly.Location }
                );

                Assert.IsNotNull(ctx.Create("MyCollection"));
            }
        }
    }

    #region Test Classes

    /// <summary>
    /// Implements both <see cref="ObservableCollection{T}"/> and PHP's <see cref="Iterator"/>.
    /// </summary>
    public class ObservableCollection : ObservableCollection<object>, Iterator, ArrayAccess
    {
        private int _position = 0;

        public ObservableCollection() : base()
        {

        }

        public ObservableCollection(PhpArray phpArray)
        {
            if (phpArray.IntrinsicEnumerator == null)
            {
                return;
            }

            foreach (var item in phpArray)
            {
                Add(item.Value.Alias);
            }
        }

        public void SetAll(PhpArray phpArray)
        {
            if (phpArray.IntrinsicEnumerator == null)
            {
                return;
            }

            Clear();

            foreach (var item in phpArray)
            {
                Add(item.Value.Alias);
            }
        }

        public void rewind()
        {
            _position = 0;
        }

        public void next()
        {
            _position++;
        }

        public bool valid()
        {
            return _position >= 0 && _position < Count;
        }

        public PhpValue key()
        {
            return _position;
        }

        public PhpValue current()
        {
            return PhpValue.FromClr(this[_position]);
        }

        public PhpValue offsetGet(PhpValue offset)
        {
            return PhpValue.FromClr(this[offset.ToInt()]);
        }

        public void offsetSet(PhpValue offset, PhpValue value)
        {
            this[offset.ToInt()] = value.ToClr();
        }

        public void offsetUnset(PhpValue offset)
        {
            RemoveAt(offset.ToInt());
        }

        public bool offsetExists(PhpValue offset)
        {
            return offset.ToInt() >= 0 && offset.ToInt() < Count;
        }

        public PhpArray toArray()
        {
            return new PhpArray(this);
        }
    }

    class ClrEventTestClass
    {
        public event EventHandler e;
        public void fire() => e?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
