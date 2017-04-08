using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    /// <summary>
    /// Script representing a compiled submission.
    /// </summary>
    [DebuggerDisplay("Script ({AssemblyName.Name})")]
    sealed class Script : Context.IScript
    {
        #region Fields & Properties

        /// <summary>
        /// Set of dependency submissions.
        /// Can be empty.
        /// These scripts are expected to be evaluated when running this script.
        /// </summary>
        readonly Script[] _previousSubmissions;

        /// <summary>
        /// The entry method of the submissions global code.
        /// </summary>
        readonly Context.MainDelegate _entryPoint;

        /// <summary>
        /// Submission assembly image.
        /// </summary>
        readonly ImmutableArray<byte> _image;

        /// <summary>
        /// Siubmission assembly name.
        /// </summary>
        readonly AssemblyName _assemblyName;

        /// <summary>
        /// In case of valid submission, <c>&lt;Script&gt;</c> type representing the submissions global code.
        /// </summary>
        readonly Type _script;

        /// <summary>
        /// Refernces to scripts that preceeds this one.
        /// Current script requires these to be evaluated first.
        /// </summary>
        public IEnumerable<Script> DependingSubmissions => _previousSubmissions;

        /// <summary>
        /// Gets the assembly content.
        /// </summary>
        public ImmutableArray<byte> Image => _image;

        /// <summary>
        /// Gets the script assembly name.
        /// </summary>
        public AssemblyName AssemblyName => _assemblyName;

        #endregion

        #region Initialization

        private Script(AssemblyName assemblyName, MemoryStream peStream, MemoryStream pdbStream, PhpCompilationFactory builder, IEnumerable<Script> previousSubmissions)
        {
            _assemblyName = assemblyName;
            
            //
            peStream.Position = 0;
            if (pdbStream != null)
            {
                pdbStream.Position = 0;
            }

            var ass = builder.LoadFromStream(assemblyName, peStream, pdbStream);
            if (ass == null)
            {
                throw new ArgumentException();
            }

            peStream.Position = 0;
            _image = peStream.ToArray().ToImmutableArray();

            foreach (var t in ass.GetTypes())
            {
                var attr = t.GetTypeInfo().GetCustomAttribute<ScriptAttribute>(false);
                if (attr != null)
                {
                    _script = t;
                    _entryPoint = new Context.ScriptInfo(-1, attr.Path, t.GetTypeInfo()).Evaluate;
                    break;
                }
            }

            if (_entryPoint == null)
            {
                throw new InvalidOperationException();
            }

            // we have to "declare" the script, so its referenced symbols and compiled files are loaded into the context
            // this comes once after loading the assembly
            Context.AddScriptReference(ass);

            // find out highest dependent submissions, if any
            _previousSubmissions = CollectDependencies(ass, builder, previousSubmissions);
        }

        private Script(Context.MainDelegate entryPoint)
        {
            _entryPoint = entryPoint;
            _assemblyName = new AssemblyName();
            _image = ImmutableArray<byte>.Empty;
        }

        /// <summary>
        /// Collects scripts which declarations were used directly in the compiled assembly.
        /// These scripts are dependencies to the assembly so they must be evaluated first in order to re-use <paramref name="assembly"/> in future.
        /// </summary>
        /// <param name="assembly">Compiled assembly.</param>
        /// <param name="builder">Assembly factory.</param>
        /// <param name="previousSubmissions">All scripts referenced by the assembly compilation.</param>
        /// <returns></returns>
        private static Script[] CollectDependencies(Assembly assembly, PhpCompilationFactory builder, IEnumerable<Script> previousSubmissions)
        {
            // collect dependency scripts from referenced assemblies
            var dependencies = new HashSet<Script>();
            foreach (var refname in assembly.GetReferencedAssemblies()) // TODO: only assemblies really used within the {assembly} -> optimizes caching
            {
                var refass = builder.TryGetSubmissionAssembly(refname);
                if (refass != null)
                {
                    var refscript = previousSubmissions.First(s => s.AssemblyName.Name == refass.GetName().Name);
                    Debug.Assert(refscript != null);
                    if (refscript != null)
                    {
                        dependencies.Add(refscript);
                    }
                }
            }

            // remove dependencies of dependencies -> not needed for checking
            var toremove = new HashSet<Script>();
            foreach (var d in dependencies)
            {
                toremove.UnionWith(d.DependingSubmissions);
            }

            dependencies.ExceptWith(toremove);

            //
            return (dependencies.Count != 0)
                ? dependencies.ToArray()
                : Array.Empty<Script>();
        }

        /// <summary>
        /// Compiles <paramref name="code"/> and creates script.
        /// </summary>
        /// <param name="options">Compilation options.</param>
        /// <param name="code">Code to be compiled.</param>
        /// <param name="builder">Assembly builder.</param>
        /// <param name="previousSubmissions">Enumeration of scripts that were evaluated within current context. New submission may reference them.</param>
        /// <returns>New script reepresenting the compiled code.</returns>
        public static Script Create(Context.ScriptOptions options, string code, PhpCompilationFactory builder, IEnumerable<Script> previousSubmissions)
        {
            var tree = PhpSyntaxTree.ParseCode(code,
                new PhpParseOptions(kind: options.IsSubmission ? SourceCodeKind.Script : SourceCodeKind.Regular),
                PhpParseOptions.Default,
                options.Location.Path);


            var diagnostics = tree.Diagnostics;
            if (!HasErrors(diagnostics))
            {
                // unique in-memory assembly name
                var name = builder.GetNewSubmissionName();

                // list of scripts that were eval'ed in the context already,
                // our compilation may depend on them
                var dependingSubmissions = previousSubmissions.Where(s => !s.Image.IsDefaultOrEmpty);

                // create the compilation object
                // TODO: add conditionally declared types into the compilation tables
                var compilation = (PhpCompilation)builder.CoreCompilation
                    .WithAssemblyName(name.Name)
                    .AddSyntaxTrees(tree)
                    .AddReferences(dependingSubmissions.Select(s => MetadataReference.CreateFromImage(s.Image)));

                if (options.EmitDebugInformation)
                {
                    compilation = compilation.WithPhpOptions(compilation.Options.WithOptimizationLevel(OptimizationLevel.Debug).WithDebugPlusMode(true));
                }

                diagnostics = compilation.GetDeclarationDiagnostics();
                if (!HasErrors(diagnostics))
                {
                    var peStream = new MemoryStream();
                    var pdbStream = options.EmitDebugInformation ? new MemoryStream() : null;
                    var result = compilation.Emit(peStream, pdbStream);
                    if (result.Success)
                    {
                        return new Script(name, peStream, pdbStream, builder, previousSubmissions);
                    }
                    else
                    {
                        diagnostics = result.Diagnostics;
                    }
                }
            }

            //
            return CreateInvalid(diagnostics);
        }

        /// <summary>
        /// Initializes an invalid script that throws diagnostics upon invoking.
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        private static Script CreateInvalid(IEnumerable<Diagnostic> diagnostics)
        {
            return new Script((ctx, locals, @this) =>
            {
                foreach (var d in diagnostics)
                {
                    PhpException.Throw(PhpError.Error, d.GetMessage());
                }

                //
                return PhpValue.Void;
            });
        }

        #endregion

        /// <summary>
        /// Checks if given collection contains fatal errors.
        /// </summary>
        private static bool HasErrors(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        }

        #region Context.IScript

        public PhpValue Evaluate(Context ctx, PhpArray locals, object @this)
        {
            return _entryPoint(ctx, locals, @this);
        }

        /// <summary>
        /// Resolves global function handle(s).
        /// </summary>
        public IEnumerable<MethodInfo> GetGlobalRoutineHandle(string name)
        {
            if (_script == null)
            {
                return Enumerable.Empty<MethodInfo>();
            }
            else
            {
                return _script.GetTypeInfo().DeclaredMethods.Where(m => m.IsStatic && m.Name == name);
            }
        }

        #endregion
    }
}
