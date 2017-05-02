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
    }
}
