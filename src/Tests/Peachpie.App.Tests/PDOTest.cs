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
    /// <summary>
    /// In order to run on .NET Framework properly,
    /// all the referenced assemblies must have a strong signature.
    /// </summary>
    [TestClass]
    public class PDOTest
    {
        class DriverMock : PDODriver
        {
            public override string Name => "mock driver";

            public override DbProviderFactory DbFactory => throw new NotImplementedException();

            public override string GetLastInsertId(PDO pdo, string name) => throw new NotImplementedException();

            protected override string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options) => throw new NotImplementedException();
        }

        [TestMethod]
        public void RewriteCommandTest()
        {
            // ":name" syntax is rewritten to something understood by underlying databases

            var query = new DriverMock().RewriteCommand("CALL login_user(:user, :pass, :ip)", null, out var bound_param_map);

            Assert.AreEqual("CALL login_user(@user, @pass, @ip)", query);
            Assert.IsNotNull(bound_param_map);

            // NOTE: following might fail in future
            // PDODriver might rewrite named params tu positional params since that's how it is in PHP
            Assert.AreEqual("@user", bound_param_map["user"].String);
            Assert.AreEqual("@user", bound_param_map[":user"].String);
        }
    }
}