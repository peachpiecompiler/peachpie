using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;

#nullable enable

namespace Peachpie.App.Tests
{
    /// <summary>
    /// Class to be consumed by PHP nullability tests, covering different NullableContext combinations.
    /// </summary>
    public class NullabilitySubject
    {
        public NullabilitySubject noNull(NullabilitySubject x) => x;

        public NullabilitySubject? returnNull(NullabilitySubject x) => null;

        public NullabilitySubject argNull(NullabilitySubject? x) => new NullabilitySubject();

        public NullabilitySubject? allNull(NullabilitySubject? x) => null;

        [return: MaybeNull]
        public NullabilitySubject maybeNull() => null;

        [return: NotNull]
        public NullabilitySubject? notNull() => new NullabilitySubject();
    }

    [TestClass]
    public class NullabilityTest
    {
        private const string BaseScriptName = "nullability_base.php";
        private const string ExtensionScriptName = "nullability_extension.php";

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestNullability(bool debug)
        {
            using (var ctx = Context.CreateConsole(BaseScriptName))
            {
                // Script using NullabilitySubject class from C#
                var baseScript = CreateScript(BaseScriptName, ctx, debug);

                var compiledType =
                    baseScript
                        .GetGlobalRoutineHandle("get_nullability")
                        .Single()
                        .ReturnType;

                // [NullableContext(2)] on type
                Assert.IsTrue(HasNullableContextAttribute(compiledType, 2));

                // [Nullable(1)] on non-null parameters and return types

                Assert.IsTrue(HasParameterNullableAttribute(compiledType.GetMethod("noNull")!, 1));
                Assert.IsTrue(HasReturnNullableAttribute(compiledType.GetMethod("noNull")!, 1));

                Assert.IsTrue(!HasReturnNullableAttribute(compiledType.GetMethod("returnNull")!, 1));

                Assert.IsTrue(HasReturnNullableAttribute(compiledType.GetMethod("argNull")!, 1));

                Assert.IsTrue(!HasReturnNullableAttribute(compiledType.GetMethod("allNull")!, 1));

                Assert.IsTrue(!HasReturnNullableAttribute(compiledType.GetMethod("maybeNull")!, 1));

                Assert.IsTrue(HasReturnNullableAttribute(compiledType.GetMethod("notNull")!, 1));

                Assert.IsTrue(!HasReturnNullableAttribute(compiledType.GetMethod("phpReturnNull")!, 1));

                Assert.IsTrue(HasReturnNullableAttribute(compiledType.GetMethod("phpReturnNotNull")!, 1));

                Assert.IsTrue(!HasReturnNullableAttribute(compiledType.GetMethod("phpReturnNullExplicit")!, 1));

                Assert.IsTrue(HasReturnNullableAttribute(compiledType.GetMethod("phpReturnNotNullExplicit")!, 1));

                Assert.IsTrue(!HasParameterNullableAttribute(compiledType.GetMethod("phpParamNull")!, 1));

                // PHP script using another PHP script from different assembly
                var extensionScript = CreateScript(ExtensionScriptName, ctx, debug);

                var scriptType = extensionScript.GetGlobalRoutineHandle("nullable").Single().DeclaringType!;
                Assert.IsTrue(HasNullableContextAttribute(scriptType, 2));

                Assert.IsTrue(!HasReturnNullableAttribute(scriptType.GetMethod("nullable")!, 1));

                Assert.IsTrue(HasReturnNullableAttribute(scriptType.GetMethod("non_nullable")!, 1));
            }
        }

        private static Context.IScript CreateScript(string scriptName, Context ctx, bool debug)
        {
            string fullpath = Path.GetFullPath(scriptName);

            return Context.DefaultScriptingProvider.CreateScript(new Context.ScriptOptions()
            {
                Context = ctx,
                Location = new Location(fullpath, 0, 0),
                EmitDebugInformation = debug,
                IsSubmission = false,
                AdditionalReferences = new string[] {
                        typeof(NullabilityTest).Assembly.Location
                    },
            }, File.ReadAllText(fullpath));
        }

        private static bool HasNullableContextAttribute(Type type, byte expectedValue) =>
            type.GetCustomAttribute<NullableContextAttribute>()?.Flag == expectedValue;

        private static bool HasReturnNullableAttribute(MethodInfo method, byte expectedValue) =>
            method.ReturnParameter.GetCustomAttribute<NullableAttribute>()?.NullableFlags[0] == expectedValue;

        private static bool HasParameterNullableAttribute(MethodInfo method, byte expectedValue) =>
            method.GetParameters()[0].GetCustomAttribute<NullableAttribute>()?.NullableFlags[0] == expectedValue;
    }
}
