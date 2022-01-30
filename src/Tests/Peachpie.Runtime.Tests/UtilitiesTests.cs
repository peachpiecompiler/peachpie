using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;
using Pchp.Core.Collections;
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

        [TestMethod]
        public void Bin2HexTest()
        {
            Assert.AreEqual("", Pchp.Core.Utilities.StringUtils.BinToHex(new byte[] { }));
            Assert.AreEqual("08", Pchp.Core.Utilities.StringUtils.BinToHex(new byte[] { 8 }));
            Assert.AreEqual("ff", Pchp.Core.Utilities.StringUtils.BinToHex(new byte[] { 0xff }, "-"));
            Assert.AreEqual("ff-ff", Pchp.Core.Utilities.StringUtils.BinToHex(new byte[] { 0xff, 0xff }, "-"));
            Assert.AreEqual("ff-ff-ff", Pchp.Core.Utilities.StringUtils.BinToHex(new byte[] { 0xff, 0xff, 0xff }, "-"));
        }

        [TestMethod]
        public void ValueListTest()
        {
            var list = new ValueList<int>();
            for (int i = 0; i < 10; i++)
            {
                list.AddRange(new[] { 0, 1, 2, 3 });
                list.Add(4);
                list.Insert(list.Count, 5);
            }

            list.Insert(0, -1);
            list.Insert(1, 1);
        }
    }
}
