using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Metadata;
using Peachpie.Library.Scripting;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler.CSharp.Syntax;


namespace Peachpie.App.Tests
{
    [TestClass]
    public class CompiledILTest
    {
        static IScript Compile(string code)
        {
            using (var ctx = Context.CreateConsole(string.Empty))
            {
                return (IScript)Context.DefaultScriptingProvider.CreateScript(new Context.ScriptOptions()
                {
                    Context = ctx,
                    Location = new Location(Path.Combine(Directory.GetCurrentDirectory(), "fake.php"), 0, 0),
                    EmitDebugInformation = true,
                    IsSubmission = false,
                    AdditionalReferences = new string[] {
                        typeof(CompiledILTest).Assembly.Location,
                    },
                }, code);
            }
        }

        static CSharpDecompiler Decompile(IScript script)
        {
            var fileName = "fake.dll";
            var file = new PEFile(fileName, new PEReader(script.Image));

            var resolver = new UniversalAssemblyResolver(
                fileName,
                false,
                file.DetectTargetFrameworkId(),
                file.DetectRuntimePack(),
                PEStreamOptions.PrefetchMetadata,
                MetadataReaderOptions.None
            );

            return new CSharpDecompiler(new DecompilerTypeSystem(file, resolver), new DecompilerSettings() { });
        }

        static bool DecompileFunction(IScript script, string fnname, out IMethod method, out AstNode ast)
        {
            var minfo = script.GetGlobalRoutineHandle(fnname).Single();

            var decompiler = Decompile(script);
            var mainType = decompiler.TypeSystem.FindType(new FullTypeName(minfo.DeclaringType.FullName)).GetDefinition();
            
            method = mainType.Methods.Single(m => m.Name == minfo.Name);
            ast = decompiler.Decompile(method.MetadataToken).LastChild;

            return true;
        }

        public static void OverloadFoo(int i) { }
        public static void OverloadFoo(double d) { }
        public static void OverloadFoo(string s) { }

        [TestMethod]
        public void TestOverloadByNamedParameter()
        {
            var script = Compile($@"<?php

function test($value) {{
    {typeof(CompiledILTest).FullName.Replace('.', '\\')}::{nameof(OverloadFoo)}( s: $value );
}}
");
            DecompileFunction(script, "test", out var method, out var ast);
            var bodyCode = ast.LastChild.LastChild.Descendants.ElementAt(1).ToString().Trim();

            Assert.IsTrue(
                // method body
                $"{nameof(CompiledILTest)}.{nameof(OverloadFoo)}" == bodyCode
            );
        }

        [TestMethod]
        public void TestSimpleFunction()
        {
            var script = Compile(@"<?php

function test() { return 1; }
");
            DecompileFunction(script, "test", out var method, out var ast);

            Assert.IsTrue(method.ReturnType.FullName == typeof(long).FullName);

            var bodyCode = ast.LastChild.ToString().Trim();

            Assert.IsTrue(
                // method body
                "{\r\n\treturn 1L;\r\n}" == bodyCode
            );
        }
    }
}
