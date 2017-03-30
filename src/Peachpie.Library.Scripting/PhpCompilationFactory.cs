using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
//using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis;

namespace Peachpie.Library.Scripting
{
    class PhpCompilationFactory // : AssemblyLoadContext
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
            };

            var list = types.Distinct().Select(ass => ass.GetTypeInfo().Assembly).ToList();
            var set = new HashSet<Assembly>(list);

            for (int i = 0; i < list.Count; i++)
            {
                var ass = list[i];
                var refs = ass.GetReferencedAssemblies();
                foreach (var refname in refs)
                {
                    var refass = Assembly.Load(refname);
                    if (refass != null && set.Add(refass))
                    {
                        list.Add(refass);
                    }
                }
            }

            //
            return list.Select(ass => ass.Location);
        }

        public PhpCompilation CoreCompilation => _compilation;
        readonly PhpCompilation _compilation;
        readonly IAssemblySymbol _assemblytmp;

        readonly Dictionary<AssemblyName, Assembly> _asses = new Dictionary<AssemblyName, Assembly>();

        public Assembly LoadFromStream(AssemblyName assemblyName, MemoryStream peStream, MemoryStream pdbStream)
        {
#if NETSTANDARD1_6
            Assembly ass = null;//this.LoadFromStream(peStream, pdbStream);
#else
            Assembly ass = Assembly.Load(peStream.ToArray(), pdbStream?.ToArray());
#endif
            if (ass != null)
            {
                _asses.Add(assemblyName, ass);
            }
            return ass;
        }

        //protected override Assembly Load(AssemblyName assemblyName)
        //{
        //    _asses.TryGetValue(assemblyName, out Assembly assembly);
        //    return assembly;
        //}

        int _counter = 0;

        public AssemblyName GetNewSubmissionName()
        {
            return new AssemblyName($"<submission>`{_counter++}");
        }
    }
}
