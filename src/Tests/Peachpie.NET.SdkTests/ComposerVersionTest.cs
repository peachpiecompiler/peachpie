using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peachpie.NET.Sdk.Versioning;

namespace Peachpie.Runtime.Tests
{
    [TestClass]
    public class ComposerVersionTest
    {
        [TestMethod]
        public void ParseTest()
        {
            Assert.IsTrue(ComposerVersion.TryParse("1.2.3", out var ver));
            Assert.AreEqual("1.2.3", ver.ToString());

            Assert.IsTrue(ComposerVersion.TryParse("1.2.*", out ver));
            Assert.AreEqual("1.2.*", ver.ToString());

            Assert.IsTrue(ComposerVersion.TryParse("1.2", out ver));
            Assert.AreEqual("1.2", ver.ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse("1.2.3", out var expr));
            Assert.AreEqual("[1.2.3]", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse("1.2.*", out expr));
            Assert.AreEqual("[1.2.0,1.3.0)", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse(">=1.0 <2.0", out expr));
            Assert.AreEqual("[1.0.0,2.0.0)", expr.Evaluate().ToString());
        }
    }
}
