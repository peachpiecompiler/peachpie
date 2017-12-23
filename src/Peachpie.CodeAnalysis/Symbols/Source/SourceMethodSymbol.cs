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
using Microsoft.Cci;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a PHP class method.
    /// </summary>
    internal partial class SourceMethodSymbol : SourceRoutineSymbol
    {
        readonly SourceTypeSymbol _type;
        readonly MethodDecl/*!*/_syntax;

        MethodSymbol _lazyOverridenMethod;

        public SourceMethodSymbol(SourceTypeSymbol/*!*/type, MethodDecl/*!*/syntax)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(syntax);

            _type = type;
            _syntax = syntax;
        }

        internal override bool RequiresLateStaticBoundParam =>
            IsStatic &&                             // `static` in instance method == typeof($this)
            ControlFlowGraph != null &&             // cfg sets {Flags}
            (this.Flags & RoutineFlags.UsesLateStatic) != 0 &&
            (!_type.IsSealed || _type.IsTrait);     // `static` == `self` <=> self is sealed

        public override IMethodSymbol OverriddenMethod
        {
            get
            {
                if (_lazyOverridenMethod == null)
                {
                    _lazyOverridenMethod = this.ResolveOverride();
                }

                return _lazyOverridenMethod;
            }
        }

        internal override Signature SyntaxSignature => _syntax.Signature;

        internal override TypeRef SyntaxReturnType => _syntax.ReturnType;

        internal override AstNode Syntax => _syntax;

        internal override PHPDocBlock PHPDocBlock => _syntax.PHPDoc;

        internal override IList<Statement> Statements => _syntax.Body?.Statements;

        protected override TypeRefContext CreateTypeRefContext() => TypeRefFactory.CreateTypeRefContext(_type);

        internal override SourceFileSymbol ContainingFile => _type.ContainingFile;

        public override string Name => _syntax.Name.Name.Value;

        public override Symbol ContainingSymbol => _type;

        public override Accessibility DeclaredAccessibility => _syntax.Modifiers.GetAccessibility();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsStatic => _syntax.Modifiers.IsStatic();

        public override bool IsAbstract => !IsStatic && (_syntax.Modifiers.IsAbstract() || _type.IsInterface);

        public override bool IsOverride => !IsStatic && this.OverriddenMethod != null && this.SignaturesMatch((MethodSymbol)this.OverriddenMethod);

        public override bool IsSealed => _syntax.Modifiers.IsSealed() && IsVirtual;

        public override bool IsVirtual => !IsStatic;    // every method in PHP is virtual except static methods

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(Location.Create(ContainingFile.SyntaxTree, _syntax.Span.ToTextSpan()));
            }
        }
    }

    /// <summary>
    /// Represents a PHP trait method.
    /// </summary>
    internal class SourceTraitMethodSymbol : SourceMethodSymbol
    {
        AttributeData _lazyDeclaredVisibilityAtribute = null;

        public SourceTraitMethodSymbol(SourceTraitTypeSymbol type, MethodDecl syntax)
            : base(type, syntax)
        {
        }

        // trait method is always emitted as `public`
        protected override TypeMemberVisibility VisibilityToEmit => TypeMemberVisibility.Public;

        // abstract trait method must have an empty implementation
        public override bool IsAbstract => false;

        // abstract trait method must have an empty implementation
        internal override IList<Statement> Statements => base.IsAbstract ? Array.Empty<Statement>() : base.Statements;

        internal override IEnumerable<AttributeData> GetCustomAttributesToEmit(CommonModuleCompilationState compilationState)
        {
            foreach (var attr in base.GetCustomAttributesToEmit(compilationState))
            {
                yield return attr;
            }

            if (DeclaredAccessibility != Accessibility.Public)
            {
                if (_lazyDeclaredVisibilityAtribute == null)
                {
                    var ct = DeclaringCompilation.CoreTypes;

                    // [PhpTraitMemberVisibilityAttribute( {(int)DeclaredAccessibility} )]
                    _lazyDeclaredVisibilityAtribute = new SynthesizedAttributeData(
                        ct.PhpTraitMemberVisibilityAttribute.Ctor(ct.Int32),
                        ImmutableArray.Create(new TypedConstant(ct.Int32.Symbol, TypedConstantKind.Primitive, (int)DeclaredAccessibility)),
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                }

                yield return _lazyDeclaredVisibilityAtribute;
            }
        }
    }
}
