using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;
using Pchp.Library;

namespace Peachpie.App.Tests
{
    [TestClass]
    public class GlobTest
    {
        [TestMethod]
        public void TestSlashes()
        {
            // glob() keeps the same slashes as in the pattern
            var cwd = System.IO.Directory.GetCurrentDirectory();

            using var ctx = Context.CreateEmpty();

            var prefixes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new string[]
                {
                    cwd.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "\\",
                    cwd.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "/",
                    cwd + "\\",
                    cwd + "/",
                }
                : new string[]
                {
                    cwd + "/",
                };

            foreach (var prefix in prefixes)
            {
                var files = PhpPath.glob(ctx, prefix + "*.php");

                Assert.IsTrue(files.Count != 0, $"No files for prefix '{prefix}' in cwd '{cwd}'");

                foreach (var fileObj in files.Values)
                {
                    var file = fileObj.ToStringOrThrow(ctx);

                    Assert.IsTrue(file.StartsWith(prefix), $"Expected '{file}' to start with '{prefix}'");
                }
            }
        }
    }
}
