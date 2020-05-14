using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peachpie.NET.Sdk;

namespace Peachpie.NET.SdkTests
{
    [TestClass]
    public class SpdxTest
    {
        [TestMethod]
        public void DeprecatedSpdxTest()
        {
            Assert.AreEqual("GPL-2.0-or-later", SpdxHelpers.SanitizeSpdx("GPL-2.0+"));
            Assert.AreEqual("(GPL-2.0-or-later)", SpdxHelpers.SanitizeSpdx("(GPL-2.0+)"));
            Assert.AreEqual("(Apache-2.0 OR GPL-2.0-or-later)", SpdxHelpers.SanitizeSpdx("(Apache-2.0 OR GPL-2.0)"));
        }

        [TestMethod]
        public void InvalidSpdxTest()
        {
            Assert.AreEqual("Apache-2.0", SpdxHelpers.SanitizeSpdx("Apache 2.0"));

            // "OR" must be uppercased
            // combination of invalid license spdx
            Assert.AreEqual("(Apache-2.0 OR GPL-2.0-or-later)", SpdxHelpers.SanitizeSpdx("(Apache 2.0 or GPL 2.0)"));
            Assert.AreEqual("(BSD-2-Clause OR GPL-2.0-or-later)", SpdxHelpers.SanitizeSpdx("(BSD License or GPL 2.0)"));
        }
    }
}
