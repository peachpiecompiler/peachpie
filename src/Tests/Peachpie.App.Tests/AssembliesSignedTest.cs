using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;

namespace Peachpie.App.Tests
{
    /// <summary>
    /// In order to run on .NET Framework properly,
    /// all the referenced assemblies must have a strong signature.
    /// </summary>
    [TestClass]
    public class AssembliesSignedTest
    {
        [TestMethod]
        public void CheckSignature()
        {
            var types = new[]
            {
                typeof(Pchp.Library.Strings),
                typeof(Library.PDO.PDO),
                typeof(Library.Network.CURLFile),
                typeof(Library.Graphics.Exif),
                typeof(Library.MySql.MySql),
                typeof(Library.MsSql.MsSql),
                typeof(Library.Scripting.Standard),
            };

            var todo = new Queue<Assembly>(types.Select(t => t.Assembly));

            // collect list of all assemblies being referenced by Peachpie.App recursively
            var assemblies = new HashSet<Assembly>();

            while (todo.TryDequeue(out var ass))
            {
                if (assemblies.Add(ass))
                {
                    foreach (var r in ass.GetReferencedAssemblies())
                    {
                        todo.Enqueue(Assembly.Load(r));
                    }
                }
            }

            // assert all assemblies have key
            foreach (var ass in assemblies)
            {
                Assert.IsNotNull(ass.GetName().GetPublicKeyToken(), $"Assembly {ass} is not signed!");
            }
        }
    }
}