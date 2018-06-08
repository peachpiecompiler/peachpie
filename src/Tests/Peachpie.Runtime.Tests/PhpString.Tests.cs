using System;
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
    }
}
