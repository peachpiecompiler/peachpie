using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;

namespace Peachpie.Runtime.Tests
{
    [TestClass]
    public class PhpValueTest
    {
        [TestMethod]
        public void StringLongConversion()
        {
            Assert.AreEqual(Pchp.Core.Convert.StringToLongInteger("1"), 1);
        }

        [TestMethod]
        public void ClrConversion()
        {
            // CLR -> Value -> CLR
            var objects = new object[] { 0L, 1L, true, 1.2, new object(), "Hello", new PhpArray() };
            var values = PhpValue.FromClr(objects);
            Assert.AreEqual(objects.Length, values.Length);
            for (int i = 0; i < objects.Length; i++)
            {
                Assert.AreEqual(objects[i], values[i].ToClr());
            }
        }

        class PhpIterator : Iterator
        {
            int idx = 0;
            string[] items = new string[] { "Hello", " ", "World" };

            public PhpValue current() => (PhpValue)items[idx];

            public PhpValue key() => (PhpValue)idx;

            public void next() => idx++;

            public void rewind() => idx = 0;

            public bool valid() => idx >= 0 && idx < items.Length;
        }

        [TestMethod]
        public void ClrEnumerator()
        {
            var value = PhpValue.FromClass(new PhpIterator());

            string result = null;

            foreach (var pair in value)
            {
                result += pair.Value.String;
            }

            Assert.AreEqual("Hello World", result);
        }
    }
}
