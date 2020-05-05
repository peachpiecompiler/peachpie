using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        static MetadataReference CreateMetadataReference(string path) => MetadataReference.CreateFromFile(path);

        /// <summary>
        /// Collect references we have to pass to the compilation.
        /// </summary>
        static IEnumerable<string> MetadataReferences()
        {
            // implicit references
            var impl = new List<Assembly>(8)
            {
                typeof(object).Assembly,                 // mscorlib (or System.Runtime)
                typeof(Pchp.Core.Context).Assembly,      // Peachpie.Runtime
                typeof(Pchp.Library.Strings).Assembly,   // Peachpie.Library
                typeof(ScriptingProvider).Assembly,      // Peachpie.Library.Scripting
            };

            var set = new HashSet<Assembly>();

            set.UnionWith(impl);
            set.UnionWith(Pchp.Core.Context.GetScriptReferences().Where(ass => !IsSubmissionAssemblyName(ass.GetName())));  // PHP assemblies, excluding eval'ed code

            var todo = new List<Assembly>(set);

            for (int i = 0; i < todo.Count; i++)
            {
                var assembly = todo[i];
                var refs = assembly.GetReferencedAssemblies();
                foreach (var refname in refs)
                {
                    var refassembly = Assembly.Load(refname);
                    if (refassembly != null && set.Add(refassembly))
                    {
                        todo.Add(refassembly);
                    }
                }
            }

            return set.Select(ass => ass.Location);
        }

        PhpCompilation CreateDefaultCompilation()
        {
            return PhpCompilation.Create("project",
                references: MetadataReferences().Select(CreateMetadataReference),
                syntaxTrees: Array.Empty<PhpSyntaxTree>(),
                options: new PhpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    baseDirectory: Directory.GetCurrentDirectory(),
                    sdkDirectory: null));
        }

        public PhpCompilation CoreCompilation
        {
            get
            {
                if (_compilation == null)
                {
                    lock (this) // double-checked lock, avoid `CreateDefaultCompilation` to be called twice
                    {
                        if (_compilation == null)
                        {
                            if (Interlocked.CompareExchange(ref _compilation, CreateDefaultCompilation(), null) == null)
                            {
                                // bind reference manager, cache all references
                                _assemblytmp = _compilation.Assembly;
                            }
                        }
                    }
                }

                // TODO: if script assemblies were added to Context, alter the compilation with references to them

                //
                return _compilation;
            }
        }

        PhpCompilation _compilation;
        IAssemblySymbol _assemblytmp;

        /// <summary>
        /// Set of simple assembly names (submissions) loaded by the factory.
        /// </summary>
        readonly Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>(StringComparer.Ordinal);

        static bool IsSubmissionAssemblyName(AssemblyName assemblyName)
        {
            return assemblyName.Name.StartsWith(s_submissionAssemblyNamePrefix, StringComparison.Ordinal);
        }

        public Assembly TryGetSubmissionAssembly(AssemblyName assemblyName)
        {
            if (IsSubmissionAssemblyName(assemblyName) && _assemblies.TryGetValue(assemblyName.Name, out var assembly))
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

        const string s_submissionAssemblyNamePrefix = "<eval>`";

        public AssemblyName GetNewSubmissionName()
        {
            var id = Interlocked.Increment(ref _counter);

            return new AssemblyName(s_submissionAssemblyNamePrefix + id.ToString());
        }
    }
}