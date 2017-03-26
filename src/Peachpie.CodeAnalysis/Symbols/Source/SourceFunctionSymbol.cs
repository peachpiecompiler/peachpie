using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.FlowAnalysis;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Syntax;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a global PHP function.
    /// </summary>
    internal sealed partial class SourceFunctionSymbol : SourceRoutineSymbol
    {
        readonly SourceFileSymbol _file;
        readonly FunctionDecl _syntax;

        FieldSymbol _lazyRoutineInfoField;    // internal static RoutineInfo !name;

        /// <summary>
        /// Whether the function is declared conditionally.
        /// </summary>
        public bool IsConditional => _syntax.IsConditional;

        public SourceFunctionSymbol(SourceFileSymbol file, FunctionDecl syntax)
        {
            Contract.ThrowIfNull(file);

            _file = file;
            _syntax = syntax;
        }

        /// <summary>
        /// A field representing the function info at runtime.
        /// Lazily associated with index by runtime.
        /// </summary>
        internal FieldSymbol EnsureRoutineInfoField(Emit.PEModuleBuilder module)
        {
            if (_lazyRoutineInfoField == null)
            {
                _lazyRoutineInfoField = module.SynthesizedManager
                    .GetOrCreateSynthesizedField(_file, this.DeclaringCompilation.CoreTypes.RoutineInfo, $"[method]{this.MetadataName}", Accessibility.Internal, true, true);
            }

            return _lazyRoutineInfoField;
        }

        public override ParameterSymbol ThisParameter => null;

        internal override Signature SyntaxSignature => _syntax.Signature;

        internal override TypeRef SyntaxReturnType => _syntax.ReturnType;

        internal override AstNode Syntax => _syntax;

        internal override PHPDocBlock PHPDocBlock => _syntax.PHPDoc;

        internal override IList<Statement> Statements => _syntax.Body.Statements;

        internal override SourceFileSymbol ContainingFile => _file;

        public override NamedTypeSymbol ContainingType => _file;

        protected override TypeRefContext CreateTypeRefContext()
            => new TypeRefContext(_syntax.ContainingSourceUnit, null);

        internal QualifiedName QualifiedName => NameUtils.MakeQualifiedName(_syntax.Name, _syntax.ContainingNamespace);

        public override string Name => this.QualifiedName.ClrName();

        public override string MetadataName
        {
            get
            {
                var name = base.MetadataName;

                if (_syntax.IsConditional)
                    name += "@" + _file.Functions.TakeWhile(f => f != this).Where(f => f.QualifiedName == this.QualifiedName).Count().ToString();   // index of this function within functions with the same name

                return name;
            }
        }

        public string PhpName => this.QualifiedName.ToString();

        public override Symbol ContainingSymbol => _file.SourceModule;

        internal override IModuleSymbol ContainingModule => _file.SourceModule;

        public override AssemblySymbol ContainingAssembly => _file.DeclaringCompilation.SourceAssembly;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => false;

        public override bool IsExtern => false;

        public override bool IsOverride => false;

        public override bool IsSealed => false;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(Location.Create(ContainingFile.SyntaxTree, _syntax.Span.ToTextSpan()));
            }
        }
    }
}
