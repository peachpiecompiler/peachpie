using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using Pchp.CodeAnalysis.Utilities;
using Devsense.PHP.Syntax.Ast;
using static Pchp.CodeAnalysis.AstUtils;
using Devsense.PHP.Syntax;
using System.Threading;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.IO;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a file within the mudule as a CLR type.
    /// </summary>
    /// <remarks>
    /// namespace [DIR]{
    ///     [PhpScript]
    ///     statc class [FNAME] {
    ///         static PhpValue [Main](){ ... }
    ///     }
    /// }</remarks>
    partial class SourceFileSymbol : NamedTypeSymbol, ILambdaContainerSymbol, IPhpScriptTypeSymbol
    {
        readonly PhpCompilation _compilation;
        readonly PhpSyntaxTree _syntaxTree;

        readonly SourceGlobalMethodSymbol _mainMethod;
        readonly List<SourceTypeSymbol> _containedTypes = new List<SourceTypeSymbol>();
        readonly List<Symbol> _lazyMembers = new List<Symbol>();

        BaseAttributeData _lazyScriptAttribute;

        /// <summary>
        /// List of functions declared within the file.
        /// </summary>
        public ImmutableArray<SourceFunctionSymbol> Functions => _lazyMembers.OfType<SourceFunctionSymbol>().ToImmutableArray();

        /// <summary>
        /// List of types declared within the file.
        /// </summary>
        public List<SourceTypeSymbol> ContainedTypes => _containedTypes;

        public PhpSyntaxTree SyntaxTree => _syntaxTree;

        public SourceModuleSymbol SourceModule => _compilation.SourceModule;

        public static SourceFileSymbol Create(PhpCompilation compilation, PhpSyntaxTree syntaxTree)
        {
            return syntaxTree.IsPharEntry
                ? new SourcePharEntrySymbol(compilation, syntaxTree)
                : new SourceFileSymbol(compilation, syntaxTree);
        }

        protected SourceFileSymbol(PhpCompilation compilation, PhpSyntaxTree syntaxTree)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(syntaxTree);

            _compilation = compilation;
            _syntaxTree = syntaxTree;
            _mainMethod = new SourceGlobalMethodSymbol(this);
        }

        /// <summary>
        /// Special main method representing the script global code.
        /// </summary>
        public IMethodSymbol MainMethod => _mainMethod;

        /// <summary>
        /// Lazily adds a function into the list of global functions declared within this file.
        /// </summary>
        internal void AddFunction(SourceFunctionSymbol routine)
        {
            Contract.ThrowIfNull(routine);
            _lazyMembers.Add(routine);
        }

        void ILambdaContainerSymbol.AddLambda(SourceLambdaSymbol routine)
        {
            Contract.ThrowIfNull(routine);
            _lazyMembers.Add(routine);
        }

        IEnumerable<SourceLambdaSymbol> ILambdaContainerSymbol.Lambdas
        {
            get
            {
                return _lazyMembers.OfType<SourceLambdaSymbol>();
            }
        }

        SourceLambdaSymbol ILambdaContainerSymbol.ResolveLambdaSymbol(LambdaFunctionExpr expr)
        {
            if (_lazyMembers != null)
            {
                for (int i = 0; i < _lazyMembers.Count; i++)
                {
                    if (_lazyMembers[i] is SourceLambdaSymbol s && s.Syntax == expr)
                    {
                        return s;
                    }
                }
            }

            if (expr == null)
            {
                throw new ArgumentNullException(nameof(expr));
            }

            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Collects declaration diagnostics.
        /// </summary>
        internal void GetDiagnostics(DiagnosticBag diagnostic)
        {
            // check functions duplicity
            var funcs = this.Functions;
            if (funcs.Length > 1)
            {
                var set = new HashSet<QualifiedName>();

                // handle unconditionally declared functions:
                foreach (var f in funcs)
                {
                    if (f.IsConditional)
                    {
                        continue;
                    }

                    if (!set.Add(f.QualifiedName))
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(_syntaxTree, ((FunctionDecl)f.Syntax).HeadingSpan, Devsense.PHP.Errors.FatalErrors.FunctionRedeclared, f.QualifiedName.ToString()));
                    }
                }

                //// handle conditionally declared functions: // NOTE: commented since we should allow to compile it
                //foreach (var f in funcs.Where(f => f.IsConditional))
                //{
                //    if (set.Contains(f.QualifiedName))  // does not make sense to declare function if it is declared already unconditionally
                //    {
                //        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(_syntaxTree, ((FunctionDecl)f.Syntax).HeadingSpan, Devsense.PHP.Errors.FatalErrors.FunctionRedeclared, f.PhpName));
                //    }
                //}
            }

            // check class/interface duplicity:
            var types = this.ContainedTypes;
            if (types.Count > 1)
            {
                var set = new HashSet<QualifiedName>();
                foreach (var t in types)
                {
                    if (t.Syntax.IsConditional)
                    {
                        continue;
                    }

                    if (!set.Add(t.FullName))
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(_syntaxTree, t.Syntax.HeadingSpan, Devsense.PHP.Errors.FatalErrors.TypeRedeclared, t.FullName.ToString()));
                    }
                }
            }
        }

        protected string CreateRelativeFilePath(string fullPath)
        {
            var relpath = PhpFileUtilities.GetRelativePath(
                PhpFileUtilities.NormalizeSlashes(fullPath),
                PhpFileUtilities.NormalizeSlashes(_compilation.Options.BaseDirectory));

            if (!string.IsNullOrEmpty(_compilation.Options.SubDirectory))
            {
                relpath = PhpFileUtilities.NormalizeSlashes(PathUtilities.CombinePathsUnchecked(_compilation.Options.SubDirectory, relpath));
            }

            return relpath;
        }

        public virtual string RelativeFilePath => CreateRelativeFilePath(_syntaxTree.Source.FilePath);

        /// <summary>
        /// Gets relative path excluding the file name and trailing slashes.
        /// </summary>
        internal virtual string DirectoryRelativePath
        {
            get
            {
                return (PathUtilities.GetDirectoryName(this.RelativeFilePath) ?? string.Empty)
                    .TrimEnd(PathUtilities.AltDirectorySeparatorChar);     // NormalizeRelativeDirectoryPath
            }
        }

        protected virtual DateTime LastWriteTime
        {
            get
            {
                try
                {
                    return File.GetLastWriteTimeUtc(SyntaxTree.FilePath);
                }
                catch
                {
                }

                return default;
            }
        }

        protected virtual bool IsAutoloaded
        {
            get
            {
                var autoloaded_files = DeclaringCompilation.Options.Autoload_Files;
                return autoloaded_files != null && autoloaded_files.Contains(this.RelativeFilePath);
            }
        }

        public string FileName => PathUtilities.GetFileName(SyntaxTree.Source.FilePath);

        public override string Name => PathUtilities.GetFileName(_syntaxTree.Source.FilePath, true).Replace('.', '_');

        public override string NamespaceName => WellKnownPchpNames.ScriptsRootNamespace + DirectoryRelativePath;

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            // [ScriptAttribute(RelativeFilePath, LastWriteTime_Ticks)]
            if (_lazyScriptAttribute == null)
            {
                var lastWriteTime = LastWriteTime;
                var namedproperties = ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty;

                //
                if (IsAutoloaded)
                {
                    // , IsAutoloaded = true
                    namedproperties = ImmutableArray.Create(
                        new KeyValuePair<string, TypedConstant>(
                            CoreMethods.ScriptAttribute_IsAutoloaded, DeclaringCompilation.CreateTypedConstant(true))
                        );
                }

                var scriptAttribute = new SynthesizedAttributeData(
                    DeclaringCompilation.CoreMethods.Ctors.ScriptAttribute_string_long,
                    ImmutableArray.Create(
                        DeclaringCompilation.CreateTypedConstant(this.RelativeFilePath),
                        DeclaringCompilation.CreateTypedConstant(lastWriteTime.Ticks)),
                    namedproperties);
                Interlocked.CompareExchange(ref _lazyScriptAttribute, scriptAttribute, null);
            }

            // [NullableContext(2)] - everything nullable can return/receive null by default
            var nullableContext = DeclaringCompilation.CreateNullableContextAttribute(NullableContextUtils.AnnotatedAttributeValue);

            //
            return ImmutableArray.Create<AttributeData>(_lazyScriptAttribute, nullableContext);
        }

        public override NamedTypeSymbol BaseType
        {
            get
            {
                return _compilation.CoreTypes.Object;
            }
        }

        public override int Arity => 0;

        internal override bool HasTypeArgumentsCustomModifiers => false;

        public override ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal) => GetEmptyTypeArgumentCustomModifiers(ordinal);

        public override Symbol ContainingSymbol => _compilation.SourceModule;

        internal override ModuleSymbol ContainingModule => _compilation.SourceModule;

        internal override PhpCompilation DeclaringCompilation => _compilation;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override bool IsInterface => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsStatic => true;

        public override bool IsSerializable => false;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(Location.Create(SyntaxTree, default(Microsoft.CodeAnalysis.Text.TextSpan)));
            }
        }

        public override TypeKind TypeKind => TypeKind.Class;

        internal override bool ShouldAddWinRTMembers => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override TypeLayout Layout => default(TypeLayout);

        internal override bool MangleName => false;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            var builder = ImmutableArray.CreateBuilder<Symbol>(1 + _lazyMembers.Count);

            builder.Add(_mainMethod);
            builder.AddRange(_lazyMembers);

            return builder.ToImmutable();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            if (name == _mainMethod.Name)
            {
                return ImmutableArray.Create<Symbol>(_mainMethod);
            }

            return _lazyMembers
                .Where(x => x.Name == name)
                .ToImmutableArray();
        }

        public override ImmutableArray<Symbol> GetMembersByPhpName(string name)
        {
            return _lazyMembers
                .Where(x => x.Name.StringsEqual(name, ignoreCase: true))
                .ToImmutableArray();
        }

        public override ImmutableArray<MethodSymbol> StaticConstructors => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => _lazyMembers.OfType<NamedTypeSymbol>().AsImmutable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => _lazyMembers.OfType<NamedTypeSymbol>().Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).AsImmutable();

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit() => GetMembers().OfType<FieldSymbol>();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<IMethodSymbol> GetMethodsToEmit() =>
            GetMembers()
            .Where(m => m.Kind == SymbolKind.Method && !m.IsUnreachable)
            .Cast<IMethodSymbol>();
    }

    /// <summary>
    /// <see cref="SourceFileSymbol"/> representing a PHAR entry.
    /// </summary>
    sealed class SourcePharEntrySymbol : SourceFileSymbol
    {
        SynthesizedAttributeData _lazyPharAttribute;

        public SourcePharEntrySymbol(PhpCompilation compilation, PhpSyntaxTree syntaxTree)
            : base(compilation, syntaxTree)
        {
            Debug.Assert(syntaxTree.PharStubFile != null);
        }

        public override string RelativeFilePath
        {
            get
            {
                // FilePath is already relative in PHAR entry

                return PhpFileUtilities.NormalizeSlashes(SyntaxTree.Source.FilePath);
            }
        }

        protected override DateTime LastWriteTime
        {
            get
            {
                try
                {
                    return File.GetLastWriteTimeUtc(SyntaxTree.PharStubFile.FilePath);
                }
                catch
                {
                }

                return default;
            }
        }

        protected override bool IsAutoloaded => false; // phar entries cannot be autoloaded

        // <Phar>pharentrypath
        public override string NamespaceName => WellKnownPchpNames.PharEntryRootNamespace + DirectoryRelativePath;

        public string PharName => CreateRelativeFilePath(SyntaxTree.PharStubFile.FilePath);

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            // [PharAttribute(PharFile)]
            if (_lazyPharAttribute == null)
            {
                var pharAttribute = new SynthesizedAttributeData(
                    DeclaringCompilation.CoreMethods.Ctors.PharAttribute_string,
                    ImmutableArray.Create(DeclaringCompilation.CreateTypedConstant(PharName)),
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                Interlocked.CompareExchange(ref _lazyPharAttribute, pharAttribute, null);
            }

            return base.GetAttributes().Concat(_lazyPharAttribute);
        }
    }
}
