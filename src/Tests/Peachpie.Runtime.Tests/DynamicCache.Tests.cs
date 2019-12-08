using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;

namespace Peachpie.Runtime.Tests
{
    [TestClass]
    public class DynamicCacheTests
    {
        [TestMethod]
        public void AllInitializedTest()
        {
            var cacheType = typeof(Context).Assembly.GetType("Pchp.Core.Dynamic.Cache", true);
            foreach (var t in cacheType.GetNestedTypes())
            {
                foreach (var f in t.GetFields())
                {
                    Assert.IsNotNull(f.GetValue(null), $"Field '{t.Name}.{f.Name}' is null!");
                }
            }
        }
    }
}
