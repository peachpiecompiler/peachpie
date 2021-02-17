using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peachpie.NET.Sdk.Versioning;

namespace Peachpie.NET.SdkTests
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

            Assert.IsTrue(ComposerVersion.TryParse("1.2.x", out ver));
            Assert.AreEqual("1.2.*", ver.ToString());
        }

        void AssertFloatingVersion(string versionConstrain, string expectedFloatingVersion)
        {
            Assert.IsTrue(ComposerVersionExpression.TryParse(versionConstrain, out var expr));
            Assert.AreEqual(expectedFloatingVersion, expr.Evaluate().ToString());
        }

        [TestMethod]
        public void FloatingVersionTest()
        {
            AssertFloatingVersion("1.2.3", "[1.2.3]");

            AssertFloatingVersion("=1.2.3", "[1.2.3]");

            AssertFloatingVersion("1.4.*", "[1.4.0-*,1.5.0)");      // >=1.4.0.0-dev <1.5.0.0-dev

            AssertFloatingVersion(">=1.0 <2.0", "[1.0.0-*,2.0.0)"); // >=1.0.0-dev && <2.0.0-dev

            AssertFloatingVersion(">=2.4.0,<2.4.*-dev", "[2.4.0-*,2.5.0)");

            AssertFloatingVersion(">1.2", "(1.2.0,]");

            AssertFloatingVersion(">=1.2", "[1.2.0-*,]");

            AssertFloatingVersion("<1.3", "[,1.3.0)");

            AssertFloatingVersion("1 - 2", "[1.0.0-*,3.0.0)");

            AssertFloatingVersion("~1.3", "[1.3.0-*,2.0.0)");

            AssertFloatingVersion("^1.2.3", "[1.2.3,2.0.0)");

            AssertFloatingVersion("^0.3", "[0.3.0,0.4.0)");

            AssertFloatingVersion("^0.4.5", "[0.4.5,0.5.0)");

            AssertFloatingVersion("1.0.*", "[1.0.0-*,1.1.0)");      // >=1.0.0-dev && <1.1.0-dev

            AssertFloatingVersion("~2.0|~3.0", "[2.0.0-*,4.0.0)");  // some packages use single `|` instead of double `||`

            AssertFloatingVersion(">=2.0.0, <4.0.0", "[2.0.0-*,4.0.0)");

            AssertFloatingVersion("1.2.3@dev@master", "[1.2.3]"); // ignoring flags for now

            // "~2.3, >=2.3.10"
            // "2.0.0a3"
            // "^1.10.0alpha2"
        }
    }
}
