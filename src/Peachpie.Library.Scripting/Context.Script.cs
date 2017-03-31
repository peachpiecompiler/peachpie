using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    sealed class Script : Context.IScript
    {
        readonly Script _previousSubmission;
        readonly Context.MainDelegate _entryPoint;
        readonly ImmutableArray<byte> _image;
        readonly AssemblyName _assemblyName;

        /// <summary>
        /// Optional refernce to a script that preceeds this one.
        /// Current script requires this one to be evaluated.
        /// </summary>
        public Script DependingSubmission => _previousSubmission;

        /// <summary>
        /// Gets the assembly content.
        /// </summary>
        public ImmutableArray<byte> Image => _image;

        /// <summary>
        /// Gets the script assembly name.
        /// </summary>
        public AssemblyName AssemblyName => _assemblyName;

        private Script(AssemblyName assemblyName, MemoryStream peStream, MemoryStream pdbStream, PhpCompilationFactory builder, Script previousSubmissionOpt)
        {
            _assemblyName = assemblyName;
            _previousSubmission = previousSubmissionOpt;

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
                    _entryPoint = new Context.ScriptInfo(-1, attr.Path, t.GetTypeInfo()).Evaluate;
                    break;
                }
            }

            if (_entryPoint == null)
            {
                throw new InvalidOperationException();
            }
        }

        private Script(Context.MainDelegate entryPoint, Script previousSubmissionOpt)
        {
            _entryPoint = entryPoint;
            _previousSubmission = previousSubmissionOpt;
            _assemblyName = new AssemblyName();
            _image = ImmutableArray<byte>.Empty;
        }

        static IEnumerable<Script> AllPreviousSubmissions(Script previousSubmission)
        {
            for (var submission = previousSubmission; submission != null; submission = submission.DependingSubmission)
            {
                if (!submission.Image.IsDefaultOrEmpty)
                {
                    yield return submission;
                }
            }
        }

        public static Script Create(Context.ScriptOptions options, string code, PhpCompilationFactory builder, Script previousSubmission)
        {
            var tree = PhpSyntaxTree.ParseCode(code, new PhpParseOptions(kind: SourceCodeKind.Script), PhpParseOptions.Default, options.Location.Path);
            var diagnostics = tree.Diagnostics;
            if (!HasErrors(diagnostics))
            {
                var name = builder.GetNewSubmissionName();

                var compilation = builder.CoreCompilation
                    .WithAssemblyName(name.Name)
                    .AddSyntaxTrees(tree)
                    .AddReferences(AllPreviousSubmissions(previousSubmission).Select(s => MetadataReference.CreateFromImage(s.Image)));

                diagnostics = compilation.GetDeclarationDiagnostics();
                if (!HasErrors(diagnostics))
                {
                    var peStream = new MemoryStream();
                    var pdbStream = options.EmitDebugInformation ? new MemoryStream() : null;
                    var result = compilation.Emit(peStream, pdbStream);
                    if (result.Success)
                    {
                        return new Script(name, peStream, pdbStream, builder, previousSubmission);
                    }
                    else
                    {
                        diagnostics = result.Diagnostics;
                    }
                }
            }

            //
            return CreateInvalid(diagnostics, previousSubmission);
        }

        public static Script CreateInvalid(IEnumerable<Diagnostic> diagnostics, Script previousSubmission)
        {
            return new Script((ctx, locals, @this) =>
            {
                throw new NotImplementedException("invalid script, report diagnostics");
            }, previousSubmission);
        }

        static bool HasErrors(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        }

        #region Context.IScript

        public PhpValue Evaluate(Context ctx, PhpArray locals, object @this)
        {
            return _entryPoint(ctx, locals, @this);
        }

        #endregion

    }
}
