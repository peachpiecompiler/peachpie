using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.Syntax.AST;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.Syntax;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a global PHP function.
    /// </summary>
    internal sealed class SourceFunctionSymbol : SourceRoutineSymbol
    {
        readonly SourceFileSymbol _file;
        readonly FunctionDecl _syntax;

        FieldSymbol _lazyIndexField;    // internal static int <name>idx;

        /// <summary>
        /// Whether the function is declared conditionally.
        /// </summary>
        public bool IsConditional => _syntax.IsConditional;

        public SourceFunctionSymbol(SourceFileSymbol file, FunctionDecl syntax)
        {
            Contract.ThrowIfNull(file);

            _file = file;
            _syntax = syntax;
            _params = BuildParameters(syntax.Signature, syntax.PHPDoc).AsImmutable();
        }

        /// <summary>
        /// A field representing the function index at runtime.
        /// Lazily associated with name by runtime.
        /// </summary>
        public FieldSymbol IndexField
        {
            get
            {
                if (_lazyIndexField == null)
                    _lazyIndexField = ((IWithSynthesized)_file).GetOrCreateSynthesizedField(this.DeclaringCompilation.CoreTypes.Int32, $"f<{this.Name}>idx", Accessibility.Internal, true);

                return _lazyIndexField;
            }
        }

        public override ParameterSymbol ThisParameter => null;

        internal override AstNode Syntax => _syntax;

        internal override PHPDocBlock PHPDocBlock => _syntax.PHPDoc;

        internal override IList<Statement> Statements => _syntax.Body;

        internal override SourceFileSymbol ContainingFile => _file;

        public override NamedTypeSymbol ContainingType => _file;

        protected override TypeRefContext CreateTypeRefContext()
            => new TypeRefContext(NameUtils.GetNamingContext(_syntax.Namespace, _syntax.SourceUnit.Ast), _syntax.SourceUnit, null);

        internal QualifiedName QualifiedName => NameUtils.MakeQualifiedName(_syntax.Name, _syntax.Namespace);

        public override string Name => this.QualifiedName.ClrName();

        public override string MetadataName
        {
            get
            {
                var name = base.MetadataName;

                if (this.IsConditional)
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

        public override TypeSymbol ReturnType
        {
            get
            {
                return BuildReturnType(_syntax.Signature, _syntax.PHPDoc, this.ControlFlowGraph.ReturnTypeMask);
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
