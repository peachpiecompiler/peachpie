using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis;

namespace Peachpie.Library.Scripting
{
#if NETSTANDARD

    sealed class PhpCompilationFactory : PhpCompilationFactoryBase
    {
        System.Runtime.Loader.AssemblyLoadContext AssemblyLoadContext => System.Runtime.Loader.AssemblyLoadContext.Default;

        public PhpCompilationFactory()
        {
            //AssemblyLoadContext.Resolving += AssemblyLoadContext_Resolving;
        }

        //private Assembly AssemblyLoadContext_Resolving(System.Runtime.Loader.AssemblyLoadContext assCtx, AssemblyName assName)
        //    => TryGetSubmissionAssembly(assName);

        protected override Assembly LoadFromStream(MemoryStream peStream, MemoryStream pdbStream)
            => AssemblyLoadContext.LoadFromStream(peStream, pdbStream);
    }

#else // NET461, does not have AssemblyLoadContext in System.Runtime.Loader 4.0.0 (we need >= 4.0.1)

    sealed class PhpCompilationFactory : PhpCompilationFactoryBase
    {
        public PhpCompilationFactory()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            => TryGetSubmissionAssembly(new AssemblyName(args.Name));

        protected override Assembly LoadFromStream(MemoryStream peStream, MemoryStream pdbStream)
            => Assembly.Load(peStream.ToArray(), pdbStream?.ToArray());
    }

#endif

    abstract class PhpCompilationFactoryBase
    {
        public PhpCompilationFactoryBase()
        {
            _compilation = PhpCompilation.Create("project",
                references: MetadataReferences().Select(CreateMetadataReference),
                syntaxTrees: Array.Empty<PhpSyntaxTree>(),
                options: new PhpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    baseDirectory: Directory.GetCurrentDirectory(),
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

            var xmlDomType = Type.GetType(Assembly.CreateQualifiedName("Peachpie.Library.XmlDom", "Peachpie.Library.XmlDom.XmlDom"));
            if (xmlDomType != null)
            {
                types.Add(xmlDomType);
            }

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
                _assemblies.TryGetValue(assemblyName.Name, out var assembly))
            {
                return assembly;
            }
            else
            {
                return null;
            }
        }

        protected abstract Assembly LoadFromStream(MemoryStream peStream, MemoryStream pdbStream);

        public Assembly LoadFromStream(AssemblyName assemblyName, MemoryStream peStream, MemoryStream pdbStream)
        {
            var assembly = LoadFromStream(peStream, pdbStream);
            if (assembly != null)
            {
                _assemblies.Add(assemblyName.Name, assembly);
            }

            return assembly;
        }

        static int _counter = 0;

        const string s_submissionAssemblyNamePrefix = "<submission>`";

        public AssemblyName GetNewSubmissionName()
        {
            return new AssemblyName(s_submissionAssemblyNamePrefix + (_counter++).ToString());
        }
    }
}