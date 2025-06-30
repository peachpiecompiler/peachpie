using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;
using Pchp.Core.Collections;
using Pchp.Core.Text;
using Pchp.Core.Utilities;

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
            
            Assert.AreEqual(0, list.Count);

            for (int i = 0; i < 10; i++)
            {
                list.AddRange(new[] { 0, 1, 2, 3 });
                list.Add(4);
                list.Insert(list.Count, 5);
            }

            var count2 = list.Count;
            Assert.AreEqual(10 * (6), count2);

            list.Insert(0, -1);
            list.Insert(1, 1);

            var count3 = list.Count;
            Assert.AreEqual(count2 + 2, count3);
        }

        [TestMethod]
        public void ValueListToBytesTest()
        {
            var list = new ValueList<byte>();
            list.AddBytes("hello", Encoding.UTF8);

            Assert.AreEqual(Encoding.UTF8.GetString(list.ToArray()), "hello");
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("lorem ipsum")]
        [DataRow("顧客は非常に重要です、顧客は顧客に続きます")]
        public void GetCharsTest(string input)
        {
            string value = input;

            for (int multiplier = 0; multiplier < 10; multiplier++)
            {
                var encoding = Encoding.UTF8;
                var bytes = encoding.GetBytes(value);

                var builder = new StringBuilder();
                var count = Pchp.Core.Utilities.EncodingExtensions.GetChars(encoding, bytes, builder);

                Assert.AreEqual(value.Length, count, "Length don't match");
                Assert.AreEqual(value, builder.ToString(), "String don't match");

                // add ~1M chars
                for (int i = 0; i < 1_000_000 / (input.Length + 1); i++)
                {
                    builder.Append(input);
                }
                value = builder.ToString();
            }
        }
    }
}
