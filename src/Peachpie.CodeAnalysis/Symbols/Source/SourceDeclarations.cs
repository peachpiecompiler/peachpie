using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Pchp.CodeAnalysis.Semantics;
using Roslyn.Utilities;
using Pchp.CodeAnalysis.Utilities;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents declarations within given source trees.
    /// </summary>
    internal class SourceDeclarations : ISemanticModel
    {
        #region PopulatorVisitor

        sealed class PopulatorVisitor : TreeVisitor
        {
            readonly SourceDeclarations _tables;
            readonly PhpCompilation _compilation;

            SourceFileSymbol _currentFile;

            public PopulatorVisitor(PhpCompilation compilation, SourceDeclarations tables)
            {
                _tables = tables;
                _compilation = compilation;
            }

            public void VisitSourceUnit(SourceUnit unit)
            {
                if (unit != null && unit.Ast != null)
                {
                    var file = new SourceFileSymbol(_compilation, unit.Ast);
                    _currentFile = file;
                    _tables._files.Add(file.RelativeFilePath, _currentFile);

                    VisitGlobalCode(unit.Ast);

                    //
                    if (_tables.FirstScript == null)
                        _tables.FirstScript = _currentFile;

                    //
                    _currentFile = null;
                }
            }

            public override void VisitFunctionDecl(FunctionDecl x)
            {
                var routine = new SourceFunctionSymbol(_currentFile, x);

                x.SetProperty(routine); // remember bound function symbol

                _currentFile.AddFunction(routine);

                if (!x.IsConditional)
                {
                    _tables._functions.Add(routine.QualifiedName, routine);
                }

                //
                base.VisitFunctionDecl(x);
            }

            public override void VisitTypeDecl(TypeDecl x)
            {
                var type = new SourceTypeSymbol(_currentFile, x);

                x.SetProperty(type);    // remember bound function symbol

                _tables._declaredtypes.Add(type);

                if (!x.IsConditional)
                {
                    _tables._types.Add(x.MakeQualifiedName(), type);
                }

                //
                base.VisitTypeDecl(x);
            }
        }

        #endregion

        // TODO: MultiDictionary

        readonly Dictionary<QualifiedName, SourceTypeSymbol> _types = new Dictionary<QualifiedName, SourceTypeSymbol>();
        readonly Dictionary<QualifiedName, SourceRoutineSymbol> _functions = new Dictionary<QualifiedName, SourceRoutineSymbol>();
        readonly Dictionary<string, SourceFileSymbol> _files = new Dictionary<string, SourceFileSymbol>(StringComparer.OrdinalIgnoreCase);

        readonly List<SourceTypeSymbol> _declaredtypes = new List<SourceTypeSymbol>();

        string _baseDirectory;

        internal SourceFileSymbol FirstScript { get; private set; }

        #region ISemanticModel

        ISemanticModel ISemanticModel.Next => null;

        SourceFileSymbol ISemanticModel.GetFile(string path)
        {
            // normalize path
            path = FileUtilities.NormalizeRelativePath(path, null, _baseDirectory);

            // absolute path
            if (PathUtilities.IsAbsolute(path))
            {
                path = PhpFileUtilities.GetRelativePath(path, _baseDirectory);
            }

            // ./ handled by context semantics

            // ../ handled by context semantics

            // TODO: lookup include paths
            // TODO: calling script directory

            // cwd
            return GetFile(path);
        }

        INamedTypeSymbol ISemanticModel.GetType(QualifiedName name) => GetType(name);

        IEnumerable<IPhpRoutineSymbol> ISemanticModel.ResolveFunction(QualifiedName name)
        {
            var routine = GetFunction(name);
            if (routine != null)
                yield return routine;
        }

        IPhpValue ISemanticModel.ResolveConstant(string name)
        {
            return null;
        }

        bool ISemanticModel.IsAssignableFrom(QualifiedName qname, INamedTypeSymbol from)
        {
            throw new NotImplementedException();
        }

        #endregion

        public SourceDeclarations()
        {

        }

        internal void PopulateTables(PhpCompilation compilation, IEnumerable<SourceUnit>/**/trees)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(trees);

            _baseDirectory = compilation.Options.BaseDirectory;

            var visitor = new PopulatorVisitor(compilation, this);
            trees.ForEach(visitor.VisitSourceUnit);
        }

        public SourceFileSymbol GetFile(string fname) => _files.TryGetOrDefault(fname);

        public IEnumerable<SourceFileSymbol> GetFiles() => _files.Values;

        public MethodSymbol GetFunction(QualifiedName name) => _functions.TryGetOrDefault(name);

        public IEnumerable<MethodSymbol> GetFunctions() => _functions.Values;

        /// <summary>
        /// Gets enumeration of all routines (global code, functions and methods) in source code.
        /// </summary>
        public IEnumerable<SourceRoutineSymbol> AllRoutines    // all functions + global code + methods
        {
            get
            {
                var files = _files.Values;

                //
                var funcs = files.SelectMany(f => f.Functions).OfType<SourceRoutineSymbol>();
                var mains = files.Select(f => f.MainMethod);
                var methods = _declaredtypes.SelectMany(f => f.GetMembers().OfType<SourceRoutineSymbol>());

                //
                return funcs.Concat(mains).Concat(methods);
            }
        }

        public NamedTypeSymbol GetType(QualifiedName name) => _types.TryGetOrDefault(name);

        public IEnumerable<NamedTypeSymbol> GetTypes() => _declaredtypes;
    }
}
