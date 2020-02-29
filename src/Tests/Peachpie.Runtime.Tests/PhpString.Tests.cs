using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;
using Pchp.Core.Text;

namespace Peachpie.Runtime.Tests
{
    [TestClass]
    public class PhpStringTest
    {
        [TestMethod]
        public void Substring()
        {
            //
            TestSubstring(new PhpString("helo", "world"));
            TestSubstring(new PhpString(new byte[] { 65, 97 }));

            var str = new PhpString("");
            str[8] = 'x';
            TestSubstring(str);
        }

        void TestSubstring(PhpString str)
        {
            var l = str.Length;
            for (int i = 0; i < l; i++)
            {
                for (int n = 0; n < l - i; n += 3)
                {
                    TestSubstring(str, i, n);
                }
            }
        }

        void TestSubstring(PhpString str, int offset, int length)
        {
            // test it won't crash
            var newstr = str.Substring(offset, length);

            // test it returns equally long string
            Assert.AreEqual(newstr.Length, length);
        }

        [TestMethod]
        public void Reverse()
        {
            //
            TestReverse("hello", "olleh");
            TestReverse("", "");
            TestReverse(new PhpString(new byte[] { 0, 1, 2, 3 }), new PhpString(new byte[] { 3, 2, 1, 0 }));

            // complex string
            var str = new PhpString("hello");
            str.EnsureWritable().Add(new byte[] { 1, 2, 3 });
            str.EnsureWritable().Add("world");

            var reversed = new PhpString();
            reversed.EnsureWritable().Add("dlrow");
            reversed.EnsureWritable().Add(new byte[] { 3, 2, 1 });
            reversed.EnsureWritable().Add("olleh");

            TestReverse(str, reversed);
        }

        void TestReverse(PhpString str, PhpString expected)
        {
            var reversed = str.Reverse();

            Assert.AreEqual(expected.Length, reversed.Length);
            Assert.AreEqual(expected.ToString(Encoding.ASCII), reversed.ToString(Encoding.ASCII));
        }
    }
}
