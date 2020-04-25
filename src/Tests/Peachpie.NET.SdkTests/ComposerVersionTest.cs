using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peachpie.NET.Sdk.Versioning;

namespace Peachpie.Runtime.Tests
{
    [TestClass]
    public class ComposerVersionTest
    {
        [TestMethod]
        public void VersionParseTest()
        {
            Assert.IsTrue(ComposerVersion.TryParse("1.2.3", out var ver));
            Assert.AreEqual("1.2.3", ver.ToString());

            Assert.IsTrue(ComposerVersion.TryParse("1.2.*", out ver));
            Assert.AreEqual("1.2.*", ver.ToString());

            Assert.IsTrue(ComposerVersion.TryParse("1.2", out ver));
            Assert.AreEqual("1.2", ver.ToString());
        }

        [TestMethod]
        public void FloatingVersionTest()
        {
            Assert.IsTrue(ComposerVersionExpression.TryParse("1.2.3", out var expr));
            Assert.AreEqual("[1.2.3]", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse("1.4.*", out expr));
            Assert.AreEqual("[1.4.0-*,1.5.0)", expr.Evaluate().ToString());   // >=1.4.0.0-dev <1.5.0.0-dev

            Assert.IsTrue(ComposerVersionExpression.TryParse(">=1.0 <2.0", out expr)); // >=1.0.0-dev && <2.0.0-dev
            Assert.AreEqual("[1.0.0-*,2.0.0)", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse(">1.2", out expr));
            Assert.AreEqual("(1.2.0,]", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse(">=1.2", out expr));
            Assert.AreEqual("[1.2.0-*,]", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse("<1.3", out expr));
            Assert.AreEqual("[,1.3.0)", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse("1 - 2", out expr));
            Assert.AreEqual("[1.0.0-*,3.0.0)", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse("~1.3", out expr));
            Assert.AreEqual("[1.3.0-*,2.0.0)", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse("^1.2.3", out expr));
            Assert.AreEqual("[1.2.3,2.0.0)", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse("^0.3", out expr));
            Assert.AreEqual("[0.3.0,0.4.0)", expr.Evaluate().ToString());

            Assert.IsTrue(ComposerVersionExpression.TryParse("1.0.*", out expr)); // >=1.0.0-dev && <1.1.0-dev
            Assert.AreEqual("[1.0.0-*,1.1.0)", expr.Evaluate().ToString());
        }
    }
}
