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
    }
}
