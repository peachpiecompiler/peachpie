using System;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;

namespace Peachpie.App.Tests
{
    [TestClass]
    public class PCRETest
    {
        [TestMethod]
        public void PregMatchPerf()
        {
            Pchp.Library.PCRE.preg_match(
                "/^(([A-Za-z0-9!#$%&'*+\\/=?^_`{|}~-][A-Za-z0-9!#$%&'*+\\/=?^_`{|}~\\.-]{0,63})|(\"[^(\\|\")]{0,62}\"))$/",
                "something@example.org");
        }
    }
}