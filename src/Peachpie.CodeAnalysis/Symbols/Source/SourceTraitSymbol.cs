using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// PHP trait symbol.
    /// </summary>
    internal class SourceTraitTypeSymbol : SourceTypeSymbol
    {
        /// <summary>
        /// Field holding actual <c>this</c> instance of the class that uses this trait.
        /// </summary>
        public FieldSymbol RealThisField
        {
            get
            {
                if (_lazyRealThisField == null)
                {
                    var lazyRealThisField = new SynthesizedFieldSymbol(this, TSelfParameter, "<>" + SpecialParameterSymbol.ThisName,
                        accessibility: Accessibility.Private,
                        isStatic: false,
                        isReadOnly: true);
                    Interlocked.CompareExchange(ref _lazyRealThisField, lazyRealThisField, null);
                }

                return _lazyRealThisField;
            }
        }
        FieldSymbol _lazyRealThisField; // private readonly !TSelf <>this;

        /// <summary>[PhpTrait] attribute. Initialized lazily.</summary>
        BaseAttributeData _lazyPhpTraitAttribute;

        public override NamedTypeSymbol BaseType
        {
            get
            {
                if (_lazyBaseType == null)
                {
                    _lazyBaseType = DeclaringCompilation.CoreTypes.Object.Symbol;
                    Debug.Assert(_lazyBaseType != null);
                }

                return _lazyBaseType;
            }
        }

        protected override ImmutableArray<MethodSymbol> CreateInstanceConstructors()
        {
            return ImmutableArray.Create<MethodSymbol>(new SynthesizedPhpTraitCtorSymbol(this));
        }

        public override bool IsTrait => true;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;  // traits cannot be extended

        public SourceTraitTypeSymbol(SourceFileSymbol file, TypeDecl syntax)
            : base(file, syntax)
        {
            Debug.Assert(syntax.MemberAttributes.IsTrait());
            Debug.Assert(syntax.BaseClass == null); // not expecting trait can extend another class

            _typeParameters = ImmutableArray.Create<TypeParameterSymbol>(new AnonymousTypeParameterSymbol(this, 0, "TSelf", hasReferenceTypeConstraint: true));
        }

        protected override SourceTypeSymbol NewSelf() => new SourceTraitTypeSymbol(ContainingFile, Syntax);

        protected override MethodSymbol CreateSourceMethod(MethodDecl m) => new SourceTraitMethodSymbol(this, m);

        //public override string MetadataName => MetadataHelpers.ComposeAritySuffixedMetadataName(base.MetadataName, Arity);

        public override int Arity => TypeParameters.Length;
        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _typeParameters;
        public override ImmutableArray<TypeSymbol> TypeArguments => StaticCast<TypeSymbol>.From(_typeParameters);

        public TypeSymbol TSelfParameter => _typeParameters[0];

        ImmutableArray<TypeParameterSymbol> _typeParameters;

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            foreach (var f in base.GetFieldsToEmit())
            {
                yield return f;
            }

            yield return RealThisField;
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            var attrs = base.GetAttributes();

            // [PhpTraitAttribute()]
            if (_lazyPhpTraitAttribute == null)
            {
                var lazyPhpTraitAttribute = new SynthesizedAttributeData(
                    DeclaringCompilation.CoreMethods.Ctors.PhpTraitAttribute,
                    ImmutableArray<TypedConstant>.Empty,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                Interlocked.CompareExchange(ref _lazyPhpTraitAttribute, lazyPhpTraitAttribute, null);
            }

            attrs = attrs.Add(_lazyPhpTraitAttribute);

            //
            return attrs;
        }
    }
}
