using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;
using Peachpie.Library.PDO;

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