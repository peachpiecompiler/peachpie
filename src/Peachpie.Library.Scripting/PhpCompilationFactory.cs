using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis;

namespace Peachpie.Library.Scripting
{
    class PhpCompilationFactory
#if !NET46
        : System.Runtime.Loader.AssemblyLoadContext
#endif
    {
        public PhpCompilationFactory()
        {
            _compilation = PhpCompilation.Create("project",
                references: MetadataReferences().Select(CreateMetadataReference),
                syntaxTrees: Array.Empty<PhpSyntaxTree>(),
                options: new PhpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    baseDirectory: System.IO.Directory.GetCurrentDirectory(),
                    sdkDirectory: null));

            // bind reference manager, cache all references
            _assemblytmp = _compilation.Assembly;
        }

        static MetadataReference CreateMetadataReference(string path) => MetadataReference.CreateFromFile(path);

        /// <summary>
        /// Collect references we have to pass to the compilation.
        /// </summary>
        static IEnumerable<string> MetadataReferences()
        {
            // implicit references
            var types = new List<Type>()
            {
                typeof(object),                 // mscorlib (or System.Runtime)
                typeof(Pchp.Core.Context),      // Peachpie.Runtime
                typeof(Pchp.Library.Strings),   // Peachpie.Library
                typeof(ScriptingProvider),      // Peachpie.Library.Scripting
            };

            var list = types.Distinct().Select(ass => ass.GetTypeInfo().Assembly).ToList();
            var set = new HashSet<Assembly>(list);

            for (int i = 0; i < list.Count; i++)
            {
                var assembly = list[i];
                var refs = assembly.GetReferencedAssemblies();
                foreach (var refname in refs)
                {
                    var refassembly = Assembly.Load(refname);
                    if (refassembly != null && set.Add(refassembly))
                    {
                        list.Add(refassembly);
                    }
                }
            }

            //
            return list.Select(ass => ass.Location);
        }

        public PhpCompilation CoreCompilation => _compilation;
        readonly PhpCompilation _compilation;
        readonly IAssemblySymbol _assemblytmp;

        /// <summary>
        /// Set of simple assembly names (submissions) loaded by the factory.
        /// </summary>
        readonly Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>(StringComparer.Ordinal);

        public Assembly TryGetSubmissionAssembly(AssemblyName assemblyName)
        {
            if (assemblyName.Name.StartsWith(s_submissionAssemblyNamePrefix, StringComparison.Ordinal) &&
                _assemblies.TryGetValue(assemblyName.Name, out Assembly assembly))
            {
                return assembly;
            }

            return null;
        }

        public Assembly LoadFromStream(AssemblyName assemblyName, MemoryStream peStream, MemoryStream pdbStream)
        {
#if NET46
            Assembly assembly = Assembly.Load(peStream.ToArray(), pdbStream?.ToArray());
#else
            Assembly assembly = this.LoadFromStream(peStream, pdbStream);
#endif
            if (assembly != null)
            {
                _assemblies.Add(assemblyName.Name, assembly);
            }
            return assembly;
        }

#if !NET46
        protected override Assembly Load(AssemblyName assemblyName) => TryGetSubmissionAssembly(assemblyName);
#endif

        static int _counter = 0;

        const string s_submissionAssemblyNamePrefix = "<submission>`";

        public AssemblyName GetNewSubmissionName()
        {
            return new AssemblyName(s_submissionAssemblyNamePrefix + (_counter++).ToString());
        }
    }
}
