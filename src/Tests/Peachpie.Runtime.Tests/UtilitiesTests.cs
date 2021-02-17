using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;
using Pchp.Core.Text;

namespace Peachpie.Runtime.Tests
{
    [TestClass]
    public class UtilitiesTests
    {
        [TestMethod]
        public void PathUtilsTest()
        {
            Assert.AreEqual("php", Pchp.Core.Utilities.PathUtils.GetExtension("c:/something.path/index.php").ToString());
            Assert.AreEqual("", Pchp.Core.Utilities.PathUtils.GetExtension("c:\\something.path\\index").ToString());
            Assert.AreEqual("", Pchp.Core.Utilities.PathUtils.GetExtension("/something.path/index.").ToString());

            Assert.AreEqual("file.txt", Pchp.Core.Utilities.PathUtils.GetFileName("/something.path/file.txt").ToString());
            Assert.AreEqual("file.txt", Pchp.Core.Utilities.PathUtils.GetFileName("file.txt").ToString());
        }
    }
}
